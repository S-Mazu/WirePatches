# Deco Fan

An effect: one Art Deco fan of rays and rings, on top of the clip below.

Last updated: 2026-11-13

## Vision

The sunburst of a 1930s cinema: a fan of rays and rings, drawn clean and geometric. Add the effect several times for several fans; the order of the effects decides which lies in front.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: the clip below (this is an effect)
- Performance: one shader per fan. Not measured in Arena.

## Files

| File | Purpose |
|---|---|
| `Deco Fan.wire` | The patch |
| `Resource/DecoFan.fs` | ISF shader that draws the fan |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required.

## Timing

There is no `Clock`: nothing moves by itself. Automate position, `Fan Fill (°)` or `Fan Rotation (°)` in Arena.

## Parameters

All ranges are deliberately wide; the extremes are safe.

- `Scale` (0 to 10, default 1): size of everything relative to the output height.

**Fan**
- `Fan X`, `Fan Y` (-5 to 5, defaults 0, -1): position. 0 = centre, -1 and 1 = edges of the frame.
- `Fan Size` (0 to 10, default 1).
- `Fan Rotation (°)` (-360 to 360, default 0): the direction the fan opens to, 0 = up.
- `Fan Fill (°)` (0 to 360, default 180): how far the fan is open. 180 = half circle, 360 = full circle.

**Rays**
- `Ray Count` (0 to 240, default 40): rays in a full circle; `Fan Fill (°)` reveals them. 0 = no rays.
- `Ray Length` (0 to 10, default 1.15).
- `Ray Width` (0 to 2, default 0.03).
- `Ray Style` (default Straight): Straight, Wedge, Long-Short, Tapered, Skyscraper, Beaded, Feather.
- `Ray Color`.

**Rings**
- `Ring Count` (0 to 16, default 4): fractional values fade the next ring in. 0 = no rings.
- `Ring Start` (0 to 10, default 0.3): radius of the first ring.
- `Ring Gap` (0.01 to 5, default 0.2): distance between rings.
- `Ring Width` (0 to 2, default 0.02).
- `Ring Style` (default Arch): Arch, Stepped, Chevron, Scallop, Beads, Double, Key.
- `Ring Color`.

## Any surface

- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched.
- `Scale`: 1 for any single screen; ⅓ for three stacked to keep the size of one screen.
- Exception: `Fan X` and `Fan Y` are relative to the frame on purpose, so a fan on the edge stays on the edge on any surface.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.

Tested sizes (in Wire):

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok |
| 5760×1080 | 3 side by side | ok |
| 1080×1920 | 9:16 | ok |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Adeco_fan).

## Changelog

- 2026-11-13: New section `Timing` (#108). `licenseName` set to Unlicense (#91).
- 2026-11-09: First version, split from Deco Marquee v2 (#42, #25). One fan per effect, new ray and ring styles, `Scale`.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
