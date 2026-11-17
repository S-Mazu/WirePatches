/*{
  "DESCRIPTION": "Field Viewer - shows the Motion Bridge field texture as it arrives in a patch and measures what the bridge contract promises: input and render size, where the status band is, whether its counter keeps changing, the update rate and the time since the last update. Diagnostic, no show content.",
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
      "NAME": "channel",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 3.0
    },
    {
      "NAME": "showInfo",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 1.0
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
    }
  ],
  "PASSES": [
    {
      "TARGET": "state",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "3",
      "HEIGHT": "1"
    },
    {
    }
  ]
}*/

// The contract of the Motion Bridge (tools/Motion Bridge/README.md, Output): 15/16 of the rows are
// the field (R occupancy, G motion, B visibility), 1/16 is the status band, whose every pixel holds
// the same counter in R, G and B and changes every frame. The viewer does not assume where the band
// is: it looks for it at the top and at the bottom edge and reports what it finds.
//
// state (3 x 1, persistent), each pixel with a signature in alpha:
//   pixel 0: now (s), last counter, time of the last counter change (s)
//   pixel 1: start of the rate window (s), counter steps in the window, measured rate (steps/s)
//   pixel 2: band side (+1 top, -1 bottom, 0 none)

const float SIG = 0.73;
const float LSB = 1.0 / 255.0;

float safe(float v, float lo, float hi) {
    if (!(v == v) || abs(v) > 1e9) return lo;          // NaN or Inf
    return clamp(v, lo, hi);
}

// ---------- 3 x 5 pixel font ----------
float digitGlyph(float d) {
    if (d < 0.5) return 31599.0;
    if (d < 1.5) return 29850.0;
    if (d < 2.5) return 29671.0;
    if (d < 3.5) return 31207.0;
    if (d < 4.5) return 18925.0;
    if (d < 5.5) return 31183.0;
    if (d < 6.5) return 31695.0;
    if (d < 7.5) return 18727.0;
    if (d < 8.5) return 31727.0;
    if (d < 9.5) return 31215.0;
    return 0.0;
}

const float GLYPH_X = 2728.0;

// words: 0 IN, 1 OUT, 2 BAND, 3 FPS, 4 AGE MS, 5 COUNT, 6 TOP, 7 BOTTOM, 8 NONE
float wordGlyph(int w, int i) {
    if (w == 0) { if (i == 0) return 29847.0; if (i == 1) return 23403.0; return 0.0; }
    if (w == 1) { if (i == 0) return 11114.0; if (i == 1) return 31597.0; if (i == 2) return 9367.0; return 0.0; }
    if (w == 2) { if (i == 0) return 15083.0; if (i == 1) return 23530.0; if (i == 2) return 23403.0; if (i == 3) return 15211.0; return 0.0; }
    if (w == 3) { if (i == 0) return 4815.0; if (i == 1) return 4843.0; if (i == 2) return 14478.0; return 0.0; }
    if (w == 4) { if (i == 0) return 23530.0; if (i == 1) return 31567.0; if (i == 2) return 29391.0; if (i == 4) return 23549.0; if (i == 5) return 14478.0; return 0.0; }
    if (w == 5) { if (i == 0) return 29263.0; if (i == 1) return 11114.0; if (i == 2) return 31597.0; if (i == 3) return 23403.0; if (i == 4) return 9367.0; return 0.0; }
    if (w == 6) { if (i == 0) return 9367.0; if (i == 1) return 11114.0; if (i == 2) return 4843.0; return 0.0; }
    if (w == 7) { if (i == 0) return 15083.0; if (i == 1) return 11114.0; if (i == 2) return 9367.0; if (i == 3) return 9367.0; if (i == 4) return 11114.0; if (i == 5) return 23549.0; return 0.0; }
    if (w == 8) { if (i == 0) return 23403.0; if (i == 1) return 11114.0; if (i == 2) return 23403.0; if (i == 3) return 29391.0; return 0.0; }
    return 0.0;
}

float numDigits(float n) {
    if (n < 10.0) return 1.0;
    if (n < 100.0) return 2.0;
    if (n < 1000.0) return 3.0;
    if (n < 10000.0) return 4.0;
    return 5.0;
}

// glyph of character j of the integer n (0 <= n <= 99999), 0 past its end
float numGlyph(float n, int j) {
    float d = numDigits(n);
    float fj = float(j);
    if (fj >= d) return 0.0;
    return digitGlyph(mod(floor(n / pow(10.0, d - 1.0 - fj) + 0.001), 10.0));
}

// glyph of "w x h" at character j
float sizeGlyph(vec2 s, int j) {
    float dw = numDigits(s.x);
    float fj = float(j);
    if (fj < dw) return numGlyph(s.x, j);
    if (fj < dw + 1.0) return GLYPH_X;
    return numGlyph(s.y, j - int(dw) - 1);
}

