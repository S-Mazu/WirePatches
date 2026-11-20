/*{
  "DESCRIPTION": "Motion Bridge - Fabric Wind: ribbons of fabric blow in the wind, over the floor in front of the projection or flat across the screen. Driven by the Motion Bridge field: the ribbons flow around what stands there and flutter where something moves. The field lies on the floor or flat on the screen, at any edge. Calm without the bridge.",
  "CREDIT": "Stefan Mazur - https://github.com/S-Mazu/WirePatches",
  "ISFVSN": "2",
  "CATEGORIES": [
    "Effect"
  ],
  "INPUTS": [
    { "NAME": "inputImage", "TYPE": "image" },
    { "NAME": "scale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "clock", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1000.0 },
    { "NAME": "ribbonPlane", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "ribbonDensity", "TYPE": "float", "DEFAULT": 5.0, "MIN": 0.0, "MAX": 200.0 },
    { "NAME": "ribbonWidth", "TYPE": "float", "DEFAULT": 0.22, "MIN": 0.0, "MAX": 0.5 },
    { "NAME": "ribbonLength", "TYPE": "float", "DEFAULT": 100.0, "MIN": 0.0, "MAX": 100.0 },
    { "NAME": "ribbonsOpacity", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "fabric", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 5.0 },
    { "NAME": "fabricColor", "TYPE": "color", "DEFAULT": [0.75, 0.12, 0.16, 1.0] },
    { "NAME": "fabricSheen", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "fabricWeight", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "tatter", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "lightSource", "TYPE": "float", "DEFAULT": 135.0, "MIN": 0.0, "MAX": 360.0 },
    { "NAME": "horizonY", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "fieldPlane", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "fieldX", "TYPE": "float", "DEFAULT": 0.5, "MIN": -10.0, "MAX": 10.0 },
    { "NAME": "fieldY", "TYPE": "float", "DEFAULT": 0.5, "MIN": -10.0, "MAX": 10.0 },
    { "NAME": "fieldWidth", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "fieldHeight", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "fieldRotation", "TYPE": "float", "DEFAULT": 0.0, "MIN": -180.0, "MAX": 180.0 },
    { "NAME": "windDirection", "TYPE": "float", "DEFAULT": 270.0, "MIN": 0.0, "MAX": 360.0 },
    { "NAME": "windStrength", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "avoidStrength", "TYPE": "float", "DEFAULT": 1.0, "MIN": -10.0, "MAX": 10.0 },
    { "NAME": "flutterStrength", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "flutterLife", "TYPE": "float", "DEFAULT": 1.5, "MIN": 0.0, "MAX": 30.0 },
    { "NAME": "shadowHold", "TYPE": "float", "DEFAULT": 5.0, "MIN": 0.0, "MAX": 60.0 },
    { "NAME": "showField", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "clockH", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 24.0 },
    { "NAME": "clockM", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 60.0 },
    { "NAME": "clockS", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 60.0 },
    { "NAME": "clockMs", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1000.0 }
  ],
  "PASSES": [
    { "TARGET": "state", "PERSISTENT": true, "FLOAT": true, "WIDTH": "2", "HEIGHT": "1" },
    { "TARGET": "blurX", "FLOAT": true, "WIDTH": "160", "HEIGHT": "120" },
    { "TARGET": "blurXY", "FLOAT": true, "WIDTH": "160", "HEIGHT": "120" },
    { "TARGET": "field", "PERSISTENT": true, "FLOAT": true, "WIDTH": "160", "HEIGHT": "120" },
    { }
  ]
}*/

