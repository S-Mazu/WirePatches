/*{
  "DESCRIPTION": "Boil: redraws the picture 12 times per second with slightly different outlines, density and brightness, like shapes painted frame by frame directly on film.",
  "CREDIT": "Stefan Mazur - https://github.com/S-Mazu/WirePatches",
  "ISFVSN": "2",
  "CATEGORIES": ["Effect"],
  "INPUTS": [
    { "NAME": "inputImage", "TYPE": "image" },
    { "NAME": "boil", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 5.0 },
    { "NAME": "scale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "clockH", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 24.0 },
    { "NAME": "clockM", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 60.0 },
    { "NAME": "clockS", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 60.0 },
    { "NAME": "clockMs", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1000.0 }
  ]
}*/

float hash3(vec3 p) {
    p = fract(p * vec3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return fract((p.x + p.y) * p.z);
}

float vnoise(vec2 q, float seed) {
    vec2 i = floor(q), f = fract(q);
    f = f * f * (3.0 - 2.0 * f);
    float a = mix(hash3(vec3(i, seed)), hash3(vec3(i + vec2(1.0, 0.0), seed)), f.x);
    float b = mix(hash3(vec3(i + vec2(0.0, 1.0), seed)), hash3(vec3(i + vec2(1.0, 1.0), seed)), f.x);
    return mix(a, b, f.y);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    float amount = (boil == boil) ? clamp(boil, 0.0, 5.0) : 0.0;
    if (amount <= 0.0) {
        gl_FragColor = IMG_NORM_PIXEL(inputImage, uv);
        return;
    }

    float safeScale = clamp(scale, 0.001, 1000.0);
    vec2 res = max(RENDERSIZE, vec2(1.0));
    float perUnit = 0.5 * res.y * safeScale;   // pixels per scene unit: height is the reference
    vec2 p = (uv - 0.5) * res / perUnit;

    // a new drawing 12 times per second
    float seconds = mod(clockH * 3600.0 + clockM * 60.0 + clockS + clockMs * 0.001, 3600.0);
    float frame = floor(seconds * 12.0);

    // outlines: a slow lean of the whole form, then a hand-drawn wobble, then a ragged edge
    vec2 d = vec2(vnoise(p * 2.2, frame), vnoise(p * 2.2 + 37.0, frame + 11.0)) - 0.5;
    vec2 wobble = d * 0.045;
    d = vec2(vnoise(p * 9.0 + 5.0, frame + 3.0), vnoise(p * 9.0 + 91.0, frame + 7.0)) - 0.5;
    wobble += d * 0.018;
    d = vec2(vnoise(p * 28.0 + 13.0, frame + 5.0), vnoise(p * 28.0 + 71.0, frame + 9.0)) - 0.5;
    wobble += d * 0.006;

    vec4 c = IMG_NORM_PIXEL(inputImage, uv + wobble * amount * perUnit / res);

    // uneven paint: thin patches inside the form and a brightness that changes from frame to frame
    float blot = smoothstep(0.55, 0.85, vnoise(p * 34.0, frame + 17.0));
    c *= 1.0 - clamp(amount * 0.22, 0.0, 0.7) * blot;
    float flicker = 1.0 + amount * 0.18 * (hash3(vec3(frame, 2.0, 5.0)) - 0.5);
    c.rgb *= clamp(flicker, 0.2, 1.8);

    gl_FragColor = c;
}
