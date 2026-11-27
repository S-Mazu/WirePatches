/*{
  "DESCRIPTION": "Galaxy: per-instance data for the Wire render nodes, plus the two sprite textures. Output Data (640 x 360): one pixel per instance in each of four bands of 90 rows; band 0 position x, y and width, band 1 light colour (rgb times intensity), band 2 unused, band 3 height and, for distant galaxies, rotation in turns. Positions and sizes are stored as 0.5 + v / 2000 (scene units), because reading back clamps to 0..1; the patch decodes them per instance. Alpha is always 1, because reading back divides colour by alpha. Output Dot (640 x 360, stretched on a square): a soft light point. Output Sprite (640 x 360, stretched on a square): a small galaxy for the distant galaxies. Both are opaque (light in the colour, black around it), because the material divides colour by alpha; they are drawn additively.",
  "CREDIT": "Stefan Mazur - https://github.com/S-Mazu/WirePatches",
  "ISFVSN": "2",
  "CATEGORIES": [
    "Generator"
  ],
  "INPUTS": [
    { "NAME": "outputMode", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 2.0 },
    { "NAME": "aspectIn", "TYPE": "float", "DEFAULT": 1.7777778, "MIN": 0.001, "MAX": 1000.0 },
    { "NAME": "scale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1000.0 },
    { "NAME": "clock", "TYPE": "float", "DEFAULT": 0.0, "MIN": -1000000.0, "MAX": 1000000.0 },
    { "NAME": "starCount", "TYPE": "float", "DEFAULT": 12000.0, "MIN": 0.0, "MAX": 30000.0 },
    { "NAME": "galaxyX", "TYPE": "float", "DEFAULT": 0.0, "MIN": -100.0, "MAX": 100.0 },
    { "NAME": "galaxyY", "TYPE": "float", "DEFAULT": 0.0, "MIN": -100.0, "MAX": 100.0 },
    { "NAME": "galaxySize", "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.0, "MAX": 100.0 },
    { "NAME": "galaxyRotation", "TYPE": "float", "DEFAULT": 0.0, "MIN": -180.0, "MAX": 180.0 },
    { "NAME": "galaxyAspect", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "coreSize", "TYPE": "float", "DEFAULT": 0.15, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "coreAspect", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "discOpacity", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "armCount", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.0, "MAX": 12.0 },
    { "NAME": "armTwist", "TYPE": "float", "DEFAULT": 300.0, "MIN": -1440.0, "MAX": 1440.0 },
    { "NAME": "armWidth", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "barSize", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "scatter", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "starSize", "TYPE": "float", "DEFAULT": 0.007, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "starTwinkle", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "hueVariation", "TYPE": "float", "DEFAULT": 30.0, "MIN": 0.0, "MAX": 180.0 },
    { "NAME": "coreColor", "TYPE": "color", "DEFAULT": [ 1.0, 0.82, 0.55, 1.0 ] },
    { "NAME": "armColor", "TYPE": "color", "DEFAULT": [ 0.55, 0.7, 1.0, 1.0 ] },
    { "NAME": "nebulaColor", "TYPE": "color", "DEFAULT": [ 1.0, 0.35, 0.65, 1.0 ] },
    { "NAME": "nebulaOpacity", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "nebulaSize", "TYPE": "float", "DEFAULT": 0.12, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "supernovaSize", "TYPE": "float", "DEFAULT": 0.15, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "supernovaColor", "TYPE": "color", "DEFAULT": [ 0.75, 0.9, 1.0, 1.0 ] },
    { "NAME": "nova0", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "nova1", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "nova2", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "nova3", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "novaSeed0", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "novaSeed1", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "novaSeed2", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "novaSeed3", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "galaxyDensity", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 100.0 },
    { "NAME": "seed", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1000.0 }
  ]
}*/
// Galaxy builds its picture from instanced sprites in Wire (2D Render). Positions, sizes and colours of
// all sprites are computed here on the graphics card, one pixel per sprite, and read back by Sample Texture:
// node math per sprite costs the processor about 35 ns per operation, which limits the star count.
//
// Index ranges (one instance list; dust lanes are drawn per pixel by GalaxyComposite.fs):
//   0 ..    3  core glow, four soft layers
//   4 ..  195  supernovae, 4 slots of 48: a flash and a ring of 47 points
// 196 ..  835  nebulae, the first 214 a soft disc haze
// 836 ..       stars, the first 30 % in the bulge (core), the rest in the disc
// 57344 .. 57599 distant galaxies (own render), the last 256 slots of a band
//
// Shapes. Disc orbits follow the density wave picture: each orbit is an m-lobed loop (m = arm count)
// turned a little more the further out it lies (Arm Twist); where neighbouring loops crowd, arms appear,
// and stars move along their loops through them, so the arms never wind up. Inside Bar Size the loops are
// long and untwisted (a bar). All shape controls are continuous, so a timeline can morph one form into another.
//
// Scene units follow the surface model (example Scaler): the output height is 2 units at Scale 1; the camera
// of the patch applies Scale. Galaxy Size is the galaxy radius in these units, Galaxy X / Y move it in galaxy
// radii, so zooming with Galaxy Size keeps the same point of the galaxy in place.

