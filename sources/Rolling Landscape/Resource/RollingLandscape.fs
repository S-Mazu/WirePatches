/*{
    "DESCRIPTION": "Rolling Landscape - Art Deco night skyline as parallax silhouettes (up to 6 layers, back to front), lit windows, stars, moon, searchlights, airships. Follows the surface model: height is the reference, everything scales with the output height and scale, width shows more or less scene, nothing is stretched. Nothing moves by itself: driven by clock from outside.",
    "CREDIT": "Stefan Mazur - https://github.com/S-Mazu/WirePatches",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator"],
    "INPUTS": [
        {"NAME": "clock",       "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 1000.0},
        {"NAME": "scale",       "TYPE": "float", "DEFAULT": 1.0,  "MIN": 0.0,  "MAX": 10.0},
        {"NAME": "parallax",    "TYPE": "float", "DEFAULT": 0.6,  "MIN": -2.0, "MAX": 2.0},
        {"NAME": "layerCount",  "TYPE": "float", "DEFAULT": 3.0,  "MIN": 0.0,  "MAX": 6.0},
        {"NAME": "density",     "TYPE": "float", "DEFAULT": 0.85, "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "horizon",     "TYPE": "float", "DEFAULT": 0.05, "MIN": -2.0, "MAX": 10.0},
        {"NAME": "seed",        "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 1000.0},
        {"NAME": "color1", "TYPE": "color", "DEFAULT": [0.30, 0.33, 0.50, 1.0]},
        {"NAME": "color2", "TYPE": "color", "DEFAULT": [0.24, 0.26, 0.42, 1.0]},
        {"NAME": "color3", "TYPE": "color", "DEFAULT": [0.18, 0.20, 0.34, 1.0]},
        {"NAME": "color4", "TYPE": "color", "DEFAULT": [0.13, 0.14, 0.26, 1.0]},
        {"NAME": "color5", "TYPE": "color", "DEFAULT": [0.08, 0.09, 0.18, 1.0]},
        {"NAME": "color6", "TYPE": "color", "DEFAULT": [0.04, 0.04, 0.10, 1.0]},
        {"NAME": "windowLights","TYPE": "float", "DEFAULT": 0.35, "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "windowColor", "TYPE": "color", "DEFAULT": [1.0, 0.82, 0.45, 1.0]},
        {"NAME": "starDensity", "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "starSize",    "TYPE": "float", "DEFAULT": 1.0,  "MIN": 0.0,  "MAX": 20.0},
        {"NAME": "starColor",   "TYPE": "color", "DEFAULT": [0.95, 0.93, 0.85, 1.0]},
        {"NAME": "starScroll",  "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "moonX",       "TYPE": "float", "DEFAULT": 0.703,"MIN": -3.0, "MAX": 3.0},
        {"NAME": "moonY",       "TYPE": "float", "DEFAULT": 0.62, "MIN": -3.0, "MAX": 3.0},
        {"NAME": "moon",        "TYPE": "float", "DEFAULT": 1.0,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "moonSize",    "TYPE": "float", "DEFAULT": 0.09, "MIN": 0.0,  "MAX": 2.0},
        {"NAME": "moonPhase",   "TYPE": "float", "DEFAULT": 0.5,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "moonRays",    "TYPE": "float", "DEFAULT": 1.0,  "MIN": 0.0,  "MAX": 10.0},
        {"NAME": "moonColor",   "TYPE": "color", "DEFAULT": [0.95, 0.90, 0.75, 1.0]},
        {"NAME": "beamDensity", "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "beamLength",  "TYPE": "float", "DEFAULT": 2.4,  "MIN": 0.0,  "MAX": 20.0},
        {"NAME": "beamColor",   "TYPE": "color", "DEFAULT": [0.95, 0.92, 0.80, 1.0]},
        {"NAME": "airshipDensity","TYPE": "float","DEFAULT": 0.0, "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "airshipColor","TYPE": "color", "DEFAULT": [0.76, 0.72, 0.60, 1.0]},
        {"NAME": "airshipDir",  "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 2.0},
        {"NAME": "backdropOn",  "TYPE": "float", "DEFAULT": 1.0,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "backdropColor","TYPE": "color","DEFAULT": [0.06, 0.08, 0.18, 1.0]}
    ]
}*/

