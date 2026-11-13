# Iris Diaphragm

An effect: a mechanical blade iris that closes over the clip below.

Last updated: 2026-11-13

## Vision

The iris of an old film camera: overlapping metal blades that turn in and close the picture, then open it again. As a transition, as a frame around a detail, or as a mask that shows another clip on the blades.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: the clip below (this is an effect)
- Performance: one shader. Not measured in Arena.

## Files

| File | Purpose |
|---|---|
| `Iris Diaphragm.wire` | The patch |
| `Resource/IrisDiaphragm.fs` | ISF shader that draws the blades |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required.

## Timing

`Clock` drives the iris: 0 = open, 50 = closed, 100 = open again, and it repeats beyond. Animate it with the clip Timeline (0 to 100) and trigger the clip in Piano mode for a single shot, or use BPM Sync for a loop.

## Parameters

All ranges are deliberately wide; the extremes are safe.

- `Scale` (0 to 10, default 1): size of the edges relative to the output height. The iris always covers the whole frame.
- `Clock` (0 to 100, default 0).

**Iris**
- `Center X`, `Center Y` (-2 to 2, default 0): centre of the opening. 0 = centre, -1 and 1 = edges of the frame.
- `Blade Count` (3 to 32, default 9).
- `Blade Rotation (°)` (-360 to 360, default 0): turns all blades.
- `Blade Twist (°)` (-360 to 360, default 60): how far the blades turn while closing; they turn back while opening.
- `Blade Curve` (-5 to 5, default 0.6): bends the blades into a pinwheel; 0 = straight, negative = the other way.

**Look**
- `Blade Color`.
- `Edge Color`: the bright edge along each blade.
- `Edge Width` (0 to 0.2, default 0.004).
- `Edge Softness` (0 to 1, default 0): soft opening.
- `Shading` (0 to 1, default 0.6): shadow of each blade on the next and a soft sheen.

Set `Blade Color` to white and use the layer as a mask (Mask Mode: One Below) to show another clip on the blades.

## Any surface

- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched.
- `Scale`: 1 for any single screen; ⅓ for three stacked to keep the edges as thin as on one screen.
- Exception: the iris always covers the whole frame, and `Center X` / `Center Y` are relative to the frame.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.

Tested sizes (in Wire):

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok |
| 5760×1080 | 3 side by side | ok |
| 1080×1920 | 9:16 | ok |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Airis_diaphragm).

## Changelog

- 2026-11-13: `Scale` is now the first parameter, before `Clock` (#92). Section `Clock` renamed to `Timing` (#108). `licenseName` set to Unlicense (#91).
- 2026-11-10: The frame is always closed outside the opening, also with few, strongly bent blades.
- 2026-11-10: Rebuilt as an effect with real, overlapping blades (#46, #30). `Iris Phase (0-100)` is now `Clock`; new `Blade Curve`, `Shading`, `Scale`. The iris covers any surface. `Iris Opacity` and `Backdrop` removed.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
