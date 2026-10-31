/*{
    "DESCRIPTION": "Water Drops - simulation step. inputImage = last state (feedback). R = height, G = previous height. Pixels (0,0) and (1,0) keep the drop bookkeeping. Every rising edge of Drop over 0.5 starts a drop that falls for Fall Frames and then hits the surface.",
    "CREDIT": "Water Drops",
    "ISFVSN": "2",
    "CATEGORIES": ["Effect"],
    "INPUTS": [
        {"NAME": "inputImage", "TYPE": "image"},
        {"NAME": "inject", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "drop", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "dropX", "TYPE": "float", "DEFAULT": 0.0, "MIN": -1.0, "MAX": 1.0},
        {"NAME": "dropY", "TYPE": "float", "DEFAULT": 0.0, "MIN": -1.0, "MAX": 1.0},
        {"NAME": "spread", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "dropSize", "TYPE": "float", "DEFAULT": 0.02, "MIN": 0.003, "MAX": 0.1},
        {"NAME": "dropStrength", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "fallFrames", "TYPE": "float", "DEFAULT": 12.0, "MIN": 0.0, "MAX": 60.0},
        {"NAME": "damping", "TYPE": "float", "DEFAULT": 0.996, "MIN": 0.95, "MAX": 0.999},
        {"NAME": "seed", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 999.0}
    ]
}*/

float hash3(vec3 p) {
    p = fract(p * vec3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return fract((p.x + p.y) * p.z);
}

bool isBook(vec2 px) { return px.y < 1.0 && px.x < 2.0; }

float hAt(vec2 px, vec2 sz, float self) {
    px = clamp(px, vec2(0.5), sz - 0.5);          // clamped edge = wave reflects at the border
    if (isBook(px)) return self;
    return IMG_NORM_PIXEL(inputImage, px / IMG_SIZE(inputImage)).r;
}

float hpAt(vec2 px, vec2 sz, float self) {
    px = clamp(px, vec2(0.5), sz - 0.5);
    if (isBook(px)) return self;
    return IMG_NORM_PIXEL(inputImage, px / IMG_SIZE(inputImage)).g;
}

void main() {
    vec2 sz = RENDERSIZE;
    vec2 px = floor(isf_FragNormCoord * sz) + 0.5;
    vec4 book = IMG_NORM_PIXEL(inputImage, vec2(0.5, 0.5) / IMG_SIZE(inputImage));
    vec4 tgt = IMG_NORM_PIXEL(inputImage, vec2(1.5, 0.5) / IMG_SIZE(inputImage));

    // ----- drop bookkeeping (only in the injecting step) -----
    float prevDrop = book.r;
    float cnt = book.g;
    float cd = book.b;                       // countdown + 1, 0 = no drop falling
    vec2 target = tgt.rg;
    bool impact = false;
    if (inject > 0.5) {
        bool trig = drop > 0.5 && prevDrop <= 0.5;
        if (trig) {
            cnt += 1.0;
            cd = floor(fallFrames + 0.5) + 1.0;
            float a = hash3(vec3(cnt, seed, 3.1)) * 6.2831853;
            float r = sqrt(hash3(vec3(cnt, seed, 7.7))) * spread;
            vec2 p = vec2(dropX, dropY) + vec2(cos(a), sin(a)) * r;
            target = clamp(0.5 + 0.5 * p, 0.0, 1.0);
        }
        impact = cd > 0.5 && cd < 1.5;
        cd = cd > 0.5 ? cd - 1.0 : 0.0;
        prevDrop = drop;
    }

    if (px.y < 1.0 && px.x < 1.0) { gl_FragColor = vec4(prevDrop, cnt, cd, 1.0); return; }
    if (px.y < 1.0 && px.x < 2.0) { gl_FragColor = vec4(target, 0.0, 1.0); return; }

    // ----- wave equation step -----
    vec4 c = IMG_NORM_PIXEL(inputImage, px / IMG_SIZE(inputImage));
    float h = c.r, hp = c.g;
    float sum = hAt(px + vec2(1.0, 0.0), sz, h) + hAt(px - vec2(1.0, 0.0), sz, h)
              + hAt(px + vec2(0.0, 1.0), sz, h) + hAt(px - vec2(0.0, 1.0), sz, h);
    float sumP = hpAt(px + vec2(1.0, 0.0), sz, hp) + hpAt(px - vec2(1.0, 0.0), sz, hp)
               + hpAt(px + vec2(0.0, 1.0), sz, hp) + hpAt(px - vec2(0.0, 1.0), sz, hp);
    float lap = sum - 4.0 * h, lapP = sumP - 4.0 * hp;
    // wave step (c^2 = 0.4, below the stability limit 0.5) + slight viscosity: damps pixel-checkerboard noise
    float hn = (2.0 * h - hp + 0.4 * lap + 0.04 * (lap - lapP)) * damping;

    if (impact) {
        // crater with rims: launches a short train of rings instead of a single one
        float r = length((px - target * sz) / sz.y) / dropSize;
        hn -= dropStrength * cos(r * 2.4) * exp(-r * r * 0.35);
    }
    gl_FragColor = vec4(hn, h, 0.0, 1.0);
}
