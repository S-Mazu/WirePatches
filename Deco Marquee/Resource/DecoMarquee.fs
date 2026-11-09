/*{
  "DESCRIPTION": "Deco Marquee - an Art Deco theatre frame with chasing marquee lamps, on top of the clip below. Clock drives the chase: 0 to 1 is one step of the pattern, BPM Sync loops seamlessly. Scale sizes everything relative to the output height. Border: Border Style (Off, Zigzag, Chevron, Steps, Greek Key, Diamonds, Scallop, Beads, Sunburst Corners), Border Width, Border Color. Bulbs: Bulb Layout (Off, Rows, Frame), Bulb Spacing (a wider surface gets more lamps), Bulb Size, Bulb Inset (distance from the edge), Bulb Glow, Bulb On Color, Bulb Off Color. Lamps always stay clear of the corner elements. Chase: Chase Mode (Run, Alternate, Fill, Bounce, Mirror, Twinkle, Wave), Chase Group (lamps per step), Chase Duty (lit share), Filament (lamps glow on after the current is gone). Shader: Resource/DecoMarquee.fs.",
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
      "NAME": "clock",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "scale",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "borderStyle",
      "TYPE": "float",
      "DEFAULT": 3.0,
      "MIN": 0.0,
      "MAX": 8.0
    },
    {
      "NAME": "borderWidth",
      "TYPE": "float",
      "DEFAULT": 0.06,
      "MIN": 0.0,
      "MAX": 2.0
    },
    {
      "NAME": "borderColor",
      "TYPE": "color",
      "DEFAULT": [
        0.83,
        0.66,
        0.25,
        1.0
      ]
    },
    {
      "NAME": "bulbLayout",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 2.0
    },
    {
      "NAME": "bulbSpacing",
      "TYPE": "float",
      "DEFAULT": 0.25,
      "MIN": 0.02,
      "MAX": 5.0
    },
    {
      "NAME": "bulbSize",
      "TYPE": "float",
      "DEFAULT": 0.022,
      "MIN": 0.0,
      "MAX": 2.0
    },
    {
      "NAME": "bulbInset",
      "TYPE": "float",
      "DEFAULT": 0.1,
      "MIN": 0.0,
      "MAX": 2.0
    },
    {
      "NAME": "bulbGlow",
      "TYPE": "float",
      "DEFAULT": 0.3,
      "MIN": 0.0,
      "MAX": 5.0
    },
    {
      "NAME": "bulbOnColor",
      "TYPE": "color",
      "DEFAULT": [
        1.0,
        0.88,
        0.6,
        1.0
      ]
    },
    {
      "NAME": "bulbOffColor",
      "TYPE": "color",
      "DEFAULT": [
        0.18,
        0.13,
        0.06,
        1.0
      ]
    },
    {
      "NAME": "chaseMode",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 6.0
    },
    {
      "NAME": "chaseGroup",
      "TYPE": "float",
      "DEFAULT": 3.0,
      "MIN": 1.0,
      "MAX": 64.0
    },
    {
      "NAME": "chaseDuty",
      "TYPE": "float",
      "DEFAULT": 0.3,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "filament",
      "TYPE": "float",
      "DEFAULT": 0.3,
      "MIN": 0.0,
      "MAX": 1.0
    }
  ]
}*/

float safe(float x, float lo, float hi) { return (x == x) ? clamp(x, lo, hi) : lo; }
float cover(float d, float px) { return smoothstep(px, -px, d); }      // d < 0 inside
float tri(float x) { return abs(fract(x) - 0.5) * 2.0; }               // 1 .. 0 .. 1
float boxD(vec2 p, vec2 c, vec2 h) { vec2 d = abs(p - c) - h; return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0); }
void over(inout vec3 col, inout float a, vec3 c, float ca) {
    float na = ca + a * (1.0 - ca);
    if (na > 0.0001) col = (c * ca + col * a * (1.0 - ca)) / na;
    a = na;
}

float hash(float a, float b) { return fract(sin(a * 12.9898 + b * 78.233) * 43758.5453); }

