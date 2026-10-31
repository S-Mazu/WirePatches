/*{
    "DESCRIPTION": "Rolling Landscape v2 - Art Deco night skyline as parallax silhouettes (up to 6 layers, back to front), lit windows, moon, searchlights, airships. Nothing moves by itself: scroll is driven from outside.",
    "CREDIT": "Rolling Landscape v2",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator"],
    "INPUTS": [
        {"NAME": "scroll",      "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 1000.0},
        {"NAME": "scrollDir",   "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "parallax",    "TYPE": "float", "DEFAULT": 0.6,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "layerCount",  "TYPE": "float", "DEFAULT": 3.0,  "MIN": 0.0,  "MAX": 6.0},
        {"NAME": "density",     "TYPE": "float", "DEFAULT": 0.85, "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "horizon",     "TYPE": "float", "DEFAULT": -0.95,"MIN": -1.0, "MAX": 0.5},
        {"NAME": "seed",        "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 100.0},
        {"NAME": "color1", "TYPE": "color", "DEFAULT": [0.30, 0.33, 0.50, 1.0]},
        {"NAME": "color2", "TYPE": "color", "DEFAULT": [0.24, 0.26, 0.42, 1.0]},
        {"NAME": "color3", "TYPE": "color", "DEFAULT": [0.18, 0.20, 0.34, 1.0]},
        {"NAME": "color4", "TYPE": "color", "DEFAULT": [0.13, 0.14, 0.26, 1.0]},
        {"NAME": "color5", "TYPE": "color", "DEFAULT": [0.08, 0.09, 0.18, 1.0]},
        {"NAME": "color6", "TYPE": "color", "DEFAULT": [0.04, 0.04, 0.10, 1.0]},
        {"NAME": "windowLights","TYPE": "float", "DEFAULT": 0.35, "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "windowColor", "TYPE": "color", "DEFAULT": [1.0, 0.82, 0.45, 1.0]},
        {"NAME": "layer1On", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "layer2On", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "layer3On", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "layer4On", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "layer5On", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "layer6On", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "moonOn",      "TYPE": "float", "DEFAULT": 1.0,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "moonX",       "TYPE": "float", "DEFAULT": 1.25, "MIN": -2.0, "MAX": 2.0},
        {"NAME": "moonY",       "TYPE": "float", "DEFAULT": 0.62, "MIN": -1.2, "MAX": 1.2},
        {"NAME": "moon",        "TYPE": "float", "DEFAULT": 1.0,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "moonSize",    "TYPE": "float", "DEFAULT": 0.09, "MIN": 0.02, "MAX": 0.4},
        {"NAME": "moonColor",   "TYPE": "color", "DEFAULT": [0.95, 0.90, 0.75, 1.0]},
        {"NAME": "searchlights","TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 6.0},
        {"NAME": "beamColor",   "TYPE": "color", "DEFAULT": [0.95, 0.92, 0.80, 1.0]},
        {"NAME": "airships",    "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 3.0},
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

// "over" for straight alpha
void over(inout vec3 col, inout float a, vec3 c, float ca) {
    float na = ca + a * (1.0 - ca);
    if (na > 0.0) col = (c * ca + col * a * (1.0 - ca)) / na;
    a = na;
}

float layerOn(int i) {
    if (i == 0) return layer1On;
    if (i == 1) return layer2On;
    if (i == 2) return layer3On;
    if (i == 3) return layer4On;
    if (i == 4) return layer5On;
    return layer6On;
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
    vec2 p = (gl_FragCoord.xy - 0.5 * res) / res.y * 2.0;   // y -1..1, x +-1.78
    float pix = 2.0 / res.y;
    float dir = scrollDir < 0.5 ? 1.0 : -1.0;              // 0 = Left, 1 = Right

    vec3 col = vec3(0.0);
    float a = 0.0;

    // ---------- backdrop ----------
    if (backdropOn > 0.5) { col = backdropColor.rgb; a = 1.0; }

    // ---------- moon with ray fan ----------
    if (moon > 0.0 && moonOn > 0.5) {
        vec2 mc = vec2(moonX, moonY);
        vec2 v = p - mc;
        float r = length(v);
        float ms = moonSize;
        float ang = atan(v.y, v.x);
        float ray = step(0.5, fract(ang * 16.0 / 6.2831853));
        float fan = ray * (1.0 - smoothstep(1.3 * ms, 3.8 * ms, r)) * smoothstep(ms, 1.2 * ms, r) * 0.25;
        over(col, a, moonColor.rgb, fan * moon);
        float disc = smoothstep(pix, -pix, r - ms);
        over(col, a, moonColor.rgb, disc * moon);
    }

    // ---------- searchlights (behind the skyline), count 0-6, fractional fades in ----------
    if (searchlights > 0.0) {
        for (int k = 0; k < 6; k++) {
            float fk = float(k);
            float on = clamp(searchlights - fk, 0.0, 1.0);
            if (on <= 0.0) continue;
            // spread over the width, alternating order so few lights still cover the screen
            float slot = (k == 0) ? 2.0 : (k == 1) ? 4.0 : (k == 2) ? 0.0 : (k == 3) ? 5.0 : (k == 4) ? 1.0 : 3.0;
            vec2 base = vec2(-1.55 + slot * 0.62, horizon + 0.1);
            float aim = 0.35 * sin(scroll * (0.12 + 0.02 * fk) + fk * 2.1);
            vec2 v = p - base;
            float ang = atan(v.x, v.y);
            float beam = smoothstep(0.045, 0.0, abs(ang - aim));
            beam *= smoothstep(0.0, 0.3, v.y) * (1.0 - smoothstep(0.6, 2.4, length(v)));
            over(col, a, beamColor.rgb, beam * 0.35 * on);
        }
    }

    // ---------- airships, count 0-3, fractional fades in ----------
    if (airships > 0.0) {
        for (int k = 0; k < 3; k++) {
            float fk = float(k);
            float on = clamp(airships - fk, 0.0, 1.0);
            if (on <= 0.0) continue;
            float sz = (k == 0) ? 1.0 : ((k == 1) ? 0.65 : 0.8);
            float ay = (k == 0) ? 0.50 : ((k == 1) ? 0.74 : 0.30);
            float spd = (k == 0) ? 0.030 : ((k == 1) ? 0.020 : 0.040);
            // direction: 0 = Left, 1 = Right, 2 = Random (changes with Seed;
            // airship 2 always flies against airship 1, so Random never looks like one direction)
            float rnd = hash3(vec3(fk == 1.0 ? 0.0 : fk, 91.0, seed)) < 0.5 ? 1.0 : -1.0;
            if (k == 1) rnd = -rnd;
            float ad = airshipDir < 0.5 ? 1.0 : (airshipDir < 1.5 ? -1.0 : rnd);
            float ax = mod(0.6 + fk * 1.7 - scroll * spd * ad + 2.4, 4.8) - 2.4;
            vec2 q = (p - vec2(ax, ay)) / sz;
            float d = (length(q / vec2(0.24, 0.06)) - 1.0) * 0.06;
            d = min(d, sdBox(q, vec2(0.02, -0.07), vec2(0.04, 0.012)));
            float tail = ad > 0.0 ? 0.22 : -0.22;
            d = min(d, sdBox(q, vec2(tail, 0.0), vec2(0.025, 0.07)));
            over(col, a, airshipColor.rgb, smoothstep(pix, -pix, d * sz) * on);
        }
    }

    // ---------- skyline layers, back (0) to front (5) ----------
    for (int i = 0; i < 6; i++) {
        float fi = float(i);
        float vis = clamp(layerCount - fi, 0.0, 1.0) * step(0.5, layerOn(i));
        if (vis <= 0.0) continue;
        float t = fi / 5.0;
        float s = mix(0.60, 1.15, t);                  // item scale
        float speed = 1.0 - parallax * (1.0 - t) * 0.9;
        float yb = horizon + (1.0 - t) * 0.12;          // baseline
        float wx = p.x / s + scroll * 0.25 * speed * dir / s + fi * 7.31;
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
                float win = smoothstep(px, -px, sdBox(f, vec2(0.0), vec2(0.007, 0.010)));
                float lit = step(hash3(vec3(cell + vec2(c * 13.0, 0.0), fi + 51.0 + seed)), windowLights);
                float inside = smoothstep(px, -px, d);
                over(col, a, layerColor(i), inside * vis);
                over(col, a, windowColor.rgb, win * lit * vis);
                continue;
            }
        }
        over(col, a, layerColor(i), smoothstep(px, -px, d) * vis);
    }

    gl_FragColor = vec4(col, a);
}
