/*{
    "DESCRIPTION": "Star Globe collision: pairwise push between stars. Column i = push vector of star i (RG, encoded f*0.25+0.5). posTex: star positions (RG), radTex: visible radius (R, 0 = hidden).",
    "CREDIT": "Star Globe",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator"],
    "INPUTS": [
        {"NAME": "posTex", "TYPE": "image"},
        {"NAME": "radTex", "TYPE": "image"}
    ]
}*/

const int N = 300;

void main() {
    int i = int(floor(gl_FragCoord.x));
    // only a few rows at top and bottom are needed (read-back row, orientation-independent)
    if (i >= N || (isf_FragNormCoord.y > 0.03 && isf_FragNormCoord.y < 0.97)) { gl_FragColor = vec4(0.0); return; }
    vec2 p  = IMG_PIXEL(posTex, vec2(float(i) + 0.5, 0.5)).rg;
    float r = IMG_PIXEL(radTex, vec2(float(i) + 0.5, 0.5)).r;
    vec2 f = vec2(0.0);
    if (r > 0.0) {
        for (int j = 0; j < N; j++) {
            if (j == i) continue;
            float rj = IMG_PIXEL(radTex, vec2(float(j) + 0.5, 0.5)).r;
            if (rj <= 0.0) continue;
            vec2 d = p - IMG_PIXEL(posTex, vec2(float(j) + 0.5, 0.5)).rg;
            float dist = length(d);
            float minD = r + rj;
            if (dist < minD) {
                vec2 dir;
                if (dist > 1e-5) {
                    dir = d / dist;
                } else {
                    // identical positions: pseudo-random direction, opposite for the partner
                    float a = fract(sin(float(min(i, j)) * 12.9898 + float(max(i, j)) * 78.233) * 43758.5453) * 6.2831853;
                    dir = vec2(cos(a), sin(a)) * (i < j ? 1.0 : -1.0);
                }
                f += dir * (minD - dist) / minD;
            }
        }
    }
    // encoded: 0.5 = no push, range -2..2 (read-back clamps to 0..1)
    gl_FragColor = vec4(clamp(f * 0.25 + 0.5, 0.0, 1.0), 0.0, 1.0);
}
