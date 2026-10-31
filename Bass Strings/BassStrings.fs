/*{
    "DESCRIPTION": "Bass Strings - four round-wound double bass strings that vibrate with the music, fixed ends top and bottom. stateImage = output of BassStringsAnalysis.fs (band levels in pixels 1-4, learned range in pixel 0).",
    "CREDIT": "Bass Strings",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator"],
    "INPUTS": [
        {"NAME": "stateImage", "TYPE": "image"},
        {"NAME": "clockH", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 24.0},
        {"NAME": "clockM", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 60.0},
        {"NAME": "clockS", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 60.0},
        {"NAME": "clockMs", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1000.0},
        {"NAME": "stringsOpacity", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "backdropOn", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "backdropColor", "TYPE": "color", "DEFAULT": [0.42, 0.08, 0.04, 1.0]},
        {"NAME": "centerX", "TYPE": "float", "DEFAULT": 0.0, "MIN": -1.0, "MAX": 1.0},
        {"NAME": "spacing", "TYPE": "float", "DEFAULT": 0.18, "MIN": 0.0, "MAX": 0.5},
        {"NAME": "lineWidth", "TYPE": "float", "DEFAULT": 18.0, "MIN": 1.0, "MAX": 80.0},
        {"NAME": "edgeSoft", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 20.0},
        {"NAME": "color1", "TYPE": "color", "DEFAULT": [0.86, 0.82, 0.75, 1.0]},
        {"NAME": "color2", "TYPE": "color", "DEFAULT": [0.86, 0.82, 0.75, 1.0]},
        {"NAME": "color3", "TYPE": "color", "DEFAULT": [0.86, 0.82, 0.75, 1.0]},
        {"NAME": "color4", "TYPE": "color", "DEFAULT": [0.86, 0.82, 0.75, 1.0]},
        {"NAME": "lightAngle", "TYPE": "float", "DEFAULT": -35.0, "MIN": -90.0, "MAX": 90.0},
        {"NAME": "shine", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "windingPitch", "TYPE": "float", "DEFAULT": 5.0, "MIN": 1.5, "MAX": 30.0},
        {"NAME": "windingDepth", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "amplitude", "TYPE": "float", "DEFAULT": 0.04, "MIN": 0.0, "MAX": 0.25},
        {"NAME": "vibeSpeed", "TYPE": "float", "DEFAULT": 9.0, "MIN": 0.5, "MAX": 30.0},
        {"NAME": "overtones", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "blurAmount", "TYPE": "float", "DEFAULT": 0.6, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "twist", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "nodeTop", "TYPE": "float", "DEFAULT": 1.6, "MIN": -2.0, "MAX": 4.0},
        {"NAME": "nodeBottom", "TYPE": "float", "DEFAULT": -0.15, "MIN": -4.0, "MAX": 2.0},
        {"NAME": "showMeter", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0}
    ]
}*/

const float PI = 3.14159265;

float level(int i) {
    return IMG_NORM_PIXEL(stateImage, (vec2(float(i + 1), 0.0) + 0.5) / IMG_SIZE(stateImage)).r;
}

vec4 strColor(int i) {
    return i == 0 ? color1 : (i == 1 ? color2 : (i == 2 ? color3 : color4));
}

// displacement at normalized position s between the fixed ends (standing waves: modes 1-3)
float disp(float s, float t, float f, float ph, float ampPx, float ov) {
    return ampPx * (sin(PI * s) * cos(2.0 * PI * f * t + ph)
                  + ov * 0.6 * sin(2.0 * PI * s) * cos(2.0 * PI * f * 2.03 * t + ph * 1.7)
                  + ov * 0.35 * sin(3.0 * PI * s) * cos(2.0 * PI * f * 2.97 * t + ph * 2.3));
}

// "a over b" in premultiplied colour
void over(inout vec4 dst, vec3 c, float a) {
    dst.rgb = c * a + dst.rgb * (1.0 - a);
    dst.a = a + dst.a * (1.0 - a);
}

