/*{
  "DESCRIPTION": "Fire Edges - flames and smoke grow from the frame edges, on top of the clip below. Real-time fluid simulation. Follows the surface model: height is the reference, Scale multiplies all sizes.",
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
      "NAME": "speed",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "rise",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 5.0
    },
    {
      "NAME": "windAngle",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 360.0
    },
    {
      "NAME": "windStrength",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "swirl",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 5.0
    },
    {
      "NAME": "fireAmount",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "flameLength",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "burst",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "flameCore",
      "TYPE": "color",
      "DEFAULT": [
        1.0,
        0.93,
        0.68,
        1.0
      ]
    },
    {
      "NAME": "flameMid",
      "TYPE": "color",
      "DEFAULT": [
        1.0,
        0.48,
        0.06,
        1.0
      ]
    },
    {
      "NAME": "flameTip",
      "TYPE": "color",
      "DEFAULT": [
        0.62,
        0.07,
        0.02,
        1.0
      ]
    },
    {
      "NAME": "smokeAmount",
      "TYPE": "float",
      "DEFAULT": 0.5,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "smokeLife",
      "TYPE": "float",
      "DEFAULT": 4.0,
      "MIN": 0.1,
      "MAX": 120.0
    },
    {
      "NAME": "smokeColor",
      "TYPE": "color",
      "DEFAULT": [
        0.42,
        0.41,
        0.4,
        1.0
      ]
    },
    {
      "NAME": "bottomOn",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "topOn",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "leftOn",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "rightOn",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "edgeFrom",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "edgeTo",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 1.0
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
      "TARGET": "curlBuf",
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "advected",
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "divBuf",
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "pB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "state",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT",
      "HEIGHT": "min($HEIGHT,540.0)"
    },
    {
      "TARGET": "blurA",
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT/4.0",
      "HEIGHT": "min($HEIGHT,540.0)/4.0"
    },
    {
      "TARGET": "blurB",
      "FLOAT": true,
      "WIDTH": "$WIDTH*min($HEIGHT,540.0)/$HEIGHT/16.0",
      "HEIGHT": "min($HEIGHT,540.0)/16.0"
    },
    {}
  ]
}*/

const int JACOBI = 16;

const float PI2 = 6.2831853;
const int PASS_TIME = 0, PASS_CURL = 1, PASS_ADVECT = 2, PASS_DIV = 3, PASS_P0 = 4;
const int PASS_PROJECT = 4 + JACOBI, PASS_BLUR_A = 5 + JACOBI, PASS_BLUR_B = 6 + JACOBI, PASS_RENDER = 7 + JACOBI;