// The Motion Bridge field (tools/Motion Bridge/README.md, Output): x 0..1 along the front,
// depth 0..1 into the area at 15/16 of the height, R occupancy, G motion, B visibility; the top
// sixteenth is the status band whose counter changes every frame.
//
// Two planes, chosen independently for the ribbons and for the field:
//   Floor:  a view over the floor in front of the projection. A pixel at height y below the
//           horizon H sees the floor at the distance Z = H / (H - y) (1 at the bottom edge).
//           Plane coordinates (X, Z): X = (u - 0.5) * aspect * Z, in output heights.
//   Screen: the frame itself, plane coordinates ((u - 0.5) * aspect, v), in output heights.
// The field is a rectangle in its plane (Field X, Y, Width, Height, Rotation); at rest it fills
// the reference area of the plane with the front at the bottom: on the floor from the bottom
// edge (Z = 1) to Z = FAR, on the screen the whole frame.
//
// Ribbons are strips of fabric anchored at the upwind edge of their plane and blown along the
// Wind Direction. Waves travel along them with Clock; they ripple out of the plane (shading),
// twist and swing sideways. Occupancy pushes them aside (the gradient of the field across the
// ribbon), motion excites a fast flutter that fades over Flutter Life.
//
// state (2 x 1, persistent, signature in alpha):
//   pixel 0: now (s), last counter, time of the last counter change (s)
//   pixel 1: frame time dt (s), real time (s) for the flutter, visibility in use (0/1)
// blurX, blurXY (160 x 120 over x and depth): r occupancy, g motion blurred; b visibility unblurred
// field (160 x 120, persistent): r excitation, g occupancy smoothed, b 1 in a held shadow

const float SIG = 0.67;
const float LSB = 1.0 / 255.0;
const float FAR = 4.0;             // floor distance of the far edge of the field at rest
const float ZFADE = 12.0;          // floor distance where the ribbons have faded out
const float BLUR_X = 0.06;         // blur radius of the field, in field units
const float BLUR_D = 0.06;
const float BAND_Y = 31.0 / 32.0;  // middle of the status band
const float FIELD_H = 15.0 / 16.0; // the field rows in the texture
const float TAU = 6.2831853;

float safe(float v, float lo, float hi) {
    if (!(v == v) || abs(v) > 1e9) return lo;
    return clamp(v, lo, hi);
}

