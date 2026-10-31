/*{
    "DESCRIPTION": "Fire & Smoke - simulation step. inputImage = last state (feedback): R = heat, G = smoke, B/A = velocity (sim pixels per frame). maskImage = the clip under the effect (bright = burns). Pixel (0,0) keeps the bookkeeping (flare edge, frame counter, flare boost).",
    "CREDIT": "Fire & Smoke",
    "ISFVSN": "2",
    "CATEGORIES": ["Effect"],
    "INPUTS": [
        {"NAME": "inputImage", "TYPE": "image"},
        {"NAME": "maskImage", "TYPE": "image"},
        {"NAME": "bottomOn", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "topOn", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "leftOn", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "rightOn", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "edgeFrom", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "edgeTo", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "gaps", "TYPE": "float", "DEFAULT": 30.0, "MIN": 0.0, "MAX": 100.0},
        {"NAME": "seed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 999.0},
        {"NAME": "pointOn", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "pointX", "TYPE": "float", "DEFAULT": 0.0, "MIN": -1.0, "MAX": 1.0},
        {"NAME": "pointY", "TYPE": "float", "DEFAULT": -0.5, "MIN": -1.0, "MAX": 1.0},
        {"NAME": "pointSize", "TYPE": "float", "DEFAULT": 0.05, "MIN": 0.005, "MAX": 0.3},
        {"NAME": "maskOn", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "maskLevel", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "flame", "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "smoke", "TYPE": "float", "DEFAULT": 0.25, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "flameHeight", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "smokeLife", "TYPE": "float", "DEFAULT": 1.5, "MIN": 0.2, "MAX": 15.0},
        {"NAME": "rise", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "windAngle", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 360.0},
        {"NAME": "windStrength", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "turbulence", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "flare", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0}
    ]
}*/

