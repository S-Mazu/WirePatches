/*{
    "DESCRIPTION": "Deco Marquee v2 - Art Deco theatre portal: up to 5 fans (rays + rings, each with its own look) with X/Y/Z and fill angle, marquee bulbs with chase, closed ornament border. Nothing moves by itself.",
    "CREDIT": "Deco Marquee v2",
    "ISFVSN": "2",
    "CATEGORIES": ["Generator"],
    "INPUTS": [
        {"NAME": "fan1X", "TYPE": "float", "DEFAULT": 0, "MIN": -2.0, "MAX": 2.0},
        {"NAME": "fan1Y", "TYPE": "float", "DEFAULT": -1, "MIN": -2.0, "MAX": 2.0},
        {"NAME": "fan1Z", "TYPE": "float", "DEFAULT": 0, "MIN": -0.5, "MAX": 5.0},
        {"NAME": "fan1Fill", "TYPE": "float", "DEFAULT": 180, "MIN": 0.0, "MAX": 360.0},
        {"NAME": "fan1Rot", "TYPE": "float", "DEFAULT": 0, "MIN": -180.0, "MAX": 180.0},
        {"NAME": "fan1RayCount", "TYPE": "float", "DEFAULT": 40.0, "MIN": 0.0, "MAX": 96.0},
        {"NAME": "fan1RayLength", "TYPE": "float", "DEFAULT": 1.15, "MIN": 0.0, "MAX": 3.0},
        {"NAME": "fan1RayWidth", "TYPE": "float", "DEFAULT": 0.03, "MIN": 0.0, "MAX": 0.2},
        {"NAME": "fan1RayStyle", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "fan1RayColor", "TYPE": "color", "DEFAULT": [0.83, 0.66, 0.25, 1.0]},
        {"NAME": "fan1RingCount", "TYPE": "float", "DEFAULT": 4.0, "MIN": 0.0, "MAX": 8.0},
        {"NAME": "fan1RingStart", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "fan1RingGap", "TYPE": "float", "DEFAULT": 0.2, "MIN": 0.02, "MAX": 1.0},
        {"NAME": "fan1RingWidth", "TYPE": "float", "DEFAULT": 0.02, "MIN": 0.0, "MAX": 0.2},
        {"NAME": "fan1RingStyle", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "fan1RingColor", "TYPE": "color", "DEFAULT": [0.95, 0.88, 0.70, 1.0]},
        {"NAME": "fan2X", "TYPE": "float", "DEFAULT": -0.67, "MIN": -2.0, "MAX": 2.0},
        {"NAME": "fan2Y", "TYPE": "float", "DEFAULT": 0, "MIN": -2.0, "MAX": 2.0},
        {"NAME": "fan2Z", "TYPE": "float", "DEFAULT": 1, "MIN": -0.5, "MAX": 5.0},
        {"NAME": "fan2Fill", "TYPE": "float", "DEFAULT": 0, "MIN": 0.0, "MAX": 360.0},
        {"NAME": "fan2Rot", "TYPE": "float", "DEFAULT": 0, "MIN": -180.0, "MAX": 180.0},
        {"NAME": "fan2RayCount", "TYPE": "float", "DEFAULT": 40.0, "MIN": 0.0, "MAX": 96.0},
        {"NAME": "fan2RayLength", "TYPE": "float", "DEFAULT": 1.15, "MIN": 0.0, "MAX": 3.0},
        {"NAME": "fan2RayWidth", "TYPE": "float", "DEFAULT": 0.03, "MIN": 0.0, "MAX": 0.2},
        {"NAME": "fan2RayStyle", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "fan2RayColor", "TYPE": "color", "DEFAULT": [0.83, 0.66, 0.25, 1.0]},
        {"NAME": "fan2RingCount", "TYPE": "float", "DEFAULT": 4.0, "MIN": 0.0, "MAX": 8.0},
        {"NAME": "fan2RingStart", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "fan2RingGap", "TYPE": "float", "DEFAULT": 0.2, "MIN": 0.02, "MAX": 1.0},
        {"NAME": "fan2RingWidth", "TYPE": "float", "DEFAULT": 0.02, "MIN": 0.0, "MAX": 0.2},
        {"NAME": "fan2RingStyle", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "fan2RingColor", "TYPE": "color", "DEFAULT": [0.95, 0.88, 0.70, 1.0]},
        {"NAME": "fan3X", "TYPE": "float", "DEFAULT": 0.67, "MIN": -2.0, "MAX": 2.0},
        {"NAME": "fan3Y", "TYPE": "float", "DEFAULT": 0, "MIN": -2.0, "MAX": 2.0},
        {"NAME": "fan3Z", "TYPE": "float", "DEFAULT": 1, "MIN": -0.5, "MAX": 5.0},
        {"NAME": "fan3Fill", "TYPE": "float", "DEFAULT": 0, "MIN": 0.0, "MAX": 360.0},
        {"NAME": "fan3Rot", "TYPE": "float", "DEFAULT": 0, "MIN": -180.0, "MAX": 180.0},
        {"NAME": "fan3RayCount", "TYPE": "float", "DEFAULT": 40.0, "MIN": 0.0, "MAX": 96.0},
        {"NAME": "fan3RayLength", "TYPE": "float", "DEFAULT": 1.15, "MIN": 0.0, "MAX": 3.0},
        {"NAME": "fan3RayWidth", "TYPE": "float", "DEFAULT": 0.03, "MIN": 0.0, "MAX": 0.2},
        {"NAME": "fan3RayStyle", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "fan3RayColor", "TYPE": "color", "DEFAULT": [0.83, 0.66, 0.25, 1.0]},
        {"NAME": "fan3RingCount", "TYPE": "float", "DEFAULT": 4.0, "MIN": 0.0, "MAX": 8.0},
        {"NAME": "fan3RingStart", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "fan3RingGap", "TYPE": "float", "DEFAULT": 0.2, "MIN": 0.02, "MAX": 1.0},
        {"NAME": "fan3RingWidth", "TYPE": "float", "DEFAULT": 0.02, "MIN": 0.0, "MAX": 0.2},
        {"NAME": "fan3RingStyle", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "fan3RingColor", "TYPE": "color", "DEFAULT": [0.95, 0.88, 0.70, 1.0]},
        {"NAME": "fan4X", "TYPE": "float", "DEFAULT": -0.67, "MIN": -2.0, "MAX": 2.0},
        {"NAME": "fan4Y", "TYPE": "float", "DEFAULT": 1, "MIN": -2.0, "MAX": 2.0},
        {"NAME": "fan4Z", "TYPE": "float", "DEFAULT": 2, "MIN": -0.5, "MAX": 5.0},
        {"NAME": "fan4Fill", "TYPE": "float", "DEFAULT": 0, "MIN": 0.0, "MAX": 360.0},
        {"NAME": "fan4Rot", "TYPE": "float", "DEFAULT": 180, "MIN": -180.0, "MAX": 180.0},
        {"NAME": "fan4RayCount", "TYPE": "float", "DEFAULT": 40.0, "MIN": 0.0, "MAX": 96.0},
        {"NAME": "fan4RayLength", "TYPE": "float", "DEFAULT": 1.15, "MIN": 0.0, "MAX": 3.0},
        {"NAME": "fan4RayWidth", "TYPE": "float", "DEFAULT": 0.03, "MIN": 0.0, "MAX": 0.2},
        {"NAME": "fan4RayStyle", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "fan4RayColor", "TYPE": "color", "DEFAULT": [0.83, 0.66, 0.25, 1.0]},
        {"NAME": "fan4RingCount", "TYPE": "float", "DEFAULT": 4.0, "MIN": 0.0, "MAX": 8.0},
        {"NAME": "fan4RingStart", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "fan4RingGap", "TYPE": "float", "DEFAULT": 0.2, "MIN": 0.02, "MAX": 1.0},
        {"NAME": "fan4RingWidth", "TYPE": "float", "DEFAULT": 0.02, "MIN": 0.0, "MAX": 0.2},
        {"NAME": "fan4RingStyle", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "fan4RingColor", "TYPE": "color", "DEFAULT": [0.95, 0.88, 0.70, 1.0]},
        {"NAME": "fan5X", "TYPE": "float", "DEFAULT": 0.67, "MIN": -2.0, "MAX": 2.0},
        {"NAME": "fan5Y", "TYPE": "float", "DEFAULT": 1, "MIN": -2.0, "MAX": 2.0},
        {"NAME": "fan5Z", "TYPE": "float", "DEFAULT": 2, "MIN": -0.5, "MAX": 5.0},
        {"NAME": "fan5Fill", "TYPE": "float", "DEFAULT": 0, "MIN": 0.0, "MAX": 360.0},
        {"NAME": "fan5Rot", "TYPE": "float", "DEFAULT": 180, "MIN": -180.0, "MAX": 180.0},
        {"NAME": "fan5RayCount", "TYPE": "float", "DEFAULT": 40.0, "MIN": 0.0, "MAX": 96.0},
        {"NAME": "fan5RayLength", "TYPE": "float", "DEFAULT": 1.15, "MIN": 0.0, "MAX": 3.0},
        {"NAME": "fan5RayWidth", "TYPE": "float", "DEFAULT": 0.03, "MIN": 0.0, "MAX": 0.2},
        {"NAME": "fan5RayStyle", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "fan5RayColor", "TYPE": "color", "DEFAULT": [0.83, 0.66, 0.25, 1.0]},
        {"NAME": "fan5RingCount", "TYPE": "float", "DEFAULT": 4.0, "MIN": 0.0, "MAX": 8.0},
        {"NAME": "fan5RingStart", "TYPE": "float", "DEFAULT": 0.3, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "fan5RingGap", "TYPE": "float", "DEFAULT": 0.2, "MIN": 0.02, "MAX": 1.0},
        {"NAME": "fan5RingWidth", "TYPE": "float", "DEFAULT": 0.02, "MIN": 0.0, "MAX": 0.2},
        {"NAME": "fan5RingStyle", "TYPE": "float", "DEFAULT": 0.0, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "fan5RingColor", "TYPE": "color", "DEFAULT": [0.95, 0.88, 0.70, 1.0]},

        {"NAME": "bulbsOn",     "TYPE": "float", "DEFAULT": 1.0,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "bulbLayout",  "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 2.0},
        {"NAME": "bulbCount",   "TYPE": "float", "DEFAULT": 28.0, "MIN": 2.0,  "MAX": 120.0},
        {"NAME": "bulbSize",    "TYPE": "float", "DEFAULT": 0.022,"MIN": 0.002,"MAX": 0.1},
        {"NAME": "bulbInset",   "TYPE": "float", "DEFAULT": 0.1,  "MIN": 0.0,  "MAX": 0.8},
        {"NAME": "bulbGlow",    "TYPE": "float", "DEFAULT": 0.3,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "chase",       "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "chaseMode",   "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 2.0},
        {"NAME": "chaseGroup",  "TYPE": "float", "DEFAULT": 3.0,  "MIN": 1.0,  "MAX": 12.0},
        {"NAME": "chaseDuty",   "TYPE": "float", "DEFAULT": 0.3,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "bulbOnColor", "TYPE": "color", "DEFAULT": [1.0, 0.88, 0.60, 1.0]},
        {"NAME": "bulbOffColor","TYPE": "color", "DEFAULT": [0.18, 0.13, 0.06, 1.0]},

        {"NAME": "borderOn",    "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "borderStyle", "TYPE": "float", "DEFAULT": 0.0,  "MIN": 0.0,  "MAX": 2.0},
        {"NAME": "borderWidth", "TYPE": "float", "DEFAULT": 0.06, "MIN": 0.0,  "MAX": 0.3},
        {"NAME": "borderColor", "TYPE": "color", "DEFAULT": [0.83, 0.66, 0.25, 1.0]},

        {"NAME": "backdropOn",  "TYPE": "float", "DEFAULT": 1.0,  "MIN": 0.0,  "MAX": 1.0},
        {"NAME": "backdropColor","TYPE": "color","DEFAULT": [0.03, 0.04, 0.045, 1.0]}
    ]
}*/

