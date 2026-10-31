/*{
    "DESCRIPTION": "Bass Strings - adaptive sound analysis (feedback state). inputImage = last state, specImage = Spectrum In as 1024 x 1 texture. Pixel (0,0): R/G = learned range low/high edge (log2 Hz), A = clock of the last frame. Pixels (1..4,0): R = band level after soft gate and fade, G = raw band level.",
    "CREDIT": "Bass Strings",
    "ISFVSN": "2",
    "CATEGORIES": ["Effect"],
    "INPUTS": [
        {"NAME": "inputImage", "TYPE": "image"},
        {"NAME": "specImage", "TYPE": "image"},
        {"NAME": "clockH", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 24.0},
        {"NAME": "clockM", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 60.0},
        {"NAME": "clockS", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 60.0},
        {"NAME": "clockMs", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1000.0},
        {"NAME": "overallBoost", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 20.0},
        {"NAME": "peakMix", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "rangeDepthDb", "TYPE": "float", "DEFAULT": -40.0, "MIN": -80.0, "MAX": 0.0},
        {"NAME": "noiseGate", "TYPE": "float", "DEFAULT": 0.001, "MIN": 0.0, "MAX": 0.1},
        {"NAME": "rangeShrink", "TYPE": "float", "DEFAULT": 20.0, "MIN": 1.0, "MAX": 120.0},
        {"NAME": "minLoHz", "TYPE": "float", "DEFAULT": 100.0, "MIN": 20.0, "MAX": 2000.0},
        {"NAME": "minHiHz", "TYPE": "float", "DEFAULT": 5000.0, "MIN": 200.0, "MAX": 20000.0},
        {"NAME": "hzPerBin", "TYPE": "float", "DEFAULT": 23.4375, "MIN": 1.0, "MAX": 100.0},
        {"NAME": "width1", "TYPE": "float", "DEFAULT": 20.0, "MIN": 0.0, "MAX": 100.0},
        {"NAME": "width2", "TYPE": "float", "DEFAULT": 25.0, "MIN": 0.0, "MAX": 100.0},
        {"NAME": "width3", "TYPE": "float", "DEFAULT": 25.0, "MIN": 0.0, "MAX": 100.0},
        {"NAME": "width4", "TYPE": "float", "DEFAULT": 30.0, "MIN": 0.0, "MAX": 100.0},
        {"NAME": "boost1", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 50.0},
        {"NAME": "boost2", "TYPE": "float", "DEFAULT": 2.0, "MIN": 0.0, "MAX": 50.0},
        {"NAME": "boost3", "TYPE": "float", "DEFAULT": 4.0, "MIN": 0.0, "MAX": 50.0},
        {"NAME": "boost4", "TYPE": "float", "DEFAULT": 8.0, "MIN": 0.0, "MAX": 50.0},
        {"NAME": "gate", "TYPE": "float", "DEFAULT": 0.05, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "fade1", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.01, "MAX": 5.0},
        {"NAME": "fade2", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.01, "MAX": 5.0},
        {"NAME": "fade3", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.01, "MAX": 5.0},
        {"NAME": "fade4", "TYPE": "float", "DEFAULT": 0.2, "MIN": 0.01, "MAX": 5.0}
    ]
}*/

const float BINS = 1024.0;

float spec(float i) {
    return IMG_NORM_PIXEL(specImage, vec2((i + 0.5) / BINS, 0.5)).r;
}

vec4 stateAt(float i) {
    return IMG_NORM_PIXEL(inputImage, vec2(i + 0.5, 0.5) / IMG_SIZE(inputImage));
}

void main() {
    vec2 px = floor(isf_FragNormCoord * IMG_SIZE(inputImage));
    if (px.y > 0.5 || px.x > 4.5) { gl_FragColor = vec4(0.0); return; }

    float clock = clockH * 3600.0 + clockM * 60.0 + clockS + clockMs * 0.001;   // wall clock, seconds of the day
    vec4 head = stateAt(0.0);
    float dt = clamp(clock - head.a, 0.0, 0.2);
    float lo = head.r, hi = head.g;
    float minLo = log2(minLoHz), minHi = log2(max(minHiHz, minLoHz * 1.5));
    if (hi <= 0.0) { lo = minLo; hi = minHi; }                         // first frame

    // ----- pixel 0: learn the used frequency range -----
    if (px.x < 0.5) {
        float peak = 0.0;
        for (int k = 1; k < 1024; k++) peak = max(peak, spec(float(k)));
        float thr = max(peak * pow(10.0, rangeDepthDb / 20.0), noiseGate);
        float first = -1.0, last = -1.0;
        for (int k = 1; k < 1024; k++) {
            if (spec(float(k)) > thr) { if (first < 0.0) first = float(k); last = float(k); }
        }
        if (peak > noiseGate && first > 0.0) {
            float tLo = min(log2(first * hzPerBin), minLo);
            float tHi = max(log2((last + 1.0) * hzPerBin), minHi);
            float fast = min(1.0, dt / 0.3), slow = min(1.0, dt / rangeShrink);
            lo += (tLo - lo) * (tLo < lo ? fast : slow);                     // widen fast, narrow slowly
            hi += (tHi - hi) * (tHi > hi ? fast : slow);
        }
        lo = min(lo, minLo);
        hi = max(hi, minHi);
        gl_FragColor = vec4(lo, hi, 0.0, clock);
        return;
    }

    // ----- pixels 1-4: band levels -----
    int b = int(px.x + 0.5) - 1;
    float w1 = width1, w2 = width2, w3 = width3, w4 = width4;
    float tot = max(w1 + w2 + w3 + w4, 0.001);
    float c0 = b == 0 ? 0.0 : (b == 1 ? w1 : (b == 2 ? w1 + w2 : w1 + w2 + w3));
    float c1 = c0 + (b == 0 ? w1 : (b == 1 ? w2 : (b == 2 ? w3 : w4)));
    float e0 = exp2(lo + (hi - lo) * c0 / tot) / hzPerBin;               // band edges in bins
    float e1 = exp2(lo + (hi - lo) * c1 / tot) / hzPerBin;
    if (e1 - e0 < 1.0) { float m = 0.5 * (e0 + e1); e0 = m - 0.5; e1 = m + 0.5; }

    float sum = 0.0, wsum = 0.0, pk = 0.0;
    for (int k = 1; k < 1024; k++) {
        float f = float(k);
        float w = clamp(min(f + 1.0, e1) - max(f, e0), 0.0, 1.0);        // overlap of bin k with the band
        if (w > 0.0) {
            float s = spec(f);
            sum += s * w; wsum += w; pk = max(pk, s);
        }
    }
    float mean = wsum > 0.0 ? sum / wsum : 0.0;
    float boost = b == 0 ? boost1 : (b == 1 ? boost2 : (b == 2 ? boost3 : boost4));
    float fade  = b == 0 ? fade1  : (b == 1 ? fade2  : (b == 2 ? fade3  : fade4));
    float raw = mix(mean, pk, peakMix) * overallBoost * boost;
    float lvl = min(max(raw - gate, 0.0) / max(1.0 - gate, 0.05), 1.5);   // soft gate: fades in from the threshold
    float prev = stateAt(float(b + 1)).r;
    float outL = max(lvl, prev - dt / fade);                             // falls back to 0 within Fade seconds
    gl_FragColor = vec4(max(outL, 0.0), raw, 0.0, 1.0);
}
