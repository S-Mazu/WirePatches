/*{
  "DESCRIPTION": "Water Drops - a view straight down onto water over the clip below: objects fall in, rings spread, interfere and reflect at the edges of the frame. A real wave simulation in real time, independent of the frame rate. No drop = the water calms down. Put a black clip below for drops on black. Scale sizes everything relative to the output height. Drop: every rising edge over 0.5 releases one object (a button, MIDI note, FFT kick or BPM ramp); up to 8 can be in the air at once. Object (Drop, Stone, Heart, Mixed), Drop X and Drop Y (0 = centre, -1 and 1 = frame edges), Spread (random scatter around X / Y; points beyond an edge fold back into the frame), Drop Size, Drop Strength (height of the waves), Fall Time (seconds from release to impact, 0 = instant), Heart Color. Physics: Wave Speed, Wave Life (seconds until the rings have faded). Water: Refraction (how much the waves bend the clip below), Shading (how plastic the waves look), Glints (sun glints on the crests), Light Color, Light Angle. Shader: Resource/WaterDrops.fs.",
  "CREDIT": "Stefan Mazur - https://github.com/S-Mazu/WirePatches",
  "ISFVSN": "2",
  "CATEGORIES": [
    "Effect"
  ],
  "INPUTS": [
    {
      "NAME": "inputImage",
      "TYPE": "image"
    },
    {
      "NAME": "clockH",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 24.0
    },
    {
      "NAME": "clockM",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 60.0
    },
    {
      "NAME": "clockS",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 60.0
    },
    {
      "NAME": "clockMs",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1000.0
    },
    {
      "NAME": "scale",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "drop",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "object",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 3.0
    },
    {
      "NAME": "dropX",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -2.0,
      "MAX": 2.0
    },
    {
      "NAME": "dropY",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -2.0,
      "MAX": 2.0
    },
    {
      "NAME": "spread",
      "TYPE": "float",
      "DEFAULT": 0.3,
      "MIN": 0.0,
      "MAX": 5.0
    },
    {
      "NAME": "dropSize",
      "TYPE": "float",
      "DEFAULT": 0.03,
      "MIN": 0.0,
      "MAX": 0.5
    },
    {
      "NAME": "dropStrength",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 5.0
    },
    {
      "NAME": "fallTime",
      "TYPE": "float",
      "DEFAULT": 0.4,
      "MIN": 0.0,
      "MAX": 5.0
    },
    {
      "NAME": "heartColor",
      "TYPE": "color",
      "DEFAULT": [
        0.84,
        0.16,
        0.22,
        1.0
      ]
    },
    {
      "NAME": "waveSpeed",
      "TYPE": "float",
      "DEFAULT": 0.4,
      "MIN": 0.0,
      "MAX": 5.0
    },
    {
      "NAME": "waveLife",
      "TYPE": "float",
      "DEFAULT": 6.0,
      "MIN": 0.05,
      "MAX": 60.0
    },
    {
      "NAME": "refraction",
      "TYPE": "float",
      "DEFAULT": 0.4,
      "MIN": 0.0,
      "MAX": 5.0
    },
    {
      "NAME": "shading",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 5.0
    },
    {
      "NAME": "glints",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 5.0
    },
    {
      "NAME": "lightColor",
      "TYPE": "color",
      "DEFAULT": [
        0.75,
        0.88,
        1.0,
        1.0
      ]
    },
    {
      "NAME": "lightAngle",
      "TYPE": "float",
      "DEFAULT": 135.0,
      "MIN": 0.0,
      "MAX": 360.0
    }
  ],
  "PASSES": [
    {
      "TARGET": "timeState",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "2",
      "HEIGHT": "1"
    },
    {
      "TARGET": "slots",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "8",
      "HEIGHT": "2"
    },
    {
      "TARGET": "waterB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {
      "TARGET": "waterA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {
      "TARGET": "waterB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {
      "TARGET": "waterA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {
      "TARGET": "waterB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {
      "TARGET": "waterA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {
      "TARGET": "waterB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {
      "TARGET": "waterA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {
      "TARGET": "waterB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {
      "TARGET": "waterA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {
      "TARGET": "waterB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {
      "TARGET": "waterA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {
      "TARGET": "waterB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {
      "TARGET": "waterA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {
      "TARGET": "waterB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {
      "TARGET": "waterA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,300.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,300.0)"
    },
    {}
  ]
}*/

