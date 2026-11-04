/*{
    "DESCRIPTION": "Geo Tiles - tile collage of flat geometric motifs in three colors (background, shape, accent per tile). Seed picks motif, rotation, mirror, placement and colors per tile. Fill reveals tiles in a fixed random order. Nothing moves by itself.",
    "CREDIT": "Geo Tiles",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator"],
    "INPUTS": [
        {"NAME": "columns", "TYPE": "float", "DEFAULT": 8.0, "MIN": 1.0, "MAX": 32.0},
        {"NAME": "rows", "TYPE": "float", "DEFAULT": 6.0, "MIN": 1.0, "MAX": 24.0},
        {"NAME": "seed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 999.0},
        {"NAME": "fill", "TYPE": "float", "DEFAULT": 85.0, "MIN": 0.0, "MAX": 100.0},
        {"NAME": "bigTiles", "TYPE": "float", "DEFAULT": 25.0, "MIN": 0.0, "MAX": 100.0},
        {"NAME": "animate", "TYPE": "float", "DEFAULT": 60.0, "MIN": 0.0, "MAX": 100.0},
        {"NAME": "patternShift", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "color1", "TYPE": "color", "DEFAULT": [0.92, 0.12, 0.20, 1.0]},
        {"NAME": "color2", "TYPE": "color", "DEFAULT": [0.10, 0.10, 0.22, 1.0]},
        {"NAME": "color3", "TYPE": "color", "DEFAULT": [0.96, 0.95, 0.92, 1.0]},
        {"NAME": "backdropOn", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "backdropColor", "TYPE": "color", "DEFAULT": [0.03, 0.04, 0.045, 1.0]}
    ]
}*/

#define SQ2 1.41421356
#define BIG 100.0
#define ACC 0.12          // share of accent elements in repeated patterns
#define SHIFTN 8.0        // Pattern Shift 0..1 moves patterns by 8 elements; accents repeat every 8 elements -> seamless loop