// ---------- helpers ----------
float hash3(vec3 p) {
    p = fract(p * vec3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return fract((p.x + p.y) * p.z);
}
float sdBox(vec2 p, vec2 c, vec2 h) {
    vec2 d = abs(p - c) - h;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}
// box standing on the ground (y = 0 .. top)
float boxG(vec2 q, float cx, float hw, float top) {
    return sdBox(q, vec2(cx, top * 0.5), vec2(hw, top * 0.5));
}
// isosceles triangle pointing up, base centre (cx, y0)
float triU(vec2 q, float cx, float y0, float hw, float h) {
    vec2 p = q - vec2(cx, y0);
    p.x = abs(p.x);
    vec2 n = normalize(vec2(h, hw));
    return max(dot(p - vec2(hw, 0.0), n), -p.y);
}
float circ(vec2 q, vec2 c, float r) { return length(q - c) - r; }
// 1 inside, 0 outside, soft over +-w (smoothstep with reversed edges is undefined in GLSL)
float aa(float d, float w) { return 1.0 - smoothstep(-w, w, d); }
// atan(0, 0) is undefined in GLSL
float safeAtan(float y, float x) { return (abs(x) + abs(y) > 0.0) ? atan(y, x) : 0.0; }

// "over" for straight alpha
void over(inout vec3 col, inout float a, vec3 c, float ca) {
    float na = ca + a * (1.0 - ca);
    if (na > 0.0) col = (c * ca + col * a * (1.0 - ca)) / na;
    a = na;
}

vec3 layerColor(int i) {
    if (i == 0) return color1.rgb;
    if (i == 1) return color2.rgb;
    if (i == 2) return color3.rgb;
    if (i == 3) return color4.rgb;
    if (i == 4) return color5.rgb;
    return color6.rgb;
}

const float HALF_CELL = 0.15;   // half cell width in item units
const float BEAM_CELL = 0.155;     // searchlight spacing along the ground at Beam Density 1 (scene units)
const int   BEAM_REACH = 56;       // neighbour cells checked per side; covers Beam Length up to 20
const float STAR_CELL = 0.06;      // star spacing at Star Density 1 (scene units)
const float STAR_RADIUS = 0.006;   // largest star at Star Size 1 (scene units)
const int   STAR_REACH = 3;        // neighbour cells checked per side; covers Star Size up to 20
const float SHIP_CELL = 1.2;       // airship cell length along a row (scene units)
const float SHIP_ROW = 0.24;       // airship row height (scene units)
const float SHIP_MIN_ALT = 1.1;    // airships fly at least this high above the horizon

// motif: returns (distance, window band bottom, window band top)
vec3 motif(int m, vec2 q, float W, float H, float h3) {
    float d = 1e5; float wb = 0.03; float wt = 0.0;
    if (m == 0) {            // stepped tower (ziggurat)
        d = boxG(q, 0.0, 0.50 * W, 0.55 * H);
        d = min(d, boxG(q, 0.0, 0.375 * W, 0.75 * H));
        d = min(d, boxG(q, 0.0, 0.25 * W, 0.90 * H));
        d = min(d, boxG(q, 0.0, 0.12 * W, H));
        wt = 0.88 * H;
    } else if (m == 1) {     // crown tower with arches and needle
        d = boxG(q, 0.0, 0.40 * W, 0.35 * H);
        d = min(d, boxG(q, 0.0, 0.25 * W, 0.70 * H));
        d = min(d, max(circ(q, vec2(0.0, 0.70 * H), 0.25 * W), 0.70 * H - q.y));
        d = min(d, max(circ(q, vec2(0.0, 0.70 * H + 0.06), 0.18 * W), 0.70 * H - q.y));
        d = min(d, max(circ(q, vec2(0.0, 0.70 * H + 0.11), 0.11 * W), 0.70 * H - q.y));
        d = min(d, triU(q, 0.0, 0.70 * H + 0.11, 0.05 * W, 0.08));
        d = min(d, boxG(q, 0.0, 0.006, 0.70 * H + 0.26));
        wt = 0.68 * H;
    } else if (m == 2) {     // mast tower
        d = boxG(q, 0.0, 0.50 * W, 0.30 * H);
        d = min(d, boxG(q, 0.0, 0.36 * W, 0.50 * H));
        d = min(d, boxG(q, 0.0, 0.24 * W, 0.84 * H));
        d = min(d, boxG(q, 0.0, 0.15 * W, 0.92 * H));
        d = min(d, boxG(q, 0.0, 0.06 * W, H));
        d = min(d, boxG(q, 0.0, 0.006, H + 0.10));
        wt = 0.82 * H;
    } else if (m == 3) {     // fan / zigzag crown
        d = boxG(q, 0.0, 0.45 * W, 0.40 * H);
        d = min(d, boxG(q, 0.0, 0.28 * W, 0.78 * H));
        d = min(d, triU(q, 0.0, 0.78 * H, 0.12 * W, 0.18 * H));
        d = min(d, triU(q, -0.17 * W, 0.78 * H, 0.09 * W, 0.10 * H));
        d = min(d, triU(q,  0.17 * W, 0.78 * H, 0.09 * W, 0.10 * H));
        wt = 0.76 * H;
    } else if (m == 4) {     // slab with stepped roof
        d = boxG(q, 0.0, 0.42 * W, 0.30 * H);
        d = min(d, boxG(q, 0.0, 0.20 * W, H));
        d = min(d, boxG(q, 0.0, 0.14 * W, H + 0.03));
        d = min(d, boxG(q, 0.0, 0.08 * W, H + 0.06));
        wt = 0.97 * H;
    } else if (m == 5) {     // building with water tower
        float r = 0.5 * H;
        d = boxG(q, 0.0, 0.48 * W, r);
        d = min(d, sdBox(q, vec2(-0.035, r + 0.04), vec2(0.005, 0.04)));
        d = min(d, sdBox(q, vec2( 0.035, r + 0.04), vec2(0.005, 0.04)));
        d = min(d, sdBox(q, vec2(0.0, r + 0.12), vec2(0.05, 0.045)));
        d = min(d, triU(q, 0.0, r + 0.165, 0.06, 0.05));
        wt = r - 0.02;
    } else if (m == 6) {     // radio mast
        d = boxG(q, 0.0, 0.30 * W, 0.12 * H);
        d = min(d, triU(q, 0.0, 0.0, 0.07, H));
        d = min(d, sdBox(q, vec2(0.0, 0.45 * H), vec2(0.05, 0.004)));
        d = min(d, sdBox(q, vec2(0.0, 0.70 * H), vec2(0.035, 0.004)));
        d = min(d, boxG(q, 0.0, 0.004, H + 0.06));
        wt = 0.10 * H;
    } else if (m == 7) {     // theatre with vertical sign and canopy
        d = boxG(q, 0.0, 0.48 * W, 0.45 * H);
        d = min(d, sdBox(q, vec2(0.30 * W, 0.62 * H), vec2(0.025, 0.20 * H)));
        d = min(d, sdBox(q, vec2(0.0, 0.18 * H), vec2(0.55 * W, 0.012)));
        wt = 0.43 * H;
    } else if (m == 8) {     // department store with flags
        d = boxG(q, 0.0, 0.50 * W, 0.50 * H);
        d = min(d, boxG(q, 0.0, 0.40 * W, 0.56 * H));
        d = min(d, boxG(q, 0.0, 0.25 * W, 0.62 * H));
        d = min(d, sdBox(q, vec2(-0.30 * W, 0.625 * H), vec2(0.003, 0.125 * H)));
        d = min(d, sdBox(q, vec2( 0.30 * W, 0.625 * H), vec2(0.003, 0.125 * H)));
        d = min(d, sdBox(q, vec2(-0.30 * W + 0.02, 0.72 * H), vec2(0.02, 0.012)));
        d = min(d, sdBox(q, vec2( 0.30 * W + 0.02, 0.72 * H), vec2(0.02, 0.012)));
        wt = 0.48 * H;
    } else if (m == 9) {     // bridge pylon with cables
        float deck = 0.25 * H;
        d = sdBox(q, vec2(0.0, deck), vec2(HALF_CELL, 0.012));
        d = min(d, boxG(q, -0.025, 0.012, H));
        d = min(d, boxG(q,  0.025, 0.012, H));
        d = min(d, sdBox(q, vec2(0.0, 0.85 * H), vec2(0.04, 0.01)));
        d = min(d, triU(q, 0.0, H, 0.04, 0.05));
        float ax = abs(q.x);
        if (ax < HALF_CELL) {
            float t = 1.0 - ax / HALF_CELL;
            float yc = deck + (H - deck) * t * t;
            d = min(d, abs(q.y - yc) - 0.003);
        }
        wt = 0.0;
    } else if (m == 10) {    // elevated railway, sometimes with train
        d = sdBox(q, vec2(0.0, 0.28), vec2(HALF_CELL, 0.022));
        d = min(d, boxG(q, -0.09, 0.008, 0.26));
        d = min(d, boxG(q,  0.09, 0.008, 0.26));
        d = min(d, triU(q, 0.0, 0.18, 0.09, 0.08));
        if (h3 > 0.45) {
            d = min(d, sdBox(q, vec2(0.0, 0.345), vec2(HALF_CELL - 0.004, 0.04)));
            wb = 0.32; wt = 0.37;
        } else { wt = 0.0; }
    } else if (m == 11) {    // deco street lamps
        d = boxG(q, -0.06, 0.006, H);
        d = min(d, sdBox(q, vec2(-0.06, H), vec2(0.03, 0.008)));
        d = min(d, sdBox(q, vec2(-0.06, H + 0.015), vec2(0.018, 0.007)));
        d = min(d, boxG(q, 0.08, 0.006, H * 0.8));
        d = min(d, sdBox(q, vec2(0.08, H * 0.8), vec2(0.03, 0.008)));
        d = min(d, sdBox(q, vec2(0.08, H * 0.8 + 0.015), vec2(0.018, 0.007)));
        wt = 0.0;
    } else if (m == 12) {    // power station with four chimneys
        d = boxG(q, 0.0, 0.48 * W, 0.40 * H);
        d = min(d, boxG(q, -0.30 * W, 0.018, H));
        d = min(d, boxG(q, -0.10 * W, 0.018, H));
        d = min(d, boxG(q,  0.10 * W, 0.018, H));
        d = min(d, boxG(q,  0.30 * W, 0.018, H));
        wt = 0.36 * H;
    } else {                 // neon sign on legs
        d = boxG(q, -0.25 * W, 0.006, 0.55 * H);
        d = min(d, boxG(q, 0.25 * W, 0.006, 0.55 * H));
        d = min(d, sdBox(q, vec2(0.0, 0.70 * H), vec2(0.42 * W, 0.15 * H)));
        wb = 0.56 * H; wt = 0.84 * H;
    }
    return vec3(d, wb, wt);
}

void main() {
    vec2 res = RENDERSIZE;
    // surface model: height is the reference. The visible area is y +-1/scale, x +-aspect/scale.
    float safeScale = clamp(scale, 0.001, 1000.0);
    float ppu = 0.5 * max(res.y, 1.0) * safeScale;     // pixels per scene unit
    vec2 p = (gl_FragCoord.xy - 0.5 * res) / ppu;      // scene coordinates, origin in the frame centre
    vec2 halfExt = 0.5 * res / ppu;                    // half width / half height of the frame in scene units
    float pix = 1.0 / ppu;                             // one pixel in scene units
    float hz = horizon - halfExt.y;                    // Horizon: distance above the bottom edge, in scene units

    vec3 col = vec3(0.0);
    float a = 0.0;

    // ---------- backdrop ----------
    if (backdropOn > 0.5) { col = backdropColor.rgb; a = 1.0; }

    // ---------- stars (behind the moon): one possible star per cell, density 0-1 ----------
    // positions do not depend on density or size, so both can follow FFT without the stars jumping
    if (starDensity > 0.0 && starSize > 0.0) {
        float sr = STAR_RADIUS * starSize;
        // the moon disc hides the stars, also on its dark side
        float moonMask = 1.0;
        if (moon > 0.0 && moonSize > 0.0) {
            moonMask = 1.0 - moon * aa(length(p - vec2(moonX * halfExt.x, moonY * halfExt.y)) - moonSize, pix);
        }
        // Star Scroll: stars move like one more layer behind Layer 1
        vec2 sp = p;
        if (starScroll > 0.5) sp.x += clock * 0.25 * (1.0 - parallax);
        vec2 c0 = floor(sp / STAR_CELL);
        int reach = int(min(ceil(sr / STAR_CELL + 0.9), float(STAR_REACH)));
        for (int j = -reach; j <= reach; j++) {
            for (int i = -reach; i <= reach; i++) {
                vec2 c = c0 + vec2(float(i), float(j));
                float h = hash3(vec3(c, seed + 151.0)) * 0.95;
                float on = smoothstep(h, h + 0.05, starDensity);   // fades in smoothly as density rises
                if (on <= 0.0) continue;
                vec2 jit = vec2(hash3(vec3(c, seed + 157.0)), hash3(vec3(c, seed + 163.0))) - 0.5;
                vec2 q = sp - (c + 0.5 + jit * 0.8) * STAR_CELL;
                float size = mix(0.4, 1.0, hash3(vec3(c, seed + 167.0)));
                float k = sqrt(abs(q.x)) + sqrt(abs(q.y));          // four-pointed star
                float bright = mix(0.5, 1.0, hash3(vec3(c, seed + 173.0)));
                over(col, a, starColor.rgb, aa(k * k - sr * size, pix) * on * bright * moonMask);
            }
        }
    }

    // ---------- moon with ray fan ----------
    // moonPhase: 0 = new moon, 0.25 = first quarter (right half lit), 0.5 = full, 0.75 = last quarter, 1 = new
    // moonRays: ray length relative to the classic look (1); 0 = no rays
    if (moon > 0.0 && moonSize > 0.0) {
        vec2 mc = vec2(moonX * halfExt.x, moonY * halfExt.y);   // -1 / 1 = frame edges
        vec2 v = p - mc;
        float r = length(v);
        float ms = max(moonSize, 1e-4);   // NaN guard: smoothstep needs distinct edges
        float ph = fract(moonPhase) * 6.2831853;
        float litFrac = 0.5 - 0.5 * cos(ph);             // 0 at new moon, 1 at full moon
        if (moonRays > 0.0) {
            float ang = safeAtan(v.y, v.x);
            float ray = step(0.5, fract(ang * 16.0 / 6.2831853));
            float rIn  = 1.3 * ms;
            float rOut = rIn + 2.5 * ms * moonRays;      // classic look: 3.8 * ms at moonRays = 1
            float fan = ray * (1.0 - smoothstep(rIn, rOut, r)) * smoothstep(ms, 1.2 * ms, r) * 0.25 * litFrac;
            over(col, a, moonColor.rgb, fan * moon);
        }
        // terminator: per row the lit part ends at an ellipse x = +-(w + 2e) * cos(phase)
        vec2 u = v / ms;
        float w = sqrt(max(0.0, 1.0 - u.y * u.y));       // half width of the disc in this row (NaN guard)
        float e = max(0.02, pix / ms);                   // soft edge, at least one pixel
        float xt = (max(w, abs(u.x)) + 2.0 * e) * cos(ph);   // max: also covers the anti-aliased rim outside the disc; 2e: no stray rim at new / full moon
        float lit = sin(ph) >= 0.0 ? smoothstep(xt - e, xt + e, u.x)          // waxing: right side lit
                                   : 1.0 - smoothstep(-xt - e, -xt + e, u.x); // waning: left side lit
        float disc = aa(r - ms, pix) * lit;
        over(col, a, moonColor.rgb, disc * moon);
    }

    // ---------- searchlights (behind the skyline): one possible light per ground cell, density 0-1 ----------
    // endless along x like the skyline; each pixel only checks the cells a beam can reach at its height
    float beamY = p.y - (hz + 0.1);
    if (beamDensity > 0.0 && beamLength > 0.0 && beamY > 0.0) {
        float c0 = floor(p.x / BEAM_CELL);
        // tan(max beam angle ~0.4 rad) ~ 0.42; the beam ends at beamLength
        int reach = int(min(ceil(min(beamY, beamLength) * 0.43 / BEAM_CELL) + 1.0, float(BEAM_REACH)));
        for (int k = -reach; k <= reach; k++) {
            float c = c0 + float(k);
            float h = hash3(vec3(c, 61.0, seed)) * 0.95;
            float on = smoothstep(h, h + 0.05, beamDensity);      // fades in smoothly as density rises
            if (on <= 0.0) continue;
            float jx = hash3(vec3(c, 67.0, seed)) - 0.5;
            vec2 base = vec2((c + 0.5 + jx * 0.6) * BEAM_CELL, hz + 0.1);
            float aim = 0.35 * sin(clock * (0.12 + 0.02 * mod(c, 6.0)) + c * 2.1);
            vec2 v = p - base;
            float ang = safeAtan(v.x, v.y);
            float beam = 1.0 - smoothstep(0.0, 0.045, abs(ang - aim));
            beam *= smoothstep(0.0, 0.3, v.y) * (1.0 - smoothstep(0.25 * beamLength, beamLength, length(v)));
            over(col, a, beamColor.rgb, beam * 0.35 * on);
        }
    }

    // ---------- airships: rows over the whole sky, each row with its own speed and direction, density 0-1 ----------
    // cells drift with the clock along endless rows, so they enter from outside and never jump back
    if (airshipDensity > 0.0) {
        float r0 = floor(p.y / SHIP_ROW);
        for (int rk = -1; rk <= 1; rk++) {
            float r = r0 + float(rk);
            float rowY = (r + 0.5) * SHIP_ROW;
            if (rowY < hz + SHIP_MIN_ALT) continue;
            float spd = mix(0.015, 0.045, hash3(vec3(r, 71.0, 3.0)));
            // direction: 0 = Left, 1 = Right, 2 = Random per row (changes with Seed)
            float rnd = hash3(vec3(r, 91.0, seed)) < 0.5 ? 1.0 : -1.0;
            float ad = airshipDir < 0.5 ? 1.0 : (airshipDir < 1.5 ? -1.0 : rnd);
            float u = p.x + clock * spd * ad;                  // position in the drifting row
            float ci0 = floor(u / SHIP_CELL);
            for (int ck = -1; ck <= 1; ck++) {
                float ci = ci0 + float(ck);
                float h = hash3(vec3(ci, r + 101.0, seed)) * 0.95;
                float on = smoothstep(h, h + 0.05, airshipDensity);
                if (on <= 0.0) continue;
                float sz = mix(0.5, 1.0, hash3(vec3(ci, r + 113.0, 5.0)));
                float cx = (ci + 0.5 + (hash3(vec3(ci, r + 127.0, 7.0)) - 0.5) * 0.4) * SHIP_CELL;
                float cy = rowY + (hash3(vec3(ci, r + 131.0, 9.0)) - 0.5) * 0.06;
                vec2 q = (vec2(u, p.y) - vec2(cx, cy)) / sz;
                float d = (length(q / vec2(0.24, 0.06)) - 1.0) * 0.06;
                d = min(d, sdBox(q, vec2(0.02, -0.07), vec2(0.04, 0.012)));
                float tail = ad > 0.0 ? 0.22 : -0.22;
                d = min(d, sdBox(q, vec2(tail, 0.0), vec2(0.025, 0.07)));
                over(col, a, airshipColor.rgb, aa(d * sz, pix) * on);
            }
        }
    }

    // ---------- skyline layers, back (0) to front (5) ----------
    for (int i = 0; i < 6; i++) {
        float fi = float(i);
        float vis = clamp(layerCount - fi, 0.0, 1.0);
        if (vis <= 0.0) continue;
        float t = fi / 5.0;
        float s = mix(0.60, 1.15, t);                  // item scale
        float speed = 1.0 - parallax * (1.0 - t) * 0.9;
        float yb = hz + (1.0 - t) * 0.12;               // baseline
        float wx = p.x / s + clock * 0.25 * speed / s + fi * 7.31;
        float cellW = 2.0 * HALF_CELL;
        float c = floor(wx / cellW);
        vec2 q = vec2((fract(wx / cellW) - 0.5) * cellW, (p.y - yb) / s);
        float px = pix / s;

        float d = q.y;                                  // ground below baseline
        float wb = 0.0, wt = 0.0;
        float h0 = hash3(vec3(c, fi, seed));
        if (h0 < density) {
            float h1 = hash3(vec3(c, fi + 11.0, seed));
            float h2 = hash3(vec3(c, fi + 23.0, seed));
            float h3 = hash3(vec3(c, fi + 37.0, seed));
            int m; float W; float H;
            if (i < 2) {
                m = int(floor(h1 * 5.0));
                W = mix(0.16, 0.27, h2); H = mix(0.70, 1.45, h3);
            } else if (i < 4) {
                m = 5 + int(floor(h1 * 5.0));
                W = mix(0.20, 0.28, h2); H = mix(0.32, 0.65, h3);
            } else {
                m = 10 + int(floor(h1 * 4.0));
                W = mix(0.18, 0.28, h2); H = mix(0.22, 0.45, h3);
            }
            vec3 mo = motif(m, q, W, H, h2);
            d = min(d, mo.x);
            wb = mo.y; wt = mo.z;
            if (wt > wb && windowLights > 0.0 && mo.x < -0.008 && q.y > wb && q.y < wt) {
                vec2 g = vec2(0.025, 0.035);
                vec2 cell = floor(q / g);
                vec2 f = (fract(q / g) - 0.5) * g;
                float win = aa(sdBox(f, vec2(0.0), vec2(0.007, 0.010)), px);
                float lit = step(hash3(vec3(cell + vec2(c * 13.0, 0.0), fi + 51.0 + seed)), windowLights);
                float inside = aa(d, px);
                over(col, a, layerColor(i), inside * vis);
                over(col, a, windowColor.rgb, win * lit * vis);
                continue;
            }
        }
        over(col, a, layerColor(i), aa(d, px) * vis);
    }

    gl_FragColor = vec4(col, a);
}