// ---------- border. Every edge holds a whole number of pattern periods, counted from its corners, so all four corners close the same way ----------
float borderCover(vec2 p, float px, int st, float bw, float A, float H) {
    vec2 q = vec2(A - abs(p.x), H - abs(p.y));          // distance to the nearest vertical / horizontal edge
    bool horiz = q.y <= q.x;
    float w = horiz ? q.y : q.x;                        // distance from the outer edge
    float s = horiz ? q.x : q.y;                        // distance from the nearest corner, along the edge
    float halfLen = horiz ? A : H;
    float rules = min(abs(w - 0.06 * bw) - 0.05 * bw, abs(w - 0.94 * bw) - 0.05 * bw);
    float cov = 0.0;
    float inBand = step(min(q.x, q.y), bw);

    if (st == 3) {                                      // steps: two rules that end in ziggurat corners
        cov = max(cover(abs(w - 0.15 * bw) - 0.06 * bw, px), cover(abs(w - 0.55 * bw) - 0.04 * bw, px)) * inBand;
        for (int i = 0; i < 4; i++) {
            float fi = float(i);
            cov = max(cov, cover(max(q.x - bw * (fi + 1.0) * 1.2, q.y - bw * (4.0 - fi) * 1.2), px));
        }
        return cov;
    }
    if (st == 8) {                                      // sunburst corners: a quarter sun closes each corner, rules run between them
        float R = bw * 5.0;
        float r = length(q);
        if (r >= R) cov = cover(rules, px) * inBand;
        if (r < R + px * 2.0) {
            float ang = atan(q.y, max(q.x, 1e-6));
            float t = ang / 1.5707963 * 9.0;
            float ray = cover((abs(fract(t) - 0.5) - 0.25) * 0.1745 * max(r, 1e-4), px);
            cov = max(cov, ray * cover(r - R, px));
            cov = max(cov, cover(r - R * 0.28, px));
            cov = max(cov, cover(abs(r - R) - 0.06 * bw, px));
        }
        return cov;
    }

    // all other styles: a solid corner piece, then the pattern from corner piece to corner piece
    if (q.x < bw && q.y < bw) return cover(max(q.x - bw, q.y - bw), px);
    if (inBand < 0.5) return cover(max(q.x - bw, q.y - bw), px);
    float u = s - bw;
    float len = max(halfLen - bw, bw * 0.5);

    if (st == 1) {                                      // zigzag
        float P = len / max(floor(len / (bw * 1.5) + 0.5), 1.0);
        float z = 0.2 + 0.6 * tri(u / P);
        cov = cover(abs(w / bw - z) * bw - 0.07 * bw, px);
    } else if (st == 2) {                               // chevron
        float P = len / max(floor(len / (bw * 1.2) + 0.5), 1.0);
        float f = fract(u / P + abs(w / bw - 0.5) * 0.8);
        cov = cover((abs(f - 0.5) - 0.25) * bw, px);
    } else if (st == 4) {                               // greek key
        float P = len / max(floor(len / (bw * 1.6) + 0.5), 1.0);
        vec2 c = vec2(fract(u / P) * 5.0, w / bw * 5.0);
        float t = 0.32;
        float d = boxD(c, vec2(1.0, 2.5), vec2(t, 2.0 + t));
        d = min(d, boxD(c, vec2(2.75, 4.5), vec2(1.75 + t, t)));
        d = min(d, boxD(c, vec2(4.5, 3.0), vec2(t, 1.5 + t)));
        d = min(d, boxD(c, vec2(3.5, 1.5), vec2(1.0 + t, t)));
        d = min(d, boxD(c, vec2(2.5, 2.35), vec2(t, 0.85 + t)));
        cov = cover(d * bw / 5.0, px);
    } else if (st == 5) {                               // diamonds
        float P = len / max(floor(len / (bw * 1.15) + 0.5), 1.0);
        vec2 c = vec2((fract(u / P) - 0.5) * P, w - 0.5 * bw);
        cov = cover(abs(c.x) + abs(c.y) * 0.9 - 0.38 * min(P, bw), px);
    } else if (st == 6) {                               // scallops: arches standing on the outer rule
        float P = len / max(floor(len / (bw * 1.2) + 0.5), 1.0);
        vec2 c = vec2((fract(u / P) - 0.5) * P, w - 0.1 * bw);
        float r = length(c) - P * 0.48;
        cov = cover(max(abs(r) - 0.07 * bw, -c.y), px);
    } else {                                            // beads
        float P = len / max(floor(len / (bw * 0.95) + 0.5), 1.0);
        vec2 c = vec2((fract(u / P) - 0.5) * P, w - 0.5 * bw);
        cov = cover(length(c) - 0.3 * bw, px);
    }
    return max(cov, cover(rules, px));
}

// how far the corner element reaches along an edge: bulbs stay clear of it
float cornerExtent(int st, float bw) {
    if (st <= 0) return 0.0;
    if (st == 3) return bw * 4.9;
    if (st == 8) return bw * 5.2;
    return bw * 1.1;
}

// ---------- chase ----------
float litAt(float k, float n, float phase, int mode, float group, float duty) {
    float g = n / max(floor(n / group + 0.5), 1.0);     // a whole number of groups around the loop: no seam
    if (mode == 0) return step(fract(k / g - phase), duty - 0.0001);                 // run
    if (mode == 1) return 1.0 - abs(mod(k, 2.0) - floor(mod(phase * 2.0, 2.0)));     // alternate
    if (mode == 2) return step(k + 0.5, (1.0 - tri(phase)) * n);                     // fill
    if (mode == 3) return step(abs(k - (1.0 - tri(phase)) * (n - 1.0)), group * 0.5);// bounce
    if (mode == 4) return step(fract(min(k, n - 1.0 - k) / g - phase), duty - 0.0001);// mirror
    if (mode == 5) return step(hash(k, floor(fract(phase) * 16.0)), duty);           // twinkle
    return 0.5 + 0.5 * cos(6.2831853 * (k / g - phase));                             // wave
}

