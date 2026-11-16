/*{
  "DESCRIPTION": "Field Viewer - shows the Motion Bridge field texture as it arrives in a patch: the channels, the status band with its counter, and the texture size. Diagnostic, no show content.",
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
      "NAME": "showBand",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "showSize",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 1.0
    }
  ]
}*/

// The contract of the Motion Bridge (tools/Motion Bridge/README.md, Output): the lower 15/16 of
// the texture is the field (R occupancy, G motion, B visibility), the top 1/16 is the status
// band whose every pixel counts up each frame.
const float BAND = 15.0 / 16.0;
const float LOG_FULL = 12.0;        // size bars: log2(pixels) / 12, so 4096 px is a full-width bar

vec3 sizeBar(vec3 col, vec2 uv, float y0, float px, vec3 on, vec3 off) {
    // a bar of height 0.03 from y0; length = log2(px) / 12
    if (uv.y < y0 || uv.y >= y0 + 0.03) return col;
    float len = log2(max(px, 1.0)) / LOG_FULL;
    vec3 c = uv.x < len ? on : off;
    if (fract(uv.x * LOG_FULL) < 1.5 * LOG_FULL / RENDERSIZE.x) c = vec3(0.6);   // tick every power of two
    return c;
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec4 f = IMG_NORM_PIXEL(inputImage, uv);
    float counter = IMG_NORM_PIXEL(inputImage, vec2(0.5, 31.0 / 32.0)).r;   // middle of the band
    vec2 sz = IMG_SIZE(inputImage);

    int ch = int(channel + 0.5);
    vec3 col = f.rgb;
    if (ch == 1) col = vec3(f.r);
    if (ch == 2) col = vec3(f.g);
    if (ch == 3) col = vec3(f.b);

    if (showBand > 0.5) {
        if (uv.y >= BAND) {                                             // the band: its counter as a bar
            col = uv.x < counter ? vec3(1.0, 0.9, 0.3) : vec3(0.25, 0.22, 0.1);
        }
        float line = 2.0 / RENDERSIZE.y;
        if (abs(uv.y - BAND) < line) col = vec3(1.0, 1.0, 0.0);         // yellow: expected lower edge of the band
        if (abs(uv.y - (1.0 - BAND)) < line) col = vec3(1.0, 0.0, 1.0); // magenta: where the band is if rows are flipped
    }

    if (showSize > 0.5) {                                               // from the bottom up
        col = sizeBar(col, uv, 0.02, sz.x, vec3(0.2, 0.9, 1.0), vec3(0.05, 0.15, 0.2));          // input width
        col = sizeBar(col, uv, 0.06, sz.y, vec3(1.0, 0.6, 0.2), vec3(0.2, 0.12, 0.05));          // input height
        col = sizeBar(col, uv, 0.10, RENDERSIZE.x, vec3(0.5, 0.7, 0.75), vec3(0.1, 0.14, 0.15)); // render width
        col = sizeBar(col, uv, 0.14, RENDERSIZE.y, vec3(0.75, 0.6, 0.45), vec3(0.15, 0.12, 0.09)); // render height
    }

    gl_FragColor = vec4(col, 1.0);
}