const int NSUB = 16;
const int SLOTS = 8;
const int PASS_TIME = 0, PASS_SLOTS = 1, PASS_SIM0 = 2, PASS_RENDER = 2 + NSUB;
const float IMPACT_TIME = 0.08;     // seconds over which an impact pushes the water down
const float SPLASH_TIME = 0.45;     // seconds the splash crown is visible

const float SIG = 0.5;              // marks buffer contents written by this shader
float safe(float x, float lo, float hi) { return (x == x) ? clamp(x, lo, hi) : clamp(0.0, lo, hi); }   // NaN (also in fresh buffers) becomes 0
float hash3(vec3 p) {
    p = fract(p * vec3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return fract((p.x + p.y) * p.z);
}
void over(inout vec3 col, inout float a, vec3 c, float ca) {
    float na = ca + a * (1.0 - ca);
    if (na > 0.0001) col = (c * ca + col * a * (1.0 - ca)) / na;
    a = na;
}

// ---------- object shapes, in units of the object size; negative inside ----------
float sdHeart(vec2 p) {
    p.y += 0.55;
    p.x = abs(p.x);
    p *= 0.9;
    if (p.y + p.x > 1.0) return (length(p - vec2(0.25, 0.75)) - 0.3535) / 0.9;
    vec2 a = p - vec2(0.0, 1.0);
    float m = 0.5 * max(p.x + p.y, 0.0);
    vec2 b = p - vec2(m);
    return sqrt(min(dot(a, a), dot(b, b))) * sign(p.x - p.y) / 0.9;
}
float sdStone(vec2 p, float seed) {
    float ang = atan(p.y, p.x);
    float r = 0.95 + 0.11 * sin(ang * 2.0 + seed * 6.1) + 0.07 * sin(ang * 3.0 + seed * 2.3) + 0.04 * sin(ang * 5.0 + seed * 9.7);
    return length(p * vec2(1.0, 1.25)) - r;     // a little flat, a little lumpy
}
float shapeSD(int type, vec2 p, float seed) {
    if (type == 1) return sdStone(p, seed);
    if (type == 2) return sdHeart(p);
    return length(p) - 1.0;
}
vec2 mirrorUV(vec2 t) {                       // beyond an edge the picture is mirrored, half a pixel inside: no dark border
    t = 1.0 - abs(1.0 - mod(t, 2.0));
    vec2 inset = 0.5 / max(RENDERSIZE, vec2(1.0));
    return clamp(t, inset, 1.0 - inset);
}
vec2 rot2(vec2 p, float a) { float c = cos(a), s = sin(a); return vec2(c * p.x - s * p.y, s * p.x + c * p.y); }

// ---------- buffers ----------
vec4 slotA(int i) { return IMG_NORM_PIXEL(slots, vec2((float(i) + 0.5) / float(SLOTS), 0.25)); }   // target u, v, time left, fall time
vec4 slotB(int i) { return IMG_NORM_PIXEL(slots, vec2((float(i) + 0.5) / float(SLOTS), 0.75)); }   // type, seed, size, strength
vec2 hv(int src, vec2 ij, vec2 sz) {
    ij = clamp(ij, vec2(0.0), sz - 1.0);         // closed border: waves reflect at the edge of the frame
    vec2 uv = (ij + 0.5) / IMG_SIZE(waterA);
    vec4 t = src == 0 ? IMG_NORM_PIXEL(waterA, uv) : IMG_NORM_PIXEL(waterB, uv);
    return abs(t.b - SIG) < 0.01 ? t.rg : vec2(0.0);   // a fresh buffer may hold garbage: without the signature it counts as calm water
}
float hRender(vec2 uv) { vec4 t = IMG_NORM_PIXEL(waterA, uv); return abs(t.b - SIG) < 0.01 ? safe(t.r, -10.0, 10.0) : 0.0; }

void main() {
    vec2 uv = isf_FragNormCoord;
    float safeScale = clamp(scale, 0.001, 1000.0);
    vec2 res = max(RENDERSIZE, vec2(1.0));
    float aspect = res.x / res.y;
    vec2 half_ = vec2(aspect, 1.0) / safeScale;                // half width and height of the frame in scene units

    // ---------- time and trigger ----------
    if (PASSINDEX == PASS_TIME) {
        vec4 t0 = IMG_NORM_PIXEL(timeState, vec2(0.25, 0.5));
        vec4 t1 = IMG_NORM_PIXEL(timeState, vec2(0.75, 0.5));
        if (!(abs(t0.a - SIG) < 0.01) || !(abs(t1.a - SIG) < 0.01)) { t0 = vec4(0.0, 0.0, 0.0, SIG); t1 = vec4(0.0, 0.0, 0.0, SIG); }
        t0 = vec4(safe(t0.r, -1e6, 1e6), safe(t0.g, 0.0, 0.1), safe(t0.b, 0.0, 3600.0), 1.0);
        t1 = vec4(safe(t1.r, 0.0, 1.0), safe(t1.g, 0.0, 1e7), 0.0, 1.0);
        float now = clockH * 3600.0 + clockM * 60.0 + clockS + clockMs * 0.001;
        float raw = clamp(now - t0.r, 0.0, 0.1);
        float dt = t0.r <= 0.0 ? 1.0 / 60.0 : mix(t0.g, raw, 0.25);
        dt = safe(dt, 0.0, 0.1);
        float d = safe(drop, 0.0, 1.0);
        float trig = (d > 0.5 && t1.r <= 0.5) ? 1.0 : 0.0;
        float count = t1.g + trig;
        gl_FragColor = uv.x < 0.5 ? vec4(now, dt, mod(t0.b + dt, 3600.0), SIG) : vec4(d, count, trig, SIG);
        return;
    }
    vec4 tA = IMG_NORM_PIXEL(timeState, vec2(0.25, 0.5));
    vec4 tB = IMG_NORM_PIXEL(timeState, vec2(0.75, 0.5));
    float dt = tA.g, now = tA.b;

    // ---------- objects in the air: a ring buffer of slots, the oldest is reused ----------
    if (PASSINDEX == PASS_SLOTS) {
        int i = int(floor(uv.x * float(SLOTS)));
        bool rowB = uv.y > 0.5;
        vec4 a = slotA(i), b = slotB(i);
        bool okA = a.a > 0.0 && a.a <= 60.001 && a.b >= -10.001 && a.b <= a.a + 0.001 && abs(a.r) < 1000.0 && abs(a.g) < 1000.0;
        bool okB = b.r >= -0.01 && b.r <= 2.01 && b.g >= 0.0 && b.g <= 1.0 && b.b >= 0.0 && b.b <= 100.0 && b.a >= 0.0 && b.a <= 100.0;
        if (!okA || !okB) { a = vec4(0.5, 0.5, -10.0, 1.0); b = vec4(0.0); }   // garbage in a fresh buffer: an empty slot
        float count = safe(tB.g, 0.0, 1e7);
        if (tB.b > 0.5 && i == int(mod(count - 1.0, float(SLOTS)))) {
            float ang = hash3(vec3(count, 1.3, 3.1)) * 6.2831853;
            float rad = sqrt(hash3(vec3(count, 2.9, 7.7))) * max(spread, 0.0);
            vec2 p = vec2(safe(dropX, -1000.0, 1000.0), safe(dropY, -1000.0, 1000.0)) + vec2(cos(ang), sin(ang)) * rad;
            p = abs(mod(p - 1.0, 4.0) - 2.0) - 1.0;    // the water exists only inside the frame: points beyond an edge fold back in
            float fall = max(safe(fallTime, 0.0, 60.0), 0.0001);
            int obj = int(floor(safe(object, 0.0, 3.0) + 0.5));
            if (obj == 3) obj = int(floor(hash3(vec3(count, 5.5, 1.1)) * 2.999));
            a = vec4(0.5 + 0.5 * p, fall, fall);
            b = vec4(float(obj), hash3(vec3(count, 4.4, 8.8)), safe(dropSize, 0.0, 100.0), safe(dropStrength, 0.0, 100.0));
        } else {
            a.b = max(a.b - dt, -10.0);
            if (count < 0.5) a.b = -10.0;              // nothing has fallen yet
        }
        gl_FragColor = rowB ? b : a;
        return;
    }

    vec2 sz = IMG_SIZE(waterA);
    float cellsPerUnit = 0.5 * sz.y * safeScale;

    // ---------- waves: velocity form of the wave equation with a little dispersion, several steps per frame ----------
    if (PASSINDEX >= PASS_SIM0 && PASSINDEX < PASS_RENDER) {
        int stepIdx = PASSINDEX - PASS_SIM0;
        int src = int(mod(float(stepIdx), 2.0)) == 0 ? 0 : 1;
        vec2 ij = floor(uv * sz);
        float sdt = dt / float(NSUB);
        float c = max(safe(waveSpeed, 0.0, 100.0), 0.0) * cellsPerUnit;
        c = min(c, 0.15 / max(sdt, 1e-5));                     // stability with margin: the waves never come near a cell per step
        vec2 s0 = vec2(safe(hv(src, ij, sz).r, -10.0, 10.0), safe(hv(src, ij, sz).g, -1000.0, 1000.0));
        float n = hv(src, ij + vec2(0, 1), sz).r, so = hv(src, ij - vec2(0, 1), sz).r;
        float e = hv(src, ij + vec2(1, 0), sz).r, w = hv(src, ij - vec2(1, 0), sz).r;
        float ne = hv(src, ij + vec2(1, 1), sz).r, nw = hv(src, ij + vec2(-1, 1), sz).r;
        float se = hv(src, ij + vec2(1, -1), sz).r, sw = hv(src, ij + vec2(-1, -1), sz).r;
        float nn = hv(src, ij + vec2(0, 2), sz).r, ss = hv(src, ij - vec2(0, 2), sz).r;
        float ee = hv(src, ij + vec2(2, 0), sz).r, ww = hv(src, ij - vec2(2, 0), sz).r;
        float h = s0.r, v = s0.g;
        n = safe(n, -10.0, 10.0); so = safe(so, -10.0, 10.0); e = safe(e, -10.0, 10.0); w = safe(w, -10.0, 10.0);
        ne = safe(ne, -10.0, 10.0); nw = safe(nw, -10.0, 10.0); se = safe(se, -10.0, 10.0); sw = safe(sw, -10.0, 10.0);
        nn = safe(nn, -10.0, 10.0); ss = safe(ss, -10.0, 10.0); ee = safe(ee, -10.0, 10.0); ww = safe(ww, -10.0, 10.0);
        float lap = n + so + e + w - 4.0 * h;
        float bilap = 20.0 * h - 8.0 * (n + so + e + w) + 2.0 * (ne + nw + se + sw) + (nn + ss + ee + ww);
        v += sdt * (c * c * lap - c * c * 0.9 * bilap);       // short waves run ahead: the ring train fans out like real water
        float life = max(safe(waveLife, 0.05, 1000.0), 0.05);
        v *= exp(-3.0 * sdt / life);
        h += sdt * v;
        h *= exp(-0.4 * sdt / life);

        // impacts: each object pushes its own footprint into the water over a short moment
        vec2 p = (uv - 0.5) * half_ * 2.0;
        for (int k = 0; k < SLOTS; k++) {
            vec4 a = slotA(k);
            if (a.b > 0.0 || a.b <= -IMPACT_TIME) continue;
            vec4 b = slotB(k);
            float size = b.b / safeScale * safeScale;          // sizes are in height units, already scene units
            size = max(b.b, 0.0005);
            vec2 q = rot2(p - (a.rg - 0.5) * half_ * 2.0, -b.g * 6.2831853 - now * 0.0) / size;
            float sd = shapeSD(int(b.r + 0.5), q, b.g);
            float inner = 1.0 - smoothstep(-0.35, 0.25, sd);
            float rim = exp(-(sd / 0.35) * (sd / 0.35)) * 0.3;
            h += (rim - inner) * b.a * size * 0.6 * sdt / IMPACT_TIME;
        }
        h = safe(h, -10.0, 10.0);
        v = safe(v, -1000.0, 1000.0);
        gl_FragColor = vec4(h, v, SIG, 1.0);
        return;
    }

    // ---------- picture ----------
    vec2 p = (uv - 0.5) * half_ * 2.0;                        // scene units, height is the reference
    vec2 tc = 1.0 / sz;
    float hC = hRender(uv);
    vec2 g = vec2(hRender(uv + vec2(tc.x, 0.0)) - hRender(uv - vec2(tc.x, 0.0)),
                  hRender(uv + vec2(0.0, tc.y)) - hRender(uv - vec2(0.0, tc.y))) * cellsPerUnit * 0.5;
    float lap = (hRender(uv + vec2(tc.x, 0.0)) + hRender(uv - vec2(tc.x, 0.0)) + hRender(uv + vec2(0.0, tc.y)) + hRender(uv - vec2(0.0, tc.y)) - 4.0 * hC) * cellsPerUnit;
    vec2 gs = g * 2.5 / sqrt(1.0 + dot(g, g) * 12.0);                 // steep waves stay readable instead of turning harsh
    float shade = max(safe(shading, 0.0, 100.0), 0.0);

    // the clip below, refracted by the waves
    vec2 off = -gs * max(safe(refraction, 0.0, 100.0), 0.0) * 0.5;
    vec4 src = IMG_NORM_PIXEL(inputImage, mirrorUV(uv + off / (half_ * 2.0)));
    vec3 col = src.rgb;
    float a = src.a;

    float la = radians(safe(lightAngle, -1e5, 1e5));
    vec3 L = normalize(vec3(cos(la), sin(la), 1.3));
    vec3 nrm = normalize(vec3(-gs * shade, 1.0));
    float spec = pow(max(dot(reflect(-L, nrm), vec3(0.0, 0.0, 1.0)), 0.0), 90.0) * max(safe(glints, 0.0, 100.0), 0.0);
    float sky = clamp((1.0 - nrm.z) * 14.0, 0.0, 1.0);
    float focus = clamp(-lap * 0.12 * shade, 0.0, 1.0);
    float diff = (dot(nrm, L) - L.z) * 1.4;
    col *= clamp(1.0 + diff * 0.5, 0.4, 1.6);
    vec3 lc = lightColor.rgb;
    float light = clamp(sky * 0.5 + focus * 0.35, 0.0, 1.0) * lightColor.a;
    over(col, a, lc, light * 0.6);
    over(col, a, mix(vec3(1.0), lc, 0.3), clamp(spec, 0.0, 1.0));

    // falling objects, their shadows, and the splash after an impact
    vec3 lightDir = vec3(cos(la), sin(la), 1.3);
    for (int k = 0; k < SLOTS; k++) {
        vec4 sa = slotA(k);
        if (sa.b <= -SPLASH_TIME) continue;
        vec4 sb = slotB(k);
        int type = int(sb.r + 0.5);
        float size = safe(sb.b, 0.0, 100.0);
        if (size < 1e-4) continue;                                   // too small to see; also keeps the maths away from infinity
        vec2 target = (sa.rg - 0.5) * half_ * 2.0;
        float pxs = 2.0 / (res.y * safeScale);

        if (sa.b > 0.0) {
            float kf = clamp(sa.b / max(sa.a, 1e-4), 0.0, 1.0);     // 1 = just released, 0 = impact
            float height = kf * 1.6;                                // above the water, in scene units
            // shadow on the water: it comes towards the object and meets it at the impact
            vec2 sc = target - normalize(lightDir.xy + 1e-5) * height * 0.45;
            float spin = sb.g * 6.2831853 + now * (type == 0 ? 0.0 : 2.0) * (0.5 + sb.g);
            float sdS = shapeSD(type, rot2(p - sc, -spin) / (size * (1.0 + height * 0.25)), sb.g);
            float shadow = (1.0 - smoothstep(-0.3, 0.6, sdS)) * 0.5 * (1.0 - kf * 0.6);
            col *= 1.0 - shadow * smoothstep(0.0, 0.08, 1.0 - kf);
            // the object itself: closer to the viewer = larger
            float r = size * (1.0 + kf * 3.0);
            float wob = type == 0 ? 1.0 + 0.06 * sin(now * 28.0 + sb.g * 9.0) : 1.0;
            vec2 d = rot2(p - target, -spin) / r;
            d.x /= wob;
            d.y *= wob;
            float sd = shapeSD(type, d, sb.g);
            float cov = 1.0 - smoothstep(-pxs / r, pxs / r, sd);
            float fade = clamp((1.0 - kf) * 8.0 + 0.0, 0.0, 1.0) * clamp(kf * 30.0 + 0.3, 0.0, 1.0);
            fade = clamp((1.0 - kf) * 8.0, 0.0, 1.0);
            if (cov > 0.0) {
                float dome = sqrt(clamp(-sd * 2.0, 0.0, 1.0));      // how far into the shape: a rounded body
                vec2 gradSD = vec2(shapeSD(type, d + vec2(0.01, 0.0), sb.g) - sd, shapeSD(type, d + vec2(0.0, 0.01), sb.g) - sd) / 0.01;
                vec3 sn = normalize(vec3(gradSD * (1.0 - dome) * 1.2, dome + 0.15));
                float lit = max(dot(sn, normalize(lightDir)), 0.0);
                float hi = pow(max(dot(reflect(-normalize(lightDir), sn), vec3(0.0, 0.0, 1.0)), 0.0), 60.0);
                vec3 oc;
                float oa = cov * fade;
                if (type == 0) {
                    // water: a small lens that shows the picture below upside down, with a bright rim and a glint
                    vec2 lensP = target - (p - target) * 0.7;
                    vec3 seen = IMG_NORM_PIXEL(inputImage, mirrorUV(lensP / (half_ * 2.0) + 0.5)).rgb;
                    float rim = smoothstep(0.55, 1.0, length(d));
                    oc = mix(seen * 0.85, lc, rim * 0.35) + vec3(hi * 1.2);
                    oa *= mix(0.75, 1.0, rim);
                } else if (type == 1) {
                    vec3 stone = vec3(0.42, 0.40, 0.37) * (0.85 + 0.3 * hash3(vec3(floor(d * 7.0), sb.g)));
                    oc = stone * (0.35 + 0.85 * lit) + vec3(hi * 0.25);
                } else {
                    oc = heartColor.rgb * (0.35 + 0.85 * lit) + vec3(hi * 0.6);
                    oa *= heartColor.a;
                }
                over(col, a, oc, oa);
            }
        } else {
            // splash: a crown of droplets that flies out and falls back, plus a short rebound in the middle
            float t = -sa.b / SPLASH_TIME;                         // 0 at impact, 1 when gone
            float power = clamp(sb.a, 0.0, 5.0);
            float crown = size * (1.1 + 2.6 * sqrt(t)) * (0.6 + 0.4 * min(power, 2.0));
            vec2 rel = p - target;
            float ang = atan(rel.y, rel.x);
            float cnt = 14.0;
            float cell = floor((ang + 3.14159265) / 6.2831853 * cnt);
            float ca = (cell + 0.5) / cnt * 6.2831853 - 3.14159265 + (hash3(vec3(cell, sb.g, 2.0)) - 0.5) * 0.3;
            float rr = crown * (0.85 + 0.3 * hash3(vec3(cell, sb.g, 5.0)));
            vec2 dc = vec2(cos(ca), sin(ca)) * rr;
            float dr = size * 0.22 * (0.6 + 0.8 * hash3(vec3(cell, sb.g, 9.0))) * (1.0 - t * 0.5);
            float drop_ = 1.0 - smoothstep(dr - pxs, dr + pxs, length(rel - dc));
            float splashA = drop_ * pow(1.0 - t, 1.5) * clamp(power, 0.0, 1.0);
            float jetR = size * 0.35 * sin(clamp(t * 1.6, 0.0, 1.0) * 3.14159265);
            float jet = (1.0 - smoothstep(jetR - pxs, jetR + pxs, length(rel))) * (type == 0 ? 1.0 : 0.6) * clamp(power, 0.0, 1.0);
            over(col, a, mix(vec3(1.0), lc, 0.4), clamp(max(splashA, jet), 0.0, 1.0) * 0.85);
        }
    }

    if (!(col.r == col.r && col.g == col.g && col.b == col.b && a == a)) { col = src.rgb; a = src.a; }   // never pass NaN on
    col = clamp(col, 0.0, 1e4);
    gl_FragColor = a > 0.0001 ? vec4(col, clamp(a, 0.0, 1.0)) : vec4(0.0);
}