#define ASPECT (RENDERSIZE.x / RENDERSIZE.y)   // half width of the screen

void over(inout vec3 col, inout float a, vec3 c, float ca) {
    float na = ca + a * (1.0 - ca);
    if (na > 0.0) col = (c * ca + col * a * (1.0 - ca)) / na;
    a = na;
}
float triWave(float x) { return abs(fract(x) - 0.5) * 2.0; }     // 1 .. 0 .. 1
float cover(float d, float px) { return smoothstep(px, -px, d); } // d < 0 inside

// angle from +y axis in degrees (clockwise positive), relative to rot, wrapped to -180..180
float relAngle(vec2 v, float rot) {
    float ang = degrees(atan(v.x, v.y)) - rot;
    return mod(ang + 180.0, 360.0) - 180.0;
}

// fan parameters; X/Y: -1 = centre on the screen edge, 0 = middle, +-2 = well outside
vec4 fanP(int i) {
    if (i == 0) return vec4(fan1X * ASPECT, fan1Y, fan1Z, fan1Fill);
    if (i == 1) return vec4(fan2X * ASPECT, fan2Y, fan2Z, fan2Fill);
    if (i == 2) return vec4(fan3X * ASPECT, fan3Y, fan3Z, fan3Fill);
    if (i == 3) return vec4(fan4X * ASPECT, fan4Y, fan4Z, fan4Fill);
    return vec4(fan5X * ASPECT, fan5Y, fan5Z, fan5Fill);
}
float fanRot(int i) {
    if (i == 0) return fan1Rot;
    if (i == 1) return fan2Rot;
    if (i == 2) return fan3Rot;
    if (i == 3) return fan4Rot;
    return fan5Rot;
}
vec4 rayP(int i) {
    if (i == 0) return vec4(fan1RayCount, fan1RayLength, fan1RayWidth, fan1RayStyle);
    if (i == 1) return vec4(fan2RayCount, fan2RayLength, fan2RayWidth, fan2RayStyle);
    if (i == 2) return vec4(fan3RayCount, fan3RayLength, fan3RayWidth, fan3RayStyle);
    if (i == 3) return vec4(fan4RayCount, fan4RayLength, fan4RayWidth, fan4RayStyle);
    return vec4(fan5RayCount, fan5RayLength, fan5RayWidth, fan5RayStyle);
}
vec4 rayC(int i) {
    if (i == 0) return fan1RayColor;
    if (i == 1) return fan2RayColor;
    if (i == 2) return fan3RayColor;
    if (i == 3) return fan4RayColor;
    return fan5RayColor;
}
vec4 ringP(int i) {
    if (i == 0) return vec4(fan1RingCount, fan1RingStart, fan1RingGap, fan1RingWidth);
    if (i == 1) return vec4(fan2RingCount, fan2RingStart, fan2RingGap, fan2RingWidth);
    if (i == 2) return vec4(fan3RingCount, fan3RingStart, fan3RingGap, fan3RingWidth);
    if (i == 3) return vec4(fan4RingCount, fan4RingStart, fan4RingGap, fan4RingWidth);
    return vec4(fan5RingCount, fan5RingStart, fan5RingGap, fan5RingWidth);
}
float ringSt(int i) {
    if (i == 0) return fan1RingStyle;
    if (i == 1) return fan2RingStyle;
    if (i == 2) return fan3RingStyle;
    if (i == 3) return fan4RingStyle;
    return fan5RingStyle;
}
vec4 ringC(int i) {
    if (i == 0) return fan1RingColor;
    if (i == 1) return fan2RingColor;
    if (i == 2) return fan3RingColor;
    if (i == 3) return fan4RingColor;
    return fan5RingColor;
}
float zScale(float z) { return 1.0 / (1.0 + max(z, -0.5)); }   // Z 0 = normal, higher = further away

