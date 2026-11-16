# Field Viewer

An example effect patch that shows the field texture of the [Motion Bridge](../../tools/Motion%20Bridge/) exactly as it arrives in a patch: the channels, the status band with its counter, and the size of the texture. It has no show content. You load it to check the bridge's contract in Arena, and you copy its reading of the texture into your own patch.

Last updated: 2026-11-16

## Vision

Every motion patch depends on one thing: the field texture arriving in the patch the way the bridge README (section Output) promises. Before any show patch is built on it, this viewer answers the questions a show patch cannot: does the texture reach an effect inside Arena, is it stretched to the composition, which way up do the rows arrive, and does the status band keep counting.

## Requirements

- Resolume Wire / Arena: 7.29
- Inputs: the clip below (`Texture In`), which must be the Spout source `Motion Bridge`
- Performance: one full-screen shader, light. Not measured.

## Files

| File | Purpose |
|---|---|
| `Field Viewer.wire` | The patch: `Texture In`, the shader, three controls |
| `Resource/FieldViewer.fs` | ISF shader that draws the channels, the band and the size bars |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. Start the Motion Bridge (its README, Usage). In Arena, add the Spout source `Motion Bridge` as a clip (Sources > Spout) and set the clip's *Resize* to *Stretch*.
3. Put `Field Viewer` on that clip as an effect.

## The idea in plain words

The bridge sends an image: the lower fifteen sixteenths are the field in front of the projection, the top sixteenth is a band that counts up every frame. The viewer paints that image full frame and adds three readings: the counter as a moving bar, lines where the band must be, and bars for the texture size. Each reading answers one question with the eyes, no numbers to read.

## Try it

With the bridge running and two simulated objects (`bridge.py --sim 2`), drag the objects in the bridge window:

1. **Does the field arrive?** Red blobs (occupancy) and green trails (motion) move with the objects. Black everywhere: the effect gets no texture, or it sits where the clip is not yet stretched; try the effect before and after Arena's Transform.
2. **Is it stretched?** `Show Size` On: the two lower bars (input width cyan, input height orange) must be as long as the two upper bars (render width and height, grey). Shorter input bars mean the effect sees the texture at its native size; then Resize is not Stretch, or the effect sits in the wrong place. Bar lengths are `log2(pixels) / 12`: 40 px gives 0.44, 1080 px gives 0.84, 1920 px gives 0.91, 4096 px fills the width.
3. **Which way up?** `Show Band` On: the top sixteenth must show the counter as a yellow bar that grows from left to right and wraps about every 8.5 s, with a yellow line at its lower edge. If instead a plain grey strip sits at the bottom, at the magenta line, the rows arrive flipped: set `FLIP` in the bridge's `spout_out.py` to the other value and note it in the bridge README.
4. **Is it alive?** Stop the bridge. The bar stops (frozen last frame) or the picture goes black; both are what a show patch must treat as "fade out". Note which one it is in the bridge README.

Record the four results in the bridge README (Status) and in issue #115.

## How the logic works

- `Texture In` → `Field Viewer (ISF)` → `Premultiply Alpha` → `Texture Out`, the chain of every effect in this repository.
- The shader samples the texture with `IMG_NORM_PIXEL` at the fragment's normalized coordinate: x along the front, y into the area. It never uses pixel positions, so it works at any texture size.
- The counter is sampled at (0.5, 31/32), the middle of the band. `IMG_SIZE(inputImage)` gives the texture size as the effect sees it; `RENDERSIZE` the composition size.
- No `Scale` and no `Clock`: the viewer shows the texture as it is, so the surface model does not apply (docs/PATCH_RULES.md, "Examples and tools").

## Using it in your own patch

Copy the three lines that matter from `Resource/FieldViewer.fs` into your shader:

```glsl
vec4 field = IMG_NORM_PIXEL(inputImage, vec2(x, depth * 15.0 / 16.0));   // x, depth in 0..1
float counter = IMG_NORM_PIXEL(inputImage, vec2(0.5, 31.0 / 32.0)).r;   // changes every frame while the bridge runs
```

`field.r` is occupancy, `field.g` motion, `field.b` visibility. To fade out without the bridge, keep the last counter in a persistent buffer and fade when it has not changed for a few frames.

## Parameters

- `Channel` (All / Occupancy / Motion / Visibility, default All): All shows red = occupancy, green = motion, blue = visibility; the others show one channel as grey.
- `Show Band` (Off / On, default On): the counter bar in the band, the yellow line at the band's lower edge, the magenta line where the band would be if the rows arrived flipped.
- `Show Size` (Off / On, default On): four bars from the bottom: input width, input height, render width, render height.

## Any surface

- The viewer fills whatever composition it runs in; the field is stretched to it, which is exactly what the bridge contract defines (x over the whole projection width).

Tested sizes:

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | untested |
| 5760×1080 | 3 side by side | untested |
| 1920×3240 | 3 stacked | untested |
| 1080×1920 | 9:16 | untested |
| 4096×512 | 8:1 | untested |
| 512×512 | 1:1 | untested |

## Known issues

Untested: built in a cloud session, not yet loaded in Wire or Arena (#115).

## Changelog

- 2026-11-16: First version (#115): channels, status band with counter bar and orientation lines, size bars. Untested.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
