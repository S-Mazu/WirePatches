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
    { "TARGET": "raw", "PERSISTENT": true, "FLOAT": true, "WIDTH": "$WIDTH/2", "HEIGHT": "$HEIGHT/2" },
    { "TARGET": "env", "PERSISTENT": true, "FLOAT": true, "WIDTH": "$WIDTH/2", "HEIGHT": "$HEIGHT/2" },
    { "TARGET": "pushT", "PERSISTENT": true, "FLOAT": true, "WIDTH": "$WIDTH/2", "HEIGHT": "$HEIGHT/2" },
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
// twist and swing sideways. They part around what stands there like a stream around a post
// (pushT), motion excites a fast flutter that fades over Flutter Life.
//
// state (2 x 1, persistent, signature in alpha):
//   pixel 0: now (s), last counter, time of the last counter change (s)
//   pixel 1: frame time dt (s), real time (s) for the flutter, visibility in use (0/1)
// blurX, blurXY (160 x 120 over x and depth): r occupancy, g motion blurred; b visibility unblurred
// field (160 x 120, persistent): r excitation, g occupancy smoothed, b 1 in a held shadow
// raw (output / 2): one column per ribbon, rows along the wind: r the half width of the object
//   the ribbon meets in this row (x Avoid Strength, plus half a ribbon), g its centre relative to
//   the ribbon
// env (output / 2): the same columns and rows, rows along the wind: the half width of
//   the object the ribbon meets there, spread along the ribbon as a wake behind it (r, centre
//   relative to the ribbon in g) and ahead of it (b, centre in a); its size carries the aspect
// pushT (output / 2, persistent), the same columns and rows:
//   r sideways offset of the ribbon there (output heights, smoothed over time), g excitation,
//   b unused;

const float SIG = 0.67;
const float LSB = 1.0 / 255.0;
const float FAR = 4.0;             // floor distance of the far edge of the field at rest
const float ZFADE = 12.0;          // floor distance where the ribbons have faded out
const float BLUR_X = 0.06;         // blur radius of the field, in field units
const float BLUR_D = 0.06;
const float BAND_Y = 31.0 / 32.0;  // middle of the status band
const float FIELD_H = 15.0 / 16.0; // the field rows in the texture
const float HATCH_H = 0.013;         // Show Field: period of the hatching where nothing is evaluated, output heights
const vec2 FIELD_RES = vec2(160.0, 120.0);   // size of the field pass
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
// wander and fan out towards the free end, with soft creases and small wrinkles; the wind's
// waves travel along it; the strip twists and its edges roll. A fine weave perturbs the
// normal where it is larger than a pixel and turns into roughness where it is smaller.
//
// Light is physically based and computed in linear colour, then tone mapped (ACES) in main():
//   - silk and lame: anisotropic GGX with height-correlated visibility and Schlick Fresnel
//   - rags, velvet, chiffon, linen: the cloth model (Charlie sheen, Neubelt visibility,
//     wrapped diffuse tinted by a subsurface colour)
//   (formulas after Filament, google.github.io/filament, sections 4.10 and 4.12)
//   - three lights: the key light from Light Source, a hemisphere of ambient light, and a
//     light behind the fabric that shines through thin fabric
//   - a soft reflection of the key light (a large softbox) for the glossy fabrics

const float PI = 3.14159265;

