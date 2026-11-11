/*{
  "DESCRIPTION": "Star Globe - glowing points over the clip below: fireflies that roam the whole frame, or stars caught in a vessel that fall, bounce, collide and simmer. Runs in real time, independent of the frame rate. Put a black clip below for points on black. Scale sizes everything relative to the output height. Physics: Gravity and Gravity Dir (270 = down), Bounce (how much speed a point keeps at the wall and at other points), Air Drag, Simmer (random kicks, makes a pile boil), Wander (points fly about on their own, like fireflies), Collide (points push each other apart; 0 = they pass through each other). Container: Shape (Frame = the edges of the output, Circle, Polygon, Heart), Corners (for Polygon), Container X and Container Y (0 = centre, -1 and 1 = frame edges), Size (radius in output heights), Width (1 = round, larger = wider), Rotation, Line Width (0 hides the line; the points alone then show the shape), Line Color. Stars: Amount (up to 512; a new point appears inside the container), Star Size (radius of the solid core, also the collision size), Glow Size, Star Color, Glow Color, Twinkle (each point flickers on its own). Shader: Resource/StarGlobe.fs.",
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
      "NAME": "clockH",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 24.0
    },
    {
      "NAME": "clockM",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 60.0
    },
    {
      "NAME": "clockS",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 60.0
    },
    {
      "NAME": "clockMs",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1000.0
    },
    {
      "NAME": "scale",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "gravity",
      "TYPE": "float",
      "DEFAULT": 1.5,
      "MIN": 0.0,
      "MAX": 20.0
    },
    {
      "NAME": "gravityDir",
      "TYPE": "float",
      "DEFAULT": 270.0,
      "MIN": 0.0,
      "MAX": 360.0
    },
    {
      "NAME": "bounce",
      "TYPE": "float",
      "DEFAULT": 0.4,
      "MIN": 0.0,
      "MAX": 2.0
    },
    {
      "NAME": "airDrag",
      "TYPE": "float",
      "DEFAULT": 0.3,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "simmer",
      "TYPE": "float",
      "DEFAULT": 0.3,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "wander",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "collide",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 2.0
    },
    {
      "NAME": "shape",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 3.0
    },
    {
      "NAME": "corners",
      "TYPE": "float",
      "DEFAULT": 6.0,
      "MIN": 3.0,
      "MAX": 24.0
    },
    {
      "NAME": "containerX",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -2.0,
      "MAX": 2.0
    },
    {
      "NAME": "containerY",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -2.0,
      "MAX": 2.0
    },
    {
      "NAME": "size",
      "TYPE": "float",
      "DEFAULT": 0.85,
      "MIN": 0.0,
      "MAX": 5.0
    },
    {
      "NAME": "width",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.1,
      "MAX": 10.0
    },
    {
      "NAME": "rotation",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -360.0,
      "MAX": 360.0
    },
    {
      "NAME": "lineWidth",
      "TYPE": "float",
      "DEFAULT": 0.004,
      "MIN": 0.0,
      "MAX": 0.2
    },
    {
      "NAME": "lineColor",
      "TYPE": "color",
      "DEFAULT": [
        0.85,
        0.8,
        0.7,
        1.0
      ]
    },
    {
      "NAME": "amount",
      "TYPE": "float",
      "DEFAULT": 120.0,
      "MIN": 0.0,
      "MAX": 512.0
    },
    {
      "NAME": "starSize",
      "TYPE": "float",
      "DEFAULT": 0.015,
      "MIN": 0.0,
      "MAX": 0.2
    },
    {
      "NAME": "glowSize",
      "TYPE": "float",
      "DEFAULT": 0.04,
      "MIN": 0.0,
      "MAX": 0.5
    },
    {
      "NAME": "starColor",
      "TYPE": "color",
      "DEFAULT": [
        1.0,
        1.0,
        1.0,
        1.0
      ]
    },
    {
      "NAME": "glowColor",
      "TYPE": "color",
      "DEFAULT": [
        0.75,
        0.85,
        1.0,
        1.0
      ]
    },
    {
      "NAME": "twinkle",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1.0
    }
  ],
  "PASSES": [
    {
      "TARGET": "timeState",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "1",
      "HEIGHT": "1"
    },
    {
      "TARGET": "partsB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "512",
      "HEIGHT": "2"
    },
    {
      "TARGET": "partsA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "512",
      "HEIGHT": "2"
    },
    {
      "TARGET": "partsB",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "512",
      "HEIGHT": "2"
    },
    {
      "TARGET": "partsA",
      "PERSISTENT": true,
      "FLOAT": true,
      "WIDTH": "512",
      "HEIGHT": "2"
    },
    {}
  ]
}*/

