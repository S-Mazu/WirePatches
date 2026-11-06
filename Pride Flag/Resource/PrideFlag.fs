/*{
    "DESCRIPTION": "Pride Flag - a flag that moves lightly in the wind: Rainbow, Straight Ally or Progress. Follows the surface model: height is the reference, Scale multiplies all sizes. Nothing moves by itself: driven by clock from outside.",
    "CREDIT": "Stefan Mazur - https://github.com/S-Mazu/WirePatches",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator"],
    "INPUTS": [
        {"NAME": "clock", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1000.0},
        {"NAME": "scale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0},
        {"NAME": "windStrength", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 5.0},
        {"NAME": "waves", "TYPE": "float", "DEFAULT": 1.5, "MIN": 0.0, "MAX": 20.0},
        {"NAME": "foldShading", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "flag", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "flagX", "TYPE": "float", "DEFAULT": 0.0, "MIN": -20.0, "MAX": 20.0},
        {"NAME": "flagY", "TYPE": "float", "DEFAULT": 0.0, "MIN": -20.0, "MAX": 20.0},
        {"NAME": "flagSize", "TYPE": "float", "DEFAULT": 1.2, "MIN": 0.0, "MAX": 20.0},
        {"NAME": "backdropOn", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "backdropColor", "TYPE": "color", "DEFAULT": [0.0, 0.0, 0.0, 1.0]}
    ]
}*/

const float PI2 = 6.2831853;
const float ASPECT = 1.5;      // flag width / height (3:2)

// the wind phases run at whole cycles over the 0-1000 Clock range, so the Clock loop is seamless
const float CYCLES_1 = 0.5;    // per clock unit
const float CYCLES_2 = 0.8;

vec3 rainbow(float v) {
    float i = floor((1.0 - v) * 6.0);          // top to bottom
    if (i < 0.5) return vec3(0.894, 0.012, 0.012);   // red
    if (i < 1.5) return vec3(1.000, 0.549, 0.000);   // orange
    if (i < 2.5) return vec3(1.000, 0.929, 0.000);   // yellow
    if (i < 3.5) return vec3(0.000, 0.502, 0.149);   // green
    if (i < 4.5) return vec3(0.000, 0.302, 1.000);   // blue
    return vec3(0.459, 0.027, 0.529);                // violet
}

// straight ally: black and white stripes with a rainbow chevron, apex at the top centre
vec3 straightAlly(float u, float v) {
    float x = abs(u - 0.5) * ASPECT;
    float band = 1.0 - x / 0.75 - v;           // 0 on the outer edge of the chevron (top centre to bottom corners)
    if (band >= 0.0 && band < 0.667) return rainbow(v);
    return mod(floor((1.0 - v) * 6.0), 2.0) < 0.5 ? vec3(0.0) : vec3(1.0);
}

// progress: rainbow stripes with a chevron at the left edge, pointing right
vec3 progress(float u, float v) {
    float s = u * ASPECT + abs(v - 0.5);
    if (s < 0.32) return vec3(1.0);                // white
    if (s < 0.4275) return vec3(0.961, 0.663, 0.722);// pink
    if (s < 0.535) return vec3(0.357, 0.808, 0.980);// light blue
    if (s < 0.6425) return vec3(0.380, 0.224, 0.082);// brown
    if (s < 0.75) return vec3(0.0);                // black
    return rainbow(v);
}

vec3 flagColor(float u, float v) {
    u = clamp(u, 0.0, 1.0);
    v = clamp(v, 0.0, 0.9999);
    if (flag < 0.5) return rainbow(v);
    if (flag < 1.5) return straightAlly(u, v);
    return progress(u, v);
}

void main() {
    // surface model: height is the reference. The visible area is y +-1/scale, x +-aspect/scale.
    vec2 res = max(RENDERSIZE, vec2(1.0));
    float ppu = 0.5 * res.y * clamp(scale, 0.001, 1000.0);   // pixels per scene unit
    float pix = 1.0 / ppu;
    vec2 p = (isf_FragNormCoord - 0.5) * res * pix - vec2(flagX, flagY);

    float h = max(flagSize, 1e-4);
    float w = h * ASPECT;
    float wind = max(windStrength, 0.0);
    float k = max(waves, 0.0);

    // along the flag: 0 at the fixed left edge, 1 at the free right edge
    float u0 = p.x / w + 0.5;
    float along = clamp(u0, 0.0, 1.0);
    float grip = along * (2.0 - along);                 // the free end swings most
    float ph1 = PI2 * (k * along - CYCLES_1 * clock);
    float ph2 = PI2 * (1.7 * k * along - CYCLES_2 * clock + 0.3);

    // cloth: the height follows the waves, and the free end moves in and out a little
    float lift = 0.1 * h * wind * grip * (sin(ph1) + 0.35 * sin(ph2));
    float pull = 0.03 * w * wind * grip * cos(ph1);
    float u = (p.x + pull) / w + 0.5;
    float v = (p.y - lift) / h + 0.5;

    // antialiased edge in scene units
    float d = max(abs(u - 0.5) * w - 0.5 * w, abs(v - 0.5) * h - 0.5 * h);
    float alpha = 1.0 - smoothstep(-pix, pix, d);

    vec3 col = vec3(0.0);
    if (alpha > 0.0) {
        // four samples per pixel soften the edges between the colors
        vec2 du = vec2(pix / w, pix / h);
        col = 0.25 * (flagColor(u - 0.125 * du.x, v - 0.375 * du.y)
                    + flagColor(u + 0.375 * du.x, v - 0.125 * du.y)
                    + flagColor(u - 0.375 * du.x, v + 0.125 * du.y)
                    + flagColor(u + 0.125 * du.x, v + 0.375 * du.y));
        // folds: light and shadow follow the slope of the cloth
        float slope = grip * (cos(ph1) + 0.35 * cos(ph2));
        float fold = clamp(slope * min(wind * 2.0, 1.0) * foldShading * 0.35, -0.85, 0.85);
        col = fold > 0.0 ? mix(col, vec3(1.0), fold * 0.6) : col * (1.0 + fold);
    }

    if (backdropOn > 0.5) gl_FragColor = vec4(mix(backdropColor.rgb, col, alpha), 1.0);
    else gl_FragColor = vec4(col, alpha);    // straight alpha, premultiplied by the patch
}
