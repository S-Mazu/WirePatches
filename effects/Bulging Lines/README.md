# Bulging Lines

Bulging Lines is an effect on the [Motion Bridge](../../tools/Motion%20Bridge/) field: ribbons fly out of the depth of the picture towards the viewer, over the floor in front of the projection. They flow around whatever stands on the floor and flutter strongly where something moves. The background is transparent, so the ribbons lie over the layers below.

**It needs the Motion Bridge.** The patch reacts only to the bridge's Spout texture `Motion Bridge`: the bridge with a 2D lidar, set up for the room, must run on the same computer as Arena. Without it the ribbons fly straight and calm.

Last updated: 2026-11-17

## Vision

The picture continues the room. Looking at the projection, you look out over the dance floor: near the screen is near in the picture, far from it is far, towards the horizon. Fabric ribbons stream towards the audience. When nothing happens the picture is calm. Someone standing parts the ribbons like a stone in a stream; someone moving shakes them as if a gust had caught the fabric, and they settle again.

## Requirements

- Resolume Wire / Arena: 7.29
- Tool: the [Motion Bridge](../../tools/Motion%20Bridge/) with its sensor, running and set up for the room (area and front drawn, empty room learned). Its interface is described under [Output](../../tools/Motion%20Bridge/README.md#output).
- Inputs: the clip it sits on (`Texture In`), a Spout clip of `Motion Bridge`.
- Performance: one full-screen shader plus three small passes at 160 × 120. Light; not measured.

## Files

| File | Purpose |
|---|---|
| `Bulging Lines.wire` | The patch |
| `Resource/BulgingLines.fs` | ISF shader: sign of life of the bridge, blurred field, excitation that fades out, the ribbons in perspective |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. Start the Motion Bridge and set it up (its README).
3. In Arena (bridge README, Usage, "In Arena"):
   1. Drag the Spout source `Motion Bridge` into a clip slot (Sources > Spout). The clip must have the texture's own size (*Width* / *Height*, e.g. 19 × 48); a fresh Spout clip takes it by itself.
   2. Set the layer's *Auto Size* (layer panel, Video) to *Stretch*.
   3. Drag `Bulging Lines` onto the clip (a clip effect, after the clip's Transform).
   4. Set the layer's blend mode to *Alpha*, so the ribbons lie over the layers below.
   5. Set the phase source of `Clock` to *Timeline*, *Duration* 1000 s (see Timing).
   6. More looks: duplicate the clip into other columns and change its settings (colour, density, flutter). Each clip is an ordinary clip.

If anything looks wrong, check the field with a Spout clip carrying the example [Field Viewer](../../examples/Field%20Viewer/).

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

## How it works

What the bridge sends is described in its README (Output; "From the room to the picture" under Usage). This patch takes the field and does the following, in one ISF shader with five passes (`Resource/BulgingLines.fs`):

1. **Sign of life** (pass 1, 2 × 1, kept from frame to frame): the patch reads the status band at the top of the texture at three points across the width. It counts as live when R = G = B > 0 there and the value changes. The time since the last change decides how much the field may act: full up to 0.25 s, nothing after 1 s. The clock of the computer (`System Time`) gives the frame time, so this works at any frame rate.
2. **Blur** (passes 2 and 3, 160 × 120): occupancy (R) and motion (G) of the field are blurred over about 6 % of the field in x and in depth. Single cells and sensor noise therefore do not make the ribbons jitter.
3. **Memory** (pass 4, 160 × 120, kept from frame to frame):
   - *Excitation*: blurred motion × 2.5, at most 1. Where nothing new moves, it falls off over `Flutter Life`. This is why the fabric keeps fluttering a moment after a movement.
   - *Occupancy*: follows the blurred occupancy × 1.5 (at most 1) with a delay of 0.15 s, so the ribbons part smoothly instead of jumping with the bridge's 10 measurements per second.
   - Both are multiplied by the sign of life: without the bridge they fall to 0.
4. **Picture** (pass 5, output size):
   - Perspective: the bottom edge of the frame is the front of the bridge (depth 0, near); a pixel higher up sees the floor further away, up to the horizon at `Horizon Y`. Depth 1, the far edge of the area, lies at four times the distance of the bottom edge, about three quarters of the way to the horizon; beyond it the floor continues empty. The front spans the whole width at the bottom edge; further away the area narrows with the perspective.
   - Ribbons are lines along the depth, `Ribbon Density` per output height at the bottom edge, across the whole width, also beside the area.
   - *Flow around*: each ribbon is pushed sideways away from occupancy, by the difference of the occupancy left and right of it × `Avoid Strength`. In front of and behind something the ribbons close again. Inside an occupied spot they fade to 30 %.
   - *Flutter*: where excitation is high, the ribbons swing sideways in two fast waves and twist (their shading darkens and lightens), × `Flutter Strength`.
   - *Flight*: each ribbon is broken into segments of random length that travel towards the viewer with `Clock`.
   - Ribbons fade towards the horizon and where they would get finer than a few pixels, so the distance does not flicker.
   - The background stays transparent; only the ribbons have alpha. The band and the raw field are never shown.

`Show Field` draws the processed field on the floor: red = occupancy, green = excitation, magenta = the edge of the area. If the red spots do not sit where people stand, check the front and the area in the bridge.

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

- 2026-11-17: Rebuilt as an effect on the Spout clip of the Motion Bridge field texture (#115), moved from `sources/` to `effects/`: ribbons fly towards the viewer over the floor in perspective, flow around occupancy and flutter where something moves; transparent background, no Backdrop. Replaces the OSC version with its 8 people slots and bulges. Tested in Arena (1920×1080) with the live sensor and simulated objects, slider extremes and bridge stop.
- 2026-11-16: Marked as not working: the Motion Bridge changed its interface from OSC to a Spout texture (#115); rebuild pending.
- 2026-11-14: First version (#73, PoC for #69): lines bulge per tracked person (8 slots), depth from the surface weakens the bulge, heartbeat fade-out without the bridge, `Show People` for setting up.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
