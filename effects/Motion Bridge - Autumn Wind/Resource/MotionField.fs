/*{
  "DESCRIPTION": "Motion Bridge - Autumn Wind: the Motion Bridge field, prepared and placed in the picture. Output Map: per frame position r motion (excitation), g occupancy, b 1 where a sensor sees the field now, a 1. Output View: Show Field as a premultiplied overlay.",
  "CREDIT": "Stefan Mazur - https://github.com/S-Mazu/WirePatches",
  "ISFVSN": "2",
  "CATEGORIES": [
    "Effect"
  ],
  "INPUTS": [
    { "NAME": "inputImage", "TYPE": "image" },
    { "NAME": "outputMode", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "aspectIn", "TYPE": "float", "DEFAULT": 1.7777778, "MIN": 0.001, "MAX": 1000.0 },
    { "NAME": "scale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "horizonY", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "fieldPlane", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "fieldX", "TYPE": "float", "DEFAULT": 0.5, "MIN": -10.0, "MAX": 10.0 },
    { "NAME": "fieldY", "TYPE": "float", "DEFAULT": 0.5, "MIN": -10.0, "MAX": 10.0 },
    { "NAME": "fieldWidth", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "fieldHeight", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "fieldSize", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "fieldRotation", "TYPE": "float", "DEFAULT": 0.0, "MIN": -180.0, "MAX": 180.0 },
    { "NAME": "shadowHold", "TYPE": "float", "DEFAULT": 5.0, "MIN": 0.0, "MAX": 60.0 },
    { "NAME": "clockH", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 24.0 },
    { "NAME": "clockM", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 60.0 },
    { "NAME": "clockS", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 60.0 },
    { "NAME": "clockMs", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1000.0 }
  ],
  "PASSES": [
    { "TARGET": "state", "PERSISTENT": true, "FLOAT": true, "WIDTH": "2", "HEIGHT": "1" },
    { "TARGET": "blurX", "PERSISTENT": true, "FLOAT": true, "WIDTH": "160", "HEIGHT": "120" },
    { "TARGET": "blurXY", "PERSISTENT": true, "FLOAT": true, "WIDTH": "160", "HEIGHT": "120" },
    { "TARGET": "field", "PERSISTENT": true, "FLOAT": true, "WIDTH": "160", "HEIGHT": "120" },
    { }
  ]
}*/
// The Motion Bridge field (tools/Motion Bridge/README.md, Output): x 0..1 along the base line,
// depth 0..1 into the area at 15/16 of the height, R occupancy, G motion, B visibility; the top
// sixteenth is the status band whose counter changes every frame.
//
// Passes 0 to 3 follow the field preparation of the Motion Bridge patches (Fireflies, Fabric Wind):
// the bridge's sign of life, occupancy and motion spread with a soft maximum (not averaged), motion fading out over 1.5 s,
// occupancy smoothed over 0.15 s, held shadows. The last pass places the field in the picture
// exactly as in Fireflies (Field Plane Floor / Screen, Field X, Y, Width, Height, Size, Rotation,
// Horizon Y) and writes either
//   Map  (outputMode 0): what an item at this frame position reads, r motion, g occupancy,
//                        b 1 where a sensor sees the field now; the items sample it in Wire.
//   View (outputMode 1): Show Field, premultiplied: light grey where evaluated, red occupancy,
//                        green motion, hatched where nothing is evaluated, white the base line,
//                        magenta the other edges.
// The frame is in the scene coordinates of the Scaler (examples/Scaler): origin in the centre,
// y +-1/Scale, x +-aspect/Scale; the aspect comes from Patch Info (aspectIn).
const float SIG = 0.67;
const float LSB = 1.0 / 255.0;
const float FAR = 4.0;             // floor distance (Z) of the far edge of the field at rest
const float REF = 2.0;             // scene units per output height at Scale 1 (Scaler: y +-1)
const float HATCH_H = 0.013;       // Show Field: period of the hatching, output heights
const float BLUR_X = 0.06;         // blur radius of the field, in field units
const float BLUR_D = 0.06;
const float BAND_Y = 31.0 / 32.0;  // middle of the status band
const float FIELD_H = 15.0 / 16.0; // the field rows in the texture
const vec2 FIELD_RES = vec2(160.0, 120.0);
const float EXC_LIFE = 1.5;        // seconds until the memory of motion has faded

float safe(float v, float lo, float hi) {
    if (!(v == v) || abs(v) > 1e9) return lo;
    return clamp(v, lo, hi);
}