float hash3(vec3 p) {
    p = fract(p * vec3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return fract((p.x + p.y) * p.z);
}

float pickMotif(float r) {
    if (r < 0.007937) return 0.0;
    if (r < 0.031746) return 1.0;
    if (r < 0.055556) return 2.0;
    if (r < 0.063492) return 3.0;
    if (r < 0.095238) return 4.0;
    if (r < 0.111111) return 5.0;
    if (r < 0.119048) return 6.0;
    if (r < 0.134921) return 7.0;
    if (r < 0.150794) return 8.0;
    if (r < 0.158730) return 9.0;
    if (r < 0.190476) return 10.0;
    if (r < 0.214286) return 11.0;
    if (r < 0.230159) return 12.0;
    if (r < 0.246032) return 13.0;
    if (r < 0.261905) return 14.0;
    if (r < 0.285714) return 15.0;
    if (r < 0.301587) return 16.0;
    if (r < 0.317460) return 17.0;
    if (r < 0.357143) return 18.0;
    if (r < 0.373016) return 19.0;
    if (r < 0.388889) return 20.0;
    if (r < 0.404762) return 21.0;
    if (r < 0.420635) return 22.0;
    if (r < 0.436508) return 23.0;
    if (r < 0.444444) return 24.0;
    if (r < 0.452381) return 25.0;
    if (r < 0.460317) return 26.0;
    if (r < 0.476190) return 27.0;
    if (r < 0.492063) return 28.0;
    if (r < 0.500000) return 29.0;
    if (r < 0.515873) return 30.0;
    if (r < 0.531746) return 31.0;
    if (r < 0.547619) return 32.0;
    if (r < 0.555556) return 33.0;
    if (r < 0.571429) return 34.0;
    if (r < 0.587302) return 35.0;
    if (r < 0.603175) return 36.0;
    if (r < 0.619048) return 37.0;
    if (r < 0.634921) return 38.0;
    if (r < 0.650794) return 39.0;
    if (r < 0.674603) return 40.0;
    if (r < 0.690476) return 41.0;
    if (r < 0.706349) return 42.0;
    if (r < 0.722222) return 43.0;
    if (r < 0.746032) return 44.0;
    if (r < 0.769841) return 45.0;
    if (r < 0.777778) return 46.0;
    if (r < 0.785714) return 47.0;
    if (r < 0.801587) return 48.0;
    if (r < 0.817460) return 49.0;
    if (r < 0.833333) return 50.0;
    if (r < 0.849206) return 51.0;
    if (r < 0.865079) return 52.0;
    if (r < 0.880952) return 53.0;
    if (r < 0.888889) return 54.0;
    if (r < 0.896825) return 55.0;
    if (r < 0.904762) return 56.0;
    if (r < 0.912698) return 57.0;
    if (r < 0.920635) return 58.0;
    if (r < 0.936508) return 59.0;
    if (r < 0.944444) return 60.0;
    if (r < 0.952381) return 61.0;
    if (r < 0.960317) return 62.0;
    if (r < 0.968254) return 63.0;
    if (r < 0.976190) return 64.0;
    if (r < 0.992063) return 65.0;
    return 66.0;
}


// ---------- signed distance helpers (negative = inside) ----------
float sdCircle(vec2 p, float r) { return length(p) - r; }

float sdBox(vec2 p, vec2 b) {
    vec2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

// triangle pointing +y: base on y = b, half width w, height h
float sdTriUp(vec2 p, float b, float w, float h) {
    float e = (h * abs(p.x) / w + p.y - b - h) / sqrt(h * h / (w * w) + 1.0);
    return max(b - p.y, e);
}

float sdRhombus(vec2 p, vec2 r) {
    p = abs(p);
    return (p.x / r.x + p.y / r.y - 1.0) / sqrt(1.0 / (r.x * r.x) + 1.0 / (r.y * r.y));
}

float sdLens(vec2 p, float d, float r) {      // vertical lens: two circles at (+-d, 0)
    return max(length(p - vec2(d, 0.0)) - r, length(p + vec2(d, 0.0)) - r);
}

float sdStar5(vec2 p, float r, float rf) {
    vec2 k1 = vec2(0.809016994375, -0.587785252292);
    vec2 k2 = vec2(-k1.x, k1.y);
    p.x = abs(p.x);
    p -= 2.0 * max(dot(k1, p), 0.0) * k1;
    p -= 2.0 * max(dot(k2, p), 0.0) * k2;
    p.x = abs(p.x);
    p.y -= r;
    vec2 ba = rf * vec2(-k1.y, k1.x) - vec2(0.0, 1.0);
    float h = clamp(dot(p, ba) / dot(ba, ba), 0.0, r);
    return length(p - ba * h) * sign(p.y * ba.x - p.x * ba.y);
}

vec2 rotA(vec2 p, float a) { float c = cos(a), s = sin(a); return vec2(c * p.x - s * p.y, s * p.x + c * p.y); }

vec2 rot45(vec2 p, float sgn) { return vec2(p.x + sgn * p.y, p.y - sgn * p.x) / SQ2; }

// quartered circle: x = quadrants 1/3 (shape), y = quadrants 2/4 (accent)
vec2 ball(vec2 p, float r) {
    float c = length(p) - r;
    float e = min(abs(p.x), abs(p.y));
    float dq = (p.x * p.y > 0.0) ? -e : e;
    return vec2(max(c, dq), max(c, -dq));
}

// element is accent?  id = element cell, k = tile key
bool isAcc(vec2 id, float k) { return hash3(vec3(mod(id, SHIFTN) + 0.31, k + 5.3)) < ACC; }

// route a distance to shape (x) or accent (y)
vec2 put(float d, bool acc) { return acc ? vec2(BIG, d) : vec2(d, BIG); }

vec2 uni(vec2 a, vec2 b) { return min(a, b); }

// small primitives for the combined tile
float mini(float k, vec2 p) {
    if (k < 0.5) return sdCircle(p, 0.7);
    if (k < 1.5) return sdRhombus(p, vec2(0.8, 0.8));
    if (k < 2.5) return max(length(p + vec2(0.0, 1.0)) - 1.0, -(p.y + 1.0));
    if (k < 3.5) return sdTriUp(p, -0.8, 0.8, 1.6);
    if (k < 4.5) return length(p + 1.0) - 1.9;
    return max(sdCircle(p, 0.85), -sdCircle(p, 0.4));
}

// ---------- motifs ----------
// q: tile space, shorter side spans -1..1, ext = half extent of the tile in q units
// s: pattern shift 0..1, k: tile key. returns (shape distance, accent distance)
vec2 motif(float m, vec2 q, vec2 ext, float s, float k) {
    float P = 0.5;
    s *= SHIFTN;
    // ===== patterns: fill the whole tile =====
    if (m < 0.5) {                       // 0 stripes, accent stripe
        float y = q.y / P + s;
        return put((abs(fract(y) - 0.5) - 0.25) * P, isAcc(vec2(floor(y), 0.0), k));
    }
    if (m < 1.5) {                       // 1 waves, accent row
        float y = q.y / P - 0.2 * sin((q.x / P + s) * 6.2831853);
        float row = floor(y + 0.5);
        return put((abs(fract(y) - 0.5) - 0.2) * P * 0.8, isAcc(vec2(row, 1.0), k));
    }
    if (m < 2.5) {                       // 2 scales
        vec2 c = vec2(q.x / P + s, q.y / P);
        float row = floor(c.y);
        c.x += 0.5 * mod(row, 2.0);
        vec2 f = vec2(fract(c.x) - 0.5, fract(c.y));
        float d = max(length(f) - 0.5, -(length(f) - 0.25)) * P;
        return put(d, isAcc(vec2(floor(c.x), row), k));
    }
    if (m < 3.5) {                       // 3 zigzag
        // moves across the zigzag (sliding along it reads as jitter)
        float x = q.x / P;
        float y = q.y / P - (abs(fract(x) - 0.5) - 0.25) * 0.9 + s;
        return put((abs(fract(y) - 0.5) - 0.2) * P * 0.7, isAcc(vec2(floor(y), 3.0), k));
    }
    if (m < 4.5) {                       // 4 chevron row, single accent triangles
        vec2 c = vec2(q.x / P + s, q.y / (2.0 * P));
        vec2 f = fract(c) - 0.5;
        vec2 l = vec2(f.y * 2.0 * P, f.x * P);
        return put(sdTriUp(l, -0.45 * P, 0.9 * P, 0.9 * P), isAcc(floor(c), k));
    }
    if (m < 5.5) {                       // 5 dots
        vec2 c = q / P + vec2(s, 0.0);
        return put((length(fract(c) - 0.5) - 0.28) * P, isAcc(floor(c), k));
    }
    if (m < 6.5) {                       // 6 checkerboard (period = 2 squares)
        vec2 c = q / P + vec2(2.0 * s, 0.0);
        vec2 f = fract(c) - 0.5;
        float e = min(0.5 - abs(f.x), 0.5 - abs(f.y));
        float inside = mod(floor(c.x) + floor(c.y), 2.0) < 0.5 ? -1.0 : 1.0;
        return put(inside * e * P, isAcc(floor(c), k));
    }
    if (m < 7.5) {                       // 7 diamonds
        vec2 c = q / P + vec2(s, 0.0);
        return put(sdRhombus(fract(c) - 0.5, vec2(0.42, 0.42)) * P, isAcc(floor(c), k));
    }
    if (m < 8.5) {                       // 8 teeth, move towards the tips
        vec2 c = vec2(q.x / P, q.y / P + s);
        vec2 f = vec2(fract(c.x) - 0.5, fract(c.y));
        return put(sdTriUp(f, 0.0, 0.5, 0.9) * P * 0.85, isAcc(floor(c), k));
    }
    if (m < 9.5) {                       // 9 bars
        float x = q.x / (P * 0.6) + s;
        return put((abs(fract(x) - 0.5) - 0.2) * P * 0.6, isAcc(vec2(floor(x), 9.0), k));
    }
    if (m < 10.5) {                      // 10 quarter circles, random corner per cell
        vec2 c = q / P + vec2(s, 0.0);
        vec2 id = floor(c);
        float h = hash3(vec3(mod(id, SHIFTN), k + 2.7));
        vec2 cr = vec2(step(0.5, fract(h * 2.0)), step(0.5, h));
        return put((length(fract(c) - cr) - 0.92) * P, isAcc(id, k));
    }
    if (m < 11.5) {                      // 11 staggered quarter circles (fish rows)
        vec2 c = vec2(q.x / P + s, q.y / P);
        float row = floor(c.y);
        c.x += 0.5 * mod(row, 2.0);
        vec2 f = fract(c);
        return put((length(f) - 0.85) * P, isAcc(vec2(floor(c.x), row), k));
    }
    if (m < 12.5) {                      // 12 windows
        float Q = 0.3;
        vec2 c = q / Q + vec2(s, 0.0);
        return put(sdBox(fract(c) - 0.5, vec2(0.2, 0.3)) * Q, isAcc(floor(c), k));
    }
    if (m < 13.5) {                      // 13 half split
        return vec2(q.x, BIG);
    }
    if (m < 14.5) {                      // 14 solid with accent stripe at an edge
        return vec2(-1.0, abs(q.y + ext.y - 0.2) - 0.08);
    }
    if (m < 15.5) {                      // 15 big triangles along the tile
        // triangles stay, the accent steps from triangle to triangle
        float x = q.x;
        float d = sdTriUp(vec2(fract(x) - 0.5, q.y), -ext.y, 0.5, 2.0 * ext.y);
        return put(d, isAcc(vec2(floor(x) - floor(s), 15.0), k));
    }
    if (m < 16.5) {                      // 16 quartered circles strewn diagonally, shrinking
        vec2 a = ball(q - vec2(-0.55, 0.55), 0.34);
        a = uni(a, ball(q, 0.25));
        a = uni(a, ball(q - vec2(0.5, -0.5), 0.17));
        return uni(a, ball(q - vec2(0.85, -0.85), 0.1));
    }
    if (m < 17.5) {                      // 17 leaves on a stem, one accent
        vec2 r = vec2(BIG, BIG);
        r.x = sdBox(q, vec2(0.035, ext.y));
        for (int i = 0; i < 5; i++) {
            float fi = float(i);
            float y = -0.8 + fi * 0.4;
            float sg = mod(fi, 2.0) < 0.5 ? 1.0 : -1.0;
            vec2 p = rot45(q - vec2(sg * 0.22, y), sg);
            float d = sdLens(p, 0.21, 0.34);
            if (isAcc(vec2(fi, 17.0), k + 1.0)) r.y = min(r.y, d); else r.x = min(r.x, d);
        }
        return r;
    }
    if (m < 18.5) {                      // 18 combined tile: grid of mini motifs
        float nx = max(1.0, floor(ext.x / ext.y * 2.0 + 0.5));
        vec2 n = vec2(nx, 2.0);
        vec2 csz = 2.0 * ext / n;
        vec2 u = (q + ext) / csz;
        vec2 id = min(floor(u), n - 1.0);
        float hs = 0.5 * min(csz.x, csz.y);
        vec2 p = (u - id - 0.5) * csz / hs;
        float h1 = hash3(vec3(id, k + 8.1));
        float h2 = hash3(vec3(id, k + 9.7));
        p = rotA(p, 6.2831853 * s / SHIFTN * (h1 < 0.5 ? 1.0 : -1.0));
        float d = mini(floor(h1 * 6.0), p) * hs;
        if (h2 < 0.4) return vec2(d, BIG);
        if (h2 < 0.75) return vec2(-d, BIG);
        return vec2(BIG, d);
    }
    // ===== figures: fitted into the centre square |q| < 1 =====
    if (m < 19.5) {                      // 19 overlapping circles
        return vec2(sdCircle(q + vec2(0.25, 0.1), 0.62), sdCircle(q - vec2(0.38, 0.22), 0.42));
    }
    if (m < 20.5) {                      // 20 arch
        vec2 c = q - vec2(0.0, -0.85);
        float d = max(max(length(c) - 1.0, -c.y), -(length(c) - 0.5));
        return vec2(d, max(length(c) - 0.3, -c.y));
    }
    if (m < 21.5) {                      // 21 quarter circle, accent ring
        float l = length(q + 1.0);
        float d = max(l - 1.85, sdBox(q, vec2(1.0)));
        return vec2(max(d, -(abs(l - 1.2) - 0.12)), max(abs(l - 1.2) - 0.12, sdBox(q, vec2(1.0))));
    }
    if (m < 22.5) {                      // 22 tree, accent trunk
        float d = sdTriUp(q, -0.6, 0.75, 0.7);
        d = min(d, sdTriUp(q, -0.2, 0.6, 0.65));
        d = min(d, sdTriUp(q, 0.2, 0.45, 0.65));
        return vec2(d, sdBox(q - vec2(0.0, -0.75), vec2(0.1, 0.17)));
    }
    if (m < 23.5) {                      // 23 flower, accent centre
        float d = sdCircle(q - vec2(0.45, 0.0), 0.42);
        d = min(d, sdCircle(q + vec2(0.45, 0.0), 0.42));
        d = min(d, sdCircle(q - vec2(0.0, 0.45), 0.42));
        d = min(d, sdCircle(q + vec2(0.0, 0.45), 0.42));
        return vec2(max(d, -sdCircle(q, 0.2)), sdCircle(q, 0.2));
    }
    if (m < 24.5) {                      // 24 leaf
        vec2 r = rot45(q, 1.0);
        return vec2(max(sdLens(r, 0.6, 0.95), -sdBox(r, vec2(0.035, 0.62))), BIG);
    }
    if (m < 25.5) {                      // 25 drop
        float d = min(sdCircle(q - vec2(0.0, -0.3), 0.55), sdTriUp(q, -0.3, 0.5, 1.15));
        return vec2(d, sdCircle(q - vec2(-0.15, -0.35), 0.15));
    }
    if (m < 26.5) {                      // 26 heart
        float d = sdRhombus(q - vec2(0.0, -0.1), vec2(0.7, 0.7));
        d = min(d, sdCircle(q - vec2(0.35, 0.25), 0.495));
        d = min(d, sdCircle(q - vec2(-0.35, 0.25), 0.495));
        return vec2(d, BIG);
    }
    if (m < 27.5) {                      // 27 double chevron arrow, second in accent
        float a = sdTriUp(vec2(q.y, q.x + 0.3), -0.8, 0.9, 1.3);
        float b = sdTriUp(vec2(q.y, q.x + 0.85), -0.8, 0.9, 1.3);
        float a2 = sdTriUp(vec2(q.y, q.x - 0.3), -0.8, 0.9, 1.3);
        return vec2(max(a, -b), max(a2, -a));
    }
    if (m < 28.5) {                      // 28 rising sun
        vec2 c = q - vec2(0.0, -0.9);
        float d = min(length(c) - 0.6, abs(length(c) - 1.2) - 0.06);
        return vec2(max(d, -c.y), max(abs(length(c) - 0.9) - 0.08, -c.y));
    }
    if (m < 29.5) {                      // 29 bird, accent beak
        float d = sdCircle(q - vec2(-0.05, -0.15), 0.5);
        d = min(d, sdCircle(q - vec2(0.35, 0.42), 0.27));
        d = min(d, sdTriUp(vec2(q.y + 0.25, q.x), -0.95, 0.32, 0.75));
        d = max(d, -sdCircle(q - vec2(0.42, 0.48), 0.06));
        return vec2(d, sdTriUp(vec2(q.y - 0.42, q.x), 0.58, 0.1, 0.28));
    }
    if (m < 30.5) {                      // 30 nested diamond, accent core
        float d = max(sdRhombus(q, vec2(0.95)), -sdRhombus(q, vec2(0.55)));
        return vec2(d, sdRhombus(q, vec2(0.3)));
    }
    if (m < 31.5) {                      // 31 diagonal half with accent corner
        float d = max((q.x + q.y) / SQ2, sdBox(q, vec2(1.0)));
        return vec2(d, max(-(q.x + q.y) / SQ2 + 0.9, sdBox(q, vec2(1.0))));
    }
    if (m < 32.5) {                      // 32 target, rings move outward with shift
        float r = length(q);
        float ring = r / 0.36 - s;
        float d = max((abs(fract(ring) - 0.5) - 0.25) * 0.36, r - 0.95);
        return put(d, isAcc(vec2(floor(ring + 0.5), 32.0), k));
    }
    if (m < 33.5) {                      // 33 speech bubble
        float d = sdBox(q - vec2(0.0, 0.15), vec2(0.65, 0.4)) - 0.2;
        d = min(d, sdTriUp(vec2(q.x + 0.35, -q.y), 0.3, 0.22, 0.55));
        return vec2(d, BIG);
    }
    if (m < 34.5) {                      // 34 dome with zigzag cut
        vec2 c = q - vec2(0.0, -0.9);
        float dome = max(length(c) - 1.25, -c.y);
        float zz = -0.25 + 0.7 * abs(fract(q.x * 1.25 + 0.5) - 0.5);
        float cut = zz - q.y;
        return vec2(max(dome, -cut), max(dome, cut));
    }
    if (m < 35.5) {                      // 35 half circles sitting on the edge
        float x = q.x / 1.0;
        float id = floor(x + 0.5);
        float d = max(length(vec2(x - id, q.y + ext.y)) - 0.5, -(q.y + ext.y));
        return put(d, isAcc(vec2(id, 35.0), k));
    }
    if (m < 36.5) {                      // 36 quartered circle
        return ball(q, 0.85);
    }
    if (m < 37.5) {                      // 37 comb: bar with hanging half circles
        float d = sdBox(q - vec2(0.0, 0.6), vec2(0.95, 0.16));
        vec2 r = vec2(d, BIG);
        for (int i = 0; i < 3; i++) {
            float fi = float(i);
            vec2 c = q - vec2(-0.63 + fi * 0.63, 0.44);
            float h = max(length(c) - 0.3, c.y - 0.06);
            if (isAcc(vec2(fi, 37.0), k + 2.0)) r.y = min(r.y, h); else r.x = min(r.x, h);
        }
        return r;
    }
    if (m < 38.5) {                      // 38 stacked diamonds, one accent
        vec2 r = vec2(BIG, BIG);
        for (int i = 0; i < 3; i++) {
            float fi = float(i);
            float d = sdRhombus(q - vec2(0.0, -0.65 + fi * 0.65), vec2(0.42, 0.33));
            if (i == 1 || isAcc(vec2(fi, 38.0), k)) r.y = min(r.y, d); else r.x = min(r.x, d);
        }
        return r;
    }
    // ===== more patterns (zoomable) =====
    if (m < 39.5) {                      // 39 concentric squares
        float t = max(abs(q.x), abs(q.y)) / P - s;
        return put((abs(fract(t) - 0.5) - 0.25) * P, isAcc(vec2(floor(t + 0.5), 39.0), k));
    }
    if (m < 40.5) {                      // 40 rows of rainbow arches
        vec2 c = vec2(q.x / (2.0 * P) + s, q.y / (2.0 * P));
        vec2 f = vec2(fract(c.x) - 0.5, fract(c.y));
        float r = length(f);
        float d = max((abs(fract(r * 3.0) - 0.5) - 0.25) / 3.0, r - 0.5) * 2.0 * P;
        return put(d, isAcc(floor(c), k));
    }
    if (m < 41.5) {                      // 41 herringbone blocks
        vec2 c = q / P + vec2(s, 0.0);
        vec2 id = floor(c);
        vec2 f = fract(c) - 0.5;
        vec2 b = mod(id.x + id.y, 2.0) < 0.5 ? vec2(0.44, 0.17) : vec2(0.17, 0.44);
        return put(sdBox(f, b) * P, isAcc(id, k));
    }
    if (m < 42.5) {                      // 42 plus grid
        vec2 c = q / P + vec2(s, 0.0);
        vec2 f = fract(c) - 0.5;
        float d = min(sdBox(f, vec2(0.32, 0.09)), sdBox(f, vec2(0.09, 0.32))) * P;
        return put(d, isAcc(floor(c), k));
    }
    if (m < 43.5) {                      // 43 ring grid
        vec2 c = q / P + vec2(s, 0.0);
        return put((abs(length(fract(c) - 0.5) - 0.28) - 0.1) * P, isAcc(floor(c), k));
    }
    if (m < 44.5) {                      // 44 half-circle truchet
        vec2 c = q / P + vec2(s, 0.0);
        vec2 id = floor(c);
        vec2 f = fract(c) - 0.5;
        float o = floor(hash3(vec3(mod(id, SHIFTN), k + 4.4)) * 4.0);
        vec2 dir = o < 0.5 ? vec2(0.0, -0.5) : o < 1.5 ? vec2(0.5, 0.0) : o < 2.5 ? vec2(0.0, 0.5) : vec2(-0.5, 0.0);
        return put((length(f - dir) - 0.5) * P, isAcc(id, k));
    }
    if (m < 45.5) {                      // 45 triangle truchet
        vec2 c = q / P + vec2(s, 0.0);
        vec2 id = floor(c);
        vec2 f = fract(c) - 0.5;
        float o = floor(hash3(vec3(mod(id, SHIFTN), k + 6.6)) * 4.0);
        vec2 sg = o < 0.5 ? vec2(1.0, 1.0) : o < 1.5 ? vec2(-1.0, 1.0) : o < 2.5 ? vec2(-1.0, -1.0) : vec2(1.0, -1.0);
        float d = dot(f, sg) / SQ2 * P;
        return put(d, isAcc(id, k));
    }
    if (m < 46.5) {                      // 46 diagonal stripes
        float y = (q.x + q.y) / SQ2 / P + s;
        return put((abs(fract(y) - 0.5) - 0.25) * P, isAcc(vec2(floor(y), 46.0), k));
    }
    if (m < 47.5) {                      // 47 plaid, crossings in accent
        float a = (abs(fract(q.x / P + s) - 0.5) - 0.12) * P;
        float b = (abs(fract(q.y / P) - 0.5) - 0.12) * P;
        return vec2(min(a, b), max(a, b));
    }
    if (m < 48.5) {                      // 48 sunburst from a corner
        vec2 p = q + ext;
        float ang = atan(p.y, p.x) / 1.5707963 * 12.0 + s;
        float d = (abs(fract(ang) - 0.5) - 0.25) * length(p) * 1.5707963 / 12.0;
        return put(d, isAcc(vec2(floor(ang), 48.0), k));
    }
    if (m < 49.5) {                      // 49 rainbow rings from a corner
        float t = length(q + ext) / P - s;
        return put((abs(fract(t) - 0.5) - 0.25) * P, isAcc(vec2(floor(t + 0.5), 49.0), k));
    }
    if (m < 50.5) {                      // 50 brick wall
        vec2 c = vec2(q.x / (2.0 * P) + s, q.y / P);
        float row = floor(c.y);
        c.x += 0.5 * mod(row, 2.0);
        vec2 l = (fract(c) - 0.5) * vec2(2.0 * P, P);
        return put(sdBox(l, vec2(P - 0.05, 0.5 * P - 0.05)), isAcc(vec2(floor(c.x), row), k));
    }
    if (m < 51.5) {                      // 51 drop grid
        vec2 c = q / P + vec2(s, 0.0);
        vec2 f = fract(c) - 0.5;
        float d = min(sdCircle(f - vec2(0.0, -0.12), 0.24), sdTriUp(f, -0.12, 0.22, 0.42)) * P;
        return put(d, isAcc(floor(c), k));
    }
    if (m < 52.5) {                      // 52 dots and rings alternating
        vec2 c = q / P + vec2(2.0 * s, 0.0);
        vec2 id = floor(c);
        float r = length(fract(c) - 0.5);
        float d = mod(id.x + id.y, 2.0) < 0.5 ? r - 0.3 : abs(r - 0.28) - 0.08;
        return put(d * P, isAcc(id, k));
    }
    // ===== more figures =====
    if (m < 53.5) {                      // 53 star, accent centre
        return vec2(sdStar5(q, 0.95, 0.45), sdCircle(q, 0.15));
    }
    if (m < 54.5) {                      // 54 crescent with a dot
        return vec2(max(sdCircle(q, 0.85), -sdCircle(q - vec2(0.38, 0.22), 0.7)), sdCircle(q - vec2(0.45, 0.1), 0.14));
    }
    if (m < 55.5) {                      // 55 house, accent door
        float d = min(sdBox(q - vec2(0.0, -0.4), vec2(0.6, 0.5)), sdTriUp(q, 0.05, 0.82, 0.85));
        return vec2(d, sdBox(q - vec2(0.0, -0.65), vec2(0.15, 0.25)));
    }
    if (m < 56.5) {                      // 56 mushroom, accent dots
        float cap = max(sdCircle(q - vec2(0.0, -0.05), 0.85), -(q.y + 0.05));
        float d = min(cap, sdBox(q - vec2(0.0, -0.47), vec2(0.2, 0.48)));
        float dots = min(sdCircle(q - vec2(-0.4, 0.25), 0.13), min(sdCircle(q - vec2(0.12, 0.5), 0.12), sdCircle(q - vec2(0.45, 0.18), 0.11)));
        return vec2(d, max(dots, cap));
    }
    if (m < 57.5) {                      // 57 fish, accent band
        float body = sdLens(vec2(q.y, q.x + 0.1), 0.45, 0.75);
        float d = min(body, sdTriUp(vec2(q.y, -q.x), -0.98, 0.35, 0.5));
        d = max(d, -sdCircle(q - vec2(-0.4, 0.07), 0.07));
        return vec2(d, max(body, abs(q.x - 0.05) - 0.07));
    }
    if (m < 58.5) {                      // 58 butterfly, accent body
        vec2 p = vec2(abs(q.x), q.y);
        float d = min(sdCircle(p - vec2(0.42, 0.3), 0.4), sdCircle(p - vec2(0.33, -0.35), 0.3));
        return vec2(d, sdBox(q, vec2(0.07, 0.6)) - 0.03);
    }
    if (m < 59.5) {                      // 59 pinwheel, alternating blades
        vec2 r = vec2(BIG, BIG);
        vec2 p = q;
        for (int i = 0; i < 4; i++) {
            float d = max(max(-p.x, p.y - 0.95), (p.x - p.y) / SQ2);
            if (mod(float(i), 2.0) < 0.5) r.x = min(r.x, d); else r.y = min(r.y, d);
            p = vec2(-p.y, p.x);
        }
        return r;
    }
    if (m < 60.5) {                      // 60 cloud
        float d = min(sdCircle(q - vec2(-0.45, -0.1), 0.4), sdCircle(q - vec2(0.05, 0.15), 0.5));
        d = min(d, sdCircle(q - vec2(0.5, -0.1), 0.35));
        d = min(d, sdBox(q - vec2(0.0, -0.3), vec2(0.85, 0.2)));
        return vec2(max(d, -(q.y + 0.5)), BIG);
    }
    if (m < 61.5) {                      // 61 eye
        float lens = sdLens(vec2(q.y, q.x), 0.5, 1.0);
        return vec2(max(lens, -sdCircle(q, 0.32)), max(sdCircle(q, 0.32), -sdCircle(q, 0.12)));
    }
    if (m < 62.5) {                      // 62 cross, accent centre
        float d = min(sdBox(q, vec2(0.9, 0.28)), sdBox(q, vec2(0.28, 0.9)));
        return vec2(d, sdBox(q, vec2(0.16)));
    }
    if (m < 63.5) {                      // 63 hourglass
        return vec2(sdTriUp(q, -0.9, 0.7, 0.9), sdTriUp(vec2(q.x, -q.y), -0.9, 0.7, 0.9));
    }
    if (m < 64.5) {                      // 64 tulip, accent leaf
        float cup = max(sdCircle(q - vec2(0.0, 0.1), 0.45), q.y - 0.1);
        float d = min(cup, sdTriUp(q - vec2(-0.28, 0.0), 0.0, 0.17, 0.5));
        d = min(d, sdTriUp(q, 0.0, 0.2, 0.6));
        d = min(d, sdTriUp(q - vec2(0.28, 0.0), 0.0, 0.17, 0.5));
        d = min(d, sdBox(q - vec2(0.0, -0.6), vec2(0.04, 0.35)));
        return vec2(d, sdLens(rot45(q - vec2(0.25, -0.6), 1.0), 0.15, 0.25));
    }
    if (m < 65.5) {                      // 65 spiral
        float r = length(q);
        float t = r / 0.28 - atan(q.y, q.x) / 6.2831853;
        return vec2(max((abs(fract(t) - 0.5) - 0.25) * 0.28, r - 0.95), BIG);
    }
    // 66 mountains with snow cap
    float big = sdTriUp(q - vec2(-0.2, 0.0), -0.8, 0.75, 1.5);
    float small = sdTriUp(q - vec2(0.45, 0.0), -0.8, 0.5, 0.9);
    return vec2(min(max(big, q.y - 0.3), small), max(big, 0.3 - q.y));
}

void main() {
    vec2 res = RENDERSIZE;
    vec2 fc = isf_FragNormCoord * res;
    float cols = floor(max(columns, 1.0));
    float rws = floor(max(rows, 1.0));
    float sd = floor(seed);
    vec2 cellSz = res / vec2(cols, rws);
    vec2 cell = min(floor(fc / cellSz), vec2(cols, rws) - 1.0);

    // ----- big tiles: 2x2 blocks may merge into 2x2, 2x1 or 1x2 tiles -----
    vec2 org = cell;
    vec2 sz = vec2(1.0);
    vec2 blk = floor(cell / 2.0);
    vec2 o = blk * 2.0;
    if (o.x + 1.0 < cols && o.y + 1.0 < rws && hash3(vec3(blk, sd + 101.0)) < bigTiles / 100.0) {
        float t = floor(hash3(vec3(blk, sd + 211.0)) * 3.0);
        if (t < 0.5)      { org = o; sz = vec2(2.0, 2.0); }
        else if (t < 1.5) { org = vec2(o.x, cell.y); sz = vec2(2.0, 1.0); }
        else              { org = vec2(cell.x, o.y); sz = vec2(1.0, 2.0); }
    }
    vec3 key = vec3(org + vec2(0.37, 0.71) * (sz.x * 2.0 + sz.y), sd);
    float tk = hash3(key + vec3(0.0, 0.0, 29.0)) * 97.0;

    vec3 col = vec3(0.0);
    float a = 0.0;
    if (backdropOn > 0.5) { col = backdropColor.rgb; a = 1.0; }

    // fixed rank per tile: raising Fill only adds tiles
    if (hash3(key + vec3(0.0, 0.0, 7.0)) < fill / 100.0) {
        vec2 tsz = sz * cellSz;
        float hs = 0.5 * min(tsz.x, tsz.y);
        vec2 q = (fc - org * cellSz - 0.5 * tsz) / hs;
        vec2 ext = 0.5 * tsz / hs;
        float pw = 1.0 / hs;

        float m = pickMotif(hash3(key + vec3(0.0, 0.0, 13.0)));
        float rot = floor(hash3(key + vec3(0.0, 0.0, 17.0)) * 4.0);
        // figures with an up direction stay upright (mirror only)
        bool upright = (m > 21.5 && m < 22.5) || (m > 24.5 && m < 26.5) || (m > 28.5 && m < 29.5) || (m > 32.5 && m < 33.5)
                    || (m > 54.5 && m < 57.5) || (m > 57.5 && m < 58.5) || (m > 59.5 && m < 60.5) || (m > 63.5 && m < 64.5) || m > 65.5;
        if (upright) rot = 0.0;
        if (hash3(key + vec3(0.0, 0.0, 19.0)) < 0.5) q.x = -q.x;
        if (rot > 0.5) { q = vec2(-q.y, q.x); ext = ext.yx; }
        if (rot > 1.5) { q = vec2(-q.y, q.x); ext = ext.yx; }
        if (rot > 2.5) { q = vec2(-q.y, q.x); ext = ext.yx; }

        // animation: fixed rank per tile, Animate (%) picks which tiles follow Pattern Shift
        bool anim = hash3(key + vec3(0.0, 0.0, 43.0)) < animate / 100.0;
        float dir = hash3(key + vec3(0.0, 0.0, 47.0)) < 0.5 ? 1.0 : -1.0;
        float sA = anim ? patternShift * dir : 0.0;

        bool zoomable = m < 12.5 || (m > 14.5 && m < 15.5) || (m > 38.5 && m < 52.5);
        bool fixedFig = (m > 20.5 && m < 21.5) || (m > 30.5 && m < 31.5) || (m > 34.5 && m < 35.5);
        // half/solid never move; the combined tile only turns its mini motifs (a pulse would re-split its grid)
        bool still = (m > 12.5 && m < 14.5) || (m > 17.5 && m < 18.5);
        float sc = 1.0;
        if (zoomable) {
            // zoom per tile: fine texture .. few big cropped shapes
            float hz = hash3(key + vec3(0.0, 0.0, 53.0));
            sc = hz < 0.15 ? 0.5 : hz < 0.35 ? 0.7 : hz < 0.65 ? 1.0 : hz < 0.85 ? 1.4 : 2.2;
            q /= sc;
        } else if (!still) {
            if (m > 18.5 && !fixedFig) {
                // free placement: size and offset per tile, sometimes a big crop
                float h1 = hash3(key + vec3(0.0, 0.0, 31.0));
                sc = h1 < 0.15 ? 1.8 + 4.0 * h1 : 0.6 + 0.9 * h1;
                vec2 off = (vec2(hash3(key + vec3(0.0, 0.0, 37.0)), hash3(key + vec3(0.0, 0.0, 41.0))) - 0.5) * 0.8;
                q = (q - off) / sc;
            }
            // figure motion: rotate (one turn per loop), pulse or bob (4 per loop)
            float ty = floor(hash3(key + vec3(0.0, 0.0, 59.0)) * 3.0);
            if ((upright || fixedFig) && ty < 0.5) ty = 1.0;
            float w = 6.2831853 * sA;
            if (ty < 0.5) q = rotA(q, w);
            else if (ty < 1.5) { float ps = 1.0 + 0.12 * sin(w * 4.0); q /= ps; sc *= ps; }
            else q.y -= 0.1 * sin(w * 4.0);
        }

        vec2 d = motif(m, q, ext / sc, sA, tk) * sc;
        float cS = clamp(0.5 - d.x / pw, 0.0, 1.0);
        float cA = clamp(0.5 - d.y / pw, 0.0, 1.0);

        // colors: background, shape, accent = a permutation of the three
        float pk = floor(hash3(key + vec3(0.0, 0.0, 23.0)) * 6.0);
        vec3 c1 = color1.rgb, c2 = color2.rgb, c3 = color3.rgb;
        vec3 bg = (pk < 2.0) ? c1 : (pk < 4.0) ? c2 : c3;
        vec3 fg = (pk < 0.5) ? c2 : (pk < 1.5) ? c3 : (pk < 2.5) ? c1 : (pk < 3.5) ? c3 : (pk < 4.5) ? c1 : c2;
        vec3 ac = c1 + c2 + c3 - bg - fg;
        // shape and accent share edges: weigh coverages so no background shows through the seam
        float wA = cA;
        float wS = min(cS, 1.0 - cA);
        col = bg * (1.0 - wS - wA) + fg * wS + ac * wA;
        a = 1.0;
    }

    gl_FragColor = vec4(col, a);
}