const int N = 512;
const int NSUB = 4;
const int PASS_TIME = 0, PASS_SIM0 = 1, PASS_RENDER = 1 + NSUB;
const float SIG = 0.5;              // marks buffer contents written by this shader
const float OFF = 10000.0;          // position of a point that is not in use
const float VMAX = 6.0;             // speed limit, in half output heights per second

float safe(float x, float lo, float hi) { return (x == x) ? clamp(x, lo, hi) : clamp(0.0, lo, hi); }
float hash3(vec3 p) {
    p = fract(p * vec3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return fract((p.x + p.y) * p.z);
}
float noise1(float t, float seed) {                 // smooth random curve 0..1
    float i = floor(t), f = fract(t);
    f = f * f * (3.0 - 2.0 * f);
    return mix(hash3(vec3(i, seed, 7.1)), hash3(vec3(i + 1.0, seed, 7.1)), f);
}
void over(inout vec3 col, inout float a, vec3 c, float ca) {
    float na = ca + a * (1.0 - ca);
    if (na > 0.0001) col = (c * ca + col * a * (1.0 - ca)) / na;
    a = na;
}
vec2 rot2(vec2 p, float a) { float c = cos(a), s = sin(a); return vec2(c * p.x - s * p.y, s * p.x + c * p.y); }

// ---------- container shapes; signed distance in scene units, negative inside ----------
float sdHeart(vec2 p) {                             // a heart of about unit half height, centred
    p *= 0.53;
    p.y += 0.55;
    p.x = abs(p.x);
    if (p.y + p.x > 1.0) return (length(p - vec2(0.25, 0.75)) - 0.3535) / 0.53;
    vec2 a = p - vec2(0.0, 1.0);
    float m = 0.5 * max(p.x + p.y, 0.0);
    vec2 b = p - vec2(m);
    float s = p.x - p.y;
    return sqrt(min(dot(a, a), dot(b, b))) * (s < 0.0 ? -1.0 : 1.0) / 0.53;
}
float sdPolygon(vec2 p, float r, float n) {         // regular polygon, circumradius r, one corner up
    float an = 3.14159265 / n;
    float ang = atan(p.x, p.y);
    float bn = mod(ang, 2.0 * an) - an;
    p = length(p) * vec2(cos(bn), abs(sin(bn)));
    p -= r * vec2(cos(an), sin(an));
    p.y += clamp(-p.y, 0.0, r * sin(an));
    return length(p) * (p.x < 0.0 ? -1.0 : 1.0);
}

struct Box { int shape; vec2 c; float size; float width; float rot; float corners; vec2 half_; };

float sdFrame(vec2 p, vec2 h) {
    vec2 d = abs(p) - h;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}
float sdShape(vec2 p, Box k) {
    if (k.shape == 0) return sdFrame(p, k.half_);  // Frame: the edges of the output are the wall
    vec2 q = rot2(p - k.c, -k.rot);
    q.x /= k.width;
    float squash = min(k.width, 1.0);
    if (k.shape == 1) return (length(q) - k.size) * squash;
    if (k.shape == 2) return sdPolygon(q, k.size, k.corners) * squash;
    return sdHeart(q / max(k.size, 1e-4)) * k.size * squash;
}
float sdContainer(vec2 p, Box k) {                  // the points never leave the frame, whatever the shape
    return max(sdShape(p, k), sdFrame(p, k.half_));
}
vec2 wallNormal(vec2 p, Box k) {
    float e = 1e-3 * k.half_.y;                     // a small step in scene units
    vec2 g = vec2(sdContainer(p + vec2(e, 0.0), k) - sdContainer(p - vec2(e, 0.0), k),
                  sdContainer(p + vec2(0.0, e), k) - sdContainer(p - vec2(0.0, e), k));
    float l = length(g);
    return l > 1e-8 ? g / l : vec2(0.0, 1.0);
}

vec4 part(int src, int j) {                         // row 0: position and velocity; row 1: signature
    vec2 t = vec2((float(j) + 0.5) / float(N), 0.25);
    vec2 t2 = vec2(t.x, 0.75);
    vec4 sg = src == 0 ? IMG_NORM_PIXEL(partsA, t2) : IMG_NORM_PIXEL(partsB, t2);
    if (!(abs(sg.r - SIG) < 0.01)) return vec4(OFF, OFF, 0.0, 0.0);   // a fresh buffer (zeros or garbage): no point yet
    return src == 0 ? IMG_NORM_PIXEL(partsA, t) : IMG_NORM_PIXEL(partsB, t);
}
bool valid(vec4 s) {
    return s.x == s.x && s.y == s.y && s.z == s.z && s.w == s.w && abs(s.x) <= OFF && abs(s.y) <= OFF && abs(s.z) <= 1e4 && abs(s.w) <= 1e4;
}

void main() {
    vec2 uv = isf_FragNormCoord;
    float safeScale = clamp(scale, 0.001, 1000.0);
    vec2 inSize = IMG_SIZE(inputImage);
    vec2 res = max(inSize.x > 0.5 && inSize.y > 0.5 ? inSize : RENDERSIZE, vec2(1.0));   // in Arena RENDERSIZE is the size of the current pass, not of the output
    float aspect = res.x / res.y;
    vec2 half_ = vec2(aspect, 1.0) / safeScale;            // half width and height of the frame in scene units

    // ---------- time ----------
    if (PASSINDEX == PASS_TIME) {
        vec4 t0 = IMG_NORM_PIXEL(timeState, vec2(0.5));
        if (!(abs(t0.a - SIG) < 0.01)) t0 = vec4(0.0, 0.0, 0.0, SIG);
        t0 = vec4(safe(t0.r, -1e6, 1e6), safe(t0.g, 0.0, 0.1), safe(t0.b, 0.0, 3600.0), SIG);
        float now = clockH * 3600.0 + clockM * 60.0 + clockS + clockMs * 0.001;
        float raw = clamp(now - t0.r, 0.0, 0.1);
        float dt = t0.r <= 0.0 ? 1.0 / 60.0 : mix(t0.g, raw, 0.25);
        dt = safe(dt, 0.0, 0.1);
        gl_FragColor = vec4(now, dt, mod(t0.b + dt, 3600.0), SIG);
        return;
    }
    vec4 tS = IMG_NORM_PIXEL(timeState, vec2(0.5));
    float dt = safe(tS.g, 0.0, 0.1), simTime = safe(tS.b, 0.0, 3600.0);

    Box k;
    k.shape = int(floor(safe(shape, 0.0, 3.0) + 0.5));
    k.half_ = half_;
    k.c = vec2(safe(containerX, -1000.0, 1000.0), safe(containerY, -1000.0, 1000.0)) * half_;
    k.size = max(safe(size, 0.0, 1000.0), 1e-4);
    k.width = clamp(safe(width, 0.0, 1000.0), 0.02, 1000.0);
    k.rot = radians(safe(rotation, -1e5, 1e5));
    k.corners = floor(safe(corners, 3.0, 64.0) + 0.5);

    float amountF = safe(amount, 0.0, float(N));
    int count = int(ceil(amountF));
    float r = max(safe(starSize, 0.0, 100.0), 0.0);         // radius of the solid core, also the collision radius

    // ---------- points: forces, collision, wall ----------
    if (PASSINDEX >= PASS_SIM0 && PASSINDEX < PASS_RENDER) {
        int stepIdx = PASSINDEX - PASS_SIM0;
        int src = int(mod(float(stepIdx), 2.0)) == 0 ? 0 : 1;
        int i = int(floor(uv.x * float(N)));
        if (uv.y > 0.5) { gl_FragColor = vec4(SIG, 0.0, 0.0, 1.0); return; }
        float sdt = dt / float(NSUB);
        vec4 s = part(src, i);
        bool was = valid(s) && abs(s.x) < OFF * 0.5;
        if (i >= count) { gl_FragColor = vec4(OFF, OFF, 0.0, 0.0); return; }
        float seed = float(i);
        if (!was) {
            // a new point appears somewhere inside the container, at rest
            vec2 lo = k.shape == 0 ? -half_ : k.c - vec2(k.size * max(k.width, 1.0) * 1.5);
            vec2 hi = k.shape == 0 ? half_ : k.c + vec2(k.size * max(k.width, 1.0) * 1.5);
            vec2 pos = k.shape == 0 ? vec2(0.0) : k.c;
            for (int t = 0; t < 24; t++) {
                vec2 cand = mix(lo, hi, vec2(hash3(vec3(seed, float(t), simTime)), hash3(vec3(seed, float(t) + 50.0, simTime + 3.1))));
                if (sdContainer(cand, k) < -r) { pos = cand; break; }
            }
            gl_FragColor = vec4(pos, 0.0, 0.0);
            return;
        }
        vec2 pos = s.xy, v = s.zw;
        float bnc = max(safe(bounce, 0.0, 100.0), 0.0);

        // gravity, self-propelled wandering, random kicks (simmer), air drag
        float ga = radians(safe(gravityDir, -1e5, 1e5));
        v += vec2(cos(ga), sin(ga)) * max(safe(gravity, 0.0, 1000.0), 0.0) * sdt;
        float wnd = max(safe(wander, 0.0, 1000.0), 0.0);
        if (wnd > 0.0) {
            // each point steers towards its own slowly turning direction; near the wall it turns away
            float wa = 6.2831853 * 2.0 * noise1(simTime * 0.35 + seed * 0.37, seed);
            vec2 dir = vec2(cos(wa), sin(wa));
            float look = 0.04 * k.half_.y + length(v) * 0.15;        // look a little further ahead when fast
            vec2 ahead = pos + dir * look;
            float sdw = sdContainer(ahead, k);
            float near = smoothstep(-look, -0.5 * look, sdw + r);   // fully mirrored well before the wall: no sliding along it
            vec2 nw = wallNormal(ahead, k);                          // in a corner this points diagonally out of it
            float out_ = dot(dir, nw);
            if (out_ > 0.0) dir = normalize(dir - nw * out_ * 2.0 * near + 1e-6);   // mirror only the part that heads into the wall
            v += (dir * wnd * 0.25 - v) * (1.0 - exp(-1.5 * sdt)) * min(wnd, 1.0);
        }
        float ka = 6.2831853 * hash3(vec3(seed, simTime * 977.0, float(stepIdx)));
        float kr = sqrt(-2.0 * log(max(hash3(vec3(seed + 0.5, simTime * 613.0, float(stepIdx) + 9.0)), 1e-6)));
        v += vec2(cos(ka), sin(ka)) * kr * max(safe(simmer, 0.0, 1000.0), 0.0) * 0.6 * sqrt(sdt);
        v *= exp(-max(safe(airDrag, 0.0, 1000.0), 0.0) * sdt);

        // collision: overlapping points push each other apart and bounce off each other
        float coll = max(safe(collide, 0.0, 100.0), 0.0);
        if (coll > 0.0 && r > 0.0) {
            vec2 corr = vec2(0.0), dv = vec2(0.0);
            float contacts = 0.0;
            float minD = 2.0 * r;
            for (int j = 0; j < N; j++) {
                if (j >= count) break;
                if (j == i) continue;
                vec4 o = part(src, j);
                if (!valid(o) || abs(o.x) >= OFF * 0.5) continue;
                vec2 d = pos - o.xy;
                float dist = length(d);
                if (dist >= minD) continue;
                vec2 n;
                if (dist > minD * 1e-4) n = d / dist;
                else {
                    float a2 = hash3(vec3(float(min(i, j)), float(max(i, j)), 1.7)) * 6.2831853;
                    n = vec2(cos(a2), sin(a2)) * (i < j ? 1.0 : -1.0);
                }
                corr += n * (minD - dist) * 0.5;
                contacts += 1.0;
                float vrel = dot(v - o.zw, n);
                if (vrel < 0.0) dv -= n * vrel * (1.0 + min(bnc, 1.0)) * 0.5;
            }
            corr /= max(1.0, contacts * 0.5);                   // averaged over the contacts: a dense pile cannot build up energy
            dv /= max(1.0, contacts);
            float cl = length(corr);
            if (cl > r) corr *= r / cl;                         // a crowded point moves at most one radius per step
            pos += corr * min(coll, 2.0) * 0.8;
            v += dv * min(coll, 1.0);
        }

        float sp = length(v), vmax = VMAX * k.half_.y;          // at most a few output heights per second
        if (sp > vmax) v *= vmax / sp;
        pos += v * sdt;

        // wall: back inside, and bounce off it
        for (int t = 0; t < 2; t++) {
            float sd = sdContainer(pos, k);
            if (sd + r > 0.0) {
                vec2 n = wallNormal(pos, k);
                pos -= n * (sd + r);
                float vn = dot(v, n);
                if (vn > 0.0) {
                    v -= n * vn * (1.0 + bnc);
                    float sa = (hash3(vec3(seed, simTime * 131.0, float(stepIdx * 2 + t))) - 0.5) * 0.5;   // a rough wall: up to ±14° scatter
                    v = rot2(v, sa);
                }
            }
        }
        if (!(pos.x == pos.x && pos.y == pos.y && v.x == v.x && v.y == v.y)) { pos = k.shape == 0 ? vec2(0.0) : k.c; v = vec2(0.0); }
        pos = clamp(pos, vec2(-OFF * 0.4), vec2(OFF * 0.4));
        gl_FragColor = vec4(pos, v);
        return;
    }

    // ---------- picture ----------
    vec2 p = (uv - 0.5) * half_ * 2.0;
    float px = 2.0 / (res.y * safeScale);                     // one output pixel in scene units
    vec4 src = IMG_NORM_PIXEL(inputImage, uv);
    vec3 col = src.rgb;
    float a = src.a;

    float lw = max(safe(lineWidth, 0.0, 100.0), 0.0);
    if (lw > 0.0) {
        float sd = sdShape(p, k);
        float line = 1.0 - smoothstep(lw * 0.5, lw * 0.5 + px, abs(sd));
        over(col, a, lineColor.rgb, line * lineColor.a);
    }

    float gr = max(safe(glowSize, 0.0, 100.0), 0.0);
    float tw = clamp(safe(twinkle, 0.0, 1.0), 0.0, 1.0);
    float glow = 0.0, core = 0.0;
    float lastFade = amountF - floor(amountF);
    if (lastFade < 1e-4) lastFade = 1.0;
    float reach = max(gr * 3.0, r + px);
    for (int j = 0; j < N; j++) {
        if (j >= count) break;
        vec4 s = part(0, j);
        if (!valid(s)) continue;
        vec2 d = p - s.xy;
        if (abs(d.x) > reach || abs(d.y) > reach) continue;
        float fade = j == count - 1 ? lastFade : 1.0;
        float flick = smoothstep(0.35, 0.85, noise1(simTime * 1.3 + float(j) * 0.71, float(j) + 0.5));
        float bright = mix(1.0, flick * 1.25, tw) * fade;
        float dd = dot(d, d);
        if (gr > 0.0) glow += exp(-dd / (gr * gr)) * bright;
        if (r > 0.0) core = max(core, (1.0 - smoothstep(r - px, r + px, sqrt(dd))) * clamp(bright, 0.0, 1.0));
    }
    over(col, a, glowColor.rgb, (1.0 - exp(-glow * 0.8)) * glowColor.a);
    over(col, a, starColor.rgb, core * starColor.a);

    if (!(col.r == col.r && col.g == col.g && col.b == col.b && a == a)) { col = src.rgb; a = src.a; }   // never pass NaN on
    col = clamp(col, 0.0, 1e4);
    gl_FragColor = a > 0.0001 ? vec4(col, clamp(a, 0.0, 1.0)) : vec4(0.0);
}