vec2 rot(vec2 v, float a) {
    float c = cos(a), s = sin(a);
    return vec2(c * v.x - s * v.y, s * v.x + c * v.y);
}

// frame position -> scene coordinates (Scaler): origin in the centre, y +-1/S, x +-aspect/S
vec2 toScene(vec2 uv, float aspect, float S) {
    return vec2((uv.x - 0.5) * 2.0 * aspect / S, (uv.y - 0.5) * 2.0 / S);
}

// frame position -> plane coordinates (xy) and whether the plane is there (z). Screen: scene
// coordinates. Floor: (X, Z), X in scene units, Z the distance (1 at the bottom edge).
vec3 toPlane(vec2 uv, bool onFloor, float aspect, float S, float hS) {
    vec2 p = toScene(uv, aspect, S);
    if (!onFloor) return vec3(p, 1.0);
    float yb = p.y + 1.0 / S;                          // scene units above the bottom edge
    if (yb >= hS - 1e-4) return vec3(0.0, 0.0, 0.0);
    float Z = hS / (hS - yb);
    return vec3(p.x * Z, Z, 1.0);
}

// frame position -> field coordinates (x along the base line, y = depth) and whether inside (z)
vec3 toField(vec2 uv, float aspect, float S, float hS) {
    bool onFloor = safe(fieldPlane, 0.0, 1.0) < 0.5;
    vec3 P = toPlane(uv, onFloor, aspect, S, hS);
    if (P.z < 0.5) return vec3(-1.0, -1.0, 0.0);
    float fxp = safe(fieldX, -100.0, 100.0);
    float fyp = safe(fieldY, -100.0, 100.0);
    float W = 2.0 * aspect / S;                        // the frame (on the floor: at the bottom edge), scene units
    vec2 c = onFloor ? vec2((fxp - 0.5) * W, 1.0 + fyp * (FAR - 1.0)) : vec2((fxp - 0.5) * W, (fyp - 0.5) * 2.0 / S);
    vec2 E = onFloor ? vec2(W, FAR - 1.0) : vec2(W, 2.0 / S);
    float r = radians(safe(fieldRotation, -3600.0, 3600.0));
    vec2 dl = rot(P.xy - c, -r);
    float cr = abs(cos(r)), sr = abs(sin(r));
    vec2 ref = vec2(cr * E.x + sr * E.y, cr * E.y + sr * E.x);       // the frame extent the field side lies on
    float fs = safe(fieldSize, 0.0, 100.0);
    vec2 sz = max(vec2(safe(fieldWidth, 0.0, 100.0), safe(fieldHeight, 0.0, 100.0)) * fs * ref, vec2(1e-4));
    vec2 f = dl / sz + 0.5;
    float inside = (f.x >= 0.0 && f.x <= 1.0 && f.y >= 0.0 && f.y <= 1.0) ? 1.0 : 0.0;
    return vec3(f, inside);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    float aspect = safe(aspectIn, 0.001, 1000.0);
    float S = clamp(scale, 0.001, 1000.0);                  // safeScale
    float hS = clamp(horizonY * REF, 0.001, 1e5);          // horizon above the bottom edge, scene units

    // ---------- pass 0: time, the bridge's sign of life, visibility in use ----------
    if (PASSINDEX == 0) {
        vec4 s0 = IMG_NORM_PIXEL(state, vec2(0.25, 0.5));
        vec4 s1 = IMG_NORM_PIXEL(state, vec2(0.75, 0.5));
        float now = clockH * 3600.0 + clockM * 60.0 + clockS + clockMs * 0.001;
        bool fresh = !(abs(s0.a - SIG) < 0.01) || !(abs(s1.a - SIG) < 0.01) || now < s0.r;
        if (fresh) {
            s0 = vec4(now, -1.0, now - 1000.0, SIG);
            s1 = vec4(1.0 / 60.0, 0.0, 0.0, SIG);
        }
        float dt = clamp(now - s0.r, 0.0, 0.1);
        dt = safe(mix(s1.r, dt, 0.25), 0.0, 0.1);

        // the band: R = G = B > 0 and equal across the width
        vec3 b0 = IMG_NORM_PIXEL(inputImage, vec2(0.2, BAND_Y)).rgb;
        vec3 b1 = IMG_NORM_PIXEL(inputImage, vec2(0.5, BAND_Y)).rgb;
        vec3 b2 = IMG_NORM_PIXEL(inputImage, vec2(0.8, BAND_Y)).rgb;
        vec3 mn = min(min(b0, b1), b2), mx = max(max(b0, b1), b2);
        float spread = max(max(mx.r, mx.g), mx.b) - min(min(mn.r, mn.g), mn.b);
        bool band = spread < 1.5 * LSB && mn.r > 0.5 * LSB;
        float counter = (b0.r + b1.r + b2.r) / 3.0;
        float lastCounter = safe(s0.g, -1.0, 1.0);
        float lastChange = safe(s0.b, now - 100000.0, now);
        if (band && abs(counter - lastCounter) > 0.5 * LSB) {
            lastChange = now;
            lastCounter = counter;
        }
        float rt = mod(safe(s1.g, 0.0, 3600.0) + dt, 3600.0);              // real time

        // visibility is in use when any part of the field is seen (a sensor is there);
        // without it (simulated objects only) B is 0 everywhere and is ignored
        float visMax = 0.0;
        for (int i = 0; i < 8; i++) {
            for (int k = 0; k < 8; k++) {
                vec2 q = vec2((float(i) + 0.5) / 8.0, (float(k) + 0.5) / 8.0 * FIELD_H);
                visMax = max(visMax, IMG_NORM_PIXEL(inputImage, q).b);
            }
        }
        float visUsed = visMax > 0.5 ? 1.0 : max(safe(s1.b, 0.0, 1.0) - dt * 0.5, 0.0);
        gl_FragColor = uv.x < 0.5 ? vec4(now, lastCounter, lastChange, SIG) : vec4(dt, rt, visUsed, SIG);
        return;
    }

    vec4 s0 = IMG_NORM_PIXEL(state, vec2(0.25, 0.5));
    vec4 s1 = IMG_NORM_PIXEL(state, vec2(0.75, 0.5));
    float age = safe(s0.r - s0.b, 0.0, 100000.0);
    float alive = 1.0 - smoothstep(0.25, 1.0, age);       // fade the field's influence out without the bridge
    float dt = safe(s1.r, 0.0, 0.1);

    // ---------- pass 1: spread along x (r occupancy, g motion), in field space; b visibility ----------
    // A soft maximum, not an average: the lidar sees only the side of a body facing it, a thin arc
    // one or two cells deep; averaging would thin it out below the thresholds of the items, the
    // maximum keeps its strength and widens it to a zone of BLUR_X around it.
    if (PASSINDEX == 1) {
        vec2 mx = vec2(0.0);
        for (int k = -6; k <= 6; k++) {
            float o = float(k) / 6.0;
            float w = exp(-o * o * 2.0);
            float fx = uv.x + o * BLUR_X;
            vec2 v = IMG_NORM_PIXEL(inputImage, vec2(clamp(fx, 0.0, 1.0), uv.y * FIELD_H)).rg;
            if (fx < 0.0 || fx > 1.0) v = vec2(0.0);
            mx = max(mx, v * w);
        }
        float vis = IMG_NORM_PIXEL(inputImage, vec2(uv.x, uv.y * FIELD_H)).b;
        gl_FragColor = vec4(mx, vis, 1.0);
        return;
    }

    // ---------- pass 2: spread along depth (soft maximum, as pass 1) ----------
    if (PASSINDEX == 2) {
        vec2 mx = vec2(0.0);
        for (int k = -6; k <= 6; k++) {
            float o = float(k) / 6.0;
            float w = exp(-o * o * 2.0);
            float fd = uv.y + o * BLUR_D;
            vec2 v = IMG_NORM_PIXEL(blurX, vec2(uv.x, clamp(fd, 0.0, 1.0))).rg;
            if (fd < 0.0 || fd > 1.0) v = vec2(0.0);
            mx = max(mx, v * w);
        }
        float vis = IMG_NORM_PIXEL(blurX, uv).b;
        gl_FragColor = vec4(mx, vis, 1.0);
        return;
    }

    // ---------- pass 3: excitation and smoothed occupancy; shadows keep their state ----------
    if (PASSINDEX == 3) {
        vec4 prev = IMG_NORM_PIXEL(field, uv);
        vec4 cur = IMG_NORM_PIXEL(blurXY, uv);
        vec2 now_ = cur.rg * alive;
        float e = safe(prev.r, 0.0, 1.0);
        float o = safe(prev.g, 0.0, 1.0);
        float hold = safe(shadowHold, 0.0, 60.0);
        bool shadow = s1.b > 0.5 && cur.b < 0.5 && hold > 0.0 && alive > 0.5;
        if (shadow) {
            // not seen: no new motion, the occupancy fades out slowly instead of dropping to 0
            e = e * exp(-dt / EXC_LIFE);
            o = o * exp(-dt / hold);
        } else {
            e = max(clamp(now_.g * 2.5, 0.0, 1.0), e * exp(-dt / EXC_LIFE));
            o = mix(o, clamp(now_.r * 1.5, 0.0, 1.0), 1.0 - exp(-dt / 0.15));
        }
        gl_FragColor = vec4(e, o, shadow ? 1.0 : 0.0, 1.0);
        return;
    }

    // ---------- pass 4: the field in the picture ----------
    vec3 F = toField(uv, aspect, S, hS);
    bool seenNow = alive > 0.5;
    if (seenNow && F.z > 0.5 && safe(s1.b, 0.0, 1.0) > 0.5)
        seenNow = IMG_NORM_PIXEL(inputImage, vec2(clamp(F.x, 0.0, 1.0), clamp(F.y, 0.0, 1.0) * FIELD_H)).b > 0.5;

    // the processed field here, interpolated by hand (Wire reads float textures unfiltered)
    float occ = 0.0, ex = 0.0;
    if (F.z > 0.5) {
        vec2 fp = clamp(F.xy, 0.0, 1.0) * FIELD_RES - 0.5;
        vec2 fi = floor(fp);
        vec2 fq = fp - fi;
        vec2 f0 = (clamp(fi, vec2(0.0), FIELD_RES - 1.0) + 0.5) / FIELD_RES;
        vec2 f1 = (clamp(fi + 1.0, vec2(0.0), FIELD_RES - 1.0) + 0.5) / FIELD_RES;
        vec4 fv = mix(mix(IMG_NORM_PIXEL(field, f0), IMG_NORM_PIXEL(field, vec2(f1.x, f0.y)), fq.x),
                      mix(IMG_NORM_PIXEL(field, vec2(f0.x, f1.y)), IMG_NORM_PIXEL(field, f1), fq.x), fq.y);
        occ = safe(fv.g, 0.0, 1.0);
        ex = safe(fv.r, 0.0, 1.0);
    }

    if (safe(outputMode, 0.0, 1.0) < 0.5) {
        gl_FragColor = vec4(ex, occ, (F.z > 0.5 && seenNow) ? 1.0 : 0.0, 1.0);
        return;
    }

    // View: hatched where nothing is evaluated (outside the field, outside the area, in a shadow,
    // everything without the bridge); light grey where a sensor sees, red occupancy, green motion;
    // in the hatched part only what is held there; white the base line, magenta the other edges
    vec4 outC = vec4(0.0);
    if (F.z < 0.5 || !seenNow) {
        float hp = mod((gl_FragCoord.x + gl_FragCoord.y) / max(HATCH_H * RENDERSIZE.y, 2.0), 1.0);
        float line = 1.0 - smoothstep(0.18, 0.26, abs(hp - 0.5));      // a soft diagonal stripe
        float ha = 0.8;
        outC.rgb = mix(outC.rgb, mix(vec3(0.0), vec3(0.28), line), ha);
        outC.a = ha;
    }
    if (F.z > 0.5) {
        vec3 fcol = seenNow ? mix(vec3(0.85), vec3(occ, ex, 0.0), max(occ, ex)) : vec3(occ, ex, 0.0);
        float fa = seenNow ? max(max(occ, ex) * 0.8, 0.45) : max(occ, ex) * 0.8;
        vec3 Fx = toField(uv + vec2(1.0 / RENDERSIZE.x, 0.0), aspect, S, hS);
        vec3 Fy = toField(uv + vec2(0.0, 1.0 / RENDERSIZE.y), aspect, S, hS);
        float dfx = max(length(vec2(Fx.x - F.x, Fy.x - F.x)), 1e-6);
        float dfd = max(length(vec2(Fx.y - F.y, Fy.y - F.y)), 1e-6);
        float side = min(F.x, 1.0 - F.x) / dfx;
        float farEdge = (1.0 - F.y) / dfd;
        float front = F.y / dfd;
        if (min(side, farEdge) < 1.5) { fcol = vec3(1.0, 0.0, 1.0); fa = 1.0; }
        if (front < 2.5) { fcol = vec3(1.0); fa = 1.0; }
        outC.rgb = mix(outC.rgb, fcol, fa);
        outC.a = max(outC.a, fa);
    }
    if (!(outC.r == outC.r && outC.g == outC.g && outC.b == outC.b && outC.a == outC.a)) outC = vec4(0.0);
    outC.a = clamp(outC.a, 0.0, 1.0);
    gl_FragColor = vec4(clamp(outC.rgb, 0.0, 1.0) * outC.a, outC.a);
}
