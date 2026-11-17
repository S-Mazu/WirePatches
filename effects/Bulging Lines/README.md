# Bulging Lines

Bulging Lines is an effect on the [Motion Bridge](../../tools/Motion%20Bridge/) field: ribbons fly out of the depth of the picture towards the viewer, over the floor in front of the projection. They flow around whatever stands on the floor and flutter strongly where something moves. The background is transparent, so the ribbons lie over the layers below.

Last updated: 2026-11-17

## Vision

The picture continues the room. Looking at the projection, you look out over the dance floor: near the screen is near in the picture, far from it is far, towards the horizon. Fabric ribbons stream towards the audience. When nothing happens the picture is calm. Someone standing parts the ribbons like a stone in a stream; someone moving shakes them as if a gust had caught the fabric, and they settle again.

## Requirements

- Resolume Wire / Arena: 7.29
- Inputs: the Motion Bridge field on the layer it sits on (`Texture In`, Spout source `Motion Bridge`); see [Output](../../tools/Motion%20Bridge/README.md#output) of the bridge.
- Performance: one full-screen shader plus three small passes at 160 × 120. Light; not measured.

## Files

| File | Purpose |
|---|---|
| `Bulging Lines.wire` | The patch |
| `Resource/BulgingLines.fs` | ISF shader: sign of life of the bridge, blurred field, excitation that fades out, the ribbons in perspective |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. Start the Motion Bridge and set it up (its README).
3. In Arena (the recipe of the bridge README, Output):
   1. Add the Spout source `Motion Bridge` to a layer as a clip at the texture's own size (*Width*/*Height* as the texture, e.g. 19 × 48; if not, click the clip's resize button, *Maximize Video*).
   2. Set the layer's *Auto Size* (layer panel, Video) to *Stretch*.
   3. Put `Bulging Lines` on the **layer** as an effect, and set the layer's blend mode to *Alpha* to lay the ribbons over the layers below.
   4. Set the phase source of `Clock` to *Timeline*, *Duration* 1000 s (see Timing).

Check the field with the example [Field Viewer](../../examples/Field%20Viewer/) first if anything looks wrong.

## Timing

`Clock` (0 to 1000, default 0) drives the flight of the ribbons: if the clock stops, the ribbons stand. Start with phase source *Timeline*, *Duration* 1000 s, *In* 0, *Out* 1000. BPM Sync works as well.

The reaction to the room runs in real time and does not follow `Clock`: occupancy is smoothed over 0.15 s, the flutter fades over `Flutter Life`. The bridge sends 30 frames per second. When its status band stops changing for more than 0.25 s, the field's influence fades out within a second and the ribbons fly straight; no black frame, nothing frozen.

## Parameters

All ranges are deliberately wide; the extremes are safe (tested in Arena).

- `Scale` (0 to 10, default 1): size of all elements and of the view relative to the output height.
- `Clock` (0 to 1000, default 0): flight of the ribbons, see Timing.

**Ribbons**
- `Ribbon Density` (0 to 200, default 24): ribbons per output height at the front edge, at `Scale` 1. 0 = none.
- `Ribbon Width` (0 to 0.5, default 0.015): width of a ribbon in output heights at the front edge. When it exceeds the spacing the ribbons merge into bands.
- `Ribbon Color` (default pale blue).
- `Ribbons Opacity` (0 to 1, default 1).

**View**
- `Horizon Y` (0 to 10, default 0.7): height of the horizon above the bottom edge, in output heights, multiplied by `Scale`. Above 1 the horizon is outside the frame and the floor looks flatter.

**Physics**
- `Avoid Strength` (0 to 10, default 1): how far the ribbons move around what stands on the floor. 0 = they run straight through.
- `Flutter Strength` (0 to 10, default 1): how strongly the ribbons flutter and twist where something moves.
- `Flutter Life` (0 to 30, default 1.5): seconds until the flutter has calmed down.

- `Show Field` (Off / On, default Off): shows the field on the floor (red occupancy, green excitation, magenta edge of the area), for setting up.

## The field in the picture

- The bottom edge of the frame is the front of the bridge (depth 0); the far edge of the area (depth 1) lies at four times the distance of the bottom edge, about three quarters of the way up to the horizon. Beyond it the floor continues empty.
- The front spans the whole width of the frame at the bottom edge; further away the area narrows with the perspective. `x` of the field runs from the left to the right edge.
- Occupancy is blurred over about 6 % of the field (in x and depth) before it deflects the ribbons, so single cells and sensor noise do not jitter them. Motion is blurred the same way and excites the fabric.
- The ribbons cover the whole width, also beside the area, so wide surfaces are filled.

## Any surface

- Follows the surface model: height is the reference, width shows more or less floor, nothing is stretched. Ribbon spacing and width are in output heights at the front edge, multiplied by `Scale`.
- `Scale`: 1 for any single screen; ⅓ for three stacked to keep the ribbon spacing and put the horizon low.
- Exception: the field itself spans the whole width at the front edge, because the bridge contract maps the front onto the whole projection width (a zone that grows with the frame, docs/PATCH_RULES.md).
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. Here the clip must keep the texture's own size and the layer stretches it (Installation).

Tested sizes:

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok (Arena, live sensor and simulated objects) |
| 5760×1080 | 3 side by side | untested |
| 1920×3240 | 3 stacked | untested |
| 1080×1920 | 9:16 | untested |
| 4096×512 | 8:1 | untested |
| 512×512 | 1:1 | untested |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Abulging_lines).

## Changelog

- 2026-11-17: Rebuilt as an effect on the Motion Bridge field texture (#115), moved from `sources/` to `effects/`: ribbons fly towards the viewer over the floor in perspective, flow around occupancy and flutter where something moves; transparent background, no Backdrop. Replaces the OSC version with its 8 people slots and bulges. Tested in Arena (1920×1080) with the live sensor and simulated objects, slider extremes and bridge stop.
- 2026-11-16: Marked as not working: the Motion Bridge changed its interface from OSC to a Spout texture (#115); rebuild pending.
- 2026-11-14: First version (#73, PoC for #69): lines bulge per tracked person (8 slots), depth from the surface weakens the bulge, heartbeat fade-out without the bridge, `Show People` for setting up.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
