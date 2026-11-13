/*{
    "DESCRIPTION": "Bass Strings - adaptive sound analysis (feedback state). inputImage = last state, specImage = Spectrum In as 1024 x 1 texture. Pixel (0,0): R/G = learned range low/high edge (log2 Hz), A = clock of the last frame. Pixels (1..4,0): R = band level after soft gate and fade, G = raw band level, B = swing phase of the string in cycles.",
    "CREDIT": "Stefan Mazur - https://github.com/S-Mazu/WirePatches",
    "ISFVSN": "2",
    "CATEGORIES": ["Effect"],
    "INPUTS": [
        {"NAME": "inputImage", "TYPE": "image"},
        {"NAME": "specImage", "TYPE": "image"},
        {"NAME": "clockH", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 24.0},
        {"NAME": "clockM", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 60.0},
        {"NAME": "clockS", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 60.0},
        {"NAME": "clockMs", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1000.0},
        {"NAME": "boost", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 20.0},
        {"NAME": "gate", "TYPE": "float", "DEFAULT": 0.05, "MIN": 0.0, "MAX": 1.0},
        {"NAME": "fade", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.01, "MAX": 5.0},
        {"NAME": "vibeSpeed", "TYPE": "float", "DEFAULT": 9.0, "MIN": 0.0, "MAX": 60.0}
    ]
}*/

const float BINS = 1024.0;
const float HZ_PER_BIN = 23.4375;     // 48 kHz; at 44.1 kHz the band edges shift slightly
const float RANGE_DEPTH_DB = -40.0;   // how far below the loudest content still counts as used range
const float RANGE_SHRINK = 20.0;      // seconds until the learned range narrows again
const float NOISE_GATE = 0.001;
const float MIN_LO_HZ = 100.0;        // the learned range never gets narrower than this
const float MIN_HI_HZ = 5000.0;

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
    float minLo = log2(MIN_LO_HZ), minHi = log2(MIN_HI_HZ);
    if (hi <= 0.0) { lo = minLo; hi = minHi; }                         // first frame

    // ----- pixel 0: learn the used frequency range -----
    if (px.x < 0.5) {
        float peak = 0.0;
        for (int k = 1; k < 1024; k++) peak = max(peak, spec(float(k)));
        float thr = max(peak * pow(10.0, RANGE_DEPTH_DB / 20.0), NOISE_GATE);
        float first = -1.0, last = -1.0;
        for (int k = 1; k < 1024; k++) {
            if (spec(float(k)) > thr) { if (first < 0.0) first = float(k); last = float(k); }
        }
        if (peak > NOISE_GATE && first > 0.0) {
            float tLo = min(log2(first * HZ_PER_BIN), minLo);
            float tHi = max(log2((last + 1.0) * HZ_PER_BIN), minHi);
            float fast = min(1.0, dt / 0.3), slow = min(1.0, dt / RANGE_SHRINK);
            lo += (tLo - lo) * (tLo < lo ? fast : slow);                     // widen fast, narrow slowly
            hi += (tHi - hi) * (tHi > hi ? fast : slow);
        }
        lo = min(lo, minLo);
        hi = max(hi, minHi);
        gl_FragColor = vec4(lo, hi, 0.0, clock);
        return;
    }

    // ----- pixels 1-4: band levels and swing phases -----
    int b = int(px.x + 0.5) - 1;
    float c0 = b == 0 ? 0.0 : (b == 1 ? 0.20 : (b == 2 ? 0.45 : 0.70));  // band edges, share of the log range
    float c1 = b == 0 ? 0.20 : (b == 1 ? 0.45 : (b == 2 ? 0.70 : 1.00));
    float e0 = exp2(lo + (hi - lo) * c0) / HZ_PER_BIN;                   // band edges in bins
    float e1 = exp2(lo + (hi - lo) * c1) / HZ_PER_BIN;
    if (e1 - e0 < 1.0) { float m = 0.5 * (e0 + e1); e0 = m - 0.5; e1 = m + 0.5; }

    float pk = 0.0;
    for (int k = 1; k < 1024; k++) {
        float f = float(k);
        if (min(f + 1.0, e1) - max(f, e0) > 0.0) pk = max(pk, spec(f));  // loudest note in the band
    }
    float bandBoost = b == 0 ? 1.0 : (b == 1 ? 2.0 : (b == 2 ? 4.0 : 8.0));  // high bands carry less energy
    float bandFade = max(fade, 0.01) * (b == 3 ? 0.667 : 1.0);           // the highest string rings shorter
    float raw = pk * boost * bandBoost;
    float lvl = min(max(raw - gate, 0.0) / max(1.0 - gate, 0.05), 1.5);  // soft gate: fades in from the threshold
    vec4 prev = stateAt(float(b + 1));
    float outL = max(lvl, prev.r - dt / bandFade);                       // falls back to 0 within Fade seconds

    // swing phase in cycles, advanced per frame: changing Vibe Speed never jumps. Strings are a fourth apart.
    float hz = vibeSpeed * pow(4.0 / 3.0, float(b));
    float phase = fract(prev.b + hz * dt);

    gl_FragColor = vec4(max(outL, 0.0), raw, phase, 1.0);
}
