/*{
    "DESCRIPTION": "Fire & Smoke - renders heat as flames and smoke density as soft smoke, over the input clip or on transparent.",
    "CREDIT": "Fire & Smoke",
    "ISFVSN": "2",
    "CATEGORIES": ["Effect"],
    "INPUTS": [
        {"NAME": "inputImage", "TYPE": "image"},
        {"NAME": "sourceImage", "TYPE": "image"},
        {"NAME": "showInput", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "flameCore", "TYPE": "color", "DEFAULT": [1.0, 0.95, 0.75, 1.0]},
        {"NAME": "flameMid", "TYPE": "color", "DEFAULT": [1.0, 0.55, 0.1, 1.0]},
        {"NAME": "flameTip", "TYPE": "color", "DEFAULT": [0.75, 0.12, 0.03, 1.0]},
        {"NAME": "smokeColor", "TYPE": "color", "DEFAULT": [0.5, 0.48, 0.46, 1.0]}
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

// bilinear state lookup from texel centres (independent of host filtering)
vec2 S(vec2 uv, vec2 sz) {
    vec2 p = clamp(uv * sz, vec2(0.5), sz - 0.5) - 0.5;
    vec2 i = floor(p);
    vec2 f = p - i;
    vec2 c = i + 0.5;
    vec2 a = IMG_NORM_PIXEL(inputImage, c / sz).rg;
    vec2 b = IMG_NORM_PIXEL(inputImage, (c + vec2(1.0, 0.0)) / sz).rg;
    vec2 d = IMG_NORM_PIXEL(inputImage, (c + vec2(0.0, 1.0)) / sz).rg;
    vec2 e = IMG_NORM_PIXEL(inputImage, (c + vec2(1.0, 1.0)) / sz).rg;
    if (c.y < 1.0 && c.x < 1.0) a = vec2(0.0);
    return mix(mix(a, b, f.x), mix(d, e, f.x), f.y);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 sz = IMG_SIZE(inputImage);
    float frame = IMG_NORM_PIXEL(inputImage, vec2(0.5) / sz).g;

    // fine flicker detail on top of the coarser simulation
    vec2 res = RENDERSIZE;
    vec2 n = vec2(vnoise(vec3(uv * res / 18.0, frame * 0.12)), vnoise(vec3(uv * res / 18.0 + 7.3, frame * 0.12))) - 0.5;
    vec2 base = S(uv, sz);
    vec2 hs = S(uv + n * 0.006 * min(base.r, 1.0), sz);
    float heat = hs.r;
    float smk = base.g;

    // flame colour ramp: tip -> mid -> core with rising heat
    vec3 fc = mix(flameTip.rgb, flameMid.rgb, smoothstep(0.15, 0.55, heat));
    fc = mix(fc, flameCore.rgb, smoothstep(0.55, 1.1, heat));
    float fa = smoothstep(0.06, 0.55, heat);

    float sa = (1.0 - exp(-smk * 0.6)) * (0.7 + 0.3 * vnoise(vec3(uv * res / 90.0, frame * 0.02)));
    vec3 sc = smokeColor.rgb * (0.75 + 0.5 * vnoise(vec3(uv * res / 45.0, frame * 0.015))) + fc * min(heat, 1.0) * 0.35;

    // composite in premultiplied space: input, smoke over it, flames over that, plus glow
    vec4 src = IMG_NORM_PIXEL(sourceImage, uv);
    vec3 P = showInput > 0.5 ? src.rgb * src.a : vec3(0.0);
    float A = showInput > 0.5 ? src.a : 0.0;
    P = P * (1.0 - sa) + sc * sa;
    A = A * (1.0 - sa) + sa;
    P = P * (1.0 - fa) + fc * fa;
    A = A * (1.0 - fa) + fa;
    P += fc * smoothstep(0.3, 1.5, heat) * 0.35;

    gl_FragColor = A > 0.0001 ? vec4(P / A, A) : vec4(0.0);
}
