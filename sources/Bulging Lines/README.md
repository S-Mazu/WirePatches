# Bulging Lines

Bulging Lines is a source: horizontal lines run across the whole frame and bulge out where people stand in front of the surface, the closer to it the stronger. Driven by the [Motion Bridge](../../tools/Motion%20Bridge/) tool (2D lidar) over OSC; without it the lines stay calm. With `Backdrop On` off it has alpha.

Last updated: 2026-11-14

## Vision

An interactive wall for a foyer, a stage curtain or a club: the picture notices the audience. Each person pushes the lines like a hand pressing into a stretched fabric. Calm and graphic when nobody is there, alive when people walk along.

## Requirements

- Resolume Wire / Arena: 7.29
- Inputs: OSC from the [Motion Bridge](../../tools/Motion%20Bridge/README.md#output) (`/lidar/x`, `/lidar/depth`, `/lidar/active`, `/lidar/alive`, 8 slots)
- Performance: one full-screen shader, light. Not measured.

## Files

| File | Purpose |
|---|---|
| `Bulging Lines.wire` | The patch: OSC input, heartbeat, controls |
| `Resource/BulgingLines.fs` | ISF shader that draws the lines and bulges for up to 8 people |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. Arena: Preferences > OSC > OSC Input on, port 7000, then **restart Arena** (otherwise patches inside clips may not receive OSC).
3. Start the Motion Bridge and set it up (see its README). It sends to Arena on 7000 and to Wire on 8000 by default.

## Timing

There is no `Clock`: the bulges follow the people in real time, about 10 times per second. The bridge smooths the positions and fades people in and out. When the bridge stops sending (no change of `/lidar/alive` for 0.3 s), the bulges fade out within 1 s and the lines rest.

## Parameters

All ranges are deliberately wide; the extremes are safe.

- `Scale` (0 to 10, default 1): size of all elements relative to the output height.

**Lines**
- `Line Count` (0 to 400, default 24): lines per output height at `Scale` 1. 0 = no lines.
- `Line Width` (0 to 0.5, default 0.006): thickness in output heights. 0 = no lines; when the width exceeds the spacing the lines merge into a plain area.
- `Line Color`.

**Bulge**
- `Bulge Height` (-5 to 5, default 0.25): how far the lines move at a person, in output heights; negative values bend them down. Extreme values press the lines together into vertical bands.
- `Bulge Width` (0 to 10, default 0.35): half width of one bulge in output heights. 0 = no bulge.
- `Depth Influence` (0 to 1, default 0.7): 0 = every person bulges fully, 1 = only people right at the surface bulge fully, far ones barely.
- `Show People` (Off / On, default Off): a red vertical marker per tracked person, for setting up.

**Backdrop**
- `Backdrop On` (Off / On, default On): off makes the background transparent.
- `Backdrop Color`.

## Any surface

- Follows the surface model: height is the reference, width shows more or less of the lines, nothing is stretched.
- `Scale`: 1 for any single screen; ⅓ for three stacked to keep the line spacing of one screen.
- Exceptions: the people are placed by x 0..1 across the **whole** frame width, because the bridge maps its zone onto the whole surface (a zone that grows with the frame, docs/PATCH_RULES.md). Bulge size and line spacing follow the height.
- Size-dependent parameters: `Line Width`, `Bulge Height` and `Bulge Width` are in output heights, multiplied by `Scale`.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.

Tested sizes:

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok (Arena, live sensor and simulated people) |
| 5760×1080 | 3 side by side | ok (Arena) |
| 1920×3240 | 3 stacked | ok (Arena) |
| 1080×1920 | 9:16 | ok (Arena) |
| 4096×512 | 8:1 | ok (Arena) |
| 512×512 | 1:1 | ok (Arena) |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Abulging_lines).

## Changelog

- 2026-11-14: First version (#73, PoC for #69): lines bulge per tracked person (8 slots), depth from the surface weakens the bulge, heartbeat fade-out without the bridge, `Show People` for setting up.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
