# Scaler

An example source patch: it shows how our patches handle oversized surfaces, such as several projectors or LED walls. Load it, change the surface and `Scale`, and watch what happens. It is a reference to learn from and copy, not show content.

Last updated: 2026-11-06

## Vision

Everyone on the team should be able to see the rule at work and take it into their own patches: height is the reference, the width only decides how much of the scene is visible, and nothing is stretched.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: none
- Performance: light, node based. Not measured.

## Files

| File | Purpose |
|---|---|
| `Scaler.wire` | The patch, with notes on the canvas |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required.

## Try it

**In Arena**
1. Load `Scaler` as a clip.
2. Move `Scale`.
3. Set `Width` and `Height` on the clip to another surface, such as 5760×1080 or 1920×3240. A clip keeps its size when the composition changes, so set it on the clip or reload the clip.

**In Wire**
- Open the patch to read the node chain. The yellow notes explain each step.
- Change the patch size in the patch settings to try other surfaces.

## What you see

| Element | Meaning |
|---|---|
| White circle | Radius 1. At `Scale` 1 it touches top and bottom on any surface |
| Gray squares | One reference height each. A wider surface shows more of them |
| Red markers | Anchored to the corners of the frame |
| Yellow line | Anchored to the bottom edge with a fixed offset |
| Text | Live values: size, aspect, `Scale`, camera zoom |

Try this: at 1920×3240 and `Scale` ⅓ the circle has the size it has on one 1080 px projector, and the surface shows three squares on top of each other. The yellow line stays at the bottom.

## How it works

One `Camera 2D` feeds every render node:

```
safeScale = clamp(Scale, 0.001, 1000)
zoom      = clamp(1 / aspect, 1, 1000000) × safeScale
```

`aspect` comes from `Patch Info` at runtime. Without the camera Wire follows the shorter side, so portrait surfaces would change the size of everything. The first part of the zoom corrects that. The clamps catch `Scale` 0 and invalid values.

The visible area is then:

```
y = ±1 / safeScale
x = ±aspect / safeScale
```

Anchor elements with these edges. An offset from an edge stays in reference units. The bottom line, for example, sits at `-1 / safeScale + 0.25`.

**To use it in your patch:** copy the camera nodes, connect `Camera 2D` to the `camera` input of every `Shape Render` and `2D Render`, and add `Scale` as a control. In an ISF shader there is no camera; use `pixels per unit = 0.5 × RENDERSIZE.y × safeScale` instead.

The full rules are in the section "Surface model" in [CLAUDE.md](../CLAUDE.md).

## Parameters

- `Scale` (0 to 10, default 1): size of everything relative to the output height. The extremes are safe.

## Any surface

- Follows the surface model. This patch demonstrates it.
- `Scale`: 1 for any single screen, also 4K; ⅓ for three stacked to keep the size of one screen.
- Exceptions: the text uses a fixed share of the output height and ignores `Scale`, so it stays readable.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.
- Size-dependent parameters: all shapes scale with the output height and `Scale`.

Tested sizes (in Wire):

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok |
| 3840×2160 | 16:9 | ok |
| 5760×1080 | 3 side by side | ok at `Scale` ½ |
| 1920×3240 | 3 stacked | ok at `Scale` ⅓ |
| 1080×1920 | 9:16 | ok |
| 4096×512 | 8:1 | ok |
| 512×512 | 1:1 | ok, also at `Scale` 0 and 10 |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Ascaler).

## Changelog

- 2026-11-06: First version.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
