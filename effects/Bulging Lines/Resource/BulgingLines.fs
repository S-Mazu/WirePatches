/*{
  "DESCRIPTION": "Bulging Lines - ribbons fly out of the depth of the picture towards the viewer, over the floor in front of the projection. Driven by the Motion Bridge field: the ribbons flow around what stands on the floor and flutter where something moves. Near the projection = near in the picture. Calm without the bridge.",
  "CREDIT": "Stefan Mazur - https://github.com/S-Mazu/WirePatches",
  "ISFVSN": "2",
  "CATEGORIES": [
    "Effect"
  ],
  "INPUTS": [
    { "NAME": "inputImage", "TYPE": "image" },
    { "NAME": "scale", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "clock", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1000.0 },
    { "NAME": "ribbonDensity", "TYPE": "float", "DEFAULT": 24.0, "MIN": 0.0, "MAX": 200.0 },
    { "NAME": "ribbonWidth", "TYPE": "float", "DEFAULT": 0.015, "MIN": 0.0, "MAX": 0.5 },
    { "NAME": "ribbonColor", "TYPE": "color", "DEFAULT": [0.85, 0.9, 1.0, 1.0] },
    { "NAME": "ribbonsOpacity", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "horizonY", "TYPE": "float", "DEFAULT": 0.7, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "avoidStrength", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "flutterStrength", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 10.0 },
    { "NAME": "flutterLife", "TYPE": "float", "DEFAULT": 1.5, "MIN": 0.0, "MAX": 30.0 },
    { "NAME": "showField", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "clockH", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 24.0 },
    { "NAME": "clockM", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 60.0 },
    { "NAME": "clockS", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 60.0 },
    { "NAME": "clockMs", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 1000.0 }
  ],
  "PASSES": [
    { "TARGET": "state", "PERSISTENT": true, "FLOAT": true, "WIDTH": "2", "HEIGHT": "1" },
    { "TARGET": "blurX", "FLOAT": true, "WIDTH": "160", "HEIGHT": "120" },
    { "TARGET": "blurXY", "FLOAT": true, "WIDTH": "160", "HEIGHT": "120" },
    { "TARGET": "field", "PERSISTENT": true, "FLOAT": true, "WIDTH": "160", "HEIGHT": "120" },
    { }
  ]
}*/

// The Motion Bridge field (tools/Motion Bridge/README.md, Output): x 0..1 along the front,
// depth 0..1 into the area at 15/16 of the height, R occupancy, G motion; the top sixteenth
// is the status band whose counter changes every frame.
//
// Picture: a view over the floor of the area. The bottom edge of the frame is the front
// (depth 0, near), the floor recedes to a horizon. A pixel at height y below the horizon H
// sees the floor at the distance Z = H / (H - y): 1 at the bottom edge, infinite at H.
// Depth 1 of the field lies at Z = FAR. Floor x in output heights: X = (u - 0.5) * aspect * Z,
// so the front spans the whole width and the area narrows towards the horizon.
//
// Ribbons are the lines X = const, deflected sideways around occupancy (the gradient of the
// blurred occupancy) and shaken where motion has excited the fabric. Segments fly towards
// the viewer along Z.
//
// state (2 x 1, persistent, signature in alpha):
//   pixel 0: now (s), last counter, time of the last counter change (s)
//   pixel 1: frame time dt (s), real time (s) for the flutter; the flight runs with Clock
// field (160 x 120 over x and depth, persistent): r excitation (decays over Flutter Life),
//   g occupancy, blurred and smoothed over time

const float SIG = 0.61;
const float LSB = 1.0 / 255.0;
const float FAR = 4.0;             // floor distance of the far edge of the area (bottom edge = 1)
const float BLUR_X = 0.06;         // blur radius of the field, in field units
const float BLUR_D = 0.06;
const float BAND_Y = 31.0 / 32.0;  // middle of the status band
const float FIELD_H = 15.0 / 16.0; // the field rows in the texture

float safe(float v, float lo, float hi) {
    if (!(v == v) || abs(v) > 1e9) return lo;
    return clamp(v, lo, hi);
}

