# Scaler

An example source patch that shows how our patches adapt to any output surface: one projector, several projectors next to each other or on top of each other, or an LED wall of any shape. It has no show content. You load it to see the idea working, and you copy its nodes into your own patch.

Last updated: 2026-11-13

## Vision

A patch is usually built on one screen, often 1920×1080. In the show it may have to fill 5760×1080 or a tall LED wall. Without any preparation, the picture then gets stretched, shapes end up in the wrong place, or the composition only looks right on the screen it was built on.

This patch shows the solution we use everywhere, so that anyone on the team can understand it and build it into their own patches.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: none. The patch shows its picture as soon as it is loaded.
- Performance: light, node based. Not measured.

## Files

| File | Purpose |
|---|---|
| `Scaler.wire` | The patch, with notes on the canvas |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required.

## The idea in plain words

Think of the patch as a camera looking at a scene.

- **The height of the output is the reference.** A shape that is half as high as the output on one screen is also half as high as the output on any other screen.
- **The width only decides how much of the scene you see.** A wider surface does not stretch the picture. It simply shows more of the scene to the left and right, in the same way that a wider window shows more of the landscape outside.
- **`Scale` decides how big everything is.** Every patch has this control, and you set it for each show in Arena. A higher value makes everything bigger, so less of the scene fits on the surface. A lower value makes everything smaller, so more of the scene fits.

The scene itself does not know how big the output is. Only the camera does. That is why one small group of nodes is enough to make a patch work on any surface.

## Try it

**In Arena**

1. Load `Scaler` as a clip. The text in the picture explains what each element shows.
2. Move `Scale` and watch the elements grow and shrink.
3. Change the surface: set `Width` and `Height` on the clip, for example to 5760×1080 or 1920×3240.

A clip takes the composition size when it is loaded and then keeps that size, even when the composition changes. So after changing the composition size, reload the clip or set its `Width` and `Height` yourself.

**In Wire**

Open the patch and read the node chain from left to right. The yellow notes on the canvas explain each step. To try another surface, change the patch size in the patch settings.

## What you see in the picture

| Element | What it shows |
|---|---|
| White circle | Its radius is 1. At `Scale` 1 it touches the top and bottom of the output on any surface, and it always stays round. |
| Gray squares | Each one is as wide and as high as one output height at `Scale` 1. A wider surface shows more squares. Nothing gets stretched. |
| Red markers | They always sit on the four corners of the frame. They show how to attach something to the edge of the frame. |
| Yellow line | It always stays the same distance above the bottom edge. It shows how to attach something to one edge with a fixed distance. |
| Text | Explains the elements and shows the current size, aspect ratio, `Scale` and camera zoom. |

A good experiment: set the clip to 1920×3240 (three projectors on top of each other) and `Scale` to 0.33. The circle then has the same size in pixels as on a single 1080 px high projector, three squares are stacked on top of each other, and the yellow line stays at the bottom.

## How the logic works

### Why a camera is needed

Wire's shapes and meshes use coordinates that follow the **shorter** side of the output:

- On a landscape output, y runs from -1 to 1, and x runs further, for example from -1.78 to 1.78 at 16:9. This is what we want.
- On a portrait output, x runs from -1 to 1, and y runs further. The reference has silently changed from the height to the width, so everything would change its size.

The camera corrects this and also applies `Scale`.

### The calculation

The patch calculates one zoom value for the camera:

```
safeScale = clamp(Scale, 0.001, 1000)
zoom      = clamp(1 / aspect, 1, 1000000) × safeScale
```

`aspect` is width divided by height, and the `Patch Info` node delivers it while the patch is running. Line by line:

- `1 / aspect` is greater than 1 only on a portrait output. It corrects exactly the problem described above.
- The first `clamp` keeps that correction at 1 on landscape outputs, where nothing needs correcting.
- `safeScale` protects the patch from the `Scale` slider. At `Scale` 0 the camera would otherwise divide by zero and the picture would break.

With this zoom, the visible part of the scene is always:

```
y from -1 / safeScale to +1 / safeScale
x from -aspect / safeScale to +aspect / safeScale
```

### The nodes in this patch

All of them are in the top left of the canvas, under the note "1. CAMERA".

| Node in the patch | Node type | What it does |
|---|---|---|
| `Patch Info` | Patch Info | Delivers the aspect ratio at runtime. |
| `1 / Aspect` | 1 / x | Calculates 1 / aspect. |
| `Portrait Zoom` | Clamp, `min` 1, `max` 1000000 | Keeps the correction at 1 on landscape outputs. |
| `Scale` | Float In, 0 to 10, default 1 | The control you see in Arena. |
| `Safe Scale` | Clamp, `min` 0.001, `max` 1000 | Protects against invalid values. |
| `Camera Zoom` | Multiply | Multiplies `Portrait Zoom` and `Safe Scale`. |
| `Camera 2D` | Camera 2D | Receives the result at its `zoom` input. |

## Using it in your own patch

### Step 1: add the camera nodes

Copy the seven nodes from the table above into your patch, or build them again. Connect them like this:

```
Patch Info (aspectratio) → 1 / Aspect → Portrait Zoom (value) ┐
                                                              ├→ Camera Zoom → Camera 2D (zoom)
Scale → Safe Scale (value) ───────────────────────────────────┘
```

