/*{
  "DESCRIPTION": "Motion Bridge - Fireflies: fireflies fly about, blink on and off, swarm around whatever stands still and flee from motion. Fleeing always wins. Reverse swaps the roles. Driven by the Motion Bridge field (Spout). Calm without the bridge; transparent background.",
  "CREDIT": "Stefan Mazur - https://github.com/S-Mazu/WirePatches",
  "ISFVSN": "2",
  "CATEGORIES": [
    "Effect"
  ],
  "INPUTS": [
    { "NAME": "inputImage", "TYPE": "image" },
    { "NAME": "scale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "fireflyPlane", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "fireflyDensity", "TYPE": "float", "DEFAULT": 40.0, "MIN": 0.0, "MAX": 500.0 },
    { "NAME": "performanceCap", "TYPE": "float", "DEFAULT": 512.0, "MIN": 0.0, "MAX": 1024.0 },
    { "NAME": "fireflySize", "TYPE": "float", "DEFAULT": 0.006, "MIN": 0.0, "MAX": 0.2 },
    { "NAME": "glowSize", "TYPE": "float", "DEFAULT": 0.02, "MIN": 0.0, "MAX": 0.5 },
    { "NAME": "fireflyColor", "TYPE": "color", "DEFAULT": [1.0, 0.98, 0.75, 1.0] },
    { "NAME": "glowColor", "TYPE": "color", "DEFAULT": [0.65, 1.0, 0.25, 1.0] },
    { "NAME": "fireflyTwinkle", "TYPE": "float", "DEFAULT": 0.4, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "fireflyLife", "TYPE": "float", "DEFAULT": 3.0, "MIN": 0.0, "MAX": 30.0 },
    { "NAME": "darkTime", "TYPE": "float", "DEFAULT": 1.5, "MIN": 0.0, "MAX": 30.0 },
    { "NAME": "wander", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "airDrag", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "swarmStrength", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "fleeStrength", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "reverse", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "haloOn", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "haloColor", "TYPE": "color", "DEFAULT": [1.0, 0.85, 0.55, 1.0] },
    { "NAME": "haloOpacity", "TYPE": "float", "DEFAULT": 0.35, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "horizonY", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "fieldPlane", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "fieldX", "TYPE": "float", "DEFAULT": 0.5, "MIN": -10.0, "MAX": 10.0 },
    { "NAME": "fieldY", "TYPE": "float", "DEFAULT": 0.5, "MIN": -10.0, "MAX": 10.0 },
    { "NAME": "fieldWidth", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "fieldHeight", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "fieldRotation", "TYPE": "float", "DEFAULT": 0.0, "MIN": -180.0, "MAX": 180.0 },
    { "NAME": "shadowHold", "TYPE": "float", "DEFAULT": 5.0, "MIN": 0.0, "MAX": 60.0 },
    { "NAME": "showField", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
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
    { "TARGET": "partsB", "PERSISTENT": true, "FLOAT": true, "WIDTH": "1024", "HEIGHT": "2" },
    { "TARGET": "partsA", "PERSISTENT": true, "FLOAT": true, "WIDTH": "1024", "HEIGHT": "2" },
    { }
  ]
}*/

// The Motion Bridge field (tools/Motion Bridge/README.md, Output): x 0..1 along the front,
// depth 0..1 into the area at 15/16 of the height, R occupancy, G motion, B visibility; the top
// sixteenth is the status band whose counter changes every frame.
//
// Two planes, chosen independently for the fireflies (Firefly Plane) and for the field (Field
// Plane), as in Motion Bridge - Fabric Wind:
//   Screen: the frame, plane coordinates ((u - 0.5) * aspect, v), in output heights; the
//           fireflies fly over the whole frame.
//   Floor:  a view over the floor in front of the projection; a pixel at height y below the
//           horizon H sees the floor at the distance Z = H / (H - y) (1 at the bottom edge),
//           plane coordinates (X, Z), X = (u - 0.5) * aspect * Z. The fireflies hover over the
//           floor from the bottom edge (Z = 1) to Z = FAR (or the top edge, when the horizon lies
//           above the frame), drawn as round lights that get smaller with the distance and fade
//           out towards the far end.
// Each firefly reads the field where it is.
//
// Passes (pass targets are only read where they are not being written, see Star Globe):
//   0 state (2 x 1): now, last counter, time of the last counter change | dt, real time, visibility in use
//   1, 2 blurX, blurXY (160 x 120): occupancy and motion blurred, visibility unblurred
//   3 field (160 x 120): r excitation, g occupancy smoothed, b 1 in a held shadow
//   4 partsB from partsA, 5 partsA from partsB (1024 x 2): two simulation steps per frame;
//     row 0 position and velocity, row 1 signature, heading, turning sense
//   6 the picture

