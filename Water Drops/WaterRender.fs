/*{
    "DESCRIPTION": "Water Drops - renders the simulated surface seen from above: light, gloss and sky reflection on the waves, plus the falling drop.",
    "CREDIT": "Water Drops",
    "ISFVSN": "2",
    "CATEGORIES": ["Effect"],
    "INPUTS": [
        {"NAME": "inputImage", "TYPE": "image"},
        {"NAME": "fallFrames", "TYPE": "float", "DEFAULT": 12.0, "MIN": 0.0, "MAX": 60.0},
        {"NAME": "dropSize", "TYPE": "float", "DEFAULT": 0.02, "MIN": 0.003, "MAX": 0.1},
        {"NAME": "relief", "TYPE": "float", "DEFAULT": 8.0, "MIN": 0.0, "MAX": 20.0},
        {"NAME": "shine", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 2.0},
        {"NAME": "lightAngle", "TYPE": "float", "DEFAULT": 135.0, "MIN": 0.0, "MAX": 360.0},
        {"NAME": "backdropColor", "TYPE": "color", "DEFAULT": [0.02, 0.10, 0.14, 1.0]},
        {"NAME": "waveColor", "TYPE": "color", "DEFAULT": [0.65, 0.85, 0.95, 1.0]},
        {"NAME": "backdropOn", "TYPE": "float", "DEFAULT": 1.0, "MIN": 0.0, "MAX": 1.0}
    ]
}*/

// bilinear height lookup from texel centres: smooth whatever filtering the host uses
float H(vec2 uv, vec2 ssz) {
    vec2 p = uv * ssz - 0.5;
    vec2 i = floor(p);
    vec2 f = p - i;
    vec2 c = (i + 0.5) / ssz;
    vec2 t = 1.0 / ssz;
    float a = IMG_NORM_PIXEL(inputImage, c).r;
    float b = IMG_NORM_PIXEL(inputImage, c + vec2(t.x, 0.0)).r;
    float d = IMG_NORM_PIXEL(inputImage, c + vec2(0.0, t.y)).r;
    float e = IMG_NORM_PIXEL(inputImage, c + t).r;
    return mix(mix(a, b, f.x), mix(d, e, f.x), f.y);
}

void main() {
    vec2 uv = isf_FragNormCoord;
    vec2 ssz = IMG_SIZE(inputImage);
    vec2 t = 1.0 / ssz;

    // ----- surface normal from the height field -----
    float hL = H(uv - vec2(t.x, 0.0), ssz);
    float hR = H(uv + vec2(t.x, 0.0), ssz);
    float hD = H(uv - vec2(0.0, t.y), ssz);
    float hU = H(uv + vec2(0.0, t.y), ssz);
    float hC = H(uv, ssz);
    if (uv.y * ssz.y < 1.5 && uv.x * ssz.x < 2.5) { hL = hC; hR = hC; hD = hC; hU = hC; }
    vec2 g = vec2(hR - hL, hU - hD) * 0.5 * relief;
    vec3 n = normalize(vec3(-g, 1.0));

    float la = radians(lightAngle);
    vec3 L = normalize(vec3(cos(la), sin(la), 1.3));
    vec3 V = vec3(0.0, 0.0, 1.0);

    float diff = dot(n, L);
    float spec = pow(max(dot(reflect(-L, n), V), 0.0), 80.0) * shine;
    float sky = clamp((1.0 - n.z) * 12.0, 0.0, 1.0);             // tilted surface reflects the bright sky
    float lap = (hL + hR + hD + hU - 4.0 * hC) * relief;          // converging crests focus light
    float focus = clamp(-lap * 4.0, 0.0, 1.0);

    vec3 col = backdropColor.rgb * (0.65 + 0.35 * diff / max(L.z, 0.001));
    col += waveColor.rgb * (sky * 0.55 + focus * 0.35) + vec3(spec) * mix(vec3(1.0), waveColor.rgb, 0.3);
    float a = 1.0;
    if (backdropOn < 0.5) {                                         // no backdrop: only the waves, flat water transparent
        a = clamp(sky * 0.8 + focus * 0.5 + spec, 0.0, 1.0);
        col = waveColor.rgb * (sky * 0.55 + focus * 0.35) + vec3(spec);
        col = a > 0.0 ? col / a : col;
    }

    // ----- falling drop: shrinks while it falls away from the viewer -----
    vec4 book = IMG_NORM_PIXEL(inputImage, vec2(0.5, 0.5) / IMG_SIZE(inputImage));
    vec4 tgt = IMG_NORM_PIXEL(inputImage, vec2(1.5, 0.5) / IMG_SIZE(inputImage));
    float cd = book.b;                                            // frames left + 1
    if (cd > 0.5 && fallFrames >= 1.0) {
        float k = clamp((cd - 1.0) / fallFrames, 0.0, 1.0);       // 1 = just released, 0 = impact
        vec2 res = RENDERSIZE;
        vec2 d = (uv - tgt.rg) * res / res.y;
        float r = dropSize * (1.0 + 5.0 * k);
        float dist = length(d) / r;
        if (dist < 1.0) {
            vec3 sn = normalize(vec3(d / r, sqrt(max(1.0 - dist * dist, 0.0))));
            float sd = max(dot(sn, L), 0.0);
            float ss = pow(max(dot(reflect(-L, sn), V), 0.0), 40.0);
            vec3 dc = mix(backdropColor.rgb, waveColor.rgb, 0.35 + 0.5 * sd) + vec3(ss);
            float edge = clamp((1.0 - dist) * r * res.y * 0.5, 0.0, 1.0);
            float fade = clamp((1.0 - k) * 6.0, 0.0, 1.0);        // fades in right after release
            float da = edge * fade;
            col = mix(col, dc, da);
            a = max(a, da);
        }
    }
    gl_FragColor = vec4(col, a);
}