float hash3(vec3 p) {
    p = fract(p * vec3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return fract((p.x + p.y) * p.z);
}
float vnoise(vec3 p) {
    vec3 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = mix(mix(hash3(i), hash3(i + vec3(1, 0, 0)), f.x), mix(hash3(i + vec3(0, 1, 0)), hash3(i + vec3(1, 1, 0)), f.x), f.y);
    float b = mix(mix(hash3(i + vec3(0, 0, 1)), hash3(i + vec3(1, 0, 1)), f.x), mix(hash3(i + vec3(0, 1, 1)), hash3(i + vec3(1, 1, 1)), f.x), f.y);
    return mix(a, b, f.z);
}
float fbm(vec3 p) { return 0.6 * vnoise(p) + 0.3 * vnoise(p * 2.03 + 11.0) + 0.1 * vnoise(p * 4.1 + 23.0); }
float safe(float x, float lo, float hi) { return (x == x) ? clamp(x, lo, hi) : 0.0; }

// the simulation fades to nothing outside the frame, so wind brings in clean air
vec4 stateAt(vec2 ij, vec2 sz) {
    if (ij.x < 0.0 || ij.y < 0.0 || ij.x >= sz.x || ij.y >= sz.y) return vec4(0.0);
    return IMG_NORM_PIXEL(state, (ij + 0.5) / sz);
}
vec4 advectedAt(vec2 ij, vec2 sz) {
    if (ij.x < 0.0 || ij.y < 0.0 || ij.x >= sz.x || ij.y >= sz.y) return vec4(0.0);
    return IMG_NORM_PIXEL(advected, (ij + 0.5) / sz);
}
float smokeLog(vec2 ij, vec2 sz) {
    ij = clamp(ij, vec2(0.0), sz - 1.0);
    return log(1.0 + max(IMG_NORM_PIXEL(state, (ij + 0.5) / sz).g, 0.0));
}
float smokeNear(vec2 ij, vec2 sz) {
    ij = clamp(ij, vec2(0.0), sz - 1.0);
    return IMG_NORM_PIXEL(state, (ij + 0.5) / sz).g;
}
float curlAt(vec2 ij, vec2 sz) {
    ij = clamp(ij, vec2(0.0), sz - 1.0);
    return IMG_NORM_PIXEL(curlBuf, (ij + 0.5) / sz).r;
}
float divAt(vec2 ij, vec2 sz) { return IMG_NORM_PIXEL(divBuf, (ij + 0.5) / sz).r; }
float pAt(int source, vec2 ij, vec2 sz) {
    // open boundary: pressure 0 outside, so gas can leave the frame
    if (ij.x < 0.0 || ij.y < 0.0 || ij.x >= sz.x || ij.y >= sz.y) return 0.0;
    return source == 0 ? IMG_NORM_PIXEL(pA, (ij + 0.5) / sz).r : IMG_NORM_PIXEL(pB, (ij + 0.5) / sz).r;
}

// Catmull-Rom sampling keeps smoke and flames sharp while they move
vec4 cubicState(vec2 px, vec2 sz) {
    vec2 p = px - 0.5, i = floor(p), f = p - i;
    vec2 f2 = f * f, f3 = f2 * f;
    vec2 w0 = -0.5 * f3 + f2 - 0.5 * f;
    vec2 w1 = 1.5 * f3 - 2.5 * f2 + 1.0;
    vec2 w2 = -1.5 * f3 + 2.0 * f2 + 0.5 * f;
    vec2 w3 = 0.5 * f3 - 0.5 * f2;
    vec4 sum = vec4(0.0);
    vec4 lo = vec4(1e9), hi = vec4(-1e9);
    for (int y = 0; y < 4; y++) {
        float wy = y == 0 ? w0.y : y == 1 ? w1.y : y == 2 ? w2.y : w3.y;
        for (int x = 0; x < 4; x++) {
            float wx = x == 0 ? w0.x : x == 1 ? w1.x : x == 2 ? w2.x : w3.x;
            vec4 s = stateAt(i + vec2(float(x) - 1.0, float(y) - 1.0), sz);
            sum += s * wx * wy;
            if (x == 1 || x == 2) if (y == 1 || y == 2) { lo = min(lo, s); hi = max(hi, s); }
        }
    }
    return clamp(sum, lo, hi);        // no ringing
}


// burning band along each active edge
void edgeBand(float dist, float along, vec2 inward, float band, inout float cover, inout vec2 push) {
    if (along < min(edgeFrom, edgeTo) || along > max(edgeFrom, edgeTo)) return;
    float k = 1.0 - smoothstep(band * 0.55, band, dist);
    if (k <= 0.0) return;
    cover = max(cover, k);
    push += inward * k;
}

void burnArea(vec2 p, vec2 uv, vec2 halfExt, float t, inout float cover, inout vec2 push) {
    float band = 0.035 + 0.05 * min(fireAmount, 6.0);
    if (bottomOn > 0.5) edgeBand(p.y + halfExt.y, uv.x, vec2(0.0, 1.0), band, cover, push);
    if (topOn > 0.5) edgeBand(halfExt.y - p.y, uv.x, vec2(0.0, -1.0), band, cover, push);
    if (leftOn > 0.5) edgeBand(p.x + halfExt.x, uv.y, vec2(1.0, 0.0), band, cover, push);
    if (rightOn > 0.5) edgeBand(halfExt.x - p.x, uv.y, vec2(-1.0, 0.0), band, cover, push);
}


void main() {
    vec2 uv = isf_FragNormCoord;

    // ----- clock: real time per frame, so the speed does not depend on the frame rate -----
    if (PASSINDEX == PASS_TIME) {
        vec4 t0 = IMG_NORM_PIXEL(timeState, vec2(0.25, 0.5));
        vec4 t1 = IMG_NORM_PIXEL(timeState, vec2(0.75, 0.5));
        float now = clockH * 3600.0 + clockM * 60.0 + clockS + clockMs * 0.001;
        float raw = clamp(now - t0.r, 0.0, 0.05);
        float dt = t0.r <= 0.0 ? 1.0 / 60.0 : mix(t0.g, raw, 0.25);   // softens the millisecond steps of the system clock
        dt = safe(dt, 0.0, 0.05);
        // below 1 this is slow motion; above 1 the time step stays the same, so the fluid cannot break up
        float spd = max(speed, 0.0);
        float simDt = dt * min(spd, 1.0);
        if (uv.x < 0.5) {
            gl_FragColor = vec4(now, dt, mod(t0.b + dt * spd, 3600.0), simDt);
        } else {
            float wa = radians(windAngle);
            vec2 w = vec2(cos(wa), sin(wa)) * max(windStrength, 0.0) * simDt;
            gl_FragColor = vec4(mod(t1.rg + w, 4000.0), 0.0, 1.0);
        }
        return;
    }

    vec4 tA = IMG_NORM_PIXEL(timeState, vec2(0.25, 0.5));
    vec4 tB = IMG_NORM_PIXEL(timeState, vec2(0.75, 0.5));
    float simT = tA.b, dt = tA.a;
    vec2 windOffset = tB.rg;
    float safeScale = clamp(scale, 0.001, 1000.0);
    vec2 res = max(RENDERSIZE, vec2(1.0));
    vec2 sz = IMG_SIZE(state);
    float cellsPerUnit = 0.5 * sz.y * safeScale;

    // ----- rotation of the flow, for the small eddies -----
    if (PASSINDEX == PASS_CURL) {
        vec2 ij = floor(uv * sz);
        vec2 l = stateAt(ij - vec2(1, 0), sz).ba, r = stateAt(ij + vec2(1, 0), sz).ba;
        vec2 d = stateAt(ij - vec2(0, 1), sz).ba, u = stateAt(ij + vec2(0, 1), sz).ba;
        float w = 0.5 * ((r.y - l.y) - (u.x - d.x)) * cellsPerUnit;
        gl_FragColor = vec4(safe(w, -5000.0, 5000.0), 0.0, 0.0, 1.0);
        return;
    }

    // ----- move everything, apply forces, burn -----
    if (PASSINDEX == PASS_ADVECT) {
        vec2 px = (floor(uv * sz) + 0.5);
        vec2 ij = floor(px);
        float wa = radians(windAngle);
        vec2 wind = vec2(cos(wa), sin(wa)) * max(windStrength, 0.0);
        vec4 here = stateAt(ij, sz);
        // wind is carried in the transport: it moves all smoke and flames together, not one stream of it
        vec2 back = px - (here.ba + wind) * dt * cellsPerUnit;
        vec4 s = cubicState(back, sz);
        float heat = max(s.r, 0.0), smk = max(s.g, 0.0);
        vec2 v = s.ba;
        // smoke spreads softly into the air around it
        float around = 0.25 * (smokeNear(ij + vec2(1, 0), sz) + smokeNear(ij - vec2(1, 0), sz) + smokeNear(ij + vec2(0, 1), sz) + smokeNear(ij - vec2(0, 1), sz));
        smk = mix(smk, around, clamp(dt * 9.0, 0.0, 0.4));

        vec2 p = (uv - 0.5) * res / (0.5 * res.y * safeScale);   // scene units, origin in the frame centre
        vec2 halfExt = 0.5 * res / (0.5 * res.y * safeScale);

        // buoyancy: hot gas rises; smoke carries a little warmth
        // Speed above 1 makes the flow stronger instead of the time step larger
        float flow = 1.0 + max(speed - 1.0, 0.0) * 0.3;
        float up = max(rise, 0.0) * flow;
        v.y += dt * up * (2.1 * min(heat, 3.0) + 0.1 * min(smk, 3.0));

        // eddies that keep the smoke alive
        float wC = curlAt(ij, sz);
        vec2 eta = 0.5 * vec2(abs(curlAt(ij + vec2(1, 0), sz)) - abs(curlAt(ij - vec2(1, 0), sz)),
                              abs(curlAt(ij + vec2(0, 1), sz)) - abs(curlAt(ij - vec2(0, 1), sz)));
        eta = eta / (length(eta) + 1e-4);
        v += dt * 0.05 * vec2(eta.y, -eta.x) * clamp(wC, -120.0, 120.0);   // fixed and limited: stronger values tear the fluid into pixels
        float swirlAmt = max(swirl, 0.0);
        swirlAmt = (swirlAmt <= 1.0 ? swirlAmt : 1.0 + (swirlAmt - 1.0) * 0.5) * flow;

        // large rolling turbulence; it travels with the wind
        vec2 q = p - windOffset;
        float e = 0.02;
        vec2 curl = vec2(0.0);
        for (int o = 0; o < 2; o++) {
            float sc = o == 0 ? 2.2 : 8.0;
            float tt = simT * (o == 0 ? 0.5 : 1.3);
            float n1 = vnoise(vec3((q + vec2(0.0, e)) * sc, tt)), n2 = vnoise(vec3((q - vec2(0.0, e)) * sc, tt));
            float n3 = vnoise(vec3((q + vec2(e, 0.0)) * sc, tt)), n4 = vnoise(vec3((q - vec2(e, 0.0)) * sc, tt));
            // Swirl adds to the large eddies; the fine ones stay at their normal strength
            curl += vec2(n1 - n2, n4 - n3) / (2.0 * e * sc) * (o == 0 ? 0.55 : 1.0 / max(swirlAmt, 1.0));
        }
        v += dt * 8.0 * swirlAmt * curl * clamp(heat * 1.0 + smk * 0.45 + 0.05, 0.0, 1.1);
        v *= exp(-dt * 1.1);

        // cooling: hot gas loses heat fast (fourth power), cool gas slowly. Burnt-out gas becomes smoke.
        float len = max(flameLength, 0.02);
        float rate = 3.2 / len;
        float h0 = heat;
        float c3 = 1.0 + 3.0 * dt * rate * heat * heat * heat;
        heat = heat / max(pow(c3, 1.0 / 3.0), 1.0);
        heat *= exp(-dt * rate * 0.75);
        float smokeAmt = max(smokeAmount, 0.0);
        smk += max(h0 - heat, 0.0) * smokeAmt * 0.7;
        smk *= exp(-dt / (max(smokeLife, 0.05) * (1.0 + max(smokeAmt - 2.0, 0.0) * 0.7)));

        // sources: each patch has its own burning area
        float cover = 0.0;
        vec2 push = vec2(0.0);
        burnArea(p, uv, halfExt, simT, cover, push);

        float fire = max(fireAmount, 0.0);
        float amp = 1.0 + 2.0 * clamp(burst, 0.0, 1.0);
        // uneven flicker that moves up through the source
        float fl = 0.2 + 1.6 * fbm(vec3(p.x * 4.5, p.y * 4.5 - simT * 3.0, simT * 1.4));
        float target = cover * fl * min(fire, 1.4) * amp * 1.05;
        if (fire > 0.0) heat = max(heat, mix(heat, target, 1.0 - exp(-dt * 32.0)));
        v += dt * push * cover * (1.6 + 2.2 * fire) * amp;
        v.y += dt * cover * up * (0.3 + fl) * min(fire, 2.0) * amp;
        smk += dt * cover * smokeAmt * (fire > 0.05 ? 0.35 : 1.2) * amp;

        // above 1 fire feeds itself: flames that exist keep burning and spread. Nothing ignites in clean air.
        if (fire > 1.0) {
            // flames that exist are pulled towards a rising pattern of tongues and holes, so they never become a solid sheet
            float n = fbm(vec3(p.x * 4.6, p.y * 1.7 - simT * 2.8, simT * 0.9));
            float a = clamp((fire - 1.0) / 9.0, 0.0, 1.0);
            float wanted = smoothstep(0.34, 0.72, n + a * 0.08) * 1.3;
            float alive = smoothstep(0.06, 0.2, heat);
            heat = mix(heat, max(heat * 0.92, wanted), alive * (1.0 - exp(-dt * (1.5 + 13.0 * a))));
        }
        // dense smoke stays longer and collects under the top of the frame, so a fog builds up from the source
        if (smokeAmt > 2.0) {
            float ceiling = smoothstep(halfExt.y * 0.35, halfExt.y, p.y) * clamp((smokeAmt - 2.0) * 0.25, 0.0, 1.0);
            v.y -= max(v.y, 0.0) * ceiling * clamp(dt * 7.0, 0.0, 1.0);
        }

        v = clamp(v, vec2(-30.0), vec2(30.0));
        gl_FragColor = vec4(safe(heat, 0.0, 1.35), safe(smk, 0.0, 40.0), safe(v.x, -30.0, 30.0), safe(v.y, -30.0, 30.0));
        return;
    }

    // ----- pressure: makes the gas flow around and push itself, like a real fluid -----
    if (PASSINDEX == PASS_DIV) {
        vec2 ij = floor(uv * sz);
        float d = 0.5 * (advectedAt(ij + vec2(1, 0), sz).b - advectedAt(ij - vec2(1, 0), sz).b
                       + advectedAt(ij + vec2(0, 1), sz).a - advectedAt(ij - vec2(0, 1), sz).a);
        gl_FragColor = vec4(safe(d, -500.0, 500.0), 0.0, 0.0, 1.0);
        return;
    }
    if (PASSINDEX >= PASS_P0 && PASSINDEX < PASS_PROJECT) {
        vec2 ij = floor(uv * sz);
        int src = mod(float(PASSINDEX - PASS_P0), 2.0) < 0.5 ? 1 : 0;
        float p = 0.25 * (pAt(src, ij + vec2(1, 0), sz) + pAt(src, ij - vec2(1, 0), sz)
                        + pAt(src, ij + vec2(0, 1), sz) + pAt(src, ij - vec2(0, 1), sz) - divAt(ij, sz));
        gl_FragColor = vec4(safe(p, -5000.0, 5000.0), 0.0, 0.0, 1.0);
        return;
    }
    if (PASSINDEX == PASS_PROJECT) {
        vec2 ij = floor(uv * sz);
        vec4 a = advectedAt(ij, sz);
        vec2 g = 0.5 * vec2(pAt(1, ij + vec2(1, 0), sz) - pAt(1, ij - vec2(1, 0), sz),
                            pAt(1, ij + vec2(0, 1), sz) - pAt(1, ij - vec2(0, 1), sz));
        vec2 v = a.ba - g;
        gl_FragColor = vec4(a.r, a.g, safe(v.x, -30.0, 30.0), safe(v.y, -30.0, 30.0));
        return;
    }

    // ----- soft copies of the smoke, in log density, used only for its shading -----
    if (PASSINDEX == PASS_BLUR_A) {
        vec2 c = uv * sz;
        float sum = smokeLog(floor(c), sz);
        for (int i = 0; i < 12; i++) {
            float a = float(i) * 0.5236;
            float r = mod(float(i), 2.0) < 0.5 ? 2.5 : 5.0;
            sum += smokeLog(floor(c + vec2(cos(a), sin(a)) * r), sz);
        }
        gl_FragColor = vec4(sum / 13.0, 0.0, 0.0, 1.0);
        return;
    }
    if (PASSINDEX == PASS_BLUR_B) {
        vec2 ts = 1.0 / max(IMG_SIZE(blurA), vec2(1.0));
        float sum = IMG_NORM_PIXEL(blurA, uv).r;
        for (int i = 0; i < 12; i++) {
            float a = float(i) * 0.5236;
            float r = mod(float(i), 2.0) < 0.5 ? 2.5 : 5.0;
            sum += IMG_NORM_PIXEL(blurA, clamp(uv + vec2(cos(a), sin(a)) * r * ts, vec2(0.0), vec2(1.0))).r;
        }
        gl_FragColor = vec4(sum / 13.0, 0.0, 0.0, 1.0);
        return;
    }

    // ----- picture -----
    vec2 q = uv * sz;
    vec4 st = cubicState(q, sz);
    float scenePerPixel = 1.0 / (0.5 * res.y * safeScale);

    // fine tongues on top of the simulation: a small flow-following shift, only inside the flames
    vec2 pn = (uv - 0.5) * res * scenePerPixel;
    float fineT = simT * 1.5;
    vec2 wob = vec2(vnoise(vec3(pn * 5.5 - vec2(0.0, simT * 3.0), fineT)), vnoise(vec3(pn * 5.5 + 7.3 - vec2(0.0, simT * 3.0), fineT))) - 0.5;
    vec4 stF = cubicState(q + wob * 2.4 * min(st.r * 2.0, 1.0), sz);
    float heat = stF.r;
    float smk = st.g;

    vec3 fc = mix(flameTip.rgb, flameMid.rgb, smoothstep(0.18, 0.62, heat));
    fc = mix(fc, flameCore.rgb, smoothstep(0.75, 1.5, heat));
    float fa = smoothstep(0.16, 0.6, heat);

    // glow of the flames in the surrounding smoke
    float glow = 0.0;
    for (int i = 0; i < 6; i++) {
        float a = float(i) * 1.0472 + 0.4;
        glow += stateAt(floor(q + vec2(cos(a), sin(a)) * 14.0), sz).r;
    }
    glow = min(glow / 6.0, 1.5);

    // smoke: soft body, shaded by where the light comes from. Uses the log of the density so dense smoke keeps its form.
    float sa = 1.0 - exp(-smk * 1.15);
    float kc = sz.y / 540.0;
    vec2 ld = normalize(vec2(0.35, 1.0));
    vec2 tc = 1.0 / sz;
    vec2 d1 = ld * 12.0 * kc * tc, d2 = ld * 45.0 * kc * tc;
    float relief = (IMG_NORM_PIXEL(blurA, clamp(uv - d1, vec2(0.0), vec2(1.0))).r - IMG_NORM_PIXEL(blurA, clamp(uv + d1, vec2(0.0), vec2(1.0))).r) * 0.8
                 + (IMG_NORM_PIXEL(blurB, clamp(uv - d2, vec2(0.0), vec2(1.0))).r - IMG_NORM_PIXEL(blurB, clamp(uv + d2, vec2(0.0), vec2(1.0))).r) * 0.7;
    float occl = IMG_NORM_PIXEL(blurA, clamp(uv + ld * 28.0 * kc * tc, vec2(0.0), vec2(1.0))).r + 1.5 * IMG_NORM_PIXEL(blurB, clamp(uv + ld * 90.0 * kc * tc, vec2(0.0), vec2(1.0))).r;
    float shade = exp(-occl * 0.3);
    float lit = clamp((0.62 + 0.5 * shade) * (1.0 + clamp(relief, -1.0, 1.0) * 0.5), 0.34, 1.35);
    vec3 sc = smokeColor.rgb * lit + flameMid.rgb * (glow * 0.55 + min(heat, 1.0) * 0.2);

    // composite: clip, smoke over it, flames over that, plus light
    vec4 src = IMG_NORM_PIXEL(inputImage, uv);
    vec3 P = src.rgb * src.a;
    float A = src.a;
    P = P * (1.0 - sa) + sc * sa;
    A = A * (1.0 - sa) + sa;
    P = P * (1.0 - fa) + fc * fa;
    A = A * (1.0 - fa) + fa;
    P += fc * smoothstep(0.5, 1.35, heat) * 0.18;
    P += flameMid.rgb * glow * 0.08 * (1.0 - sa);

    gl_FragColor = A > 0.0001 ? vec4(max(P, 0.0) / A, clamp(A, 0.0, 1.0)) : vec4(0.0);
}
