/*{
  "DESCRIPTION": "Galaxy: dust lanes. inputImage is the clip below with the light of the galaxy added. Per pixel the shader finds the point of the galaxy plane behind it and dims the picture where a dust lane lies: at the inner edge of each arm, following the same arm pattern as the stars (GalaxyData.fs), broken up by noise. The picture is multiplied by exp(-absorption), so dust darkens smoothly and never cuts to black.",
  "CREDIT": "Stefan Mazur - https://github.com/S-Mazu/WirePatches",
  "ISFVSN": "2",
  "CATEGORIES": [
    "Effect"
  ],
  "INPUTS": [
    { "NAME": "inputImage", "TYPE": "image" },
    { "NAME": "scale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1000.0 },
    { "NAME": "clock", "TYPE": "float", "DEFAULT": 0.0, "MIN": -1000000.0, "MAX": 1000000.0 },
    { "NAME": "galaxyX", "TYPE": "float", "DEFAULT": 0.0, "MIN": -100.0, "MAX": 100.0 },
    { "NAME": "galaxyY", "TYPE": "float", "DEFAULT": 0.0, "MIN": -100.0, "MAX": 100.0 },
    { "NAME": "galaxySize", "TYPE": "float", "DEFAULT": 0.8, "MIN": 0.0, "MAX": 100.0 },
    { "NAME": "galaxyRotation", "TYPE": "float", "DEFAULT": 0.0, "MIN": -180.0, "MAX": 180.0 },
    { "NAME": "galaxyAspect", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "discOpacity", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "armCount", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.0, "MAX": 12.0 },
    { "NAME": "armTwist", "TYPE": "float", "DEFAULT": 300.0, "MIN": -1440.0, "MAX": 1440.0 },
    { "NAME": "armWidth", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "barSize", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "scatter", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "dustColor", "TYPE": "color", "DEFAULT": [ 0.12, 0.06, 0.03, 1.0 ] },
    { "NAME": "dustOpacity", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "dustWidth", "TYPE": "float", "DEFAULT": 0.06, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "seed", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1000.0 }
  ]
}*/

const float PI = 3.14159265359;
const float TAU = 6.28318530718;

float hash21(vec2 p) {
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}
float vnoise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    float a = hash21(i), b = hash21(i + vec2(1.0, 0.0)), c = hash21(i + vec2(0.0, 1.0)), d = hash21(i + vec2(1.0, 1.0));
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}
float fbm(vec2 p) {
    float s = 0.0, w = 0.5;
    for (int i = 0; i < 4; i++) { s += w * vnoise(p); p = p * 2.03 + 17.1; w *= 0.5; }
    return s;
}

void main() {
    vec4 base = IMG_NORM_PIXEL(inputImage, isf_FragNormCoord);

    // scene coordinates as in the example Scaler: height reference, Scale applied
    float safeScale = clamp(scale, 0.001, 1000.0);
    float ppu = 0.5 * max(RENDERSIZE.y, 1.0) * safeScale;
    vec2 p = (gl_FragCoord.xy - 0.5 * RENDERSIZE) / ppu;

    // into the galaxy plane (radius 1); the tilt flattens the plane by Galaxy Aspect
    float s = max(galaxySize, 0.0001);
    vec2 v = p / s - vec2(galaxyX, galaxyY);
    float ra = -radians(galaxyRotation);
    v = vec2(cos(ra) * v.x - sin(ra) * v.y, sin(ra) * v.x + cos(ra) * v.y);
    float A = max(galaxyAspect, 0.02);
    vec2 g = vec2(v.x, v.y / A);
    float r = length(g);

    float M = clamp(armCount, 0.0, 12.0);
    float m = max(floor(M + 0.5), 1.0);
    float B = clamp(barSize, 0.0, 1.0);
    float e = 0.5 * (1.0 - 0.85 * clamp(armWidth, 0.0, 1.0));
    float tw = radians(armTwist);
    float sgn = tw < 0.0 ? -1.0 : 1.0;
    float spin = clock * 0.05;

    // the dust lane lies at the inner edge of each arm: arm crest at m u = -90 deg, shifted by 0.7 / m
    float theta = tw * max(r - 0.8 * B, 0.0);
    float phi = atan(g.y, g.x);
    float lane = phi - theta - spin - (-0.5 * PI + 0.7 * sgn) / m;
    lane -= max(scatter, 0.0) * 0.8 * (fbm(g * 3.0 + seed) - 0.5);
    float d = mod(lane * m + PI, TAU) - PI;            // angular distance to the nearest lane, times m
    float dist = abs(d) / m * max(r, 0.05);            // in galaxy radii
    float w = max(dustWidth, 0.0) * 0.5 + 0.0001;
    float density = exp(-(dist * dist) / (w * w));

    float strength = clamp(abs(tw) * e * m * 0.6 * r, 0.0, 1.0) * step(0.5, M);
    float inBar = B > 0.0 ? 1.0 - smoothstep(0.6 * B, B, r) : 0.0;
    float envelope = smoothstep(0.12, 0.3, r) * (1.0 - smoothstep(0.75, 1.0, r));
    float clump = 0.35 + 0.9 * fbm(vec2(r * 9.0, (phi - spin) * 3.0) + seed * 1.7);

    float absorb = clamp(dustOpacity, 0.0, 1.0) * 2.2 * density * strength * (1.0 - inBar) * envelope * clump * clamp(discOpacity, 0.0, 1.0);
    vec3 a = absorb * (1.0 - clamp(dustColor.rgb, 0.0, 1.0));
    if (!(a.r == a.r)) a = vec3(0.0);
    gl_FragColor = vec4(base.rgb * exp(-a), base.a);
}