// 1 where the glyph has a pixel at lp (glyph pixels, origin bottom left, 3 wide, 5 high)
float glyphBit(float code, vec2 lp) {
    if (code <= 0.0 || lp.x < 0.0 || lp.y < 0.0 || lp.x >= 3.0 || lp.y >= 5.0) return 0.0;
    float col = floor(lp.x);
    float row = 4.0 - floor(lp.y);
    return mod(floor(code / exp2(row * 3.0 + col) + 0.001), 2.0);
}

void main() {
    vec2 uv = isf_FragNormCoord;

    // ---------- pass 0: measurements over time ----------
    if (PASSINDEX == 0) {
        vec4 s0 = IMG_NORM_PIXEL(state, vec2(0.5 / 3.0, 0.5));
        vec4 s1 = IMG_NORM_PIXEL(state, vec2(1.5 / 3.0, 0.5));
        vec4 s2 = IMG_NORM_PIXEL(state, vec2(2.5 / 3.0, 0.5));
        float now = clockH * 3600.0 + clockM * 60.0 + clockS + clockMs * 0.001;
        bool fresh = !(abs(s0.a - SIG) < 0.01) || !(abs(s1.a - SIG) < 0.01) || !(abs(s2.a - SIG) < 0.01) || now < s0.r;   // first frame or midnight
        if (fresh) {
            s0 = vec4(now, -1.0, now - 1000.0, SIG);
            s1 = vec4(now, 0.0, 0.0, SIG);
            s2 = vec4(0.0, 0.0, 0.0, SIG);
        }

        // the band: R = G = B > 0, equal across the width; sampled at the middle of the top and the bottom sixteenth
        vec3 t0 = IMG_NORM_PIXEL(inputImage, vec2(0.125, 31.0 / 32.0)).rgb;
        vec3 t1 = IMG_NORM_PIXEL(inputImage, vec2(0.375, 31.0 / 32.0)).rgb;
        vec3 t2 = IMG_NORM_PIXEL(inputImage, vec2(0.625, 31.0 / 32.0)).rgb;
        vec3 t3 = IMG_NORM_PIXEL(inputImage, vec2(0.875, 31.0 / 32.0)).rgb;
        vec3 b0 = IMG_NORM_PIXEL(inputImage, vec2(0.125, 1.0 / 32.0)).rgb;
        vec3 b1 = IMG_NORM_PIXEL(inputImage, vec2(0.375, 1.0 / 32.0)).rgb;
        vec3 b2 = IMG_NORM_PIXEL(inputImage, vec2(0.625, 1.0 / 32.0)).rgb;
        vec3 b3 = IMG_NORM_PIXEL(inputImage, vec2(0.875, 1.0 / 32.0)).rgb;
        float tol = 1.5 * LSB;
        vec3 tMin = min(min(t0, t1), min(t2, t3)), tMax = max(max(t0, t1), max(t2, t3));
        vec3 bMin = min(min(b0, b1), min(b2, b3)), bMax = max(max(b0, b1), max(b2, b3));
        float tSpread = max(max(tMax.r, tMax.g), tMax.b) - min(min(tMin.r, tMin.g), tMin.b);
        float bSpread = max(max(bMax.r, bMax.g), bMax.b) - min(min(bMin.r, bMin.g), bMin.b);
        bool topBand = tSpread < tol && tMin.r > 0.5 * LSB;
        bool botBand = bSpread < tol && bMin.r > 0.5 * LSB;

        float side = safe(s2.r, -1.0, 1.0);
        if (topBand && !botBand) side = 1.0;
        if (botBand && !topBand) side = -1.0;
        float counter = side > 0.5 ? (t0.r + t1.r + t2.r + t3.r) * 0.25 : side < -0.5 ? (b0.r + b1.r + b2.r + b3.r) * 0.25 : -1.0;
        bool found = (side > 0.5 && topBand) || (side < -0.5 && botBand);

        float lastCounter = safe(s0.g, -1.0, 1.0);
        float lastChange = safe(s0.b, now - 100000.0, now);
        float winStart = safe(s1.r, now - 100000.0, now);
        float changes = safe(s1.g, 0.0, 100000.0);
        float rate = safe(s1.b, 0.0, 1000.0);
        if (found && abs(counter - lastCounter) > 0.5 * LSB) {
            // count the counter's steps, not the frames with a change: Arena may render fewer
            // frames than the bridge sends, then one change covers several bridge frames
            float steps = mod(floor((counter - lastCounter) * 255.0 + 0.5) + 256.0, 256.0);
            if (lastCounter >= 0.0 && now - lastChange < 1.0) changes += steps;
            lastChange = now;
            lastCounter = counter;
        }
        if (now - lastChange > 1.0) side = 0.0;                          // no live band for a second
        if (now - winStart >= 1.0) {
            rate = changes / (now - winStart);
            changes = 0.0;
            winStart = now;
        }
        float idx = floor(uv.x * 3.0);
        gl_FragColor = idx < 0.5 ? vec4(now, lastCounter, lastChange, SIG)
                     : idx < 1.5 ? vec4(winStart, changes, rate, SIG)
                     : vec4(side, 0.0, 0.0, SIG);
        return;
    }

    // ---------- pass 1: the picture ----------
    vec4 s0 = IMG_NORM_PIXEL(state, vec2(0.5 / 3.0, 0.5));
    vec4 s1 = IMG_NORM_PIXEL(state, vec2(1.5 / 3.0, 0.5));
    vec4 s2 = IMG_NORM_PIXEL(state, vec2(2.5 / 3.0, 0.5));
    vec4 f = IMG_NORM_PIXEL(inputImage, uv);
    vec2 inSize = IMG_SIZE(inputImage);

    int ch = int(channel + 0.5);
    vec3 col = f.rgb;
    if (ch == 1) col = vec3(f.r);
    if (ch == 2) col = vec3(f.g);
    if (ch == 3) col = vec3(f.b);

    if (showInfo > 0.5) {
        vec2 res = max(RENDERSIZE, vec2(1.0));
        float now = s0.r;
        float age = safe(now - s0.b, 0.0, 99.999);
        float rate = safe(s1.b, 0.0, 999.0);
        float side = safe(s2.r, -1.0, 1.0);
        float counter = safe(s0.g, 0.0, 1.0);
        float sizeOk = (abs(inSize.x - res.x) < 1.5 && abs(inSize.y - res.y) < 1.5) ? 1.0 : 0.0;

        // the band: a line at its inner edge, its counter as a bar
        float line = max(2.0 / res.y, 0.002);
        if (side > 0.5) {
            if (uv.y >= 15.0 / 16.0 && uv.x < counter) col = mix(col, vec3(1.0, 0.9, 0.3), 0.6);
            if (abs(uv.y - 15.0 / 16.0) < line) col = vec3(1.0, 1.0, 0.0);
        } else if (side < -0.5) {
            if (uv.y <= 1.0 / 16.0 && uv.x < counter) col = mix(col, vec3(1.0, 0.9, 0.3), 0.6);
            if (abs(uv.y - 1.0 / 16.0) < line) col = vec3(1.0, 0.5, 0.0);
        }

        // the panel: six lines of text, a light in front of each
        vec2 p = uv * res;
        float u = max(floor(min(res.x, res.y) * 0.009), 1.0);    // one font pixel
        float lineH = 8.0 * u;
        float top = res.y * 0.5 + 3.0 * lineH;
        float x0 = 4.0 * u;
        vec2 box = vec2(x0 + (4.0 + 17.0 * 4.0) * u, 6.0 * lineH + 2.0 * u);
        if (p.x < box.x && p.y < top + u && p.y > top - box.y + u) {
            col = mix(col, vec3(0.0), 0.75);
            float k = floor((top - p.y) / lineH);                 // line 0 at the top
            float ly = (top - p.y) - k * lineH;                   // 0..lineH from the top of the line
            vec2 lp = vec2(p.x - x0, (5.0 * u - ly)) / u;          // font pixels, y up, 0..5 on the glyph
            int line_ = int(k);
            vec3 green = vec3(0.2, 0.9, 0.3), orange = vec3(1.0, 0.6, 0.1), red = vec3(1.0, 0.15, 0.1);
            vec3 light = red;
            if (line_ <= 1) light = sizeOk > 0.5 ? green : red;
            if (line_ == 2) light = side > 0.5 ? green : (side < -0.5 ? orange : red);
            if (line_ == 3) light = (rate > 20.0 && rate < 40.0) ? green : (rate > 0.5 ? orange : red);
            if (line_ == 4) light = age < 0.25 ? green : red;
            if (line_ == 5) light = age < 0.25 ? green : red;
            if (lp.x >= 0.0 && lp.x < 3.0 && lp.y >= 0.0 && lp.y < 5.0 && line_ >= 0 && line_ <= 5) col = light;

            vec2 tp = lp - vec2(4.0, 0.0);                         // text starts after the light
            int j = int(floor(tp.x / 4.0));
            vec2 gp = vec2(tp.x - float(j) * 4.0, tp.y);
            float code = 0.0;
            if (tp.x >= 0.0 && j < 7) {
                int w = line_ <= 5 ? line_ : -1;
                code = wordGlyph(w, j);
            } else if (j >= 7) {
                int jj = j - 7;
                if (line_ == 0) code = sizeGlyph(inSize, jj);
                if (line_ == 1) code = sizeGlyph(res, jj);
                if (line_ == 2) code = wordGlyph(side > 0.5 ? 6 : (side < -0.5 ? 7 : 8), jj);
                if (line_ == 3) code = numGlyph(floor(rate + 0.5), jj);
                if (line_ == 4) code = numGlyph(floor(age * 1000.0), jj);
                if (line_ == 5) code = numGlyph(floor(counter * 255.0 + 0.5), jj);
            }
            if (glyphBit(code, gp) > 0.5) col = vec3(1.0);
        }
    }

    gl_FragColor = vec4(col, 1.0);
}