float hash11(float n) { return fract(sin(n * 127.1 + 311.7) * 43758.5453); }
float hash21(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

float vnoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash21(i), b = hash21(i + vec2(1.0, 0.0));
    float c = hash21(i + vec2(0.0, 1.0)), d = hash21(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p) {
    return 0.5 * vnoise(p) + 0.3 * vnoise(p * 2.03 + 7.1) + 0.2 * vnoise(p * 4.01 + 3.3);
}

vec2 rot(vec2 v, float a) {
    float c = cos(a), s = sin(a);
    return vec2(c * v.x - s * v.y, s * v.x + c * v.y);
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

// ---------- the ribbons ----------
// Each ribbon is a strip of fabric fixed at the anchor (t = 0) and blown along the wind.
// Its surface is a height field over (across, along): a few folds run along the strip,
// wander and fan out towards the free end, with soft creases between them; the wind's
// waves travel along it; the strip twists and its edges roll. Normals come from the
// slope of that height field. Light: diffuse, an anisotropic highlight along the threads
// (Ward), a soft sheen at grazing angles (Charlie sheen), darker creases by their depth.

// fabric constants: A = (weight, twist, own tatter, folds across),
// B = (fold depth, highlight width along, highlight width across, highlight strength),
// D = (sheen roughness, sheen strength, diffuse, see-through on the back)
void fabricOf(float m, out vec4 A, out vec4 B, out vec4 D) {
    A = vec4(0.8, 0.45, 0.6, 2.0);  B = vec4(1.0, 0.6, 0.4, 0.05); D = vec4(0.8, 0.5, 0.9, 0.1);   // 0 Rags
    if (m > 0.5 && m < 1.5) { A = vec4(0.45, 0.6, 0.0, 2.5); B = vec4(0.8, 0.45, 0.10, 1.0);  D = vec4(0.35, 0.5, 0.75, 0.6); }  // 1 Silk
    if (m > 1.5 && m < 2.5) { A = vec4(2.5, 0.15, 0.0, 2.0); B = vec4(1.3, 0.6, 0.4, 0.0);    D = vec4(0.45, 1.8, 0.7, 0.0); }   // 2 Velvet
    if (m > 2.5 && m < 3.5) { A = vec4(0.25, 0.9, 0.0, 3.0); B = vec4(0.7, 0.4, 0.15, 0.35);  D = vec4(0.35, 0.5, 0.8, 1.0); }   // 3 Chiffon
    if (m > 3.5 && m < 4.5) { A = vec4(1.3, 0.3, 0.0, 2.0);  B = vec4(0.9, 0.6, 0.4, 0.08);   D = vec4(0.8, 0.6, 0.95, 0.3); }  // 4 Linen
    if (m > 4.5)            { A = vec4(1.0, 0.5, 0.0, 2.5);  B = vec4(0.8, 0.35, 0.12, 1.6);  D = vec4(0.3, 0.3, 0.35, 0.0); }  // 5 Lame
}

// how the wind moves ribbon j at distance t from the anchor:
// lat sideways offset, th twist, a0 wave strength, ph wave phase, sl slope of the waves along
void motionOf(float j, float t, float sp, float S, float ct, float rt, float ex, vec4 A,
              out float lat, out float th, out float a0, out float ph, out float sl) {
    float hj = hash11(j), hk = hash11(j + 71.3);
    float weight = max(A.x * safe(fabricWeight, 0.0, 10.0), 0.05);
    float sw = sqrt(weight);
    float wind = safe(windStrength, 0.0, 10.0);
    float fs = safe(flutterStrength, 0.0, 10.0) * ex;
    float k = TAU * 1.1 / (sw * S);                                   // heavy fabric: long, slow waves
    float v = 0.4 * wind * S / sw;
    ph = k * (t - v * ct) + hj * TAU;
    float env = smoothstep(0.0, 0.6 * S, t);                          // calm at the anchor, freer towards the end
    float gust = 0.4 + 1.2 * vnoise(vec2((t - 1.7 * v * ct) * 0.4 / S, j * 0.37 + 5.0));
    a0 = min(wind, 4.0) / (0.8 + 0.2 * weight) * gust * env;
    sl = 0.8 * a0 * cos(ph) + 0.3 * a0 * cos(2.3 * ph + hk * 4.0)
       + fs * 1.2 * cos(t * 25.0 / S - rt * 21.0 + j);
    lat = sp * 0.3 * a0 * sin(0.5 * ph + hk * 3.0)
        + sp * 0.35 * fs * (sin(t * 9.0 / S - rt * 14.0 + j * 0.9) + 0.5 * sin(t * 17.0 / S - rt * 23.0 - j * 1.3));
    th = A.y * a0 * sin(0.6 * ph + hj * 5.0) + fs * 1.1 * sin(rt * 19.0 - t * 6.0 / S + j * 1.7);
}

// the folds along the strip: rounded ridges, soft creases; u across (-1..1), result 0..1
float foldsOf(float u, float t, float j, float nf, float S, float ct, float wind) {
    float fan = 0.7 + 0.6 * smoothstep(0.0, 3.0 * S, t);             // they fan out towards the free end
    float wander = 0.45 * sin(t * 1.1 / S + j * 2.1 - ct * wind * 0.15) + 0.25 * sin(t * 2.3 / S - j * 1.3);
    float x = (u * 0.5 + 0.5) * nf * fan + wander;
    x += 0.3 * sin(u * 2.3 + j * 4.1 + t * 0.6 / S);                  // folds unevenly spaced
    float r = sin(x * 3.14159265);
    float depth = 0.55 + 0.45 * vnoise(vec2(t * 0.7 / S, j * 1.7 + 3.0));
    return sqrt(r * r + 0.012) * depth;                               // soft absolute value: creases at the zeros
}

// where ribbon j covers the pixel: x coverage, y across (-1..1), z half width as seen
vec3 shapeOf(float j, float cw, float t, float pxc, float pxs, float sp, float hw, float S,
             float tat, float lat, float th) {
    float hk = hash11(j + 71.3);
    float centre = j * sp + lat;
    float off = cw - centre;
    // twist foreshortens the strip, the folds take up some width, the edges wave
    float hwa = hw * (0.85 + 0.15 * hk) * max(abs(cos(th)), 0.06) * 0.92;
    hwa *= 1.0 + 0.05 * sin(t * 5.0 / S + j * 3.0 + sign(off) * 1.7);
    float fray = tat * 0.35 * vnoise(vec2(t * 30.0 / S, j * 3.1 + sign(off) * 7.0));
    float cov = clamp((hwa * (1.0 - fray) - abs(off)) / pxc + 0.5, 0.0, 1.0);
    float a = clamp(off / max(hwa, 1e-6), -1.0, 1.0);
    float L = safe(ribbonLength, 0.0, 1000.0) * S * (1.0 - 0.3 * hk - 0.4 * tat * hk);
    float tEnd = L * (1.0 - tat * 0.25 * vnoise(vec2(a * 2.0, j * 1.9)));
    cov *= clamp((tEnd - t) / pxs + 0.5, 0.0, 1.0) * clamp(t / pxs + 0.5, 0.0, 1.0);
    if (tat > 0.0 && cov > 0.0) {
        float hn = fbm(vec2(t * 6.0 / S, a * 0.8 + j * 4.3));
        float thr = tat * 0.65;
        cov *= smoothstep(thr - 0.04, thr + 0.04, hn);
    }
    return vec3(cov, a, hwa);
}

// how much of ribbon j covers the point (for shadows)
float coverOf(float j, float cw, float t, float soft, float sp, float hw, float S, float ct, float rt,
              float ex, float m) {
    vec4 A, B, D;
    fabricOf(m, A, B, D);
    float lat, th, a0, ph, sl;
    motionOf(j, t, sp, S, ct, rt, ex, A, lat, th, a0, ph, sl);
    float tat = clamp(A.z + safe(tatter, 0.0, 1.0), 0.0, 1.0);
    return shapeOf(j, cw, t, soft, soft, sp, hw, S, tat, lat, th).x;
}

// one ribbon: premultiplied colour at the pixel
//   j lane, cw across coordinate (deflected), t distance from the anchor, pxc/pxs pixel size
//   across/along, ex excitation, sp spacing, hw half width, S scale, ct Clock, rt real time,
//   Lp light direction in (along, across), m fabric
vec4 ribbon(float j, float cw, float t, float pxc, float pxs, float ex, float sp, float hw,
            float S, float ct, float rt, vec2 Lp, float m) {
    vec4 A, B, D;
    fabricOf(m, A, B, D);
    float lat, th, a0, ph, sl;
    motionOf(j, t, sp, S, ct, rt, ex, A, lat, th, a0, ph, sl);
    float tat = clamp(A.z + safe(tatter, 0.0, 1.0), 0.0, 1.0);
    vec3 sh = shapeOf(j, cw, t, pxc, pxs, sp, hw, S, tat, lat, th);
    float cov = sh.x;
    if (cov <= 0.0) return vec4(0.0);
    float a = sh.y, hwa = max(sh.z, 1e-6);
    float wind = safe(windStrength, 0.0, 10.0);

    // height field of the folds (in output heights) and its slope, by small steps
    float fdep = B.x * hw * 0.13;
    float du = 0.03, dt = 0.02 * S;
    float f0 = foldsOf(a, t, j, A.w, S, ct, wind);
    float gu = (foldsOf(a + du, t, j, A.w, S, ct, wind) - foldsOf(a - du, t, j, A.w, S, ct, wind)) / (2.0 * du);
    float gt = (foldsOf(a, t + dt, j, A.w, S, ct, wind) - foldsOf(a, t - dt, j, A.w, S, ct, wind)) / (2.0 * dt);
    // slopes: along = waves + folds; across = folds + rolled edges
    float sAlong = sl + fdep * gt;
    float roll = 0.9 * sign(a) * smoothstep(0.7, 1.0, abs(a));
    float sAcross = fdep * gu / hwa + roll;

    // twist: tilts the whole surface across; edge-on the strip is a thin line, so the
    // switch to the back side happens where it is hardly visible
    float cth = cos(th), sth = sin(th);
    float back = cth < 0.0 ? 1.0 : 0.0;
    float ac = max(abs(cth), 0.02);
    vec3 N = normalize(vec3(-sAlong * ac, -sAcross * ac - sth * sign(cth), ac));
    vec3 V = vec3(0.0, 0.0, 1.0);
    vec3 Lv = normalize(vec3(Lp * 0.75, 0.66));
    vec3 Hv = normalize(Lv + V);
    vec3 T = normalize(vec3(1.0, 0.0, sAlong));                       // threads along the strip
    vec3 Bt = normalize(cross(N, T));
    T = cross(Bt, N);
    float nl = max(dot(N, Lv), 0.0);
    float nv = max(N.z, 0.05);
    float nh = max(dot(N, Hv), 0.05);

    // Ward: a long highlight along the threads, narrow across
    float ht = dot(Hv, T) / B.y, hb = dot(Hv, Bt) / B.z;
    float ward = exp(-(ht * ht + hb * hb) / (nh * nh)) / (12.566 * B.y * B.z * sqrt(max(nl * nv, 0.01)));
    float spec = B.w * min(ward, 8.0) * nl * 0.35;
    // Charlie sheen: soft glow at grazing angles, the look of velvet and of silk's edges
    float sinh_ = sqrt(max(1.0 - nh * nh, 0.0));
    float inv = 1.0 / D.x;
    float charlie = (2.0 + inv) * pow(sinh_, inv) / TAU;
    float sheenT = D.y * charlie * nl / (4.0 * (nl + nv - nl * nv) + 1e-3);
    // creases darker by their depth; wave troughs a little darker
    float ao = mix(0.5, 1.0, smoothstep(0.0, 0.8, f0));
    ao *= 1.0 - 0.25 * clamp(-sin(ph), 0.0, 1.0) * min(a0, 1.0);

    float sheen = safe(fabricSheen, 0.0, 10.0);
    vec3 C = clamp(fabricColor.rgb, 0.0, 1.0);
    float wrap = clamp((dot(N, Lv) + 0.3) / 1.3, 0.0, 1.0);
    vec3 dye = C;
    if (m < 0.5) dye = C * (0.65 + 0.5 * fbm(vec2(t * 2.0 / S, a * 1.5 + j)));       // Rags: uneven, worn
    if (m > 1.5 && m < 2.5) dye = pow(C, vec3(1.4));                                     // Velvet: deep
    vec3 col = dye * (0.2 + D.z * 1.15 * wrap) * ao;
    vec3 specCol = (m > 4.5) ? C * 1.3 + 0.15 : mix(C, vec3(1.0), 0.6);                // Lame: metallic
    col += sheen * (spec * specCol + sheenT * mix(C, vec3(1.0), 0.35)) * mix(1.0, ao, 0.5);

    if (m > 3.5 && m < 4.5) {                                         // Linen: visible weave
        float F = 250.0 / S;
        float weave = 0.5 + 0.5 * sin(t * F * TAU) * sin(a * hwa * F * TAU);
        float amp = 1.0 - smoothstep(0.25, 0.5, max(pxs, pxc) * F);
        col *= 1.0 - 0.2 * weave * amp;
    }
    if (m > 4.5) {                                                    // Lame: glittering threads
        vec2 cell = floor(vec2(t, a * hwa) * 90.0 / S);
        float glit = step(0.97, hash21(cell + floor(rt * 6.0) * vec2(1.3, 7.1)));
        glit *= pow(nh, 3.0) * 2.0 * sheen * smoothstep(1.0, 2.5, S / (90.0 * max(pxs, pxc)));
        col += glit * (C + 0.3);
    }
    // the back side: darker, and thin fabric lets the light through
    if (back > 0.5) col = mix(col * 0.7, dye * (0.35 + 0.6 * wrap), D.w * 0.5);

    float alpha = 1.0;
    if (m > 2.5 && m < 3.5) alpha = clamp(0.3 + 0.45 * (1.0 - nv), 0.0, 1.0);          // Chiffon: sheer
    alpha *= cov;
    return vec4(clamp(col, 0.0, 4.0) * alpha, alpha);
}

void main() {
    vec2 uv = isf_FragNormCoord;

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
        float rt = mod(safe(s1.g, 0.0, 3600.0) + dt, 3600.0);              // real time for the flutter

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
    float rt = safe(s1.g, 0.0, 3600.0);                                // real time: flutter
    float ct = safe(clock, 0.0, 1000.0);                                // Clock: the wind's waves

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
        float life = max(safe(flutterLife, 0.0, 30.0), 0.001);
        float hold = safe(shadowHold, 0.0, 60.0);
        bool shadow = s1.b > 0.5 && cur.b < 0.5 && hold > 0.0 && alive > 0.5;
        if (shadow) {
            // not seen: no new motion, the occupancy fades out slowly instead of dropping to 0
            e = e * exp(-dt / life);
            o = o * exp(-dt / hold);
        } else {
            e = max(clamp(now_.g * 2.5, 0.0, 1.0), e * exp(-dt / life));
            o = mix(o, clamp(now_.r * 1.5, 0.0, 1.0), 1.0 - exp(-dt / 0.15));
        }
        gl_FragColor = vec4(e, o, shadow ? 1.0 : 0.0, 1.0);
        return;
    }

    // ---------- pass 4: the picture ----------
    vec2 res = max(RENDERSIZE, vec2(1.0));
    float aspect = res.x / res.y;
    float S = clamp(scale, 0.001, 1000.0);
    float H = clamp(horizonY * S, 0.001, 1000.0);        // horizon, output heights above the bottom edge
    bool rFloor = safe(ribbonPlane, 0.0, 1.0) < 0.5;
    float m = floor(safe(fabric, 0.0, 5.0) + 0.5);

    float wa = radians(safe(windDirection, -3600.0, 3600.0));
    vec2 w = vec2(cos(wa), sin(wa));                      // where the wind blows, in the ribbon plane
    vec2 n = vec2(-w.y, w.x);                             // across the ribbons
    float D = 0.1 * S;                                    // gradient step, output heights

    vec3 P = toPlane(uv, rFloor, aspect, H);

    // the field at this pixel and beside it, across the ribbons
    vec3 F = toField(uv, aspect, H);
    vec3 Fp = vec3(-1.0, -1.0, 0.0);
    vec3 Fm = vec3(-1.0, -1.0, 0.0);
    if (P.z > 0.5) {
        vec3 up = toFrame(P.xy + n * D, rFloor, aspect, H);
        vec3 um = toFrame(P.xy - n * D, rFloor, aspect, H);
        if (up.z > 0.5) Fp = toField(up.xy, aspect, H);
        if (um.z > 0.5) Fm = toField(um.xy, aspect, H);
    }
    vec4 fc = IMG_NORM_PIXEL(field, clamp(F.xy, 0.0, 1.0));
    vec4 fp = IMG_NORM_PIXEL(field, clamp(Fp.xy, 0.0, 1.0));
    vec4 fm = IMG_NORM_PIXEL(field, clamp(Fm.xy, 0.0, 1.0));
    float ex = F.z > 0.5 ? safe(fc.r, 0.0, 1.0) : 0.0;
    float occ = F.z > 0.5 ? safe(fc.g, 0.0, 1.0) : 0.0;
    float shd = F.z > 0.5 ? safe(fc.b, 0.0, 1.0) : 0.0;
    float grad = (Fp.z > 0.5 ? safe(fp.g, 0.0, 1.0) : 0.0) - (Fm.z > 0.5 ? safe(fm.g, 0.0, 1.0) : 0.0);

    vec4 acc = vec4(0.0);
    float density = safe(ribbonDensity, 0.0, 200.0);
    float width = safe(ribbonWidth, 0.0, 0.5);
    if (P.z > 0.5 && density > 0.0 && width > 0.0) {
        // pixel size in the ribbon plane, across and along the ribbons
        vec3 Px = toPlane(uv + vec2(1.0 / res.x, 0.0), rFloor, aspect, H);
        vec3 Py = toPlane(uv + vec2(0.0, 1.0 / res.y), rFloor, aspect, H);
        float pxc = 1e3, pxs = 1e3;
        if (Px.z > 0.5 && Py.z > 0.5) {
            vec2 dx = Px.xy - P.xy, dy = Py.xy - P.xy;
            pxc = max(length(vec2(dot(dx, n), dot(dy, n))), 1e-6);
            pxs = max(length(vec2(dot(dx, w), dot(dy, w))), 1e-6);
        }

        // the anchor: the most upwind point of the plane's visible area
        vec2 q0, q1, q2, q3;
        if (rFloor) {
            q0 = vec2(-0.5 * aspect, 1.0); q1 = vec2(0.5 * aspect, 1.0);
            q2 = vec2(-0.5 * aspect * ZFADE, ZFADE); q3 = vec2(0.5 * aspect * ZFADE, ZFADE);
        } else {
            q0 = vec2(-0.5 * aspect, 0.0); q1 = vec2(0.5 * aspect, 0.0);
            q2 = vec2(-0.5 * aspect, 1.0); q3 = vec2(0.5 * aspect, 1.0);
        }
        float s0 = min(min(dot(q0, w), dot(q1, w)), min(dot(q2, w), dot(q3, w)));
        float t = dot(P.xy, w) - s0;

        // flow around (or towards, when negative): the ribbons move away from occupancy
        float avoid = safe(avoidStrength, -10.0, 10.0);
        float cw = dot(P.xy, n) + avoid * D * 1.5 * grad;

        float sp = S / density;                           // ribbon spacing, output heights
        float hw = 0.5 * width * S;                       // half width
        float la = radians(safe(lightSource, -3600.0, 3600.0));
        vec2 l2 = vec2(cos(la), sin(la));
        vec2 Lp = vec2(dot(l2, w), dot(l2, n));

        float j1 = floor(cw / sp + 0.5);
        float j0 = j1 - 1.0, j2 = j1 + 1.0;
        vec4 c0 = ribbon(j0, cw, t, pxc, pxs, ex, sp, hw, S, ct, rt, Lp, m);
        vec4 c1 = ribbon(j1, cw, t, pxc, pxs, ex, sp, hw, S, ct, rt, Lp, m);
        vec4 c2 = ribbon(j2, cw, t, pxc, pxs, ex, sp, hw, S, ct, rt, Lp, m);
        // overlapping ribbons: back to front by a random depth per ribbon
        float k0 = hash11(j0 * 1.31 + 9.0), k1 = hash11(j1 * 1.31 + 9.0), k2 = hash11(j2 * 1.31 + 9.0);
        // soft shadows: a ribbon in front darkens the ones behind it, shifted away from the light
        float sd = 0.6 * hw + 0.02 * S;
        float sc = cw + Lp.y * sd, st = t + Lp.x * sd, soft = 0.35 * hw + pxc;
        float o0 = coverOf(j0, sc, st, soft, sp, hw, S, ct, rt, ex, m);
        float o1 = coverOf(j1, sc, st, soft, sp, hw, S, ct, rt, ex, m);
        float o2 = coverOf(j2, sc, st, soft, sp, hw, S, ct, rt, ex, m);
        c0.rgb *= 1.0 - 0.55 * max(k1 > k0 ? o1 : 0.0, k2 > k0 ? o2 : 0.0);
        c1.rgb *= 1.0 - 0.55 * max(k0 > k1 ? o0 : 0.0, k2 > k1 ? o2 : 0.0);
        c2.rgb *= 1.0 - 0.55 * max(k0 > k2 ? o0 : 0.0, k1 > k2 ? o1 : 0.0);
        vec4 tc; float tk;
        if (k0 > k1) { tc = c0; c0 = c1; c1 = tc; tk = k0; k0 = k1; k1 = tk; }
        if (k1 > k2) { tc = c1; c1 = c2; c2 = tc; tk = k1; k1 = k2; k2 = tk; }
        if (k0 > k1) { tc = c0; c0 = c1; c1 = tc; }
        acc = c0;
        acc = c1 + acc * (1.0 - c1.a);
        acc = c2 + acc * (1.0 - c2.a);

        float fade = 1.0;
        if (rFloor) fade *= 1.0 - smoothstep(FAR * 0.9, ZFADE, P.y);   // towards the horizon
        fade *= smoothstep(3.0, 7.0, sp / pxc);                         // spacing in screen pixels
        fade *= 1.0 - 0.7 * smoothstep(0.35, 0.8, occ) * clamp(avoid, 0.0, 1.0);
        acc *= fade * clamp(ribbonsOpacity, 0.0, 1.0) * clamp(fabricColor.a, 0.0, 1.0);
    }

    vec4 outC = vec4(acc.a > 1e-5 ? acc.rgb / acc.a : vec3(0.0), acc.a);

    if (showField > 0.5 && F.z > 0.5) {
        // red occupancy, green excitation, blue a held shadow; white the front, magenta the other edges
        vec3 fcol = vec3(occ, ex, shd * 0.8);
        float fa = max(max(max(occ, ex), shd * 0.5) * 0.8, 0.12);
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

    outC.rgb = clamp(outC.rgb, 0.0, 1.0);
    outC.a = clamp(outC.a, 0.0, 1.0);
    if (!(outC.r == outC.r) || !(outC.a == outC.a)) outC = vec4(0.0);
    gl_FragColor = outC;
}