void main() {
    vec2 res = RENDERSIZE;
    vec2 p = isf_FragNormCoord * res;
    float yn = isf_FragNormCoord.y * 2.0 - 1.0;
    float t = mod(clockH * 3600.0 + clockM * 60.0 + clockS + clockMs * 0.001, 3600.0);
    float soft = max(edgeSoft, 0.5);
    float la = radians(lightAngle);
    vec3 L = normalize(vec3(sin(la), 0.0, cos(la)));
    vec3 Hv = normalize(L + vec3(0.0, 0.0, 1.0));

    float bottom = nodeBottom;
    float sLen = max(nodeTop - bottom, 0.001);
    float s = clamp((yn - bottom) / sLen, 0.0, 1.0);
    bool still = yn < nodeBottom;                                    // below the lower fixed end: no vibration

    vec4 col = vec4(0.0);

    // ----- strings -----
    for (int i = 0; i < 4; i++) {
        float L0 = clamp(level(i), 0.0, 1.5);
        float lw = lineWidth * (1.2 - 0.13 * float(i));                 // E thickest, G thinnest
        float halfW = 0.5 * lw;
        float cx = (0.5 + 0.5 * centerX) * res.x + (float(i) - 1.5) * spacing * res.x;
        float f = vibeSpeed * pow(4.0 / 3.0, float(i));
        float ph = float(i) * 1.9;
        float ampPx = still ? 0.0 : amplitude * res.x * L0;
        float ov = overtones * clamp(L0, 0.0, 1.0);
        float u = disp(s, t, f, ph, ampPx, ov);
        float uEcho = disp(s, t - 0.5 / f, f, ph, ampPx, ov);
        float env = ampPx * (abs(sin(PI * s)) + ov * 0.6 * abs(sin(2.0 * PI * s)) + ov * 0.35 * abs(sin(3.0 * PI * s)));
        float dx = p.x - cx;
        vec3 base = strColor(i).rgb;

        // soft shadow on the ground (opposite the light)
        float sh = 1.0 - smoothstep(halfW * 0.5, halfW * 1.4 + soft, abs(dx - u + sin(la) * halfW));
        over(col, vec3(0.0), sh * 0.28 * stringsOpacity);

        // time-averaged swing band, brighter at the turning points
        if (env > 0.5) {
            float qq = dx / (env + halfW);
            float inside = 1.0 - smoothstep(1.0 - soft / (env + halfW), 1.0, abs(qq));
            float dens = 0.22 / sqrt(max(1.0 - qq * qq, 0.04));
            float blur = inside * clamp(dens, 0.0, 0.9) * blurAmount * clamp(env / halfW, 0.0, 1.0);
            over(col, base * 0.95, blur * stringsOpacity);
        }

        // round-wound string: cylinder shading + winding grooves, drawn for the echo and the string
        for (int k = 0; k < 2; k++) {
            float uu = k == 0 ? uEcho : u;
            float wgt = k == 0 ? twist * clamp(L0 * 4.0, 0.0, 1.0) * 0.8 : 1.0;
            if (wgt <= 0.0) continue;
            float v = (dx - uu) / halfW;                                 // -1..1 across the string
            float cov = 1.0 - smoothstep(1.0 - soft / halfW, 1.0 + soft / halfW, abs(v));
            if (cov <= 0.0) continue;
            float vc = clamp(v, -0.999, 0.999);
            vec3 n = vec3(vc, 0.0, sqrt(1.0 - vc * vc));
            // winding: helical grooves, slanted across the visible front
            float pitch = windingPitch * (1.15 - 0.1 * float(i));
            float w = fract((p.y + asin(vc) / (PI * 0.5) * halfW * 0.9) / pitch);
            float groove = exp(-w * w * 40.0) + exp(-(1.0 - w) * (1.0 - w) * 40.0);   // dark groove line
            float ridge = exp(-(w - 0.3) * (w - 0.3) * 30.0);                          // lit flank after it
            n.y += windingDepth * (ridge - groove) * 0.6;
            n = normalize(n);
            float diff = max(dot(n, L), 0.0);
            float spec = pow(max(dot(n, Hv), 0.0), 40.0) * shine;
            vec3 c = base * (0.25 + 0.85 * diff) * (1.0 - windingDepth * 0.45 * groove) + vec3(spec) * 0.9;
            over(col, c, cov * wgt * stringsOpacity);
        }
    }

    // ----- meter: band levels and learned range (for tuning) -----
    if (showMeter > 0.5) {
        vec4 head = IMG_NORM_PIXEL(stateImage, vec2(0.5) / IMG_SIZE(stateImage));
        for (int i = 0; i < 4; i++) {
            vec2 b0 = vec2(40.0 + float(i) * 34.0, 40.0);
            float h = clamp(level(i), 0.0, 1.5) / 1.5 * 240.0;
            if (p.x > b0.x && p.x < b0.x + 24.0 && p.y > b0.y && p.y < b0.y + 240.0)
                col = vec4(p.y < b0.y + h ? strColor(i).rgb : vec3(0.15), 1.0);
        }
        vec2 r0 = vec2(200.0, 40.0);
        if (p.x > r0.x && p.x < r0.x + 400.0 && p.y > r0.y && p.y < r0.y + 16.0) {
            float hz = log2(20.0) + (p.x - r0.x) / 400.0 * (log2(20000.0) - log2(20.0));
            col = vec4((hz > head.r && hz < head.g) ? vec3(0.9, 0.8, 0.3) : vec3(0.15), 1.0);
        }
    }

    if (backdropOn > 0.5) gl_FragColor = vec4(backdropColor.rgb * (1.0 - col.a) + col.rgb, 1.0);
    else gl_FragColor = col;                                          // premultiplied
}