const float TAU = 6.28318530718;
const float W = 640.0;
const float BAND_ROWS = 90.0;
const float N_GLOW = 4.0;
const float NOVA0 = 4.0;
const float NEB0 = 196.0;
const float HAZE_N = 214.0;
const float STAR0 = 836.0;
const float SKY0 = 57344.0;
const float ENC = 2000.0;                 // positions and sizes are stored as 0.5 + v / ENC

float hash11(float p) {
    p = fract(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return fract(p);
}
float rnd(float idx, float k) { return hash11(idx * 7.13 + k * 101.7 + seed * 13.37 + 0.5); }

vec3 hueShift(vec3 c, float deg) {
    // rotation around the grey axis
    float a = radians(deg);
    float cs = cos(a), sn = sin(a);
    vec3 k = vec3(0.57735);
    return c * cs + cross(k, c) * sn + k * dot(k, c) * (1.0 - cs);
}

vec2 rot(vec2 v, float a) { float c = cos(a), s = sin(a); return vec2(c * v.x - s * v.y, s * v.x + c * v.y); }

// galaxy plane (radius 1) to scene units; tilted = flattened by Galaxy Aspect
vec2 toScene(vec2 g, bool tilted) {
    float s = max(galaxySize, 0.0);
    vec2 v = tilted ? vec2(g.x, g.y * max(galaxyAspect, 0.0)) : g;
    v = rot(v, radians(galaxyRotation));
    return (v + vec2(galaxyX, galaxyY)) * s;
}

float twistRad() { return radians(armTwist); }
float patternSpin() { return clock * 0.05; }            // radians per clock unit: one turn in about 2 minutes

// a disc orbit: radius a (0..1), phase phi0; returns galaxy plane position and the arm factor (1 on the arm crest)
vec3 discOrbit(float idx, float a, float phi0) {
    float M = clamp(armCount, 0.0, 12.0);
    float mI = floor(M) + step(rnd(idx, 3.0), fract(M));
    float B = clamp(barSize, 0.0, 1.0);
    float inBar = B > 0.0 ? 1.0 - smoothstep(0.6 * B, B, a) : 0.0;
    float m = mix(mI, 2.0, step(0.5, inBar));
    float e = 0.5 * (1.0 - 0.85 * clamp(armWidth, 0.0, 1.0));
    e = mix(e, 0.6, inBar);
    if (m < 0.5) e = 0.0;
    float tw = twistRad();
    float theta = tw * max(a - 0.8 * B, 0.0);
    // motion along the loop relative to the pattern: inner faster, outer slower
    float omega = 0.05 * (0.8 / (a + 0.3) - 0.9);
    float u = phi0 + omega * clock;
    float rho = a * (1.0 + e * cos(m * u));
    // scatter: ordered to chaotic
    float sc = max(scatter, 0.0);
    rho *= 1.0 + sc * 0.35 * (rnd(idx, 7.0) - 0.5);
    float psi = u + theta + patternSpin() + sc * 0.6 * (rnd(idx, 8.0) - 0.5) * TAU / max(m, 1.0);
    vec2 g = rho * vec2(cos(psi), sin(psi));
    // crowding of neighbouring loops: arm crest where m u = -90 deg (turned by the twist direction)
    float sgn = tw < 0.0 ? -1.0 : 1.0;
    float strength = clamp(abs(tw) * a * e * m * 0.6, 0.0, 1.0) * (1.0 - inBar);
    float arm = 0.5 - 0.5 * sin(m * u) * sgn;
    float armF = mix(0.5, arm, strength);
    return vec3(g, armF);
}

void main() {
    vec2 nc = isf_FragNormCoord;

    // ---------- output Dot: a soft light point ----------
    if (outputMode > 1.5) {
        float r = length(nc - 0.5) * 2.0;
        float g = exp(-r * r * 14.0) * 0.6 + exp(-r * r * 90.0) * 0.8;
        g *= 1.0 - smoothstep(0.85, 1.0, r);
        gl_FragColor = vec4(vec3(clamp(g, 0.0, 1.0)), 1.0);   // opaque: the material divides colour by alpha
        return;
    }

    // ---------- output Sprite: a small galaxy for the distant ones ----------
    if (outputMode > 0.5) {
        vec2 p = (nc - 0.5) * 2.0;
        float r = length(p);
        float ang = atan(p.y, p.x);
        float M = clamp(floor(armCount + 0.5), 1.0, 12.0);
        float spiral = 0.5 + 0.5 * cos(M * (ang - twistRad() * r));
        float disc = 1.6 * exp(-r / 0.4) * (0.35 + 0.65 * pow(spiral, 3.0)) * clamp(discOpacity, 0.0, 1.0);
        float core = exp(-pow(r / max(0.25 * clamp(coreSize / 0.15, 0.2, 3.0) * 0.15 + 0.02, 0.01), 2.0));
        vec3 c = coreColor.rgb * core * 1.2 + mix(coreColor.rgb, armColor.rgb, smoothstep(0.05, 0.5, r)) * disc;
        c *= 1.0 - smoothstep(0.8, 1.0, r);
        gl_FragColor = vec4(clamp(c, 0.0, 1.0), 1.0);        // opaque, drawn additively
        return;
    }

    // ---------- output Data ----------
    float col = floor(nc.x * W);
    float row = floor(nc.y * BAND_ROWS * 4.0);
    float band = floor(row / BAND_ROWS);
    float idx = mod(row, BAND_ROWS) * W + col;

    float s = max(galaxySize, 0.0);
    float A = max(galaxyAspect, 0.0);
    float disc = clamp(discOpacity, 0.0, 1.0);
    vec2 pos = vec2(0.0);
    vec2 size = vec2(0.0);
    vec3 light = vec3(0.0);
    float skyRot = 0.0;
    float tw = max(starTwinkle, 0.0);
    float hv = max(hueVariation, 0.0);

    if (idx < N_GLOW) {
        // core glow: four soft layers
        float k = idx;
        float cs = max(coreSize, 0.0) * s * (0.5 * pow(2.0, k));
        pos = toScene(vec2(0.0), false);
        size = cs * vec2(1.0, max(coreAspect, 0.0));
        float inten = 0.6 / (1.0 + k * 1.1) * (0.92 + 0.08 * sin(clock * 0.7 + k));
        light = coreColor.rgb * inten;
    } else if (idx < NEB0) {
        // supernovae: 4 slots, flash + ring
        float j = idx - NOVA0;
        float slot = floor(j / 48.0);
        float k = j - slot * 48.0;
        float env = slot < 0.5 ? nova0 : (slot < 1.5 ? nova1 : (slot < 2.5 ? nova2 : nova3));
        float sd = slot < 0.5 ? novaSeed0 : (slot < 1.5 ? novaSeed1 : (slot < 2.5 ? novaSeed2 : novaSeed3));
        if (env > 0.0005) {
            // the star that explodes: a disc star chosen by the seed, followed on its orbit
            float sIdx = STAR0 + floor(sd * 9973.0);
            float a = 0.15 + 0.8 * hash11(sd * 977.0 + 3.1);
            float phi0 = TAU * hash11(sd * 613.0 + 1.7);
            vec3 o = discOrbit(sIdx, a, phi0);
            vec2 c = toScene(o.xy, true);
            float q = 1.0 - env;                        // progress 0 .. 1
            float R = max(supernovaSize, 0.0);          // final ring radius in scene units: diameter = Supernova Size x output height
            if (k < 0.5) {
                pos = c;
                float fs = R * (0.25 + 0.75 * sqrt(q)) * 0.9;
                size = vec2(fs);
                float inten = pow(env, 1.5) * 1.6;
                light = mix(vec3(1.0), supernovaColor.rgb, 0.4) * inten;
            } else {
                float ang = TAU * (k - 1.0) / 47.0 + sd * TAU;
                float rr = R * sqrt(q);
                vec2 off = rr * vec2(cos(ang), sin(ang) * max(A, 0.05));
                off = rot(off, radians(galaxyRotation));
                pos = c + off;
                size = vec2(R * 0.18 * (0.6 + q));
                float inten = env * 0.7;
                light = supernovaColor.rgb * inten;
            }
        }
    } else if (idx < STAR0) {
        // nebulae (the first ones a soft disc haze)
        float j = idx - NEB0;
        bool haze = j < HAZE_N;
        float a = haze ? 0.05 + 0.95 * pow(rnd(idx, 1.0), 0.8) : 0.15 + 0.85 * rnd(idx, 1.0);
        vec3 o = discOrbit(idx, a, TAU * rnd(idx, 2.0));
        pos = toScene(o.xy, true);
        float sz = max(nebulaSize, 0.0) * s * (0.5 + rnd(idx, 4.0)) * (haze ? 2.2 : 1.0);
        size = sz * vec2(1.0, max(A, 0.05));
        float breathe = 0.75 + 0.25 * sin(clock * 0.3 + TAU * rnd(idx, 5.0));
        float inten;
        vec3 c;
        if (haze) {
            inten = 0.7 * (0.25 + 0.75 * o.z * o.z) * (1.0 - smoothstep(0.7, 1.0, a));
            c = mix(coreColor.rgb, armColor.rgb, smoothstep(0.05, 0.6, a));
        } else {
            inten = 0.9 * pow(o.z, 3.0) * breathe;
            c = hueShift(nebulaColor.rgb, (rnd(idx, 6.0) - 0.5) * hv);
        }
        inten *= clamp(nebulaOpacity, 0.0, 1.0) * disc;
        light = c * inten;
    } else if (idx < SKY0) {
        float j = idx - STAR0;
        float n = clamp(floor(starCount), 0.0, SKY0 - STAR0);
        if (j < n) {
            bool bulge = j < 0.3 * n;
            float tPh = TAU * rnd(idx, 5.0);
            float twinkle = 1.0 - tw * (0.5 + 0.5 * sin(clock * (1.5 + 3.0 * rnd(idx, 9.0)) + tPh));
            float big = pow(rnd(idx, 6.0), 6.0);
            size = vec2(2.0 * max(starSize, 0.0) * (0.6 + 2.4 * big));   // Star Size relative to the output height (2 scene units)
            vec3 c;
            float inten;
            if (bulge) {
                // bulge: round in space, not flattened by the tilt
                float a = max(coreSize, 0.0) * 1.6 * sqrt(-log(0.02 + 0.98 * rnd(idx, 1.0))) * 0.6;
                float ang = TAU * rnd(idx, 2.0) + clock * 0.08 / (0.3 + a / max(coreSize, 0.001));
                vec2 g = a * vec2(cos(ang), sin(ang) * max(coreAspect, 0.0));
                g += max(scatter, 0.0) * 0.1 * (vec2(rnd(idx, 7.0), rnd(idx, 8.0)) - 0.5);
                pos = toScene(g, false);
                c = hueShift(coreColor.rgb, (rnd(idx, 3.0) - 0.5) * hv);
                inten = 0.7 * twinkle;
            } else {
                // disc: exponential profile, truncated at the rim
                float r1 = rnd(idx, 1.0);
                float a = -0.4 * log(1.0 - r1 * (1.0 - exp(-1.0 / 0.4)));
                vec3 o = discOrbit(idx, a, TAU * rnd(idx, 2.0));
                pos = toScene(o.xy, true);
                vec3 base = mix(coreColor.rgb, armColor.rgb, smoothstep(0.08, 0.55, a));
                base = mix(base, armColor.rgb * 1.15, 0.5 * o.z);
                c = hueShift(base, (rnd(idx, 3.0) - 0.5) * hv);
                inten = (0.3 + 0.9 * pow(o.z, 3.0)) * twinkle * disc;
            }
            inten *= 0.6 + 0.4 * big;
            light = c * inten;
        }
    } else {
        // distant galaxies, spread over the visible area (grows with the surface)
        float j = idx - SKY0;
        float sc = clamp(scale, 0.001, 1000.0);
        float area = max(aspectIn, 0.001) / (sc * sc);
        float n = clamp(floor(max(galaxyDensity, 0.0) * area + 0.5), 0.0, 256.0);
        if (j < n) {
            vec2 h = vec2(max(aspectIn, 0.001), 1.0) / sc;
            pos = (vec2(rnd(idx, 1.0), rnd(idx, 2.0)) * 2.0 - 1.0) * h;
            float sz = 0.07 + 0.18 * pow(rnd(idx, 3.0), 2.0);
            float sq = 0.25 + 0.75 * rnd(idx, 4.0);
            size = sz * vec2(1.0, sq);
            float inten = 0.6 + 0.4 * rnd(idx, 5.0);
            vec3 c = hueShift(vec3(1.0), (rnd(idx, 6.0) - 0.5) * hv * 2.0);
            light = c * inten;
            skyRot = rnd(idx, 7.0) + clock * 0.004 * (rnd(idx, 8.0) - 0.5);   // rotation in turns
        }
    }

    // NaN / Inf guard
    if (!(pos.x == pos.x) || !(pos.y == pos.y) || abs(pos.x) > 1.0e6 || abs(pos.y) > 1.0e6) { pos = vec2(0.0); size = vec2(0.0); light = vec3(0.0); }
    if (!(size.x == size.x) || !(size.y == size.y)) size = vec2(0.0);
    size = clamp(size, 0.0, 0.5 * ENC);
    pos = clamp(pos, -0.5 * ENC, 0.5 * ENC);

    // alpha stays 1: reading back divides colour by alpha
    if (band < 0.5) gl_FragColor = vec4(0.5 + pos / ENC, size.x / ENC, 1.0);
    else if (band < 1.5) gl_FragColor = vec4(max(light, 0.0), 1.0);
    else if (band < 2.5) gl_FragColor = vec4(0.0, 0.0, 0.0, 1.0);
    else gl_FragColor = vec4(size.y / ENC, fract(skyRot), 0.0, 1.0);
}
