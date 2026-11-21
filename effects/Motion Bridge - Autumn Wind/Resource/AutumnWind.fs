/*{
  "DESCRIPTION": "Motion Bridge - Autumn Wind: leaves, maple seeds, chestnuts, twigs and feathers drift in the wind, over the screen with depth or over the floor in perspective. Motion stirs them up; blown against something standing still they stick to it and gather there. Driven by the Motion Bridge field (Spout). Calm without the bridge; transparent background.",
  "CREDIT": "Stefan Mazur - https://github.com/S-Mazu/WirePatches",
  "ISFVSN": "2",
  "CATEGORIES": [
    "Effect"
  ],
  "INPUTS": [
    { "NAME": "inputImage", "TYPE": "image" },
    { "NAME": "scale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "windStrength", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "windDirection", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 360.0 },
    { "NAME": "fallStrength", "TYPE": "float", "DEFAULT": 0.15, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "fallDirection", "TYPE": "float", "DEFAULT": 270.0, "MIN": 0.0, "MAX": 360.0 },
    { "NAME": "gustSway", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.0, "MAX": 5.0 },
    { "NAME": "tumble", "TYPE": "float", "DEFAULT": 0.25, "MIN": 0.0, "MAX": 5.0 },
    { "NAME": "speedVariation", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "eddies", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 5.0 },
    { "NAME": "eddySize", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 5.0 },
    { "NAME": "stirStrength", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "stickStrength", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "itemPlane", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "performanceCap", "TYPE": "float", "DEFAULT": 1024.0, "MIN": 0.0, "MAX": 1024.0 },
    { "NAME": "itemSize", "TYPE": "float", "DEFAULT": 0.2, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "depthSpread", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "depthHaze", "TYPE": "float", "DEFAULT": 0.35, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "colorVariation", "TYPE": "float", "DEFAULT": 0.4, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "variationColor", "TYPE": "color", "DEFAULT": [0.62, 0.12, 0.08, 1.0] },
    { "NAME": "leafDensity", "TYPE": "float", "DEFAULT": 16.0, "MIN": 0.0, "MAX": 64.0 },
    { "NAME": "leafColor", "TYPE": "color", "DEFAULT": [0.85, 0.45, 0.12, 1.0] },
    { "NAME": "mapleSeedDensity", "TYPE": "float", "DEFAULT": 4.0, "MIN": 0.0, "MAX": 64.0 },
    { "NAME": "mapleSeedColor", "TYPE": "color", "DEFAULT": [0.78, 0.58, 0.32, 1.0] },
    { "NAME": "chestnutDensity", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 64.0 },
    { "NAME": "chestnutColor", "TYPE": "color", "DEFAULT": [0.45, 0.22, 0.1, 1.0] },
    { "NAME": "twigDensity", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.0, "MAX": 64.0 },
    { "NAME": "twigColor", "TYPE": "color", "DEFAULT": [0.4, 0.27, 0.15, 1.0] },
    { "NAME": "featherDensity", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 64.0 },
    { "NAME": "featherColor", "TYPE": "color", "DEFAULT": [0.92, 0.9, 0.85, 1.0] },
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
// Two planes for the items (Item Plane), as in Motion Bridge - Fabric Wind and Fireflies:
//   Screen: the frame, in output heights, x from -aspect/2 to aspect/2, y from 0 to 1. Every item
//           has its own depth d (0 near, 1 far): far items are smaller and slower (Depth Spread)
//           and fainter (Depth Haze). The field is read as seen from the front (issue #70): the
//           item's x gives the position along the front (Field X, Field Width), its depth d the
//           distance from the surface. So motion near the surface stirs near items, far away the
//           far ones, and someone standing still is an invisible column where items of their
//           depth gather.
//   Floor:  a view over the floor in front of the projection; a pixel at height y below the
//           horizon H sees the floor at the distance Z = H / (H - y); plane coordinates (X, Z),
//           X = (u - 0.5) * aspect * Z. The items drift over the floor from the bottom edge to
//           Z = FAR (or the top edge when the horizon lies above the frame), smaller with the
//           distance; they read the field where they are (Field Plane and the other Field controls).
//
// Passes (pass targets are only read where they are not being written, see Star Globe):
//   0 state (2 x 1): now, last counter, time of the last counter change | dt, real time, visibility in use
//   1, 2 blurX, blurXY (160 x 120): occupancy and motion blurred, visibility unblurred
//   3 field (160 x 120): r excitation, g occupancy smoothed, b 1 in a held shadow
//   4 partsB from partsA, 5 partsA from partsB (1024 x 2): two simulation steps per frame;
//     row 0 position and velocity, row 1 signature, rotation, stuck (0..1), flip angle
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
const float OFF = 10000.0;         // position of an item that is not in use

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

float vnoise(vec2 p) {                              // smooth 2D value noise 0..1
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash3(vec3(i, 1.7)), b = hash3(vec3(i + vec2(1.0, 0.0), 1.7));
    float c = hash3(vec3(i + vec2(0.0, 1.0), 1.7)), d = hash3(vec3(i + vec2(1.0, 1.0), 1.7));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
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
// an item j from the buffer that is not being written in this pass: SRC 0 = partsA, 1 = partsB
#define PART(SRC, J, OUT) { vec2 t_ = vec2((float(J) + 0.5) / float(N), 0.25); vec2 t2_ = vec2(t_.x, 0.75); vec4 sg_ = SRC == 0 ? IMG_NORM_PIXEL(partsA, t2_) : IMG_NORM_PIXEL(partsB, t2_); OUT = abs(sg_.r - SIG) < 0.01 ? (SRC == 0 ? IMG_NORM_PIXEL(partsA, t_) : IMG_NORM_PIXEL(partsB, t_)) : vec4(OFF, OFF, 0.0, 0.0); }
#define PART_ROW1(SRC, J, OUT) { vec2 t2_ = vec2((float(J) + 0.5) / float(N), 0.75); OUT = SRC == 0 ? IMG_NORM_PIXEL(partsA, t2_) : IMG_NORM_PIXEL(partsB, t2_); }

bool validPart(vec4 s) {
    return s.x == s.x && s.y == s.y && s.z == s.z && s.w == s.w && abs(s.x) < OFF * 0.5 && abs(s.y) < OFF * 0.5 && abs(s.z) <= 1e4 && abs(s.w) <= 1e4;
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

    // ---------- items: where they fly, how many, of which kind ----------
    // Screen: the frame. Floor: from the bottom edge to FAR, or to the top edge when the horizon
    // lies above the frame.
    bool fFloor = safe(itemPlane, 0.0, 1.0) < 0.5;
    float zFar = H > 1.0001 ? min(FAR, H / (H - 1.0)) : FAR;
    float area = fFloor ? aspect * (zFar * zFar - 1.0) * 0.5 : aspect;
    vec2 lo = vec2(-0.5 * aspect, 0.0), hi = vec2(0.5 * aspect, 1.0);
    if (fFloor) { lo = vec2(-0.5 * aspect * zFar, 1.0); hi = vec2(0.5 * aspect * zFar, zFar); }

    // Each Density is items per reference area (output height x output height at Scale 1; on the
    // floor its size at the bottom edge); when they ask for more than Performance Cap (and 1024),
    // all kinds are scaled down together. Item i gets its kind from where it lies in the sum.
    float k0 = safe(leafDensity, 0.0, 1e4), k1 = safe(mapleSeedDensity, 0.0, 1e4);
    float k2 = safe(chestnutDensity, 0.0, 1e4), k3 = safe(twigDensity, 0.0, 1e4);
    float k4 = safe(featherDensity, 0.0, 1e4);
    float ksum = k0 + k1 + k2 + k3 + k4;
    float wantF = ksum * area / (S * S);
    float countF = min(min(wantF, floor(safe(performanceCap, 0.0, 1e5))), float(N));
    int count = int(ceil(countF));
    float rad = max(safe(itemSize, 0.0, 100.0), 0.0) * 0.2 * S;     // half length of an item, output heights
    float spread = clamp(safe(depthSpread, 0.0, 1.0), 0.0, 1.0);

    // ---------- passes 4 and 5: two simulation steps ----------
    // Wind and fall carry the items, gusts sway them across the wind, eddies whirl them, and
    // each spins and flips (Tumble). Motion where an item is stirs it up (Stir Strength) and
    // tears it off; occupancy without motion holds it (Stick Strength): it slows down and stays
    // there while the object stands. Items that leave the frame come back on the other side.
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
        if (i >= count) { gl_FragColor = rowH ? vec4(SIG, 0.0, 0.0, 0.0) : vec4(OFF, OFF, 0.0, 0.0); return; }
        float dep = hash3(vec3(seed, 4.4, 9.9));            // its depth on the screen, 0 near .. 1 far
        if (!was) {
            vec2 rnd = vec2(hash3(vec3(seed, 1.1, rt)), hash3(vec3(seed, 2.3, rt + 3.1)));
            vec2 pos = mix(lo, hi, rnd);
            if (fFloor) {
                float Z = sqrt(1.0 + rnd.y * (zFar * zFar - 1.0));   // evenly over the floor in view
                pos = vec2((rnd.x - 0.5) * aspect * Z, Z);
            }
            gl_FragColor = rowH ? vec4(SIG, hash3(vec3(seed, 7.7, rt)) * TAU, 0.0, hash3(vec3(seed, 8.1, rt)) * TAU) : vec4(pos, 0.0, 0.0);
            return;
        }
        vec2 pos = s.xy, v = s.zw;
        float ang = safe(r1.g, -1e4, 1e4), stuck = safe(r1.b, 0.0, 1.0), flip = safe(r1.a, -1e4, 1e4);

        // depth: on the screen its own layer, on the floor the perspective does it
        float kd = fFloor ? 1.0 : 1.0 - 0.8 * spread * dep;
        float sv = clamp(safe(speedVariation, 0.0, 1.0), 0.0, 1.0);
        float kv = kd * (1.0 + sv * (hash3(vec3(seed, 3.3, 1.2)) - 0.5) * 2.0);

        // the field where the item is
        vec2 g = vec2(0.0);
        if (fFloor) {
            vec3 fu = toFrame(pos, true, aspect, H);
            if (fu.z > 0.5) FIELD_UV(fu.xy, g)
        } else {
            // as seen from the front: x along the front, the item's depth as the distance
            float fw = max(safe(fieldWidth, 0.0, 100.0), 1e-4);
            float fx = (pos.x / aspect + 0.5 - (safe(fieldX, -100.0, 100.0) - 0.5 * fw)) / fw;
            float fd = dep / max(safe(fieldHeight, 0.0, 100.0), 1e-4);
            if (fx >= 0.0 && fx <= 1.0 && fd <= 1.0) {
                vec2 fp_ = vec2(fx, fd) * FIELD_RES - 0.5; vec2 fi_ = floor(fp_); vec2 fq_ = fp_ - fi_;
                vec2 f0_ = (clamp(fi_, vec2(0.0), FIELD_RES - 1.0) + 0.5) / FIELD_RES;
                vec2 f1_ = (clamp(fi_ + 1.0, vec2(0.0), FIELD_RES - 1.0) + 0.5) / FIELD_RES;
                vec4 fv_ = mix(mix(IMG_NORM_PIXEL(field, f0_), IMG_NORM_PIXEL(field, vec2(f1_.x, f0_.y)), fq_.x),
                               mix(IMG_NORM_PIXEL(field, vec2(f0_.x, f1_.y)), IMG_NORM_PIXEL(field, f1_), fq_.x), fq_.y);
                g = vec2(safe(fv_.g, 0.0, 1.0), safe(fv_.r, 0.0, 1.0));
            }
        }
        float exc = smoothstep(0.08, 0.35, g.y);            // motion, above the sensor noise
        float still = smoothstep(0.25, 0.6, g.x) * (1.0 - exc);

        // wind, fall, gusts across the wind, eddies
        float wa = radians(safe(windDirection, -3600.0, 3600.0)), fa = radians(safe(fallDirection, -3600.0, 3600.0));
        vec2 wdir = vec2(cos(wa), sin(wa));
        vec2 target = (wdir * safe(windStrength, 0.0, 100.0) + vec2(cos(fa), sin(fa)) * safe(fallStrength, 0.0, 100.0)) * S;
        target += vec2(-wdir.y, wdir.x) * (noise1(rt * 0.7 + seed * 0.37, seed) - 0.5) * 2.0 * safe(gustSway, 0.0, 100.0) * 0.5 * S;
        // eddies: the curl of a slowly drifting noise field, small and large ones around Eddy Size
        float es = max(safe(eddySize, 0.0, 100.0), 0.01) * S;
        vec2 ep = pos / es + vec2(rt * 0.13, -rt * 0.11);
        float n0 = vnoise(ep) + 0.5 * vnoise(ep * 2.3 + 4.1);
        float nx = vnoise(ep + vec2(0.05, 0.0)) + 0.5 * vnoise((ep + vec2(0.05, 0.0)) * 2.3 + 4.1);
        float ny = vnoise(ep + vec2(0.0, 0.05)) + 0.5 * vnoise((ep + vec2(0.0, 0.05)) * 2.3 + 4.1);
        vec2 grad = vec2(nx - n0, ny - n0) / 0.05;
        target += vec2(grad.y, -grad.x) * safe(eddies, 0.0, 100.0) * 0.15 * S;
        target *= kv;
        v += (target - v) * (1.0 - exp(-2.0 * sdt)) * (1.0 - stuck);

        // motion stirs it up: a kick in a random direction (upward on the screen), and it tears off
        float st = max(safe(stirStrength, 0.0, 100.0), 0.0);
        if (exc > 0.0 && st > 0.0) {
            float ka = hash3(vec3(seed, floor(rt * 3.0), 5.1)) * TAU;
            vec2 kick = vec2(cos(ka), sin(ka));
            if (!fFloor) kick = normalize(kick + vec2(0.0, 1.2));
            v += kick * exc * st * 6.0 * S * kd * sdt;
            stuck = max(stuck - exc * st * 4.0 * sdt, 0.0);
        }
        // occupancy without motion holds it: it gets stuck while the object stands
        float sk = max(safe(stickStrength, 0.0, 100.0), 0.0);
        if (still > 0.0 && sk > 0.0) stuck = min(stuck + still * sk * 2.0 * sdt, 1.0);
        else stuck = max(stuck - 1.5 * sdt, 0.0);           // the object has gone: it lets go
        v *= 1.0 - stuck * stuck;

        float spd = length(v), vmax = 4.0 * S * kd;
        if (spd > vmax) v *= vmax / spd;
        pos += v * sdt;

        // spin and flip (Tumble), faster when it is stirred, not while it sticks
        float tb = max(safe(tumble, 0.0, 100.0), 0.0);
        float spin = (hash3(vec3(seed, 6.2, 2.4)) - 0.5) * 2.0 * tb * 4.0 * (1.0 + 4.0 * exc);
        ang += spin * sdt * (1.0 - stuck);
        flip += (0.5 + hash3(vec3(seed, 1.9, 7.3))) * tb * 6.0 * (1.0 + 3.0 * exc) * sdt * (1.0 - stuck);

        // out of the frame (or of the floor in view): back in on the other side
        if (fFloor) {
            if (pos.y < 1.0) pos.y += zFar - 1.0;
            if (pos.y > zFar) pos.y -= zFar - 1.0;
            float xe = 0.5 * aspect * pos.y + rad;
            if (pos.x < -xe) pos.x += 2.0 * xe;
            if (pos.x > xe) pos.x -= 2.0 * xe;
        } else {
            float m = rad * 1.5;
            if (pos.x < lo.x - m) pos.x += hi.x - lo.x + 2.0 * m;
            if (pos.x > hi.x + m) pos.x -= hi.x - lo.x + 2.0 * m;
            if (pos.y < lo.y - m) pos.y += hi.y - lo.y + 2.0 * m;
            if (pos.y > hi.y + m) pos.y -= hi.y - lo.y + 2.0 * m;
        }
        if (!(pos.x == pos.x && pos.y == pos.y && v.x == v.x && v.y == v.y)) { pos = mix(lo, hi, vec2(0.5)); v = vec2(0.0); }
        gl_FragColor = rowH ? vec4(SIG, mod(ang, TAU), stuck, mod(flip, TAU)) : vec4(pos, v);
        return;
    }

    // ---------- pass 6: the picture ----------
    vec2 P = vec2((uv.x - 0.5) * aspect, uv.y);
    float px = 1.0 / res.y;                                // one output pixel in output heights
    float haze = clamp(safe(depthHaze, 0.0, 1.0), 0.0, 1.0);
    float cv = clamp(safe(colorVariation, 0.0, 1.0), 0.0, 1.0);
    // the nearest item in front, and one behind it
    vec4 fC = vec4(0.0), bC = vec4(0.0);
    float fD = 1e9, bD = 1e9;
    for (int j = 0; j < N; j++) {
        if (j >= count) break;
        vec4 s;
        PART(0, j, s)
        if (!validPart(s)) continue;
        vec4 r1;
        PART_ROW1(0, j, r1)
        float fj = float(j);
        float dep = hash3(vec3(fj, 4.4, 9.9));
        // where it is in the picture and how big
        vec2 sp = s.xy;
        float k = 1.0 - 0.8 * spread * dep, depth = dep;
        if (fFloor) {
            vec3 fr = toFrame(s.xy, true, aspect, H);
            if (fr.z < 0.5) continue;
            sp = vec2((fr.x - 0.5) * aspect, fr.y);
            k = 1.0 / max(s.y, 1.0);
            depth = (s.y - 1.0) / max(FAR - 1.0, 1e-3);
        }
        float r = rad * k;
        if (r <= 0.0) continue;
        vec2 d = P - sp;
        if (abs(d.x) > r * 1.4 + px || abs(d.y) > r * 1.4 + px) continue;
        // its kind from where it lies in the sum of the densities
        float u = (fj + 0.5) / max(countF, 1.0) * ksum;
        int kind = u < k0 ? 0 : (u < k0 + k1 ? 1 : (u < k0 + k1 + k2 ? 2 : (u < k0 + k1 + k2 + k3 ? 3 : 4)));
        vec4 base = kind == 0 ? leafColor : (kind == 1 ? mapleSeedColor : (kind == 2 ? chestnutColor : (kind == 3 ? twigColor : featherColor)));
        if (hash3(vec3(fj, 2.8, 6.6)) < cv) base.rgb = mix(base.rgb, variationColor.rgb, 0.4 + 0.6 * hash3(vec3(fj, 5.2, 3.7)));
        // its own shape, turned (Tumble) and flipped like a leaf turning over
        vec2 q = rot(d, -safe(r1.g, -1e4, 1e4)) / r;
        float cf = cos(safe(r1.a, -1e4, 1e4));
        q.y /= max(abs(cf), 0.18);
        float pq = px / r / max(abs(cf), 0.18);           // one pixel in its units
        float sdf, shade = 1.0;
        if (kind == 0) {                                    // leaf: pointed, with a vein and a stalk
            float t = clamp(q.x, -1.0, 1.0);
            float hw = 0.45 * sqrt(max(1.0 - t * t, 0.0)) * (1.0 - 0.25 * t);
            sdf = max(abs(q.y) - hw, abs(q.x) - 1.0);
            float stalk = max(abs(q.y) - 0.04, max(q.x - 1.3, 1.0 - q.x));
            sdf = min(sdf, stalk);
            shade = 1.0 - 0.3 * (1.0 - smoothstep(0.02, 0.05 + pq, abs(q.y))) * step(abs(q.x), 0.95);
        } else if (kind == 1) {                             // maple seed: a seed and a wing
            float seedS = length(q - vec2(-0.7, 0.0)) - 0.24;
            vec2 wq = (q - vec2(0.15, 0.08)) / vec2(0.85, 0.3);
            float wing = (length(wq) - 1.0) * 0.3;
            sdf = min(seedS, wing);
            shade = seedS < 0.0 ? 0.7 : 1.0;
        } else if (kind == 2) {                             // chestnut: round, with a pale scar
            sdf = length(q / vec2(1.0, 0.85)) * 0.85 - 0.62;
            shade = q.y > 0.3 ? 1.35 : 1.0 - 0.25 * smoothstep(-0.2, 0.6, q.x);
        } else if (kind == 3) {                             // twig: a thin stick with a side branch
            vec2 a0 = vec2(-1.0, -0.05), a1 = vec2(1.0, 0.08);
            float h0 = clamp(dot(q - a0, a1 - a0) / dot(a1 - a0, a1 - a0), 0.0, 1.0);
            float stick = length(q - a0 - (a1 - a0) * h0) - 0.07;
            vec2 b0 = vec2(0.15, 0.02), b1 = vec2(0.6, 0.45);
            float h1 = clamp(dot(q - b0, b1 - b0) / dot(b1 - b0, b1 - b0), 0.0, 1.0);
            float br = length(q - b0 - (b1 - b0) * h1) - 0.05;
            sdf = min(stick, br);
        } else {                                            // feather: a soft vane and a shaft
            float t = clamp(q.x, -1.0, 1.0);
            float hw = 0.3 * sqrt(max(1.0 - t * t, 0.0)) * (0.6 + 0.4 * (1.0 - t));
            sdf = max(abs(q.y) - hw, abs(q.x) - 1.0);
            float shaft = max(abs(q.y) - 0.025, abs(q.x) - 1.15);
            sdf = min(sdf, shaft);
            shade = abs(q.y) < 0.03 ? 0.8 : 1.0 - 0.15 * smoothstep(0.1, 0.3, abs(q.y));
        }
        float cov = 1.0 - smoothstep(-pq, pq, sdf);
        if (cov <= 0.0) continue;
        if (kind == 4) cov *= 0.9;
        vec3 c = base.rgb * shade * (cf >= 0.0 ? 1.0 : 0.72);   // the back side is darker
        float al = cov * clamp(base.a, 0.0, 1.0) * (1.0 - haze * depth);
        if (depth < fD) { bC = fC; bD = fD; fC = vec4(c, al); fD = depth; }
        else if (depth < bD) { bC = vec4(c, al); bD = depth; }
    }
    vec3 col = vec3(0.0);
    float a = 0.0;
    over(col, a, bC.rgb, bC.a);
    over(col, a, fC.rgb, fC.a);

    vec4 outC = vec4(col, a);
    if (safe(showField, 0.0, 1.0) > 0.5) {
        vec3 F = vec3(-1.0, -1.0, 0.0);
        if (fFloor) {
            F = toField(uv, aspect, H);
        } else {
            // as the items see it: x along the front, the picture's height as the distance (near at the bottom)
            float fw = max(safe(fieldWidth, 0.0, 100.0), 1e-4);
            float fx = (uv.x - (safe(fieldX, -100.0, 100.0) - 0.5 * fw)) / fw;
            float fd = (1.0 - uv.y) / max(safe(fieldHeight, 0.0, 100.0), 1e-4);
            F = vec3(fx, fd, (fx >= 0.0 && fx <= 1.0 && fd <= 1.0) ? 1.0 : 0.0);
        }
        if (F.z > 0.5) {
            // red occupancy, green motion, blue a held shadow
            vec4 fc = IMG_NORM_PIXEL(field, clamp(F.xy, 0.0, 1.0));
            vec3 fcol = vec3(safe(fc.g, 0.0, 1.0), safe(fc.r, 0.0, 1.0), safe(fc.b, 0.0, 1.0) * 0.8);
            float fa = max(max(max(fcol.r, fcol.g), fcol.b * 0.6) * 0.8, 0.12);
            outC.rgb = mix(outC.rgb, fcol, fa * (1.0 - outC.a * 0.5));
            outC.a = max(outC.a, fa);
        }
    }
    if (!(outC.r == outC.r && outC.g == outC.g && outC.b == outC.b && outC.a == outC.a)) outC = vec4(0.0);
    // premultiplied: Arena's Alpha blend adds the colour as it is (a x (1 - alpha) + b)
    outC.a = clamp(outC.a, 0.0, 1.0);
    gl_FragColor = vec4(clamp(outC.rgb, 0.0, 1e4) * outC.a, outC.a);
}
