/*{
  "DESCRIPTION": "Galaxy: dust lanes. inputImage is the clip below with the light of the galaxy added. The dust is a thin layer in the disc plane, densest at the inner edge of each arm (the same arm pattern as the stars in GalaxyData.fs) and broken up by noise. Per pixel the shader follows the line of sight through the tilted layer and adds up the dust; seen from above this gives lanes along the arms, seen edge-on a dark band through the middle. The picture is multiplied by exp(-absorption), so dust darkens smoothly and never cuts to black.",
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
    { "NAME": "galaxyTilt", "TYPE": "float", "DEFAULT": 50.0, "MIN": -180.0, "MAX": 180.0 },
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

// dust density at a point of galaxy space (x, y in the disc plane, z height above it)
float dustAt(vec2 g, float z, float m, float tw, float sgn, float e, float B, float spin) {
    float r = length(g);
    float theta = tw * max(r - 0.8 * B, 0.0);
    float phi = atan(g.y, g.x);
    float lane = phi - theta - spin - (-0.5 * PI + 0.7 * sgn) / m;
    lane -= max(scatter, 0.0) * 0.8 * (vnoise(g * 3.0 + seed) - 0.5);
    float d = mod(lane * m + PI, TAU) - PI;            // angular distance to the nearest lane, times m
    float dist = abs(d) / m * max(r, 0.05);            // in galaxy radii
    float w = max(dustWidth, 0.0) * 0.5 + 0.0001;
    float lanes = exp(-(dist * dist) / (w * w));
    float strength = clamp(abs(tw) * e * m * 0.6 * r, 0.0, 1.0) * step(0.5, clamp(armCount, 0.0, 12.0));
    float inBar = B > 0.0 ? 1.0 - smoothstep(0.6 * B, B, r) : 0.0;
    // a thin even layer everywhere in the disc, lanes on top of it
    float inPlane = 0.25 + 0.75 * lanes * strength * (1.0 - inBar);
    float envelope = smoothstep(0.1, 0.3, r) * (1.0 - smoothstep(0.75, 1.0, r));
    float h = 0.02 + 0.03 * exp(-r / 0.3);             // dust lies flatter than the stars
    float layer = exp(-(z * z) / (h * h));
    float clump = 0.35 + 0.9 * vnoise(vec2(r * 9.0, (phi - spin) * 3.0) + seed * 1.7);
    return inPlane * envelope * layer * clump;
}

void main() {
    vec4 base = IMG_NORM_PIXEL(inputImage, isf_FragNormCoord);

    // scene coordinates as in the example Scaler: height reference, Scale applied
    float safeScale = clamp(scale, 0.001, 1000.0);
    float ppu = 0.5 * max(RENDERSIZE.y, 1.0) * safeScale;
    vec2 p = (gl_FragCoord.xy - 0.5 * RENDERSIZE) / ppu;

    // into galaxy space: v.x along the disc, v.y the projected height on screen
    float s = max(galaxySize, 0.0001);
    vec2 v = p / s - vec2(galaxyX, galaxyY);
    float ra = -radians(galaxyRotation);
    v = vec2(cos(ra) * v.x - sin(ra) * v.y, sin(ra) * v.x + cos(ra) * v.y);
    float ct = cos(radians(galaxyTilt));
    float st = sin(radians(galaxyTilt));

    float M = clamp(armCount, 0.0, 12.0);
    float m = max(floor(M + 0.5), 1.0);
    float B = clamp(barSize, 0.0, 1.0);
    float e = 0.5 * (1.0 - 0.85 * clamp(armWidth, 0.0, 1.0));
    float tw = radians(armTwist);
    float sgn = tw < 0.0 ? -1.0 : 1.0;
    float spin = clock * 0.05;

    // follow the line of sight (depth d) through galaxy space: y = v.y cos t - d sin t, z = v.y sin t + d cos t.
    // Only the stretch inside the disc (|y| < 1.3) and inside the dust layer (|z| < 0.1) matters:
    // short when seen from above, the whole disc when seen edge-on.
    float dA = -1.0e6, dB = 1.0e6;
    if (abs(st) > 1.0e-4) {
        float a1 = (v.y * ct - 1.3) / st, a2 = (v.y * ct + 1.3) / st;
        dA = max(dA, min(a1, a2)); dB = min(dB, max(a1, a2));
    } else if (abs(v.y * ct) > 1.3) { dB = dA; }
    if (abs(ct) > 1.0e-4) {
        float b1 = (-0.1 - v.y * st) / ct, b2 = (0.1 - v.y * st) / ct;
        dA = max(dA, min(b1, b2)); dB = min(dB, max(b1, b2));
    } else if (abs(v.y * st) > 0.1) { dB = dA; }
    float tau = 0.0;
    if (dB > dA) {
        const int N = 24;
        float dd = (dB - dA) / float(N);
        for (int i = 0; i < N; i++) {
            float d = dA + (float(i) + 0.5) * dd;
            float y = v.y * ct - d * st;
            float z = v.y * st + d * ct;
            tau += dustAt(vec2(v.x, y), z, m, tw, sgn, e, B, spin) * dd;
        }
    }
    tau /= 1.7725 * 0.02;                               // seen from above, a full lane of the reference layer gives tau about 1
    tau = tau / (1.0 + 0.35 * tau);                      // edge-on the path is long: soften, so the band stays a shadow, not a black cut

    float absorb = clamp(dustOpacity, 0.0, 1.0) * 2.2 * tau * clamp(discOpacity, 0.0, 1.0);
    vec3 a = absorb * (1.0 - clamp(dustColor.rgb, 0.0, 1.0));
    if (!(a.r == a.r) || !(a.g == a.g) || !(a.b == a.b)) a = vec3(0.0);
    gl_FragColor = vec4(base.rgb * exp(-min(a, vec3(20.0))), base.a);
}
