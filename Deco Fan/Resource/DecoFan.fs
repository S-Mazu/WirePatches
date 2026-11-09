/*{
  "DESCRIPTION": "Deco Fan - one Art Deco fan of rays and rings, on top of the clip below. Add the effect several times for several fans; the order of the effects decides which lies in front. Nothing moves by itself: automate position, Fill or Rotation in Arena. Scale sizes everything relative to the output height. Fan: Fan X and Fan Y (0 = centre, -1 and 1 = frame edges), Fan Size, Fan Rotation (the direction the fan opens to, 0 = up), Fan Fill (how far it is open: 180 = half circle, 360 = full circle). Rays: Ray Count (rays in a full circle), Ray Length, Ray Width, Ray Style (Straight, Wedge, Long-Short, Tapered, Skyscraper, Beaded, Feather), Ray Color. Rings: Ring Count (fractional values fade the next ring in), Ring Start, Ring Gap, Ring Width, Ring Style (Arch, Stepped, Chevron, Scallop, Beads, Double, Key), Ring Color. Shader: Resource/DecoFan.fs.",
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
      "NAME": "scale",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "fanX",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -5.0,
      "MAX": 5.0
    },
    {
      "NAME": "fanY",
      "TYPE": "float",
      "DEFAULT": -1.0,
      "MIN": -5.0,
      "MAX": 5.0
    },
    {
      "NAME": "fanSize",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "fanRotation",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -360.0,
      "MAX": 360.0
    },
    {
      "NAME": "fanFill",
      "TYPE": "float",
      "DEFAULT": 180.0,
      "MIN": 0.0,
      "MAX": 360.0
    },
    {
      "NAME": "rayCount",
      "TYPE": "float",
      "DEFAULT": 40.0,
      "MIN": 0.0,
      "MAX": 240.0
    },
    {
      "NAME": "rayLength",
      "TYPE": "float",
      "DEFAULT": 1.15,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "rayWidth",
      "TYPE": "float",
      "DEFAULT": 0.03,
      "MIN": 0.0,
      "MAX": 2.0
    },
    {
      "NAME": "rayStyle",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 6.0
    },
    {
      "NAME": "rayColor",
      "TYPE": "color",
      "DEFAULT": [
        0.83,
        0.66,
        0.25,
        1.0
      ]
    },
    {
      "NAME": "ringCount",
      "TYPE": "float",
      "DEFAULT": 4.0,
      "MIN": 0.0,
      "MAX": 16.0
    },
    {
      "NAME": "ringStart",
      "TYPE": "float",
      "DEFAULT": 0.3,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "ringGap",
      "TYPE": "float",
      "DEFAULT": 0.2,
      "MIN": 0.01,
      "MAX": 5.0
    },
    {
      "NAME": "ringWidth",
      "TYPE": "float",
      "DEFAULT": 0.02,
      "MIN": 0.0,
      "MAX": 2.0
    },
    {
      "NAME": "ringStyle",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 6.0
    },
    {
      "NAME": "ringColor",
      "TYPE": "color",
      "DEFAULT": [
        0.95,
        0.88,
        0.7,
        1.0
      ]
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

void main() {
    vec2 uv = isf_FragNormCoord;
    vec4 src = IMG_NORM_PIXEL(inputImage, uv);
    vec3 col = src.rgb;
    float a = src.a;

    float safeScale = clamp(scale, 0.001, 1000.0);
    vec2 res = max(RENDERSIZE, vec2(1.0));
    float aspect = res.x / res.y;
    float A = aspect / safeScale, H = 1.0 / safeScale;
    vec2 p = (uv - 0.5) * vec2(A, H) * 2.0;

    float size = safe(fanSize, 0.0, 1000.0);
    float fill = safe(fanFill, 0.0, 360.0);
    if (size > 0.0 && fill > 0.0) {
        vec2 q = (p - vec2(safe(fanX, -1000.0, 1000.0) * A, safe(fanY, -1000.0, 1000.0) * H)) / size;
        float px = 2.0 / (res.y * safeScale) / size;
        float r = length(q);
        float rot = safe(fanRotation, -100000.0, 100000.0);
        float ra = r > 1e-6 ? mod(degrees(atan(q.x, q.y)) - rot + 180.0, 360.0) - 180.0 : 0.0;   // angle from the direction the fan opens to
        float inFill = fill >= 359.9 ? 1.0 : cover(radians(abs(ra) - fill * 0.5) * max(r, 1e-3), px);

        // ---------- rays: Ray Count = rays in a full circle, Fill reveals them ----------
        float n = floor(safe(rayCount, 0.0, 10000.0) + 0.5);
        if (n >= 1.0) {
            float len = safe(rayLength, 0.0, 1000.0);
            float width = safe(rayWidth, 0.0, 1000.0);
            int st = int(floor(safe(rayStyle, 0.0, 6.0) + 0.5));
            float pitch = 360.0 / n;
            float t = ra / pitch;
            float k = floor(t + 0.5);
            float dAng = radians((t - k) * pitch);
            float across = abs(sin(dAng)) * r;
            float fr = clamp(r / max(len, 1e-3), 0.0, 1.0);
            float w = width * 0.5;
            float l = len;
            float d = 1.0;
            if (st == 1) w = width * fr;                                       // wedge
            if (st == 2 && mod(k, 2.0) > 0.5) l = len * 0.62;                  // long-short
            if (st == 3) w = width * (1.0 - fr);                               // tapered: narrows to a point
            if (st == 4) w = width * 0.5 * (4.0 - floor(fr * 4.0)) * 0.5;      // skyscraper: set back in steps
            if (st == 6) w = width * (0.28 + 0.9 * exp(-pow((fr - 0.8) / 0.13, 2.0)));   // feather: slim shaft, broad tip
            d = max(across - w, r - l);
            if (st == 5) {                                                     // beaded: thin line with beads along it
                float spacing = max(width * 4.0, 0.01);
                float along = (fract(r / spacing) - 0.5) * spacing;
                d = max(min(across - width * 0.12, length(vec2(across, along)) - width * 0.9), r - l);
            }
            if (abs(dAng) > 1.4) d = 1.0;                                       // far side of a wide pitch
            over(col, a, rayColor.rgb, cover(d, px) * inFill * rayColor.a);
        }

        // ---------- rings ----------
        float ringN = safe(ringCount, 0.0, 16.0);
        if (ringN > 0.0) {
            float start = safe(ringStart, 0.0, 1000.0);
            float gap = safe(ringGap, 0.001, 1000.0);
            float hw = safe(ringWidth, 0.0, 1000.0) * 0.5;
            int st = int(floor(safe(ringStyle, 0.0, 6.0) + 0.5));
            for (int j = 0; j < 16; j++) {
                float fj = float(j);
                float vis = clamp(ringN - fj, 0.0, 1.0);
                if (vis <= 0.0) break;
                float R = start + fj * gap;
                float d;
                if (st == 0) {                                                  // arch
                    d = abs(r - R) - hw;
                } else if (st == 1) {                                           // stepped
                    float sIdx = floor(abs(ra) / 18.0);
                    float Rs = R * (1.0 - 0.06 * sIdx);
                    float Rn = R * (1.0 - 0.06 * (sIdx + 1.0));
                    float side = abs(sin(radians(abs(ra) - (sIdx + 1.0) * 18.0))) * r;
                    d = min(abs(r - Rs) - hw, max(side - hw, max(r - Rs - hw, Rn - hw - r)));
                } else if (st == 2) {                                           // chevron
                    float amp = gap * 0.22;
                    d = abs(r - (R + amp * (tri(ra / 10.0) * 2.0 - 1.0))) - hw * 1.3;
                } else if (st == 3) {                                           // scallop: arches along the ring
                    d = abs(r - (R + gap * 0.3 * abs(cos(radians(ra) * 9.0)))) - hw;
                } else if (st == 4) {                                           // beads
                    float count = max(floor(6.2831853 * R / max(gap * 0.5, 0.005) + 0.5), 3.0);
                    float cell = 360.0 / count;
                    float dA = radians(ra - (floor(ra / cell) + 0.5) * cell);
                    d = length(vec2(dA * r, r - R)) - max(hw * 2.0, gap * 0.1);
                } else if (st == 5) {                                           // double line
                    d = min(abs(r - R + hw * 1.8) - hw * 0.6, abs(r - R - hw * 1.8) - hw * 0.6);
                } else {                                                        // key: a square wave around the ring
                    float cell = 12.0;
                    float idx = floor(ra / cell);
                    float amp = gap * 0.22;
                    float sgn = mod(idx, 2.0) < 0.5 ? 1.0 : -1.0;
                    float line = abs(r - (R + sgn * amp)) - hw;
                    float edgeA = radians(ra - (idx + (mod(ra, cell) > cell * 0.5 ? 1.0 : 0.0)) * cell);
                    float riser = max(abs(sin(edgeA)) * r - hw, abs(r - R) - amp - hw);
                    d = min(line, riser);
                }
                over(col, a, ringColor.rgb, cover(d, px) * inFill * vis * ringColor.a);
            }
        }
    }

    gl_FragColor = a > 0.0001 ? vec4(col, clamp(a, 0.0, 1.0)) : vec4(0.0);
}
