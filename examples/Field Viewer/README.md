# Field Viewer

An example effect patch that shows the field texture of the [Motion Bridge](../../tools/Motion%20Bridge/) exactly as it arrives in a patch. It also measures what the bridge promises: the size the patch gets, where the status band is, whether its counter keeps changing, and how many bridge frames per second arrive. It has no show content. Load it to check the bridge in Arena, and copy its reading of the texture into your own patch.

Last updated: 2026-11-17

## Vision

Every motion patch depends on the field texture arriving the way the bridge README (section Output) promises. Before a show patch is built on it, this viewer answers the questions a show patch cannot: does the texture reach the patch, is it stretched to the frame, which way up do the rows arrive, is the bridge alive, and what does the patch see when the bridge stops.

## Requirements

- Resolume Wire / Arena: 7.29
- Inputs: the layer it sits on (`Texture In`), which plays the Spout source `Motion Bridge`
- Performance: one full-screen shader and a 3 × 1 state texture, light. Not measured.

## Files

| File | Purpose |
|---|---|
| `Field Viewer.wire` | The patch: `Texture In`, `System Time`, the shader, two controls |
| `Resource/FieldViewer.fs` | ISF shader: measurements over time (pass 1), the picture with the panel (pass 2) |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. Start the Motion Bridge (its README, Usage).
3. In Arena (the recipe of the bridge README, Output):
   1. Add the Spout source `Motion Bridge` to a layer as a clip (Sources > Spout). Its *Width* and *Height* must be the texture's own size (e.g. 19 × 48). If the clip shows other values, click the clip's resize button (*Maximize Video*, next to the source name in the clip panel) until they match the texture.
   2. Set the layer's *Auto Size* (layer panel, Video) to *Stretch*.
   3. Put `Field Viewer` on the **layer** as an effect, not on the clip.

## The idea in plain words

The bridge sends a small image: fifteen sixteenths of it are the field in front of the projection, one sixteenth is a band that counts up every frame. Arena stretches that image over the layer, and the viewer paints it full frame. A panel on the left reports, with a traffic light per line, what arrives:

| Line | Shows | Green when |
|---|---|---|
| `IN` | size of the texture the patch gets | equal to `OUT` |
| `OUT` | render size of the patch | equal to `IN` |
| `BAND` | where the band was found: `TOP`, `BOTTOM` (orange: rows flipped) or `NONE` (red: no live band) | `TOP`, as the contract says |
| `FPS` | counter steps per second, i.e. bridge frames that reach the patch | 20 to 40 (the bridge sends 30) |
| `AGE MS` | milliseconds since the counter last changed | below 250 |
| `COUNT` | the counter, 0 to 255 | as `AGE MS` |

The band gets a line at its inner edge and the counter as a bar that grows and wraps about every 8.5 s.

## Try it

With the bridge running and two simulated objects (`bridge.py --sim 2`):

1. **Does the field arrive, stretched?** Red blobs (occupancy) and green trails (motion) fill the frame; `IN` equals `OUT`. Put the same Spout clip on a second layer without the viewer and switch `Show Info` off: both layers show the same picture.
2. **Which way up?** `BAND TOP`.
3. **Is it alive?** `FPS` near 30, `AGE MS` 0, `COUNT` running.
4. **Bridge stopped:** close the bridge. The picture goes black, `BAND NONE`, `FPS 0`, `AGE MS` counts up, the last `COUNT` stays. Start the bridge again: Arena picks the sender up by itself, the lights turn green.

## How the logic works

- `Texture In` → `ISF` (the shader) → `Premultiply Alpha` → `Texture Out`. `System Time` gives the shader the clock (hours, minutes, seconds, milliseconds), as in Water Drops.
- Pass 1 writes a persistent 3 × 1 float texture: the time, the last counter, the time of its last change, the counter steps in the current one-second window, the measured rate and the band side.
- The band is recognised by its signature: R = G = B > 0 and equal at four points across the width. It is looked for at 1/32 from the top and from the bottom edge, so the viewer reports a flipped texture instead of assuming one.
- `FPS` counts the counter's steps, not the frames in which it changed: Arena can render fewer frames than the bridge sends, then one change covers several steps.
- Pass 2 samples the input with `IMG_NORM_PIXEL` at the fragment's normalized coordinate, so it works at any texture size. The panel is drawn with a 3 × 5 pixel font, sized from the shorter side of the frame.
- No `Scale` and no `Clock`: the viewer shows the texture as it is, so the surface model does not apply (docs/PATCH_RULES.md, "Examples and tools").

## Using it in your own patch

Your patch is a layer effect, like the viewer. In its shader:

```glsl
vec4 field = IMG_NORM_PIXEL(inputImage, vec2(x, depth * 15.0 / 16.0));   // x: 0 left .. 1 right end of the front; depth: 0 front .. 1 far edge
float counter = IMG_NORM_PIXEL(inputImage, vec2(0.5, 31.0 / 32.0)).r;   // the band; changes every bridge frame
```

`field.r` is occupancy, `field.g` motion, `field.b` visibility. Never show the band: it is not field data. To stay calm without the bridge, copy pass 1 of `Resource/FieldViewer.fs` (the persistent state and `System Time`) and fade out when `AGE` exceeds a few hundred milliseconds. The patch sees only the field: it draws its own picture and is blended over the layers below with the layer's blend mode and its alpha.

## Parameters

- `Channel` (All / Occupancy / Motion / Visibility, default All): All shows red = occupancy, green = motion, blue = visibility; the others show one channel as grey.
- `Show Info` (Off / On, default On): the panel and the band marks. Off shows the texture as it arrives.

## Any surface

- The viewer fills whatever layer it runs on; the field is stretched to it, which is what the bridge contract defines (x over the whole projection width). The panel scales with the shorter side.

Tested sizes:

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok (Arena) |
| 5760×1080 | 3 side by side | untested |
| 1920×3240 | 3 stacked | untested |
| 1080×1920 | 9:16 | untested |
| 4096×512 | 8:1 | untested |
| 512×512 | 1:1 | untested |

## Known issues

- Not tested: whether the clip follows when the texture size changes (another area or cell size): the clip keeps its *Width* and *Height*.
- Not tested: whether column 0 really is the left end of the front (needs an object at a known place).

## Changelog

- 2026-11-17: Rebuilt in Wire (#115). The first version, written by hand, missed `Texture In` in the patch's input list, so Arena gave it a black 1 × 1 texture. Now a layer effect that measures the contract: input and render size, band position, counter steps per second, time since the last change. Tested in Arena (1920×1080) with the bridge's live sensor and simulated objects, including stopping and restarting the bridge.
- 2026-11-16: First version (#115): channels, status band with counter bar and orientation lines, size bars. Untested.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