float hash3(vec3 p) {
    p = fract(p * vec3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return fract((p.x + p.y) * p.z);
}

float vnoise(vec3 p) {
    vec3 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = mix(mix(hash3(i), hash3(i + vec3(1, 0, 0)), f.x), mix(hash3(i + vec3(0, 1, 0)), hash3(i + vec3(1, 1, 0)), f.x), f.y);
    float b = mix(mix(hash3(i + vec3(0, 0, 1)), hash3(i + vec3(1, 0, 1)), f.x), mix(hash3(i + vec3(0, 1, 1)), hash3(i + vec3(1, 1, 1)), f.x), f.y);
    return mix(a, b, f.z);
}

// state lookup with manual bilinear filtering from texel centres; bookkeeping pixel reads as empty
vec4 S(vec2 px, vec2 sz) {
    px = clamp(px, vec2(0.5), sz - 0.5);
    vec2 p = px - 0.5;
    vec2 i = floor(p);
    vec2 f = p - i;
    vec2 c = i + 0.5;
    vec4 a = IMG_NORM_PIXEL(inputImage, c / sz);
    vec4 b = IMG_NORM_PIXEL(inputImage, (c + vec2(1.0, 0.0)) / sz);
    vec4 d = IMG_NORM_PIXEL(inputImage, (c + vec2(0.0, 1.0)) / sz);
    vec4 e = IMG_NORM_PIXEL(inputImage, (c + vec2(1.0, 1.0)) / sz);
    if (c.y < 1.0 && c.x < 1.0) a = vec4(0.0);
    if (c.y < 1.0 && c.x + 1.0 < 1.0) b = vec4(0.0);
    return mix(mix(a, b, f.x), mix(d, e, f.x), f.y);
}

void main() {
    vec2 sz = IMG_SIZE(inputImage);
    vec2 px = floor(isf_FragNormCoord * sz) + 0.5;
    vec2 uv = px / sz;
    float aspect = sz.x / sz.y;

    // ----- bookkeeping: flare edge, frame counter, flare boost -----
    vec4 book = IMG_NORM_PIXEL(inputImage, vec2(0.5, 0.5) / sz);
    float frame = book.g + 1.0;
    float boost = book.b * 0.93;
    if (flare > 0.5 && book.r <= 0.5) boost = 1.0;
    if (px.x < 1.0 && px.y < 1.0) { gl_FragColor = vec4(flare, mod(frame, 100000.0), boost, 0.0); return; }

    // ----- advection (semi-Lagrangian) with a little diffusion for soft edges -----
    vec4 here = S(px, sz);
    vec2 vel = here.ba;
    vec2 bp = px - vel;
    vec4 st = S(bp, sz);
    vec4 nb = (S(bp + vec2(1.0, 0.0), sz) + S(bp - vec2(1.0, 0.0), sz) + S(bp + vec2(0.0, 1.0), sz) + S(bp - vec2(0.0, 1.0), sz)) * 0.25;
    st = mix(st, nb, vec4(0.12, 0.45, 0.2, 0.2));
    float heat = st.r, smk = st.g;
    vec2 v = st.ba;

    // ----- forces -----
    float t = frame * 0.015;
    v.y += rise * (0.08 + 0.25 * heat + 0.03 * min(smk, 1.0));                  // updraft everywhere (stands in for pressure) + hot gas rises faster
    float wa = radians(windAngle);
    vec2 wind = vec2(cos(wa), sin(wa)) * windStrength * 8.0;
    v += (wind - v) * 0.06 * windStrength;                               // wind drags everything along
    // swirls: curl of a changing noise field, two scales (big rolls + licking tongues)
    vec2 q = uv * vec2(aspect, 1.0);
    float e = 0.02;
    vec2 curl = vec2(0.0);
    for (int o = 0; o < 2; o++) {
        float sc = o == 0 ? 4.0 : 22.0;
        float tt = o == 0 ? t : t * 3.0;
        float n1 = vnoise(vec3((q + vec2(0.0, e)) * sc, tt)), n2 = vnoise(vec3((q - vec2(0.0, e)) * sc, tt));
        float n3 = vnoise(vec3((q + vec2(e, 0.0)) * sc, tt)), n4 = vnoise(vec3((q - vec2(e, 0.0)) * sc, tt));
        curl += vec2(n1 - n2, -(n3 - n4)) / (2.0 * e * sc) * (o == 0 ? 0.6 : 1.4);
    }
    v += curl * turbulence * 0.2 * (0.35 + min(heat * 2.0 + smk, 1.5));
    v *= 0.985;
    v = clamp(v, vec2(-8.0), vec2(8.0));

    // ----- cooling, smoke from fire, smoke dissolving -----
    smk += heat * 0.005 * smoke;
    heat *= mix(0.86, 0.97, flameHeight);
    smk *= exp(-1.0 / (smokeLife * 60.0));

    // ----- sources -----
    float w = 2.5;                                                       // edge band in sim pixels
    float gp = gaps / 100.0;
    float em = 0.0;
    vec2 push = vec2(0.0);
    // along-edge position, band test, gap noise per edge
    if (bottomOn > 0.5 && px.y < w) {
        float s = uv.x; float g = vnoise(vec3(s * 25.0, seed, t * 6.0));
        float k = step(edgeFrom, s) * step(s, edgeTo) * smoothstep(gp - 0.1, gp + 0.1, g);
        em = max(em, k); push += vec2(0.0, 1.0) * k;
    }
    if (topOn > 0.5 && px.y > sz.y - w) {
        float s = uv.x; float g = vnoise(vec3(s * 25.0, seed + 17.0, t * 6.0));
        float k = step(edgeFrom, s) * step(s, edgeTo) * smoothstep(gp - 0.1, gp + 0.1, g);
        em = max(em, k); push += vec2(0.0, -1.0) * k;
    }
    if (leftOn > 0.5 && px.x < w) {
        float s = uv.y; float g = vnoise(vec3(s * 25.0, seed + 31.0, t * 6.0));
        float k = step(edgeFrom, s) * step(s, edgeTo) * smoothstep(gp - 0.1, gp + 0.1, g);
        em = max(em, k); push += vec2(1.0, 0.0) * k;
    }
    if (rightOn > 0.5 && px.x > sz.x - w) {
        float s = uv.y; float g = vnoise(vec3(s * 25.0, seed + 47.0, t * 6.0));
        float k = step(edgeFrom, s) * step(s, edgeTo) * smoothstep(gp - 0.1, gp + 0.1, g);
        em = max(em, k); push += vec2(-1.0, 0.0) * k;
    }
    if (pointOn > 0.5) {
        vec2 c = vec2(0.5 + 0.5 * pointX, 0.5 + 0.5 * pointY);
        vec2 d = (uv - c) * vec2(aspect, 1.0);
        em = max(em, 1.0 - smoothstep(pointSize * 0.6, pointSize, length(d)));
    }
    if (maskOn > 0.5) {
        vec4 m = IMG_NORM_PIXEL(maskImage, uv);
        float lum = dot(m.rgb, vec3(0.299, 0.587, 0.114)) * m.a;
        em = max(em, smoothstep(maskLevel - 0.08, maskLevel + 0.08, lum));
    }

    float flick = 0.35 + 0.65 * vnoise(vec3(px.x * 0.12, px.y * 0.05, frame * 0.2));
    float amp = 1.0 + 1.5 * boost;
    heat = max(heat, em * flame * flick * amp * 1.4);
    smk += em * smoke * 0.03 * amp * (1.0 - 0.85 * flame);           // direct smoke mainly when there is no flame
    v += push * (0.3 + flame) * 0.5 * amp;
    v.y += em * rise * boost * 1.5;

    gl_FragColor = vec4(min(heat, 2.0), min(smk, 3.0), v);
}
