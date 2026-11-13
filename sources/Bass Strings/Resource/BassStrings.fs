/*{
    "DESCRIPTION": "Bass Strings - four round-wound double bass strings that vibrate with the music, at any angle, with fixed ends at the frame. Follows the surface model: height is the reference, Scale multiplies all sizes. stateImage = output of BassStringsAnalysis.fs (band levels and swing phases in pixels 1-4).",
    "CREDIT": "Stefan Mazur - https://github.com/S-Mazu/WirePatches",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator"],
    "INPUTS": [
        {"NAME": "stateImage", "TYPE": "image"},
        {"NAME": "scale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0},
        {"NAME": "amplitude", "TYPE": "float", "DEFAULT": 0.14, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "waves", "TYPE": "float", "DEFAULT": 4.0, "MIN": 0.5, "MAX": 100.0},
        {"NAME": "motionBlur", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "overtones", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "stringAngle", "TYPE": "float", "DEFAULT": 0.0, "MIN": -180.0, "MAX": 180.0},
        {"NAME": "spacing", "TYPE": "float", "DEFAULT": 0.18, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "lineWidth", "TYPE": "float", "DEFAULT": 0.033, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "stringsOpacity", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "color1", "TYPE": "color", "DEFAULT": [0.86, 0.82, 0.75, 1.0]},
        {"NAME": "color2", "TYPE": "color", "DEFAULT": [0.86, 0.82, 0.75, 1.0]},
        {"NAME": "color3", "TYPE": "color", "DEFAULT": [0.86, 0.82, 0.75, 1.0]},
        {"NAME": "color4", "TYPE": "color", "DEFAULT": [0.86, 0.82, 0.75, 1.0]},
        {"NAME": "lightAngle", "TYPE": "float", "DEFAULT": 135.0, "MIN": 0.0, "MAX": 360.0},
        {"NAME": "shine", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "backdropOn", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "backdropColor", "TYPE": "color", "DEFAULT": [0.42, 0.08, 0.04, 1.0]}
    ]
}*/

const float PI = 3.14159265;
const float SHUTTER_SAMPLES = 16.0;   // positions per frame for the motion blur
const float WIND_PITCH = 0.0093;      // distance of the winding grooves (scene units)
const float WIND_DEPTH = 0.6;

vec4 stateOf(int i) {
    return IMG_NORM_PIXEL(stateImage, (vec2(float(i + 1), 0.0) + 0.5) / IMG_SIZE(stateImage));
}

vec4 strColor(int i) {
    return i == 0 ? color1 : (i == 1 ? color2 : (i == 2 ? color3 : color4));
}

// displacement at normalized position s between the ends (standing waves: Waves half waves, plus 2 overtones)
// phase in cycles; the overtones run at exact multiples
float disp(float s, float phase, float ph0, float amp, float ov, float k) {
    float a = 2.0 * PI * phase;
    float end = smoothstep(0.0, 0.04, s) * smoothstep(0.0, 0.04, 1.0 - s);   // fixed ends, also for fractional Waves
    return amp * end * (sin(k * PI * s) * cos(a + ph0)
                      + ov * 0.6 * sin(2.0 * k * PI * s) * cos(2.0 * a + ph0 * 1.7)
                      + ov * 0.35 * sin(3.0 * k * PI * s) * cos(3.0 * a + ph0 * 2.3));
}

// "a over b" in premultiplied colour
void over(inout vec4 dst, vec3 premult, float a) {
    dst.rgb = premult + dst.rgb * (1.0 - a);
    dst.a = a + dst.a * (1.0 - a);
}