`Scale` belongs directly after `Clock` in the patch's control list and outside any group. If the patch has no `Clock`, `Scale` comes first.

### Step 2: connect the camera to the render nodes

This is the step that makes it work. Connect the output of `Camera 2D` to the `camera` input of **every** node that draws your final picture:

- every `Shape Render`
- every `2D Render`

Your shapes and meshes stay as they are. You do not have to change their sizes or positions, and nothing is added between them and the render node.

If you forget one render node, that part of the picture will not follow `Scale` and will no longer match the rest.

There is one exception. If a render node only prepares a small texture that is used somewhere else, such as a flower that is copied many times, it does not get the camera. Give that render node a fixed square size, and draw the texture on a square. The render node that draws the final picture gets the camera. `Flower Sax` works this way.

### Step 3: place things in the scene

Decide for each element what it should be attached to:

| The element should… | Then… | Example in this patch |
|---|---|---|
| stay in the middle | use a normal position; (0, 0) is the center | White circle |
| fill the width of any surface | repeat it or distribute it, and let the wider surface show more of it | Gray squares |
| stick to a corner or edge | calculate its position from the frame edges, see below | Red markers |
| keep a fixed distance from an edge | add the distance to the edge position | Yellow line |

The frame edges in scene units are:

```
Edge Y = 1 / safeScale
Edge X = aspect / safeScale
```

Examples:

- Top right corner: position (`Edge X`, `Edge Y`).
- Slightly above the bottom edge: y = `-Edge Y + 0.25`.

The nodes `Edge Y`, `Edge X` and `Horizon Y` in this patch do exactly that. Distances such as the 0.25 stay in normal scene units and automatically grow and shrink with `Scale`.

Sizes, distances and speeds in scene units need no extra work, because the camera scales all of them.

### Step 4: remove anything that assumes one screen

- Do not use fixed numbers such as 1.778 for the aspect ratio, or 1920 and 1080 for the size. Use `Patch Info`.
- Do not offer controls in pixels, such as `Line Width (px)`.
- Do not give a mesh a fixed 16:9 shape if its texture follows the patch size, because the picture is distorted on every other surface.

### Step 5: test it

Try these surfaces in Wire by changing the patch size. Round things must stay round, and elements must stay where you attached them.

| Surface | Size | `Scale` |
|---|---|---|
| One projector | 1920×1080 | 1 |
| 4K projector | 3840×2160 | 1 |
| Three projectors side by side | 5760×1080 | 1 |
| Three projectors stacked | 1920×3240 | 1 and 0.33 |
| Portrait | 1080×1920 | 1 |
| Wide LED strip | 4096×512 | 1 |

Also move `Scale` to 0 and to its maximum. The picture must stay stable.

### If your patch uses an ISF shader

A shader does not use the `Camera 2D` node. Calculate the same thing at the start of the shader, using `RENDERSIZE` for the output size:

```glsl
vec2 res = max(RENDERSIZE, vec2(1.0));
float safeScale = clamp(scale, 0.001, 1000.0);
float pixelsPerUnit = 0.5 * res.y * safeScale;

// position of this pixel in scene units, (0, 0) is the center
vec2 p = (isf_FragNormCoord - 0.5) * res / pixelsPerUnit;

// the frame edges in scene units
vec2 edge = 0.5 * res / pixelsPerUnit;
```

Then draw with `p`, and use `edge` in the same way as `Edge X` and `Edge Y` above. `Rolling Landscape` and `Bass Strings` are complete examples.

## Which `Scale` should I use?

You choose this for each show in Arena. It does not depend on the resolution of a single projector, so 4K also uses 1.

| Surface | `Scale` | Result |
|---|---|---|
| One screen of any resolution | 1 | The normal picture |
| Several screens side by side | 1 | Same element size, more scene to the sides |
| Three screens stacked, keep the size of one screen | 0.33 | Same element size, more scene above and below |
| Three screens stacked, use the whole height | 1 | Everything three times as big |
| LED wall | Try it | Start at 1 and change it until the elements have the size you want |

## Parameters

- `Scale` (0 to 10, default 1): size of everything relative to the output height. The extremes are safe.

## Any surface

- Follows the surface model. This patch demonstrates it.
- `Scale`: see the table above.
- Exceptions: the text follows the output size and ignores `Scale`, so it stays readable on any surface.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.
- Size-dependent parameters: all shapes scale with the output height and `Scale`.

The full rules are in the section "Surface model" in [docs/PATCH_RULES.md](../../docs/PATCH_RULES.md).

Tested sizes (in Wire):

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok, also in Arena |
| 3840×2160 | 16:9 | ok |
| 5760×1080 | 3 side by side | ok at `Scale` ½ |
| 1920×3240 | 3 stacked | ok at `Scale` ⅓ |
| 1080×1920 | 9:16 | ok |
| 4096×512 | 8:1 | ok, the text is small |
| 512×512 | 1:1 | ok, also at `Scale` 0 and 10 |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Aexample%3Ascaler).

## Changelog

- 2026-11-13: Moved to `examples/Scaler/`; the surface model rules moved to `docs/PATCH_RULES.md`; label `example:scaler` (#88).
- 2026-11-08: The picture now explains each element and how to try it. The README explains the logic and how to use it in your own patch.
- 2026-11-06: First version.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