// filament: a lamp glows on after the current is gone, so it follows the last moments of the chase
float bulbLit(float k, float n, float phase, int mode, float group, float duty, float fil) {
    if (fil <= 0.0) return litAt(k, n, phase, mode, group, duty);
    float sum = 0.0, weight = 0.0, w = 1.0;
    for (int j = 0; j < 6; j++) {
        sum += w * litAt(k, n, phase - float(j) / 5.0 * fil * 0.25, mode, group, duty);
        weight += w;
        w *= 0.6;
    }
    return sum / weight;
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec4 src = IMG_NORM_PIXEL(inputImage, uv);
    vec3 col = src.rgb;
    float a = src.a;

    float safeScale = clamp(scale, 0.001, 1000.0);
    vec2 res = max(RENDERSIZE, vec2(1.0));
    float aspect = res.x / res.y;
    float A = aspect / safeScale, H = 1.0 / safeScale;   // half width and half height of the frame in scene units
    vec2 p = (uv - 0.5) * vec2(A, H) * 2.0;
    float px = 2.0 / (res.y * safeScale);

    int style = int(floor(safe(borderStyle, 0.0, 8.0) + 0.5));
    float bw = safe(borderWidth, 0.0, 100.0);
    if (style > 0 && bw > 0.0) over(col, a, borderColor.rgb, borderCover(p, px, style, bw, A, H) * borderColor.a);

    int lay = int(floor(safe(bulbLayout, 0.0, 2.0) + 0.5));
    float size = safe(bulbSize, 0.0, 100.0);
    if (lay > 0 && size > 0.0) {
        float spacing = max(safe(bulbSpacing, 0.02, 100.0), size * 0.5);
        float inset = safe(bulbInset, 0.0, 100.0);
        float glowAmt = safe(bulbGlow, 0.0, 100.0);
        float phase = (clock == clock) ? clock : 0.0;
        int mode = int(floor(safe(chaseMode, 0.0, 6.0) + 0.5));
        float group = max(floor(safe(chaseGroup, 1.0, 1000.0) + 0.5), 1.0);
        float duty = safe(chaseDuty, 0.0, 1.0);
        float fil = safe(filament, 0.0, 1.0);

        float ext = style > 0 && bw > 0.0 ? cornerExtent(style, bw) : 0.0;
        float clear = inset < ext ? ext + size * 1.6 : size * 1.5;
        float y0 = max(H - inset, 0.0), x0 = max(A - inset, 0.0);
        float xs = max(inset < ext ? A - clear : (lay == 2 ? x0 : A - clear), 0.0);
        float ys = max(inset < ext ? H - clear : y0 - spacing, 0.0);
        if (lay == 1) ys = 0.0;

        float nH = floor(2.0 * xs / spacing + 0.5) + 1.0;
        float stepH = nH > 1.0 ? 2.0 * xs / (nH - 1.0) : 1.0;
        bool sides = lay == 2 && ys > 0.0;
        float nV = sides ? floor(2.0 * ys / spacing + 0.5) + 1.0 : 0.0;
        float stepV = nV > 1.0 ? 2.0 * ys / (nV - 1.0) : 1.0;
        float total = max(2.0 * nH + 2.0 * nV, 1.0);
        float reach = size * 12.0 + stepH;

        float glow = 0.0, discLit = 0.0, bestD = 1e5;
        for (int e = 0; e < 4; e++) {
            if ((e == 1 || e == 3) && !sides) continue;
            bool vertical = e == 1 || e == 3;
            float n = vertical ? nV : nH;
            float st = vertical ? stepV : stepH;
            float along = e == 0 ? p.x + xs : e == 1 ? ys - p.y : e == 2 ? xs - p.x : p.y + ys;
            float base = e == 0 ? 0.0 : e == 1 ? nH : e == 2 ? nH + nV : 2.0 * nH + nV;
            float i0 = clamp(floor(along / st + 0.5), 0.0, n - 1.0);
            for (int o = -1; o <= 1; o++) {
                float i = i0 + float(o);
                if (i < 0.0 || i > n - 1.0) continue;
                vec2 bp = e == 0 ? vec2(-xs + i * st, y0) : e == 1 ? vec2(x0, ys - i * st) : e == 2 ? vec2(xs - i * st, -y0) : vec2(-x0, -ys + i * st);
                float d = length(p - bp);
                if (d > reach) continue;
                float lit = bulbLit(base + i, total, phase, mode, group, duty, fil);
                glow = max(glow, lit * exp(-max(d - size, 0.0) / max(size * 2.5, 1e-5)));
                if (d < bestD) { bestD = d; discLit = lit; }
            }
        }
        over(col, a, bulbOnColor.rgb, clamp(glow * glowAmt * 0.8, 0.0, 1.0) * bulbOnColor.a);
        over(col, a, mix(bulbOffColor.rgb, bulbOnColor.rgb, discLit), cover(bestD - size, px) * mix(bulbOffColor.a, bulbOnColor.a, discLit));
    }

    gl_FragColor = a > 0.0001 ? vec4(col, clamp(a, 0.0, 1.0)) : vec4(0.0);
}