// draws one fan (rays, then rings) over col/a
void drawFan(inout vec3 col, inout float a, vec2 p, float pxs, int i) {
    vec4 f = fanP(i);
    float fill = f.w;
    if (fill <= 0.0) return;
    float s = zScale(f.z);
    vec2 q = (p - f.xy) / s;                 // fan space
    float px = pxs / s;
    float r = length(q);
    float ra = relAngle(q, fanRot(i));
    // angular mask: open from the rotation direction to both sides
    float halfFill = fill * 0.5;
    float inFill = fill >= 359.9 ? 1.0 : cover(radians(abs(ra) - halfFill) * max(r, 1e-3), px);

    // rays: fixed pitch (Ray Count = rays in a full circle), fill reveals them
    vec4 rp = rayP(i);
    vec4 rcol = rayC(i);
    float rayLength = rp.y, rayWidth = rp.z;
    float n = floor(rp.x + 0.5);
    if (n >= 1.0) {
        float pitch = 360.0 / n;
        float t = ra / pitch;
        float k = floor(t + 0.5);
        float dAng = radians((t - k) * pitch);
        float across = abs(sin(dAng)) * r;
        int st = int(floor(rp.w + 0.5));
        float len = rayLength;
        float w = rayWidth * 0.5;
        if (st == 1) w = rayWidth * clamp(r / max(rayLength, 1e-3), 0.0, 1.0);          // wedge
        if (st == 2 && mod(k, 2.0) > 0.5) len = rayLength * 0.62;                       // long-short
        float d = max(across - w, r - len);
        if (abs(dAng) > 1.4) d = 1.0;                                                     // far side of a wide pitch
        over(col, a, rcol.rgb, cover(d, px) * inFill * rcol.a);
    }

    // rings
    vec4 gp = ringP(i);
    vec4 gcol = ringC(i);
    float ringCount = gp.x, ringStart = gp.y, ringSpacing = gp.z, ringWidth = gp.w;
    if (ringCount > 0.0) {
        int st = int(floor(ringSt(i) + 0.5));
        for (int j = 0; j < 8; j++) {
            float fj = float(j);
            float vis = clamp(ringCount - fj, 0.0, 1.0);
            if (vis <= 0.0) break;
            float R = ringStart + fj * ringSpacing;
            float hw = ringWidth * 0.5;
            float d;
            if (st == 0) {
                d = abs(r - R) - hw;
            } else if (st == 1) {                         // stepped: steps inward to the sides, joined by risers
                float stepAng = 18.0;
                float sIdx = floor(abs(ra) / stepAng);
                float Rs = R * (1.0 - 0.06 * sIdx);
                d = abs(r - Rs) - hw;
                float edge = (sIdx + 1.0) * stepAng;
                float Rn = R * (1.0 - 0.06 * (sIdx + 1.0));
                float sideDist = abs(sin(radians(abs(ra) - edge))) * r;
                float riser = max(sideDist - hw, max(r - Rs - hw, Rn - hw - r));
                d = min(d, riser);
            } else {                                      // chevron: zigzag ring
                float amp = ringSpacing * 0.22;
                float Rz = R + amp * (triWave(ra / 10.0) * 2.0 - 1.0);
                d = abs(r - Rz) - hw * 1.3;
            }
            over(col, a, gcol.rgb, cover(d, px) * inFill * vis * gcol.a);
        }
    }
}

