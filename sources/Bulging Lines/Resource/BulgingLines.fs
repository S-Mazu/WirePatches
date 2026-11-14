/*{
    "DESCRIPTION": "Bulging Lines: horizontal lines across the whole frame bulge out where people stand in front of the surface. Driven by the Motion Bridge (lidar) over OSC: per slot x (0..1 along the surface), depth (0 = at the surface, 1 = far) and active (0..1).",
    "CREDIT": "Stefan Mazur, https://github.com/S-Mazu/WirePatches",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator"],
    "INPUTS": [
        {"NAME": "scale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0},
        {"NAME": "lineCount", "TYPE": "float", "DEFAULT": 24.0, "MIN": 0.0, "MAX": 400.0},
        {"NAME": "lineWidth", "TYPE": "float", "DEFAULT": 0.006, "MIN": 0.0, "MAX": 0.5},
        {"NAME": "lineColor", "TYPE": "color", "DEFAULT": [0.92, 0.88, 0.80, 1.0]},
        {"NAME": "bulgeHeight", "TYPE": "float", "DEFAULT": 0.25, "MIN": -5.0, "MAX": 5.0},
        {"NAME": "bulgeWidth", "TYPE": "float", "DEFAULT": 0.35, "MIN": 0.0, "MAX": 10.0},
        {"NAME": "depthInfluence", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "showPeople", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "backdropOn", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "backdropColor", "TYPE": "color", "DEFAULT": [0.04, 0.04, 0.06, 1.0]},
        {"NAME": "x0", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "d0", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "a0", "TYPE": "float", "DEFAULT": 0.0},
        {"NAME": "x1", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "d1", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "a1", "TYPE": "float", "DEFAULT": 0.0},
        {"NAME": "x2", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "d2", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "a2", "TYPE": "float", "DEFAULT": 0.0},
        {"NAME": "x3", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "d3", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "a3", "TYPE": "float", "DEFAULT": 0.0},
        {"NAME": "x4", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "d4", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "a4", "TYPE": "float", "DEFAULT": 0.0},
        {"NAME": "x5", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "d5", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "a5", "TYPE": "float", "DEFAULT": 0.0},
        {"NAME": "x6", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "d6", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "a6", "TYPE": "float", "DEFAULT": 0.0},
        {"NAME": "x7", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "d7", "TYPE": "float", "DEFAULT": 0.0}, {"NAME": "a7", "TYPE": "float", "DEFAULT": 0.0},
        {"NAME": "stale", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0}
    ]
}*/

// Surface model: height is the reference. p is in height units divided by Scale:
// y spans -1/s..1/s, x spans -aspect/s..aspect/s. People are placed by x 0..1 across the
// whole frame width, because the sensor zone is mapped onto the whole surface.

float safe(float v, float lo, float hi) {
    return (v == v) ? clamp(v, lo, hi) : lo;      // NaN -> lo
}

vec4 over(vec4 dst, vec3 rgb, float a) {           // straight alpha: rgb over dst with coverage a
    float oa = a + dst.a * (1.0 - a);
    vec3 orgb = oa > 0.0 ? (rgb * a + dst.rgb * dst.a * (1.0 - a)) / oa : vec3(0.0);
    return vec4(orgb, oa);
}

float bell(float t) {                              // smooth, compact: 1 at 0, 0 beyond |t| = 1
    t = clamp(abs(t), 0.0, 1.0);
    return 0.5 + 0.5 * cos(3.14159265 * t);
}

float aspect;
float s;
float halfW;

float bump(float px, float x, float d, float a) {
    float act = safe(a, 0.0, 1.0);
    if (act <= 0.0) return 0.0;
    float bx = (safe(x, -1.0, 2.0) * 2.0 - 1.0) * aspect / s;
    float near = mix(1.0, 1.0 - safe(d, 0.0, 1.0), safe(depthInfluence, 0.0, 1.0));
    return act * near * bell((px - bx) / halfW);
}

float offsetAt(float px) {
    float sum = bump(px, x0, d0, a0) + bump(px, x1, d1, a1) + bump(px, x2, d2, a2) + bump(px, x3, d3, a3)
              + bump(px, x4, d4, a4) + bump(px, x5, d5, a5) + bump(px, x6, d6, a6) + bump(px, x7, d7, a7);
    sum *= 1.0 - smoothstep(0.3, 1.0, safe(stale, 0.0, 1.0));   // no OSC for 0.3..1 s: bulges fade out
    return safe(bulgeHeight, -5.0, 5.0) * sum;
}

void main() {
    vec2 res = max(RENDERSIZE, vec2(1.0));
    aspect = res.x / res.y;
    s = safe(scale, 0.001, 1000.0);
    halfW = max(safe(bulgeWidth, 0.0, 10.0), 1e-4);

    vec2 p = (gl_FragCoord.xy - 0.5 * res) / res.y * 2.0 / s;
    float px = 2.0 / res.y / s;                   // one pixel in p units

    vec4 col = safe(backdropOn, 0.0, 1.0) > 0.5 ? vec4(backdropColor.rgb, 1.0) : vec4(0.0);

    float n = safe(lineCount, 0.0, 400.0);
    if (n >= 0.5) {
        float spacing = 2.0 / n;
        float off = offsetAt(p.x);
        float slope = (offsetAt(p.x + px) - offsetAt(p.x - px)) / (2.0 * px);
        float q = p.y - off;
        float dist = abs(fract(q / spacing + 0.5) - 0.5) * spacing / sqrt(1.0 + slope * slope);
        float hw = 0.5 * safe(lineWidth, 0.0, 0.5);
        float cover = 1.0 - smoothstep(hw, hw + px, dist);
        cover *= clamp(2.0 * hw / px, 0.0, 1.0);    // lines thinner than a pixel fade out, width 0 = no line
        col = over(col, lineColor.rgb, cover * safe(lineColor.a, 0.0, 1.0));
    }

    if (safe(showPeople, 0.0, 1.0) > 0.5) {       // setup aid: a vertical marker per active person
        float xs[8];
        xs[0] = x0; xs[1] = x1; xs[2] = x2; xs[3] = x3; xs[4] = x4; xs[5] = x5; xs[6] = x6; xs[7] = x7;
        float ac[8];
        ac[0] = a0; ac[1] = a1; ac[2] = a2; ac[3] = a3; ac[4] = a4; ac[5] = a5; ac[6] = a6; ac[7] = a7;
        for (int i = 0; i < 8; i++) {
            float act = safe(ac[i], 0.0, 1.0);
            if (act <= 0.0) continue;
            float bx = (safe(xs[i], -1.0, 2.0) * 2.0 - 1.0) * aspect / s;
            float m = (1.0 - smoothstep(2.0 * px, 3.0 * px, abs(p.x - bx))) * act;
            col = over(col, vec3(1.0, 0.3, 0.2), m);
        }
    }

    gl_FragColor = col;
}