float hash11(float n) { return fract(sin(n * 127.1 + 311.7) * 43758.5453); }
float hash21(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

void main() {
    vec2 uv = isf_FragNormCoord;

    // ---------- pass 0: time and the bridge's sign of life ----------
    if (PASSINDEX == 0) {
        vec4 s0 = IMG_NORM_PIXEL(state, vec2(0.25, 0.5));
        vec4 s1 = IMG_NORM_PIXEL(state, vec2(0.75, 0.5));
        float now = clockH * 3600.0 + clockM * 60.0 + clockS + clockMs * 0.001;
        bool fresh = !(abs(s0.a - SIG) < 0.01) || !(abs(s1.a - SIG) < 0.01) || now < s0.r;
        if (fresh) {
            s0 = vec4(now, -1.0, now - 1000.0, SIG);
            s1 = vec4(1.0 / 60.0, 0.0, 0.0, SIG);
        }
        float dt = clamp(now - s0.r, 0.0, 0.1);
        dt = safe(mix(s1.r, dt, 0.25), 0.0, 0.1);

        // the band: R = G = B > 0 and equal across the width
        vec3 b0 = IMG_NORM_PIXEL(inputImage, vec2(0.2, BAND_Y)).rgb;
        vec3 b1 = IMG_NORM_PIXEL(inputImage, vec2(0.5, BAND_Y)).rgb;
        vec3 b2 = IMG_NORM_PIXEL(inputImage, vec2(0.8, BAND_Y)).rgb;
        vec3 mn = min(min(b0, b1), b2), mx = max(max(b0, b1), b2);
        float spread = max(max(mx.r, mx.g), mx.b) - min(min(mn.r, mn.g), mn.b);
        bool band = spread < 1.5 * LSB && mn.r > 0.5 * LSB;
        float counter = (b0.r + b1.r + b2.r) / 3.0;
        float lastCounter = safe(s0.g, -1.0, 1.0);
        float lastChange = safe(s0.b, now - 100000.0, now);
        if (band && abs(counter - lastCounter) > 0.5 * LSB) {
            lastChange = now;
            lastCounter = counter;
        }
        float rt = mod(safe(s1.g, 0.0, 3600.0) + dt, 3600.0);              // real time for the flutter
        gl_FragColor = uv.x < 0.5 ? vec4(now, lastCounter, lastChange, SIG) : vec4(dt, rt, 0.0, SIG);
        return;
    }

    vec4 s0 = IMG_NORM_PIXEL(state, vec2(0.25, 0.5));
    vec4 s1 = IMG_NORM_PIXEL(state, vec2(0.75, 0.5));
    float age = safe(s0.r - s0.b, 0.0, 100000.0);
    float alive = 1.0 - smoothstep(0.25, 1.0, age);       // fade the field's influence out without the bridge
    float dt = safe(s1.r, 0.0, 0.1);
    float rt = safe(s1.g, 0.0, 3600.0);                                // real time: flutter
    float ct = safe(clock, 0.0, 1000.0);                                // Clock: flight

    // ---------- pass 1: blur along x (r occupancy, g motion), in field space ----------
    if (PASSINDEX == 1) {
        vec2 sum = vec2(0.0);
        float wsum = 0.0;
        for (int k = -6; k <= 6; k++) {
            float o = float(k) / 6.0;
            float w = exp(-o * o * 2.0);
            float fx = uv.x + o * BLUR_X;
            vec2 v = IMG_NORM_PIXEL(inputImage, vec2(clamp(fx, 0.0, 1.0), uv.y * FIELD_H)).rg;
            if (fx < 0.0 || fx > 1.0) v = vec2(0.0);
            sum += v * w;
            wsum += w;
        }
        gl_FragColor = vec4(sum / wsum, 0.0, 1.0);
        return;
    }

    // ---------- pass 2: blur along depth ----------
    if (PASSINDEX == 2) {
        vec2 sum = vec2(0.0);
        float wsum = 0.0;
        for (int k = -6; k <= 6; k++) {
            float o = float(k) / 6.0;
            float w = exp(-o * o * 2.0);
            float fd = uv.y + o * BLUR_D;
            vec2 v = IMG_NORM_PIXEL(blurX, vec2(uv.x, clamp(fd, 0.0, 1.0))).rg;
            if (fd < 0.0 || fd > 1.0) v = vec2(0.0);
            sum += v * w;
            wsum += w;
        }
        gl_FragColor = vec4(sum / wsum, 0.0, 1.0);
        return;
    }

    // ---------- pass 3: excitation and smoothed occupancy ----------
    if (PASSINDEX == 3) {
        vec4 prev = IMG_NORM_PIXEL(field, uv);
        vec2 now_ = IMG_NORM_PIXEL(blurXY, uv).rg * alive;
        float e = safe(prev.r, 0.0, 1.0);
        float o = safe(prev.g, 0.0, 1.0);
        float life = max(safe(flutterLife, 0.0, 30.0), 0.001);
        e = max(clamp(now_.g * 2.5, 0.0, 1.0), e * exp(-dt / life));
        o = mix(o, clamp(now_.r * 1.5, 0.0, 1.0), 1.0 - exp(-dt / 0.15));
        gl_FragColor = vec4(e, o, 0.0, 1.0);
        return;
    }

    // ---------- pass 4: the picture ----------
    vec2 res = max(RENDERSIZE, vec2(1.0));
    float aspect = res.x / res.y;
    float S = clamp(scale, 0.001, 1000.0);
    float H = clamp(horizonY * S, 0.001, 1000.0);        // horizon, output heights above the bottom edge
    vec4 outC = vec4(0.0);

    if (uv.y < H) {
        float Z = H / (H - uv.y);                         // floor distance, 1 at the bottom edge
        float X = (uv.x - 0.5) * aspect * Z;              // floor x in output heights
        float fx = X / aspect + 0.5;                      // field x
        float fd = (Z - 1.0) / (FAR - 1.0);               // field depth
        bool inField = fx >= 0.0 && fx <= 1.0 && fd >= 0.0 && fd <= 1.0;

        float ex = 0.0, occ = 0.0, grad = 0.0;
        float gs = BLUR_X;                                // gradient step in field x
        vec4 fc = IMG_NORM_PIXEL(field, vec2(clamp(fx, 0.0, 1.0), clamp(fd, 0.0, 1.0)));
        vec4 fl = IMG_NORM_PIXEL(field, vec2(clamp(fx - gs, 0.0, 1.0), clamp(fd, 0.0, 1.0)));
        vec4 fr = IMG_NORM_PIXEL(field, vec2(clamp(fx + gs, 0.0, 1.0), clamp(fd, 0.0, 1.0)));
        if (inField) {
            ex = safe(fc.r, 0.0, 1.0);
            occ = safe(fc.g, 0.0, 1.0);
            float gL = (fx - gs >= 0.0) ? safe(fl.g, 0.0, 1.0) : 0.0;
            float gr = (fx + gs <= 1.0) ? safe(fr.g, 0.0, 1.0) : 0.0;
            grad = gr - gL;
        }

        float sp = S / max(ribbonDensity, 0.001);         // ribbon spacing, output heights
        float hw = 0.5 * max(ribbonWidth, 0.0) * S;       // half width
        // how far X moves per screen pixel: along the row (aspect * Z / width) and, at the sides,
        // across rows (the lines converge to the vanishing point and run almost horizontally)
        float dXdx = aspect * Z / res.x;
        float dXdy = abs(uv.x - 0.5) * aspect * Z * Z / (H * res.y);
        float pxw = max(sqrt(dXdx * dXdx + dXdy * dXdy), 1e-6);

        // flow around: the lines X = const move away from occupied cells
        float psi = X + safe(avoidStrength, 0.0, 10.0) * BLUR_X * aspect * grad * 1.5;
        // flutter: a fast sideways wave where motion has excited the fabric
        float fs = safe(flutterStrength, 0.0, 10.0) * ex;
        float wave = sin(Z * 9.0 - rt * 14.0 + X * 4.0) + 0.5 * sin(Z * 17.0 - rt * 23.0 - X * 7.0);
        psi += fs * sp * 0.35 * wave;

        float u = psi / sp;
        float lane = floor(u + 0.5);
        float du = abs(u - lane) * sp;
        float cov = clamp((hw - du) / pxw + 0.5, 0.0, 1.0);
        if (ribbonDensity <= 0.0 || hw <= 0.0) cov = 0.0;

        // segments flying towards the viewer
        float h = hash11(lane);
        float L = 1.0 + 2.0 * h;                          // segment period in floor distance
        float s = Z + ct * 0.8 + h * 13.0;
        float seg = s / L;
        float f = fract(seg);
        float duty = 0.45 + 0.4 * hash21(vec2(lane, floor(seg)));
        float soft = 0.08;
        float vis = smoothstep(0.0, soft, f) * (1.0 - smoothstep(duty - soft, duty, f));

        // fabric: rounded across the ribbon, twisting where it flutters
        float across = clamp(du / max(hw, 1e-6), 0.0, 1.0);
        float shade = 0.7 + 0.3 * sqrt(max(1.0 - across * across, 0.0));
        float twist = sin(Z * 6.0 - rt * 19.0 + lane * 1.7);
        shade *= 1.0 - min(fs, 1.5) * 0.45 * (0.5 + 0.5 * twist);

        // fade towards the horizon and where the lines get finer than a few pixels
        float fade = 1.0 - smoothstep(FAR * 0.9, FAR * 3.0, Z);
        fade *= smoothstep(3.0, 7.0, sp / pxw);           // spacing in screen pixels
        // inside what stands there the ribbons thin out
        fade *= 1.0 - 0.7 * smoothstep(0.35, 0.8, occ);

        float a = cov * vis * fade * clamp(ribbonsOpacity, 0.0, 1.0) * clamp(ribbonColor.a, 0.0, 1.0);
        outC = vec4(ribbonColor.rgb * shade, a);

        if (showField > 0.5) {
            vec3 fcol = vec3(occ, ex, 0.0);
            float fa = inField ? max(max(occ, ex) * 0.8, 0.12) : 0.0;
            // outline of the area
            float edge = min(min(abs(fx), abs(1.0 - fx)) * aspect / Z, min(abs(fd), abs(1.0 - fd)) * (FAR - 1.0) / (Z * Z) * H);
            if (inField && edge * res.y < 1.5) { fcol = vec3(1.0, 0.0, 1.0); fa = 1.0; }
            outC.rgb = mix(outC.rgb, fcol, fa * (1.0 - outC.a * 0.5));
            outC.a = max(outC.a, fa);
        }
    }

    outC.rgb = clamp(outC.rgb, 0.0, 1.0);
    outC.a = clamp(outC.a, 0.0, 1.0);
    if (!(outC.r == outC.r) || !(outC.a == outC.a)) outC = vec4(0.0);
    gl_FragColor = outC;
}