void main() {
    // surface model: height is the reference. The visible area is y +-1/scale, x +-aspect/scale.
    vec2 res = max(RENDERSIZE, vec2(1.0));
    float ppu = 0.5 * res.y * clamp(scale, 0.001, 1000.0);   // pixels per scene unit
    float pix = 1.0 / ppu;
    vec2 p = (isf_FragNormCoord - 0.5) * res * pix;
    vec2 halfExt = 0.5 * res * pix;

    // string space: y along the strings, x across them. The strings always span the whole frame.
    float ang = -radians(stringAngle);                      // positive String Rotation turns counter-clockwise
    float sa = sin(ang), ca = cos(ang);
    float xs = dot(p, vec2(ca, -sa));
    float ys = dot(p, vec2(sa, ca));
    float spanX = abs(halfExt.x * ca) + abs(halfExt.y * sa);
    float spanY = abs(halfExt.x * sa) + abs(halfExt.y * ca);
    float s = clamp((ys + spanY) / max(2.0 * spanY, 1e-6), 0.0, 1.0);

    // Light Source: where the light comes from in the frame (0 = right, 90 = up). Only its part across
    // the strings shades them, so the light stays fixed in the frame when the strings turn.
    float la = radians(lightAngle);
    vec3 L = normalize(vec3(dot(vec2(cos(la), sin(la)), vec2(ca, -sa)), 0.0, 1.0));
    vec3 Hv = normalize(L + vec3(0.0, 0.0, 1.0));
    float k = max(waves, 0.0);
    float shutter = clamp(motionBlur, 0.0, 1.0) * 0.5;       // 1 = half a cycle: the string sweeps its whole swing

    vec4 col = vec4(0.0);

    for (int i = 0; i < 4; i++) {
        vec4 st = stateOf(i);
        float L0 = clamp(st.r, 0.0, 1.5);
        float halfW = max(0.5 * max(lineWidth, 0.0) * (1.2 - 0.13 * float(i)), 1e-5);   // E thickest, G thinnest
        float cx = (float(i) - 1.5) * spacing * 2.0 * spanX;
        float dx = xs - cx;
        float amp = max(amplitude, 0.0) * L0;
        float ov = clamp(overtones, 0.0, 1.0) * clamp(L0, 0.0, 1.0);
        float ph0 = float(i) * 1.9;

        // nothing of this string reaches the pixel: skip it
        float reach = amp * (1.0 + 0.95 * ov) + 2.0 * halfW + 3.0 * pix;
        if (abs(dx) > reach) continue;

        vec3 base = strColor(i).rgb;
        float n = (shutter > 0.0 && amp > 0.0) ? SHUTTER_SAMPLES : 1.0;
        float pitch = WIND_PITCH * (1.15 - 0.1 * float(i));
        float windDetail = smoothstep(2.0, 5.0, pitch / pix);   // grooves near pixel size would shimmer: fade them out

        vec3 sumC = vec3(0.0);
        float sumA = 0.0, sumS = 0.0;
        float u = disp(s, st.b, ph0, amp, ov, k);
        for (float j = 0.0; j < n; j += 1.0) {
            // the string one sample further back in time; the edge softens with the distance it travels,
            // so the samples blend into a smooth blur instead of showing single ghost images
            float uNext = disp(s, st.b - (j + 1.0) / n * shutter, ph0, amp, ov, k);
            float soft = max(pix, abs(uNext - u) * 0.5);
            float v = (dx - u) / halfW;

            float cov = 1.0 - smoothstep(1.0 - soft / halfW, 1.0 + soft / halfW, abs(v));
            float sh = 1.0 - smoothstep(halfW * 0.5, halfW * 1.4 + soft, abs(dx - u + L.x * halfW));
            sumS += sh;
            if (cov > 0.0) {
                // round-wound string: cylinder shading and slanted winding grooves
                float vc = clamp(v, -0.999, 0.999);
                vec3 nrm = vec3(vc, 0.0, sqrt(1.0 - vc * vc));
                float w = fract((ys + asin(vc) / (PI * 0.5) * halfW * 0.9) / pitch);
                float groove = (exp(-w * w * 40.0) + exp(-(1.0 - w) * (1.0 - w) * 40.0)) * windDetail;
                float ridge = exp(-(w - 0.3) * (w - 0.3) * 30.0) * windDetail;
                nrm.y += WIND_DEPTH * (ridge - groove) * 0.6;
                nrm = normalize(nrm);
                float diff = max(dot(nrm, L), 0.0);
                float spec = pow(max(dot(nrm, Hv), 0.0), 40.0) * shine;
                vec3 c = base * (0.25 + 0.85 * diff) * (1.0 - WIND_DEPTH * 0.45 * groove) + vec3(spec) * 0.9;
                sumC += c * cov;
                sumA += cov;
            }
            u = uNext;
        }
        // soft shadow on the ground (opposite the light), then the string
        float shadowA = sumS / n * 0.28 * stringsOpacity;
        over(col, vec3(0.0), shadowA);
        over(col, sumC / n * stringsOpacity, sumA / n * stringsOpacity);
    }

    if (backdropOn > 0.5) gl_FragColor = vec4(backdropColor.rgb * (1.0 - col.a) + col.rgb, 1.0);
    else gl_FragColor = col;                                          // premultiplied
}