const float SIG = 0.67;
const float LSB = 1.0 / 255.0;
const float FAR = 4.0;             // floor distance of the far edge of the field at rest
const float BLUR_X = 0.06;         // blur radius of the field, in field units
const float BLUR_D = 0.06;
const float BAND_Y = 31.0 / 32.0;  // middle of the status band
const float FIELD_H = 15.0 / 16.0; // the field rows in the texture
const vec2 FIELD_RES = vec2(160.0, 120.0);
const float TAU = 6.2831853;
const float EXC_LIFE = 1.5;        // seconds until the memory of motion has faded
const int N = 1024;
const float OFF = 10000.0;         // position of a firefly that is not in use

float safe(float v, float lo, float hi) {
    if (!(v == v) || abs(v) > 1e9) return lo;
    return clamp(v, lo, hi);
}

float hash3(vec3 p) {
    p = fract(p * vec3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return fract((p.x + p.y) * p.z);
}

float noise1(float t, float seed) {                 // smooth random curve 0..1
    float i = floor(t), f = fract(t);
    f = f * f * (3.0 - 2.0 * f);
    return mix(hash3(vec3(i, seed, 7.1)), hash3(vec3(i + 1.0, seed, 7.1)), f);
}

vec2 rot(vec2 v, float a) {
    float c = cos(a), s = sin(a);
    return vec2(c * v.x - s * v.y, s * v.x + c * v.y);
}

void over(inout vec3 col, inout float a, vec3 c, float ca) {
    float na = ca + a * (1.0 - ca);
    if (na > 0.0001) col = (c * ca + col * a * (1.0 - ca)) / na;
    a = na;
}

// frame position -> plane coordinates (xy) and whether the plane is there (z)
vec3 toPlane(vec2 uv, bool onFloor, float aspect, float H) {
    if (!onFloor) return vec3((uv.x - 0.5) * aspect, uv.y, 1.0);
    if (uv.y >= H - 1e-4) return vec3(0.0, 0.0, 0.0);
    float Z = H / (H - uv.y);
    return vec3((uv.x - 0.5) * aspect * Z, Z, 1.0);
}

// plane coordinates -> frame position (xy) and whether it exists (z)
vec3 toFrame(vec2 P, bool onFloor, float aspect, float H) {
    if (!onFloor) return vec3(P.x / aspect + 0.5, P.y, 1.0);
    if (P.y < 1e-3) return vec3(0.0, 0.0, 0.0);
    return vec3(P.x / (aspect * P.y) + 0.5, H - H / P.y, 1.0);
}

// frame position -> field coordinates (x along the front, y = depth) and whether inside (z)
vec3 toField(vec2 uv, float aspect, float H) {
    bool onFloor = safe(fieldPlane, 0.0, 1.0) < 0.5;
    vec3 P = toPlane(uv, onFloor, aspect, H);
    if (P.z < 0.5) return vec3(-1.0, -1.0, 0.0);
    float fxp = safe(fieldX, -100.0, 100.0);
    float fyp = safe(fieldY, -100.0, 100.0);
    vec2 c = onFloor ? vec2((fxp - 0.5) * aspect, 1.0 + fyp * (FAR - 1.0)) : vec2((fxp - 0.5) * aspect, fyp);
    vec2 E = onFloor ? vec2(aspect, FAR - 1.0) : vec2(aspect, 1.0);
    float r = radians(safe(fieldRotation, -3600.0, 3600.0));
    vec2 dl = rot(P.xy - c, -r);
    float cr = abs(cos(r)), sr = abs(sin(r));
    vec2 ref = vec2(cr * E.x + sr * E.y, cr * E.y + sr * E.x);       // the frame extent the field side lies on
    vec2 sz = max(vec2(safe(fieldWidth, 0.0, 100.0), safe(fieldHeight, 0.0, 100.0)) * ref, vec2(1e-4));
    vec2 f = dl / sz + 0.5;
    float inside = (f.x >= 0.0 && f.x <= 1.0 && f.y >= 0.0 && f.y <= 1.0) ? 1.0 : 0.0;
    return vec3(f, inside);
}

// The processed field at a frame position: x occupancy, y excitation (0 outside the field).
// Interpolated by hand (Wire reads float textures unfiltered). Macros, because Wire's ISF reads
// images only directly in main().
#define FIELD_UV(UVP, OUT) { vec3 ff_ = toField(UVP, aspect, H); OUT = vec2(0.0); if (ff_.z > 0.5) { vec2 fp_ = clamp(ff_.xy, 0.0, 1.0) * FIELD_RES - 0.5; vec2 fi_ = floor(fp_); vec2 fq_ = fp_ - fi_; vec2 f0_ = (clamp(fi_, vec2(0.0), FIELD_RES - 1.0) + 0.5) / FIELD_RES; vec2 f1_ = (clamp(fi_ + 1.0, vec2(0.0), FIELD_RES - 1.0) + 0.5) / FIELD_RES; vec4 fv_ = mix(mix(IMG_NORM_PIXEL(field, f0_), IMG_NORM_PIXEL(field, vec2(f1_.x, f0_.y)), fq_.x), mix(IMG_NORM_PIXEL(field, vec2(f0_.x, f1_.y)), IMG_NORM_PIXEL(field, f1_), fq_.x), fq_.y); OUT = vec2(safe(fv_.g, 0.0, 1.0), safe(fv_.r, 0.0, 1.0)); } }
// a firefly j from the buffer that is not being written in this pass: SRC 0 = partsA, 1 = partsB
#define PART(SRC, J, OUT) { vec2 t_ = vec2((float(J) + 0.5) / float(N), 0.25); vec2 t2_ = vec2(t_.x, 0.75); vec4 sg_ = SRC == 0 ? IMG_NORM_PIXEL(partsA, t2_) : IMG_NORM_PIXEL(partsB, t2_); OUT = abs(sg_.r - SIG) < 0.01 ? (SRC == 0 ? IMG_NORM_PIXEL(partsA, t_) : IMG_NORM_PIXEL(partsB, t_)) : vec4(OFF, OFF, 0.0, 0.0); }
#define PART_ROW1(SRC, J, OUT) { vec2 t2_ = vec2((float(J) + 0.5) / float(N), 0.75); OUT = SRC == 0 ? IMG_NORM_PIXEL(partsA, t2_) : IMG_NORM_PIXEL(partsB, t2_); }

bool validPart(vec4 s) {
    return s.x == s.x && s.y == s.y && s.z == s.z && s.w == s.w && abs(s.x) < OFF * 0.5 && abs(s.y) < OFF * 0.5 && abs(s.z) <= 1e4 && abs(s.w) <= 1e4;
}

// how brightly firefly j glows at time t: on for about Firefly Life, off for about Dark Time,
// each with its own durations and phase; while on it twinkles
float litOf(float j, float t) {
    float life = max(safe(fireflyLife, 0.0, 30.0), 0.0) * (0.7 + 0.6 * hash3(vec3(j, 1.3, 2.1)));
    float dark = max(safe(darkTime, 0.0, 30.0), 0.0) * (0.7 + 0.6 * hash3(vec3(j, 4.7, 5.3)));
    float on = 1.0;
    if (life <= 0.001) on = 0.0;
    else if (dark > 0.001) {
        float period = life + dark;
        float ph = mod(t + hash3(vec3(j, 8.9, 0.7)) * period, period);
        float fade = min(0.3, 0.3 * life);
        on = smoothstep(0.0, fade, ph) * (1.0 - smoothstep(life - fade, life, ph));
    }
    float tw = clamp(safe(fireflyTwinkle, 0.0, 1.0), 0.0, 1.0);
    float flick = smoothstep(0.3, 0.85, noise1(t * 1.7 + j * 0.71, j + 0.5));
    return on * mix(1.0, flick * 1.3, tw);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 inSize = IMG_SIZE(inputImage);
    // in a pass, RENDERSIZE is the size of the pass; the input has the output's size
    vec2 res = max(inSize.x > 0.5 && inSize.y > 0.5 ? inSize : RENDERSIZE, vec2(1.0));
    float aspect = res.x / res.y;
    float S = clamp(scale, 0.001, 1000.0);
    float H = clamp(horizonY * S, 0.001, 1000.0);

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
        // without it (simulated objects only, older bridge) B is 0 everywhere and is ignored
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
    float rt = safe(s1.g, 0.0, 3600.0);                    // real time

    // ---------- pass 1: blur along x (r occupancy, g motion), in field space; b visibility ----------
    if (PASSINDEX == 1) {
        vec2 sum = vec2(0.0);
        float wsum = 0.0;
        for (int k = -6; k <= 6; k++) {
            float o = float(k) / 6.0;
            float w = exp(-o * o * 2.0);
            float fx = uv.x + o * BLUR_X;
            vec2 v = IMG_NORM_PIXEL(inputImage, vec2(clamp(fx, 0.0, 1.0), uv.y * FIELD_H)).rg;
            if (fx < 0.0 || fx > 1.0) v = vec2(0.0);
            sum += v * w;
            wsum += w;
        }
        float vis = IMG_NORM_PIXEL(inputImage, vec2(uv.x, uv.y * FIELD_H)).b;
        gl_FragColor = vec4(sum / wsum, vis, 1.0);
        return;
    }

    // ---------- pass 2: blur along depth ----------
    if (PASSINDEX == 2) {
        vec2 sum = vec2(0.0);
        float wsum = 0.0;
        for (int k = -6; k <= 6; k++) {
            float o = float(k) / 6.0;
            float w = exp(-o * o * 2.0);
            float fd = uv.y + o * BLUR_D;
            vec2 v = IMG_NORM_PIXEL(blurX, vec2(uv.x, clamp(fd, 0.0, 1.0))).rg;
            if (fd < 0.0 || fd > 1.0) v = vec2(0.0);
            sum += v * w;
            wsum += w;
        }
        float vis = IMG_NORM_PIXEL(blurX, uv).b;
        gl_FragColor = vec4(sum / wsum, vis, 1.0);
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

    // where the fireflies fly: the frame, or the floor from the bottom edge to FAR, or to the top
    // edge when the horizon lies above the frame
    bool fFloor = safe(fireflyPlane, 0.0, 1.0) < 0.5;
    float zFar = H > 1.0001 ? min(FAR, H / (H - 1.0)) : FAR;
    float area = fFloor ? aspect * (zFar * zFar - 1.0) * 0.5 : aspect;
    // how many fireflies: Firefly Density per reference area (output height x output height at
    // Scale 1; on the floor its size at the bottom edge), so a wider surface gets more; at most
    // Performance Cap and 1024
    float wantF = safe(fireflyDensity, 0.0, 1e5) * area / (S * S);
    float countF = min(min(wantF, floor(safe(performanceCap, 0.0, 1e5))), float(N));
    int count = int(ceil(countF));
    float lastFade = countF - floor(countF);
    if (lastFade < 1e-4) lastFade = 1.0;
    float rad = max(safe(fireflySize, 0.0, 100.0), 0.0) * S;   // radius of the bright core, output heights
    vec2 lo = vec2(-0.5 * aspect, 0.0), hi = vec2(0.5 * aspect, 1.0);
    if (fFloor) { lo = vec2(-0.5 * aspect * zFar, 1.0); hi = vec2(0.5 * aspect * zFar, zFar); }

    // ---------- passes 4 and 5: two simulation steps ----------
    // Each firefly keeps its own meandering heading (Wander). It senses the field around it,
    // in 16 points at two distances (0.15 and 0.4 output heights x Scale): what stands still attracts it (Swarm Strength), and it
    // circles the edge of that object in its own turning sense; motion repels it (Flee Strength).
    // Fleeing always wins: near motion the attraction is switched off. Reverse swaps the roles.
    if (PASSINDEX == 4 || PASSINDEX == 5) {
        int src = PASSINDEX == 4 ? 0 : 1;                  // read the other buffer
        int i = int(floor(uv.x * float(N)));
        bool rowH = uv.y > 0.5;
        float sdt = dt * 0.5;
        float seed = float(i);
        vec4 s;
        PART(src, i, s)
        vec4 r1;
        PART_ROW1(src, i, r1)
        bool was = validPart(s) && abs(r1.r - SIG) < 0.01;
        if (i >= count) { gl_FragColor = rowH ? vec4(SIG, 0.0, 0.0, 1.0) : vec4(OFF, OFF, 0.0, 0.0); return; }
        if (!was) {
            // a new firefly appears somewhere in the frame, at rest
            vec2 rnd = vec2(hash3(vec3(seed, 1.1, rt)), hash3(vec3(seed, 2.3, rt + 3.1)));
            vec2 pos = mix(lo, hi, rnd);
            if (fFloor) {
                // evenly over the floor in view: more of it far away
                float Z = sqrt(1.0 + rnd.y * (zFar * zFar - 1.0));
                pos = vec2((rnd.x - 0.5) * aspect * Z, Z);
            }
            float sense = hash3(vec3(seed, 5.5, 1.7)) < 0.5 ? -1.0 : 1.0;
            gl_FragColor = rowH ? vec4(SIG, hash3(vec3(seed, 7.7, rt)) * TAU, sense, 1.0) : vec4(pos, 0.0, 0.0);
            return;
        }
        vec2 pos = s.xy, v = s.zw;
        float hd = safe(r1.g, -1e4, 1e4);
        float sense = r1.b < 0.0 ? -1.0 : 1.0;

        // sense the field: still = occupancy without motion, mot = motion; Reverse swaps them:
        // below 0.5 normal, above 0.5 reversed, a soft change in between (an Arena Timeline
        // ramp of 30 s, Loop, swaps them every 15 s)
        float rev = smoothstep(0.45, 0.55, safe(reverse, 0.0, 1.0));
        vec2 g;
        vec3 fu = toFrame(pos, fFloor, aspect, H);
        g = vec2(0.0);
        if (fu.z > 0.5) FIELD_UV(fu.xy, g)
        float still0 = g.x * (1.0 - smoothstep(0.1, 0.4, g.y));
        float A0 = mix(still0, g.y, rev), F0 = mix(g.y, still0, rev);
        vec2 gA = vec2(0.0), gF = vec2(0.0);
        float fear = F0;
        float turn = hash3(vec3(seed, 3.9, 6.1)) * TAU;    // its own rotation of the sensing star
        for (int k = 0; k < 16; k++) {
            float ang = turn + float(k) * TAU / 8.0;
            float dist = (k < 8 ? 0.15 : 0.4) * S;
            float wk = k < 8 ? 1.0 : 0.6;
            vec2 dir = vec2(cos(ang), sin(ang));
            vec2 q = pos + dir * dist;
            vec2 gq;
            vec3 qu = toFrame(q, fFloor, aspect, H);
            gq = vec2(0.0);
            if (qu.z > 0.5) FIELD_UV(qu.xy, gq)
            float stillq = gq.x * (1.0 - smoothstep(0.1, 0.4, gq.y));
            float Aq = mix(stillq, gq.y, rev), Fq = mix(gq.y, stillq, rev);
            gA += dir * Aq * wk;
            gF += dir * Fq * wk;
            fear = max(fear, Fq * wk);
        }
        gA /= 8.0; gF /= 8.0;
        float base = 0.6 * S;                              // typical speed, output heights per second

        // flee from motion: away from it, the harder the closer
        float fl = max(safe(fleeStrength, 0.0, 100.0), 0.0);
        float afraid = smoothstep(0.08, 0.35, fear);         // above the sensor noise
        vec2 away = length(gF) > 1e-4 ? -normalize(gF) : rot(vec2(1.0, 0.0), hd);
        vec2 acc = away * afraid * fl * base * 6.0;

        // swarm around what stands still: towards it, on its soft edge, circling it
        float sw = max(safe(swarmStrength, 0.0, 100.0), 0.0);
        float calm = 1.0 - afraid;                         // fleeing always wins
        float near = smoothstep(0.005, 0.08, length(gA) + A0);
        if (near > 0.0) {
            vec2 to = length(gA) > 1e-4 ? normalize(gA) : vec2(0.0);
            float edge = 0.35;                             // the level of the soft edge it circles on
            vec2 radial = to * clamp((edge - A0) * 4.0, -1.0, 1.0);
            vec2 tang = vec2(-to.y, to.x) * sense;
            acc += (radial * 3.0 + tang * 1.5) * near * calm * sw * base * 2.0;
        }

        // wander: its own heading meanders per distance flown (as in Star Globe)
        float wnd = max(safe(wander, 0.0, 100.0), 0.0);
        float wspeed = wnd * 0.15 * S;
        float flown = max(length(v), wspeed) * sdt;
        float g1 = hash3(vec3(seed + 0.25, rt * 389.0, float(PASSINDEX) + 3.0));
        float g2 = hash3(vec3(seed + 0.75, rt * 211.0, float(PASSINDEX) + 5.0));
        float gauss = sqrt(-2.0 * log(max(g1, 1e-6))) * cos(TAU * g2);
        hd += gauss * sqrt(2.0 * flown / (0.25 * S));
        float wanderW = 1.0 - 0.85 * near * calm;          // while swarming it follows the swarm, not its own way
        v += (vec2(cos(hd), sin(hd)) * wspeed - v) * (1.0 - exp(-1.5 * sdt)) * min(wnd, 1.0) * wanderW;
        v += acc * sdt;
        v *= exp(-max(safe(airDrag, 0.0, 100.0), 0.0) * sdt);
        float vmax = base * (2.0 + 3.0 * afraid * min(fl, 3.0));
        float spd = length(v);
        if (spd > vmax) v *= vmax / spd;
        pos += v * sdt;

        // the frame (or the floor in view) is the wall: back inside, bounce, and the heading
        // turns back inside too
        vec2 m = vec2(rad);
        if (fFloor) {
            pos.y = clamp(pos.y, 1.0, zFar);
            float xe = 0.5 * aspect * pos.y;            // the floor's edges in view widen with the distance
            lo.x = -xe; hi.x = xe;
        }
        if (pos.x < lo.x + m.x) { pos.x = lo.x + m.x; v.x = abs(v.x); if (cos(hd) < 0.0) hd = 3.14159265 - hd; }
        if (pos.x > hi.x - m.x) { pos.x = hi.x - m.x; v.x = -abs(v.x); if (cos(hd) > 0.0) hd = 3.14159265 - hd; }
        if (pos.y < lo.y + m.y) { pos.y = lo.y + m.y; v.y = abs(v.y); if (sin(hd) < 0.0) hd = -hd; }
        if (pos.y > hi.y - m.y) { pos.y = hi.y - m.y; v.y = -abs(v.y); if (sin(hd) > 0.0) hd = -hd; }
        if (!(pos.x == pos.x && pos.y == pos.y && v.x == v.x && v.y == v.y)) { pos = vec2(0.0, 0.5); v = vec2(0.0); }
        hd = mod(safe(hd, -1e4, 1e4), TAU);
        gl_FragColor = rowH ? vec4(SIG, hd, sense, 1.0) : vec4(pos, v);
        return;
    }

    // ---------- pass 6: the picture ----------
    vec2 P = vec2((uv.x - 0.5) * aspect, uv.y);
    float px = 1.0 / res.y;                                // one output pixel in output heights
    vec3 col = vec3(0.0);
    float a = 0.0;

    // halo: what stands still, as a soft light that follows the blurred field
    if (safe(haloOn, 0.0, 1.0) > 0.5) {
        float h = 0.0;
        for (int k = 0; k < 9; k++) {
            vec2 o = (vec2(float(k - (k / 3) * 3), float(k / 3)) - 1.0) * 0.012;   // a zone: in frame heights, not x Scale
            vec2 gh;
            FIELD_UV(uv + vec2(o.x / aspect, o.y), gh)
            h += gh.x * (1.0 - smoothstep(0.1, 0.4, gh.y));
        }
        h /= 9.0;
        float ho = clamp(safe(haloOpacity, 0.0, 1.0), 0.0, 1.0);
        over(col, a, haloColor.rgb, smoothstep(0.02, 0.6, h) * ho * clamp(haloColor.a, 0.0, 1.0));
    }

    // fireflies: a bright core and a soft glow
    float gr = max(safe(glowSize, 0.0, 100.0), 0.0) * S;
    float glow = 0.0, core = 0.0;
    float reach = max(gr * 3.0, rad + px);
    for (int j = 0; j < N; j++) {
        if (j >= count) break;
        vec4 s;
        PART(0, j, s)
        if (!validPart(s)) continue;
        // where it is in the picture and how big: on the floor smaller with the distance
        vec2 sp = s.xy;
        float k = 1.0, far = 1.0;
        if (fFloor) {
            vec3 fr = toFrame(s.xy, true, aspect, H);
            if (fr.z < 0.5) continue;
            sp = vec2((fr.x - 0.5) * aspect, fr.y);
            k = 1.0 / max(s.y, 1.0);
            far = 1.0 - smoothstep(0.75 * FAR, FAR, s.y);   // fade out towards the far end
        }
        vec2 d = P - sp;
        if (abs(d.x) > reach * k + px || abs(d.y) > reach * k + px) continue;
        float bright = litOf(float(j), rt) * (j == count - 1 ? lastFade : 1.0) * far;
        if (bright <= 0.0) continue;
        float dd = dot(d, d);
        float gk = gr * k, rk = rad * k;
        if (gk > 0.0) glow += exp(-2.0 * dd / (gk * gk)) * bright;   // Glow Size: where it has faded to 1/8
        if (rk > 0.0) core = max(core, (1.0 - smoothstep(rk - px, rk + px, sqrt(dd))) * clamp(bright, 0.0, 1.0));
    }
    over(col, a, glowColor.rgb, (1.0 - exp(-glow * 0.5)) * clamp(glowColor.a, 0.0, 1.0));
    over(col, a, fireflyColor.rgb, core * clamp(fireflyColor.a, 0.0, 1.0));

    vec4 outC = vec4(col, a);
    if (safe(showField, 0.0, 1.0) > 0.5) {
        vec3 F = toField(uv, aspect, H);
        if (F.z > 0.5) {
            // red occupancy, green excitation, blue a held shadow; white the front, magenta the other edges
            vec4 fc = IMG_NORM_PIXEL(field, clamp(F.xy, 0.0, 1.0));
            vec3 fcol = vec3(safe(fc.g, 0.0, 1.0), safe(fc.r, 0.0, 1.0), safe(fc.b, 0.0, 1.0) * 0.8);
            float fa = max(max(max(fcol.r, fcol.g), fcol.b * 0.6) * 0.8, 0.12);
            vec3 Fx = toField(uv + vec2(1.0 / res.x, 0.0), aspect, H);
            vec3 Fy = toField(uv + vec2(0.0, 1.0 / res.y), aspect, H);
            float dfx = max(length(vec2(Fx.x - F.x, Fy.x - F.x)), 1e-6);
            float dfd = max(length(vec2(Fx.y - F.y, Fy.y - F.y)), 1e-6);
            float side = min(F.x, 1.0 - F.x) / dfx;
            float farEdge = (1.0 - F.y) / dfd;
            float front = F.y / dfd;
            if (min(side, farEdge) < 1.5) { fcol = vec3(1.0, 0.0, 1.0); fa = 1.0; }
            if (front < 2.5) { fcol = vec3(1.0); fa = 1.0; }
            outC.rgb = mix(outC.rgb, fcol, fa * (1.0 - outC.a * 0.5));
            outC.a = max(outC.a, fa);
        }
    }
    if (!(outC.r == outC.r && outC.g == outC.g && outC.b == outC.b && outC.a == outC.a)) outC = vec4(0.0);
    // premultiplied: Arena's Alpha blend adds the colour as it is (a x (1 - alpha) + b)
    outC.a = clamp(outC.a, 0.0, 1.0);
    gl_FragColor = vec4(clamp(outC.rgb, 0.0, 1e4) * outC.a, outC.a);
}