// ---------- bulbs ----------
float perimeterLen(float ins) { return 4.0 * (ASPECT - ins) + 4.0 * (1.0 - ins); }
vec2 framePos(float s, float ins) {      // s along perimeter, clockwise from top-left
    float w = 2.0 * (ASPECT - ins), h = 2.0 * (1.0 - ins);
    s = mod(s, 2.0 * (w + h));
    if (s < w) return vec2(-ASPECT + ins + s, 1.0 - ins);
    s -= w;
    if (s < h) return vec2(ASPECT - ins, 1.0 - ins - s);
    s -= h;
    if (s < w) return vec2(ASPECT - ins - s, -1.0 + ins);
    s -= w;
    return vec2(-ASPECT + ins, -1.0 + ins + s);
}
float archFill() { return fan1Fill > 0.0 ? fan1Fill : 180.0; }
float outerRingR() {
    float n = max(ceil(fan1RingCount), 1.0);
    return fan1RingStart + (n - 1.0) * fan1RingGap + fan1RingGap * 0.5;
}
vec2 bulbPos(float k, float n, int lay) {
    if (lay == 0) {                              // rows: top left->right, bottom right->left
        float per = ceil(n * 0.5);
        float y = 1.0 - bulbInset;
        if (k < per) return vec2(-ASPECT + (k + 0.5) / per * 2.0 * ASPECT, y);
        float j = k - per;
        return vec2(ASPECT - (j + 0.5) / per * 2.0 * ASPECT, -y);
    } else if (lay == 1) {                       // frame
        return framePos((k + 0.5) / n * perimeterLen(bulbInset), bulbInset);
    } else {                                     // arch along the outermost ring of fan 1
        float sp = archFill();
        float ang = radians(fan1Rot - sp * 0.5 + (k + 0.5) / n * sp);
        return vec2(fan1X * ASPECT, fan1Y) + zScale(fan1Z) * outerRingR() * vec2(sin(ang), cos(ang));
    }
}
float nearestBulb(vec2 p, float n, int lay) {
    if (lay == 0) {
        float per = ceil(n * 0.5);
        float i = clamp(floor((p.x + ASPECT) / (2.0 * ASPECT) * per), 0.0, per - 1.0);
        return p.y >= 0.0 ? i : per + (per - 1.0 - i);
    } else if (lay == 1) {
        float ins = bulbInset;
        float w = 2.0 * (ASPECT - ins), h = 2.0 * (1.0 - ins);
        vec2 q = clamp(p, vec2(-ASPECT + ins, -1.0 + ins), vec2(ASPECT - ins, 1.0 - ins));
        float dl = q.x + ASPECT - ins, dr = ASPECT - ins - q.x, dt = 1.0 - ins - q.y, db = q.y + 1.0 - ins;
        float m = min(min(dl, dr), min(dt, db));
        float s = 0.0;
        if (m == dt) s = dl;
        else if (m == dr) s = w + dt;
        else if (m == db) s = w + h + dr;
        else s = 2.0 * w + h + db;
        return floor(s / perimeterLen(ins) * n);
    } else {
        float sp = archFill();
        float aR = relAngle(p - vec2(fan1X * ASPECT, fan1Y), fan1Rot);
        return clamp(floor((aR + sp * 0.5) / sp * n), 0.0, n - 1.0);
    }
}
float bulbLit(float k, float n) {
    int m = int(floor(chaseMode + 0.5));
    if (m == 0) {                                // run
        float g = max(floor(chaseGroup + 0.5), 1.0);
        return step(fract((k - chase * g) / g), chaseDuty - 1e-4);
    } else if (m == 1) {                         // alternate
        float phase = floor(mod(chase * 2.0, 2.0));
        return 1.0 - abs(mod(k, 2.0) - phase);
    } else {                                     // fill and empty once per cycle
        float level = 1.0 - triWave(chase);
        return step(k + 0.5, level * n);
    }
}

