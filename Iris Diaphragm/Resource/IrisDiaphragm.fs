/*{
  "DESCRIPTION": "Iris Diaphragm - a mechanical blade iris that closes over the clip below. Clock drives it: 0 = open, 50 = closed, 100 = open again; animate it with the clip Timeline or BPM Sync. Scale sizes the edges relative to the output height; the iris always covers the whole frame. Iris: Center X and Center Y (0 = centre, -1 and 1 = frame edges), Blade Count, Blade Rotation, Blade Twist (how far the blades turn while closing), Blade Curve (bends the blades into a pinwheel). Look: Blade Color, Edge Color, Edge Width, Edge Softness (soft opening), Shading (shadow and sheen on the blades). Set Blade Color to white and use the layer as a mask to show another clip on the blades. Shader: Resource/IrisDiaphragm.fs.",
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
      "NAME": "clock",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 100.0
    },
    {
      "NAME": "scale",
      "TYPE": "float",
      "DEFAULT": 1.0,
      "MIN": 0.0,
      "MAX": 10.0
    },
    {
      "NAME": "centerX",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -2.0,
      "MAX": 2.0
    },
    {
      "NAME": "centerY",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -2.0,
      "MAX": 2.0
    },
    {
      "NAME": "bladeCount",
      "TYPE": "float",
      "DEFAULT": 9.0,
      "MIN": 3.0,
      "MAX": 32.0
    },
    {
      "NAME": "bladeRotation",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": -360.0,
      "MAX": 360.0
    },
    {
      "NAME": "bladeTwist",
      "TYPE": "float",
      "DEFAULT": 60.0,
      "MIN": -360.0,
      "MAX": 360.0
    },
    {
      "NAME": "bladeCurve",
      "TYPE": "float",
      "DEFAULT": 0.6,
      "MIN": -5.0,
      "MAX": 5.0
    },
    {
      "NAME": "bladeColor",
      "TYPE": "color",
      "DEFAULT": [
        0.1,
        0.09,
        0.08,
        1.0
      ]
    },
    {
      "NAME": "edgeColor",
      "TYPE": "color",
      "DEFAULT": [
        0.62,
        0.56,
        0.48,
        1.0
      ]
    },
    {
      "NAME": "edgeWidth",
      "TYPE": "float",
      "DEFAULT": 0.004,
      "MIN": 0.0,
      "MAX": 0.2
    },
    {
      "NAME": "edgeSoftness",
      "TYPE": "float",
      "DEFAULT": 0.0,
      "MIN": 0.0,
      "MAX": 1.0
    },
    {
      "NAME": "shading",
      "TYPE": "float",
      "DEFAULT": 0.6,
      "MIN": 0.0,
      "MAX": 1.0
    }
  ]
}*/

float safe(float x, float lo, float hi) { return (x == x) ? clamp(x, lo, hi) : lo; }
void over(inout vec3 col, inout float a, vec3 c, float ca) {
    float na = ca + a * (1.0 - ca);
    if (na > 0.0001) col = (c * ca + col * a * (1.0 - ca)) / na;
    a = na;
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec4 src = IMG_NORM_PIXEL(inputImage, uv);
    vec3 col = src.rgb;
    float a = src.a;

    vec2 res = max(RENDERSIZE, vec2(1.0));
    float aspect = res.x / res.y;
    float safeScale = clamp(scale, 0.001, 1000.0);
    float px = 2.0 / res.y;
    vec2 p = (uv - 0.5) * vec2(aspect, 1.0) * 2.0;            // height is the reference: y -1..1
    vec2 c = vec2(safe(centerX, -1000.0, 1000.0) * aspect, safe(centerY, -1000.0, 1000.0));
    vec2 q = p - c;
    float rho = length(q);
    float ang = rho > 1e-6 ? atan(q.y, q.x) : 0.0;

    // 0 = open, 50 = closed, 100 = open again
    float phase = safe(clock, -1e6, 1e6);
    float closed = 1.0 - abs(fract(phase / 100.0) * 2.0 - 1.0);
    closed = closed * closed * (3.0 - 2.0 * closed);

    float n = floor(safe(bladeCount, 3.0, 32.0) + 0.5);
    float sector = 6.2831853 / n;
    float span = sector * 1.7;                                 // every blade overlaps the next one
    float open = length(vec2(aspect, 1.0)) + length(c) + 0.1;  // fully open: the aperture is larger than the frame
    float r = (1.0 - closed) * open;
    float rc = max(r, 0.0005) * 2.4;                           // radius of the curved inner edge of each blade
    float rot = radians(safe(bladeRotation, -1e5, 1e5)) + radians(safe(bladeTwist, -1e5, 1e5)) * closed;
    float bend = safe(bladeCurve, -50.0, 50.0);
    float ew = safe(edgeWidth, 0.0, 100.0) * safeScale;
    float soft = px + safe(edgeSoftness, 0.0, 100.0) * 0.08;
    float shade = safe(shading, 0.0, 1.0);

    // find the blade on top: the one whose leading edge passed this point most recently
    float best = 1e9, bestDist = 0.0, bestCover = 0.0;
    for (int i = 0; i < 32; i++) {
        if (float(i) >= n) break;
        float phi = rot + float(i) * sector;
        float t = mod(ang - phi - bend * (rho - r), 6.2831853);
        if (t >= span) continue;
        vec2 dir = vec2(cos(phi + span * 0.5), sin(phi + span * 0.5));
        float d = length(q + dir * (rc - r)) - rc;           // > 0: outside the aperture disk of this blade, on the blade
        float cover = smoothstep(-soft, soft, d);
        if (cover <= 0.0) continue;
        if (t < best) { best = t; bestDist = d; bestCover = cover; }
    }

    if (bestCover > 0.0) {
        float along = best / span;                            // 0 at the leading edge, 1 where the blade disappears under the next
        vec3 body = bladeColor.rgb * (1.0 - shade * 0.5 * (1.0 - smoothstep(0.0, 0.7, along)));   // shadow of the blade above
        body *= 1.0 + shade * 0.25 * (smoothstep(0.0, 1.0, along) - 0.5);                          // a soft sheen across the plate
        float lead = 1.0 - smoothstep(ew, ew + px, best * rho);
        float inner = 1.0 - smoothstep(ew, ew + px, bestDist);
        vec3 bc = mix(body, edgeColor.rgb, max(lead, inner) * edgeColor.a);
        over(col, a, bc, bestCover * bladeColor.a);
    }

    gl_FragColor = a > 0.0001 ? vec4(col, clamp(a, 0.0, 1.0)) : vec4(0.0);
}