// fabric constants:
// A = (weight, twist, own tatter, folds across)
// B = (fold depth, roughness, anisotropy, highlight strength)
// D = (sheen roughness, sheen strength, diffuse, light through the fabric)
// E = (thread spacing in output heights, weave strength, metal 0/1, model: 0 cloth, 1 glossy)
void fabricOf(float m, out vec4 A, out vec4 B, out vec4 D, out vec4 E) {
    A = vec4(0.8, 0.45, 0.6, 2.0);  B = vec4(1.0, 0.9, 0.0, 0.0); D = vec4(0.85, 0.5, 1.0, 0.15); E = vec4(0.0025, 0.6, 0.0, 0.0);   // 0 Rags
    if (m > 0.5 && m < 1.5) { A = vec4(0.45, 0.6, 0.0, 2.5); B = vec4(0.8, 0.38, 0.6, 1.0);  D = vec4(0.35, 0.15, 0.6, 0.6); E = vec4(0.0015, 0.0, 0.0, 1.0); }   // 1 Silk
    if (m > 1.5 && m < 2.5) { A = vec4(2.5, 0.15, 0.0, 2.0); B = vec4(1.3, 0.9, 0.0, 0.0);   D = vec4(0.45, 1.0, 0.75, 0.0); E = vec4(0.0, 0.0, 0.0, 0.0); }    // 2 Velvet
    if (m > 2.5 && m < 3.5) { A = vec4(0.25, 0.9, 0.0, 3.0); B = vec4(0.7, 0.5, 0.0, 0.0);   D = vec4(0.4, 0.5, 0.95, 1.0); E = vec4(0.0015, 0.3, 0.0, 0.0); }   // 3 Chiffon
    if (m > 3.5 && m < 4.5) { A = vec4(1.3, 0.3, 0.0, 2.0);  B = vec4(0.9, 0.9, 0.0, 0.0);   D = vec4(0.8, 0.35, 1.0, 0.3); E = vec4(0.002, 0.8, 0.0, 0.0); }    // 4 Linen
    if (m > 4.5)            { A = vec4(1.0, 0.5, 0.0, 2.5);  B = vec4(0.8, 0.3, 0.5, 1.0);   D = vec4(0.3, 0.0, 0.0, 0.0); E = vec4(0.002, 0.4, 1.0, 1.0); }     // 5 Lame
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

// the folds along the strip: rounded ridges, soft creases, small wrinkles; u across (-1..1), result about 0..1
float foldsOf(float u, float t, float j, float nf, float S, float ct, float wind) {
    float fan = 0.7 + 0.6 * smoothstep(0.0, 3.0 * S, t);             // they fan out towards the free end
    float wander = 0.45 * sin(t * 1.1 / S + j * 2.1 - ct * wind * 0.15) + 0.25 * sin(t * 2.3 / S - j * 1.3);
    float x = (u * 0.5 + 0.5) * nf * fan + wander;
    x += 0.3 * sin(u * 2.3 + j * 4.1 + t * 0.6 / S);                  // folds unevenly spaced
    float r = sin(x * PI);
    float depth = 0.55 + 0.45 * vnoise(vec2(t * 0.7 / S, j * 1.7 + 3.0));
    float main_ = sqrt(r * r + 0.012) * depth;                        // soft absolute value: creases at the zeros
    // wrinkles: finer, shallower, coming and going along the strip
    float r2 = sin(x * PI * 2.7 + t * 3.1 / S + j);
    float wr = sqrt(r2 * r2 + 0.02) * 0.22 * smoothstep(0.35, 0.8, vnoise(vec2(t * 1.5 / S, j * 2.3 + 9.0)));
    return main_ + wr;
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

// how much of ribbon j covers the point (for shadows and occlusion)
float coverOf(float j, float cw, float t, float soft, float sp, float hw, float S, float ct, float rt,
              float ex, float push, float m) {
    vec4 A, B, D, E;
    fabricOf(m, A, B, D, E);
    float lat, th, a0, ph, sl;
    motionOf(j, t, sp, S, ct, rt, ex, A, lat, th, a0, ph, sl);
    lat += push;
    float tat = clamp(A.z + safe(tatter, 0.0, 1.0), 0.0, 1.0);
    return shapeOf(j, cw, t, soft, soft, sp, hw, S, tat, lat, th).x;
}

// sky light around the ribbon: brighter towards the key light, darker below
vec3 ambientOf(vec3 dir, vec3 up) {
    return mix(vec3(0.015), vec3(0.16), 0.5 + 0.5 * dot(dir, up));
}

// one ribbon: premultiplied linear colour at the pixel
//   j lane, cw across coordinate, t distance from the anchor, pxc/pxs pixel size
//   across/along, ex excitation and push sideways offset of this ribbon, sp spacing, hw half width, S scale, ct Clock, rt real time,
//   Lp light direction in (along, across), m fabric
vec4 ribbon(float j, float cw, float t, float pxc, float pxs, float ex, float push, float sp, float hw,
            float S, float ct, float rt, vec2 Lp, float m) {
    vec4 A, B, D, E;
    fabricOf(m, A, B, D, E);
    float lat, th, a0, ph, sl;
    motionOf(j, t, sp, S, ct, rt, ex, A, lat, th, a0, ph, sl);
    lat += push;                                                      // the whole strip moves around what stands there
    float tat = clamp(A.z + safe(tatter, 0.0, 1.0), 0.0, 1.0);
    vec3 sh = shapeOf(j, cw, t, pxc, pxs, sp, hw, S, tat, lat, th);
    float cov = sh.x;
    if (cov <= 0.0) return vec4(0.0);
    float a = sh.y, hwa = max(sh.z, 1e-6);
    float wind = safe(windStrength, 0.0, 10.0);

    // height field of the folds (in output heights) and its slope, by small steps
    // folds squeezed into a few pixels (a strip seen edge-on, far away) fade out instead of flickering
    float foldPx = hwa / max(A.w, 1.0) / pxc;
    float foldVis = smoothstep(1.5, 6.0, foldPx);
    float fdep = B.x * hw * 0.13 * foldVis;
    float du = 0.03, dt = 0.02 * S;
    float f0 = foldsOf(a, t, j, A.w, S, ct, wind);
    float gu = (foldsOf(a + du, t, j, A.w, S, ct, wind) - foldsOf(a - du, t, j, A.w, S, ct, wind)) / (2.0 * du);
    float gt = (foldsOf(a, t + dt, j, A.w, S, ct, wind) - foldsOf(a, t - dt, j, A.w, S, ct, wind)) / (2.0 * dt);
    float sAlong = sl + fdep * gt;
    float roll = 0.9 * sign(a) * smoothstep(0.7, 1.0, abs(a));       // rolled edges
    float sAcross = fdep * gu / hwa + roll;

    // the weave: a plain weave bump; finer than a pixel it becomes roughness (specular anti-aliasing)
    float pitch = max(E.x * S, 1e-6);
    float perPx = max(pxs, pxc) / pitch;                              // threads per pixel
    float wvis = (E.x > 0.0) ? 1.0 - smoothstep(0.12, 0.35, perPx) : 0.0;
    float wu = t / pitch, wa = a * hwa / pitch;
    sAlong += E.y * wvis * 0.6 * cos(wu * TAU) * sin(wa * TAU);
    sAcross += E.y * wvis * 0.6 * sin(wu * TAU) * cos(wa * TAU);
    // folds finer than a few pixels also turn into roughness
    float roughAA = E.y * (1.0 - wvis) * 0.25 + 0.35 * (1.0 - smoothstep(2.0, 10.0, foldPx));

    // twist: tilts the whole surface across; edge-on the strip is a thin line, so the
    // switch to the back side happens where it is hardly visible
    float cth = cos(th), sth = sin(th);
    float back = cth < 0.0 ? 1.0 : 0.0;
    float ac = max(abs(cth), 0.02);
    vec3 N = normalize(vec3(-sAlong * ac, -sAcross * ac - sth * sign(cth), ac));
    vec3 V = vec3(0.0, 0.0, 1.0);
    vec3 Lk = normalize(vec3(Lp * 0.75, 0.66));                       // key light
    vec3 Lb = normalize(vec3(-Lp * 0.5, -0.6));                       // light behind the fabric
    vec3 up = normalize(vec3(Lp * 0.6, 0.8));
    vec3 Hk = normalize(Lk + V);
    vec3 T = normalize(vec3(1.0, 0.0, sAlong));                       // threads along the strip
    vec3 Bt = normalize(cross(N, T));
    T = cross(Bt, N);
    float nl = max(dot(N, Lk), 0.0);
    float nv = max(N.z, 1e-3);
    float nh = max(dot(N, Hk), 0.0);
    float vh = max(dot(V, Hk), 0.0);

    float sheenK = safe(fabricSheen, 0.0, 10.0);
    vec3 C = pow(clamp(fabricColor.rgb, 0.0, 1.0), vec3(2.2));        // linear
    vec3 dye = C;
    if (m < 0.5) dye = C * (0.6 + 0.6 * fbm(vec2(t * 2.0 / S, a * 1.5 + j)));        // Rags: uneven, worn
    if (m > 1.5 && m < 2.5) dye = C * C * 1.2;                                           // Velvet: deep
    if (back > 0.5) dye *= 0.8;
    // creases darker by their depth (occlusion of the sky light)
    float ao = mix(0.2, 1.0, mix(0.6, smoothstep(0.0, 0.8, f0), foldVis));
    ao *= 1.0 - 0.25 * clamp(-sin(ph), 0.0, 1.0) * min(a0, 1.0);

    float KEY = 1.25;                                                  // key light, linear
    vec3 col;
    if (E.w > 0.5) {
        // glossy (silk, lame): anisotropic GGX, height-correlated Smith, Schlick Fresnel
        float metal = E.z;
        vec3 F0 = mix(vec3(0.04), C, metal);
        float pr = clamp(B.y + roughAA, 0.089, 1.0);
        float ar = pr * pr;
        float at = max(ar * (1.0 + B.z), 0.001), ab = max(ar * (1.0 - B.z), 0.001);
        float ToH = dot(T, Hk), BoH = dot(Bt, Hk);
        float a2 = at * ab;
        vec3 vv = vec3(ab * ToH, at * BoH, a2 * nh);
        float w2 = a2 / max(dot(vv, vv), 1e-7);
        float Dg = a2 * w2 * w2 / PI;
        float lV = nl * length(vec3(at * dot(T, V), ab * dot(Bt, V), nv));
        float lL = nv * length(vec3(at * dot(T, Lk), ab * dot(Bt, Lk), nl));
        float Vg = 0.5 / max(lV + lL, 1e-5);
        float fc = pow(1.0 - vh, 5.0);
        vec3 F = F0 + (1.0 - F0) * fc;
        vec3 spec = min(Dg * Vg, 60.0) * F * B.w;
        vec3 diff = (1.0 - metal) * dye * D.z / PI;
        col = (diff + spec * sheenK) * nl * KEY * PI;
        // reflection of the soft key light and the sky
        vec3 R = reflect(-V, N);
        float soft = smoothstep(0.55 + 0.4 * pr, 0.98, dot(R, Lk));
        vec3 Fr = F0 + (1.0 - F0) * pow(1.0 - nv, 5.0);
        vec3 env = (ambientOf(R, up) + soft * KEY * 0.6) * (1.0 - pr) * (1.0 - pr);
        col += env * Fr * B.w * sheenK * mix(1.0, ao, 0.5);
        col += diff * PI * ambientOf(N, up) * ao;
        col += D.y * sheenK * 0.15 * pow(1.0 - nv, 3.0) * KEY * mix(C, vec3(1.0), 0.4) * nl;   // a little sheen at the edges
    } else {
        // cloth: Charlie sheen, Neubelt visibility, wrapped diffuse with a subsurface tint
        float ra = clamp(D.x + roughAA * 0.5, 0.1, 1.0);
        float invA = 1.0 / ra;
        float s2 = max(1.0 - nh * nh, 0.0078125);
        float Dc = (2.0 + invA) * pow(s2, invA * 0.5) / (2.0 * PI);
        float Vc = 1.0 / (4.0 * (nl + nv - nl * nv) + 1e-4);
        vec3 sheenCol = (m > 1.5 && m < 2.5) ? mix(C, vec3(1.0), 0.45) * 0.9 : vec3(0.04) + sqrt(C) * 0.25;
        vec3 Fr = min(Dc * Vc, 4.0) * sheenCol * D.y * sheenK;
        vec3 sss = dye * 0.5;                                         // light scattered inside the threads
        float wrap = clamp((dot(N, Lk) + 0.5) / 2.25, 0.0, 1.0);
        vec3 Fd = dye * D.z / PI * wrap * clamp(sss + nl, 0.0, 1.0);
        col = (Fd + Fr * nl) * KEY * PI;
        col += dye * D.z * ambientOf(N, up) * ao;
        col += sheenCol * D.y * sheenK * 0.3 * pow(1.0 - nv, 2.0) * ambientOf(N, up) * 2.0;
    }
    // light through thin fabric: from the light behind, tinted by the dye
    col += D.w * dye * sqrt(dye) * max(dot(-N, Lb) + 0.25, 0.0) * 1.2 * (0.6 + 0.4 * back);

    if (m > 4.5) {                                                    // Lame: glittering threads
        vec2 cell = floor(vec2(t, a * hwa) * 90.0 / S);
        float glit = step(0.97, hash21(cell + floor(rt * 6.0) * vec2(1.3, 7.1)));
        glit *= pow(nh, 3.0) * 6.0 * sheenK * smoothstep(1.0, 2.5, S / (90.0 * max(pxs, pxc)));
        col += glit * (C + 0.2);
    }

    float alpha = 1.0;
    if (m > 2.5 && m < 3.5) alpha = clamp(0.3 + 0.45 * (1.0 - nv), 0.0, 1.0);          // Chiffon: sheer
    alpha *= cov;
    return vec4(clamp(col, 0.0, 64.0) * alpha, alpha);
}

// reads the processed field at a point of the ribbon plane: x occupancy, y excitation (0 outside).
// Interpolated by hand (Wire reads float textures unfiltered). A macro, because Wire's ISF reads
// images only directly in main().
#define FIELD_AT(PT, OUT) { vec3 fr_ = toFrame(PT, rFloor, aspect, H); vec3 ff_ = vec3(-1.0, -1.0, 0.0); if (fr_.z > 0.5) ff_ = toField(fr_.xy, aspect, H); OUT = vec2(0.0); if (ff_.z > 0.5) { vec2 fp_ = clamp(ff_.xy, 0.0, 1.0) * FIELD_RES - 0.5; vec2 fi_ = floor(fp_); vec2 fq_ = fp_ - fi_; vec2 f0_ = (clamp(fi_, vec2(0.0), FIELD_RES - 1.0) + 0.5) / FIELD_RES; vec2 f1_ = (clamp(fi_ + 1.0, vec2(0.0), FIELD_RES - 1.0) + 0.5) / FIELD_RES; vec4 fv_ = mix(mix(IMG_NORM_PIXEL(field, f0_), IMG_NORM_PIXEL(field, vec2(f1_.x, f0_.y)), fq_.x), mix(IMG_NORM_PIXEL(field, vec2(f0_.x, f1_.y)), IMG_NORM_PIXEL(field, f1_), fq_.x), fq_.y); OUT = vec2(safe(fv_.g, 0.0, 1.0), safe(fv_.r, 0.0, 1.0)); } }

// the visible part of the ribbon plane, in ribbon coordinates: (across min, max, along min, max)
vec4 planeBox(bool onFloor, float aspect, vec2 w, vec2 n) {
    vec2 q0, q1, q2, q3;
    if (onFloor) {
        q0 = vec2(-0.5 * aspect, 1.0); q1 = vec2(0.5 * aspect, 1.0);
        q2 = vec2(-0.5 * aspect * ZFADE, ZFADE); q3 = vec2(0.5 * aspect * ZFADE, ZFADE);
    } else {
        q0 = vec2(-0.5 * aspect, 0.0); q1 = vec2(0.5 * aspect, 0.0);
        q2 = vec2(-0.5 * aspect, 1.0); q3 = vec2(0.5 * aspect, 1.0);
    }
    vec4 c = vec4(dot(q0, n), dot(q1, n), dot(q2, n), dot(q3, n));
    vec4 a = vec4(dot(q0, w), dot(q1, w), dot(q2, w), dot(q3, w));
    return vec4(min(min(c.x, c.y), min(c.z, c.w)), max(max(c.x, c.y), max(c.z, c.w)),
                min(min(a.x, a.y), min(a.z, a.w)), max(max(a.x, a.y), max(a.z, a.w)));
}

// the ribbon in the first column of pushT; centred on the plane when there are more ribbons
float tableJ0(vec4 box, float sp, float cols) {
    float lo = floor(box.x / sp) - 7.0, hi = ceil(box.y / sp) + 7.0;
    if (hi - lo + 1.0 > cols) lo = floor(0.5 * (box.x + box.y) / sp) - floor(0.5 * cols);
    return lo;
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

    // ---------- passes 4 to 6: how far each ribbon moves aside, along its length ----------
    // Like a stream around a post: the occupancy across the ribbon gives an object's centre and
    // half width R; the ribbon at the offset y0 from that centre moves, as a whole strip, to the
    // offset a streamline has around a cylinder, sqrt(y0^2 + R^2). The ribbons part around the
    // object and crowd at its sides; far away they hardly move. Negative Avoid Strength draws
    // them in instead.
    if (PASSINDEX >= 4 && PASSINDEX <= 6) {
        // rows and columns only through texture coordinates: in Wire, gl_FragCoord.y runs the
        // other way than the texture's y
        vec2 TS = max(IMG_SIZE(pushT), vec2(1.0));        // raw, env and pushT have this size
        float aspect = TS.x / TS.y;                       // this pass is (width / 2) x (height / 2)
        float S = clamp(scale, 0.001, 1000.0);
        float H = clamp(horizonY * S, 0.001, 1000.0);
        bool rFloor = safe(ribbonPlane, 0.0, 1.0) < 0.5;
        float wa = radians(safe(windDirection, -3600.0, 3600.0));
        vec2 w = vec2(cos(wa), sin(wa));
        vec2 n = vec2(-w.y, w.x);
        float density = safe(ribbonDensity, 0.0, 200.0);
        float avoid = safe(avoidStrength, -10.0, 10.0);
        float sp = S / max(density, 1e-3);
        vec4 box = planeBox(rFloor, aspect, w, n);
        float jj = tableJ0(box, sp, TS.x) + floor(uv.x * TS.x);
        float jb = jj * sp;
        float al = box.z + uv.y * (box.w - box.z);
        float dl = (box.w - box.z) / TS.y;                // one row, along the ribbon
        bool live = density > 0.0 && jj <= ceil(box.y / sp) + 7.0;

        if (PASSINDEX == 4) {
            // the object here, across the ribbon (9 points, about one output height)
            if (!live) { gl_FragColor = vec4(0.0); return; }
            float stp = 0.12 * S;
            vec2 ga;
            float lm = 0.0, lc = 0.0, gl = 0.0, gr = 0.0;
            for (int k = 0; k < 9; k++) {
                float o = (float(k) - 4.0) * stp;
                FIELD_AT(n * (jb + o) + w * al, ga)
                float g = smoothstep(0.15, 0.5, ga.x);        // faint traces are no object
                lm += g; lc += g * o;
                if (k == 0) gl = g;
                if (k == 8) gr = g;
            }
            // occupied at both ends of the window: wider than a post (a wall, a crowd); the
            // ribbon cannot go round it and stays where it is
            float R0 = 0.5 * lm * stp * (1.0 - smoothstep(0.3, 0.8, min(gl, gr)));
            // the ribbon's edge, not its middle, clears the object: half a ribbon more, faded in
            // with the object itself (before the spread, so the cone carries it evenly)
            float hwp = 0.5 * safe(ribbonWidth, 0.0, 0.5) * S;
            R0 += hwp * smoothstep(0.0, 0.5 * stp, R0);
            // Avoid Strength scales the width here, before the spread, so the bend before and
            // the wake behind grow with it and the ribbons keep their angle
            R0 = min(R0 * min(abs(avoid), 10.0), 3.0 * S);
            float c0 = lm > 1e-3 ? lc / lm : 0.0;
            gl_FragColor = vec4(R0, c0, 0.0, 0.0);
            return;
        }

        if (PASSINDEX == 5) {
            if (!live) { gl_FragColor = vec4(0.0); return; }
            // Spread along the ribbon, as a cone: behind the object (the wake, r and g) the width
            // shrinks to 0 over about 1.4 x itself, before it (b and a, where the ribbon starts to
            // bend) over about 1 x. Directly from the raw width of the rows up- and downwind (128
            // each, covering the longest possible cone), so it has no memory: no feedback, nothing
            // lingers when the object goes.
            float row = floor(uv.y * TS.y);
            float st = max(ceil(min(TS.y, 4.3 * S / max(dl, 1e-6)) / 128.0), 1.0);   // rows per sample
            float Ru = 0.0, su = 0.0, cu = 0.0, Rd = 0.0, sd = 0.0, cd = 0.0;
            for (int k = 0; k < 128; k++) {
                float fk = float(k) * st;
                if (row - fk >= 0.0) {                        // upwind: this point is in its wake
                    vec4 q = IMG_NORM_PIXEL(raw, vec2(uv.x, (row - fk + 0.5) / TS.y));
                    float r = max(safe(q.r, 0.0, 100.0) - 0.7 * fk * dl, 0.0);
                    Ru = max(Ru, r); su += r * r; cu += r * r * safe(q.g, -100.0, 100.0);
                }
                if (row + fk < TS.y) {                        // downwind: the object ahead
                    vec4 q = IMG_NORM_PIXEL(raw, vec2(uv.x, (row + fk + 0.5) / TS.y));
                    float r = max(safe(q.r, 0.0, 100.0) - 1.0 * fk * dl, 0.0);
                    Rd = max(Rd, r); sd += r * r; cd += r * r * safe(q.g, -100.0, 100.0);
                }
            }
            gl_FragColor = vec4(Ru, su > 1e-20 ? cu / su : 0.0, Rd, sd > 1e-20 ? cd / sd : 0.0);
            return;
        }


        // pass 6: the offset from the envelope
        vec4 prev = IMG_NORM_PIXEL(pushT, uv);
        if (abs(prev.a - SIG) > 0.01) prev = vec4(0.0);
        float push = 0.0, e = 0.0, side = 0.0;
        if (live) {
            // the width and the centre smoothed along the ribbon (rounds the tip of the cone; the
            // centre is weighted by the width, so where the width ends it does not jump)
            float ms = clamp(floor(0.02 * S / max(dl, 1e-6) + 0.5), 1.0, 16.0) / TS.y;
            float R = 0.0, rw = 0.0, cs = 0.0, cw_ = 0.0;
            for (int k = -4; k <= 4; k++) {
                float wk = 5.0 - abs(float(k));
                vec4 q = IMG_NORM_PIXEL(env, vec2(uv.x, clamp(uv.y + float(k) * ms, 0.0, 1.0)));
                float ru = safe(q.r, 0.0, 100.0), rd = safe(q.b, 0.0, 100.0);
                float rq = max(ru, rd);
                R += wk * rq; rw += wk;
                cs += wk * (ru * safe(q.g, -100.0, 100.0) + rd * safe(q.a, -100.0, 100.0)); cw_ += wk * (ru + rd);
            }
            R /= rw;
            float ca = jb + (cw_ > 1e-12 ? cs / cw_ : 0.0);
            // motion where the ribbon is now makes it flutter (read below, once the offset is known)
            float Re = R;                                      // already x Avoid Strength
            float y0 = jb - ca;
            float yy;
            if (avoid >= 0.0) {
                // the side the ribbon passes on: for its whole length the side of the centre it is
                // on where the object is widest (sampled over its column), so it does not switch
                // sides along its length; a ribbon right at the centre goes to a random side
                float vote = 0.0;
                for (int v = 0; v < 32; v++) {
                    vec4 q = IMG_NORM_PIXEL(env, vec2(uv.x, (float(v) + 0.5) / 32.0));
                    float ru = safe(q.r, 0.0, 100.0), rd = safe(q.b, 0.0, 100.0);
                    float cv = (ru * safe(q.g, -100.0, 100.0) + rd * safe(q.a, -100.0, 100.0)) / max(ru + rd, 1e-12);
                    float rv = max(ru, rd);
                    vote += (cv > 0.0 ? -1.0 : 1.0) * rv * rv;  // the ribbon is on the side opposite to c
                }
                float sn = y0 < 0.0 ? -1.0 : 1.0;
                float sv = abs(vote) > 1e-6 ? (vote < 0.0 ? -1.0 : 1.0) : (hash11(jj * 7.13 + 3.0) < 0.5 ? -1.0 : 1.0);
                float rr = sqrt(y0 * y0 + Re * Re);
                yy = sn * rr;
                if (sv != sn) {
                    // here on the other side (the centre shifts a little along the ribbon): it
                    // crosses over gradually while the width grows, at a shallow angle; well
                    // outside the object it keeps its own side
                    float yc = mix(y0, sv * rr, smoothstep(0.0, 1.0, Re / (8.0 * abs(y0) + 0.05 * sp)));
                    yy = mix(yc, yy, smoothstep(0.5 * Re, 0.9 * Re, abs(y0)));
                }
            } else {
                yy = (y0 < 0.0 ? -1.0 : 1.0) * max(2.0 * abs(y0) - sqrt(y0 * y0 + Re * Re), 0.0);
            }
            push = clamp(ca + yy - jb, -3.0 * S, 3.0 * S);
            vec2 ga;
            for (int k = -2; k <= 2; k++) {
                FIELD_AT(n * (jb + push + float(k) * 0.1 * S) + w * al, ga)
                e = max(e, ga.y * (1.0 - 0.3 * abs(float(k))));
            }
        }
        // fabric has inertia: the ribbon swings over instead of jumping
        push = mix(safe(prev.r, -100.0, 100.0), push, 1.0 - exp(-dt / 0.15));
        gl_FragColor = vec4(push, e, side, SIG);
        return;
    }

    // ---------- pass 7: the picture ----------
    vec2 res = max(RENDERSIZE, vec2(1.0));
    float aspect = res.x / res.y;
    float S = clamp(scale, 0.001, 1000.0);
    float H = clamp(horizonY * S, 0.001, 1000.0);        // horizon, output heights above the bottom edge
    bool rFloor = safe(ribbonPlane, 0.0, 1.0) < 0.5;
    float m = floor(safe(fabric, 0.0, 5.0) + 0.5);

    float wa = radians(safe(windDirection, -3600.0, 3600.0));
    vec2 w = vec2(cos(wa), sin(wa));                      // where the wind blows, in the ribbon plane
    vec2 n = vec2(-w.y, w.x);                             // across the ribbons

    vec3 P = toPlane(uv, rFloor, aspect, H);

    // the field at this pixel (Show Field)
    vec3 F = toField(uv, aspect, H);
    vec4 fc = IMG_NORM_PIXEL(field, clamp(F.xy, 0.0, 1.0));
    float ex = F.z > 0.5 ? safe(fc.r, 0.0, 1.0) : 0.0;
    float occ = F.z > 0.5 ? safe(fc.g, 0.0, 1.0) : 0.0;

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

        float avoid = safe(avoidStrength, -10.0, 10.0);
        float cw = dot(P.xy, n);
        float sp = S / density;                           // ribbon spacing, output heights
        float hw = 0.5 * width * S;                       // half width
        float la = radians(safe(lightSource, -3600.0, 3600.0));
        vec2 l2 = vec2(cos(la), sin(la));
        vec2 Lp = vec2(dot(l2, w), dot(l2, n));

        // the ribbon whose centre (pushed aside, with its sway) is nearest to this pixel, from
        // pushT, and three on each side: beside an object the ribbons crowd and overlap
        vec2 TSz = max(IMG_SIZE(pushT), vec2(1.0));
        float aspT = TSz.x / TSz.y;                       // the same aspect as the pass that wrote it
        vec4 box = planeBox(rFloor, aspT, w, n);
        float jt0 = tableJ0(box, sp, TSz.x);
        float ty = clamp((dot(P.xy, w) - box.z) / max(box.w - box.z, 1e-6), 0.0, 1.0);
        float jc = floor(cw / sp + 0.5);
        float tyd = 1.0 / TSz.y;                           // smooth along the ribbon: a tent over +-4 rows
// pushT is read without filtering: interpolate between rows by hand, smoothed along the ribbon
#define PUSH_ROW(CX, TY, OUT) { float ry_ = clamp(TY, 0.0, 1.0) * TSz.y - 0.5; float r0_ = clamp(floor(ry_), 0.0, TSz.y - 1.0); float r1_ = min(r0_ + 1.0, TSz.y - 1.0); OUT = mix(IMG_NORM_PIXEL(pushT, vec2(CX, (r0_ + 0.5) / TSz.y)).rg, IMG_NORM_PIXEL(pushT, vec2(CX, (r1_ + 0.5) / TSz.y)).rg, clamp(ry_ - r0_, 0.0, 1.0)); }
#define PUSH_AT(COL, OUT) { OUT = vec2(0.0); if (COL >= 0.0 && COL < TSz.x) { float cx_ = (COL + 0.5) / TSz.x; vec2 pr_; float ws_ = 0.0; for (int k_ = -4; k_ <= 4; k_++) { float wk_ = 5.0 - abs(float(k_)); PUSH_ROW(cx_, ty + float(k_) * tyd, pr_) OUT += wk_ * pr_; ws_ += wk_; } OUT /= ws_; } }
#define PUSH_ONE(COL, OUT) { OUT = vec2(0.0); if (COL >= 0.0 && COL < TSz.x) PUSH_ROW((COL + 0.5) / TSz.x, ty, OUT) }
        vec4 fA, fB, fD, fE;
        fabricOf(m, fA, fB, fD, fE);
        // which ribbon lands here: the pushed positions j x spacing + offset rise with j, so a
        // binary search over all ribbons that can reach this point (up to 3 x Scale away) finds
        // it; then the nearest one, with its sway, among its neighbours
        float sLo = jc - min(ceil(3.0 * S / sp) + 2.0, 4096.0), sHi = jc + min(ceil(3.0 * S / sp) + 2.0, 4096.0);
        for (int b = 0; b < 14; b++) {
            float mid = floor(0.5 * (sLo + sHi));
            vec2 v;
            PUSH_ONE(mid - jt0, v)
            if (mid * sp + safe(v.x, -100.0, 100.0) < cw) sLo = mid; else sHi = mid;
        }
        float jb0 = sLo;
        float jn = jb0, bd = 1e9;
        for (int k = -6; k <= 6; k++) {
            float jj = jb0 + float(k);
            float col = jj - jt0;
            vec2 v;
            PUSH_ONE(col, v)
            v = vec2(safe(v.x, -100.0, 100.0), safe(v.y, 0.0, 1.0));
            float mlat, mth, ma0, mph, msl;
            motionOf(jj, t, sp, S, ct, rt, v.y, fA, mlat, mth, ma0, mph, msl);
            float d = abs(jj * sp + v.x + mlat - cw);
            if (d < bd) { bd = d; jn = jj; }
        }

        float lj[7];
        float lp[7];
        float le[7];
        for (int l = 0; l < 7; l++) {
            float jj = jn + float(l) - 3.0;
            float col = jj - jt0;
            vec2 v;
            PUSH_AT(col, v)
            lj[l] = jj;
            lp[l] = safe(v.x, -100.0, 100.0);
            le[l] = safe(v.y, 0.0, 1.0);
        }

        // each ribbon, its random depth, how much it covers the shadow point and its surroundings
        float sd = 0.6 * hw + 0.02 * S;
        float sc = cw + Lp.y * sd, st = t + Lp.x * sd, soft = 0.35 * hw + pxc;
        float wide = 0.9 * hw + pxc;
        vec4 lc[7];
        float lk[7];
        float lo[7];
        float lz[7];
        for (int l = 0; l < 7; l++) {
            lc[l] = ribbon(lj[l], cw, t, pxc, pxs, le[l], lp[l], sp, hw, S, ct, rt, Lp, m);
            lk[l] = hash11(lj[l] * 1.31 + 9.0);
            lo[l] = coverOf(lj[l], sc, st, soft, sp, hw, S, ct, rt, le[l], lp[l], m);   // soft shadow
            lz[l] = coverOf(lj[l], cw, t, wide, sp, hw, S, ct, rt, le[l], lp[l], m);    // ambient occlusion
        }
        // a ribbon in front darkens the ones behind it (shadow shifted away from the light), and
        // right next to it less ambient light reaches them; then back to front by depth
        for (int l = 0; l < 7; l++) {
            float sh = 0.0, oc = 0.0;
            for (int q = 0; q < 7; q++) {
                if (lk[q] > lk[l]) { sh = max(sh, lo[q]); oc = max(oc, lz[q]); }
            }
            lc[l].rgb *= (1.0 - 0.6 * sh) * (1.0 - 0.3 * oc);
        }
        for (int r = 0; r < 7; r++) {
            for (int l = 0; l < 7; l++) {
                float rank = 0.0;
                for (int q = 0; q < 7; q++) rank += lk[q] < lk[l] ? 1.0 : 0.0;
                if (abs(rank - float(r)) < 0.5) acc = lc[l] + acc * (1.0 - lc[l].a);
            }
        }

        float fade = 1.0;
        if (rFloor) fade *= 1.0 - smoothstep(FAR * 0.9, ZFADE, P.y);   // towards the horizon
        fade *= smoothstep(3.0, 7.0, sp / pxc);                         // spacing in screen pixels
        acc *= fade * clamp(ribbonsOpacity, 0.0, 1.0) * clamp(fabricColor.a, 0.0, 1.0);
    }

    // linear light -> film-like tone mapping (ACES, Narkowicz fit) -> display
    vec3 lin = acc.a > 1e-5 ? max(acc.rgb / acc.a, vec3(0.0)) : vec3(0.0);
    vec3 tm = clamp((lin * (2.51 * lin + 0.03)) / (lin * (2.43 * lin + 0.59) + 0.14), 0.0, 1.0);
    vec4 outC = vec4(pow(tm, vec3(1.0 / 2.2)), acc.a);

    // Show Field, outside the field and where no sensor sees: hatched, nothing can excite the ribbons
    // there (outside the area, in a shadow). Live visibility from the texture; without a sensor
    // (visibility not in use) only the outside of the field is hatched.
    bool seenNow = true;
    if (F.z > 0.5 && safe(s1.b, 0.0, 1.0) > 0.5 && alive > 0.5)
        seenNow = IMG_NORM_PIXEL(inputImage, vec2(clamp(F.x, 0.0, 1.0), clamp(F.y, 0.0, 1.0) * FIELD_H)).b > 0.5;
    if (showField > 0.5 && (F.z < 0.5 || !seenNow)) {
        float hp = mod((gl_FragCoord.x + gl_FragCoord.y) / max(HATCH_H * res.y, 2.0), 1.0);
        float line = 1.0 - smoothstep(0.18, 0.26, abs(hp - 0.5));          // a soft diagonal stripe
        float ha = 0.8;
        vec3 hcol = mix(vec3(0.0), vec3(0.28), line);
        outC.rgb = mix(outC.rgb, hcol, ha);
        outC.a = max(outC.a, ha);
    }

    if (showField > 0.5 && F.z > 0.5) {
        // seen cells brightened, red occupancy, green excitation; in the hatched part only what is held
        // there (red, green); white the base line, magenta the other edges
        vec3 fcol = seenNow ? mix(vec3(0.85), vec3(occ, ex, 0.0), max(occ, ex)) : vec3(occ, ex, 0.0);
        float fa = seenNow ? max(max(occ, ex) * 0.8, 0.45) : max(occ, ex) * 0.8;
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