void main() {
    vec2 res = RENDERSIZE;
    vec2 p = (gl_FragCoord.xy - 0.5 * res) / res.y * 2.0;
    float px = 2.0 / res.y;

    vec3 col = vec3(0.0);
    float a = 0.0;
    if (backdropOn > 0.5) { col = backdropColor.rgb; a = 1.0; }

    // ---------- fans, drawn far (high Z) to near ----------
    float done0 = 0.0, done1 = 0.0, done2 = 0.0, done3 = 0.0, done4 = 0.0;
    for (int pass = 0; pass < 5; pass++) {
        int pick = -1;
        float best = -1e9;
        for (int i = 0; i < 5; i++) {
            float dn = (i == 0) ? done0 : (i == 1) ? done1 : (i == 2) ? done2 : (i == 3) ? done3 : done4;
            float z = fanP(i).z;
            if (dn < 0.5 && z > best) { best = z; pick = i; }
        }
        if (pick < 0) break;
        if (pick == 0) done0 = 1.0; else if (pick == 1) done1 = 1.0; else if (pick == 2) done2 = 1.0;
        else if (pick == 3) done3 = 1.0; else done4 = 1.0;
        drawFan(col, a, p, px, pick);
    }

    // ---------- border (closed: every edge holds a whole number of pattern periods) ----------
    if (borderOn > 0.5 && borderWidth > 0.0) {
        float bw = borderWidth;
        float dx = ASPECT - abs(p.x), dy = 1.0 - abs(p.y);
        float vEdge = min(dx, dy);
        bool horiz = dy <= dx;
        float L = horiz ? 2.0 * ASPECT : 2.0;                  // edge length
        float s = horiz ? p.x + ASPECT : p.y + 1.0;            // 0 at a corner
        int st = int(floor(borderStyle + 0.5));
        float cov = 0.0;
        if (vEdge < bw) {
            float vb = vEdge / bw;
            if (st == 0) {                                     // zigzag between two rules
                float P = L / max(floor(L / (bw * 1.5) + 0.5), 1.0);
                float z = 0.2 + 0.6 * triWave(s / P);
                cov = max(cover(abs(vb - z) * bw - bw * 0.07, px),
                      max(cover(abs(vb - 0.06) * bw - bw * 0.05, px), cover(abs(vb - 0.94) * bw - bw * 0.05, px)));
            } else if (st == 1) {                              // chevron stripes
                float P = L / max(floor(L / (bw * 1.2) + 0.5), 1.0);
                float f = fract(s / P + abs(vb - 0.5) * 0.8);
                cov = cover((abs(f - 0.5) - 0.25) * bw, px);
            } else {                                           // two rules
                cov = max(cover(abs(vb - 0.15) * bw - bw * 0.06, px), cover(abs(vb - 0.55) * bw - bw * 0.04, px));
            }
        }
        if (st == 2) {                                         // ziggurat corners
            vec2 q = vec2(ASPECT - abs(p.x), 1.0 - abs(p.y));
            for (int i = 0; i < 4; i++) {
                float fi = float(i);
                float dd = max(q.x - bw * (fi + 1.0) * 1.2, q.y - bw * (4.0 - fi) * 1.2);
                cov = max(cov, cover(dd, px));
            }
        }
        over(col, a, borderColor.rgb, cov * borderColor.a);
    }

    // ---------- bulbs ----------
    if (bulbsOn > 0.5) {
        float nb = floor(bulbCount + 0.5);
        int lay = int(floor(bulbLayout + 0.5));
        float k0 = nearestBulb(p, nb, lay);
        float glow = 0.0;
        float discLit = 0.0;
        float bestD = 1e5;
        for (int o = -2; o <= 2; o++) {
            float k = k0 + float(o);
            if (lay == 2 && (k < 0.0 || k >= nb)) continue;
            k = mod(k, nb);
            vec2 bp = bulbPos(k, nb, lay);
            float d = length(p - bp);
            float lit = bulbLit(k, nb);
            glow = max(glow, lit * exp(-max(d - bulbSize, 0.0) / (bulbSize * 2.5)));
            if (d < bestD) { bestD = d; discLit = lit; }
        }
        over(col, a, bulbOnColor.rgb, glow * bulbGlow * 0.8 * bulbOnColor.a);
        vec3 bc = discLit > 0.5 ? bulbOnColor.rgb : bulbOffColor.rgb;
        float bca = discLit > 0.5 ? bulbOnColor.a : bulbOffColor.a;
        over(col, a, bc, cover(bestD - bulbSize, px) * bca);
    }

    gl_FragColor = vec4(col, a);
}
