# Pride Flag

A source patch with alpha: a flag that moves lightly in the wind, as a Rainbow, Straight Ally or Progress flag. Freely placed and sized.

Last updated: 2026-11-06

## Vision

A simple, dignified overlay for pride events and CSD shows: the flag waves calmly over any other content. The flags follow the [flag lexicon of CSD Deutschland](https://csd-deutschland.de/flaggenlexikon/).

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: none
- Performance: one full-screen shader, light. Not measured.

## Files

| File | Purpose |
|---|---|
| `Pride Flag.wire` | The patch |
| `Resource/PrideFlag.fs` | ISF shader that draws the flag and its wind |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required.

## Clock (required)

The patch has no internal motion. The wind is driven by the first parameter, `Clock` (0 to 1000, default 0). If the clock stops, the flag stops.

- Start value: in Arena set the phase source of `Clock` to `Timeline` with `Duration` = 1000 s for a calm wave. A shorter duration blows faster.
- The loop from 1000 back to 0 is seamless. BPM Sync works as well.

## Parameters

All ranges are deliberately wide; the extremes are safe. High `Wind Strength` with high `Waves` gives an abstract, strongly distorted image.

- `Clock` (0 to 1000, default 0): see above.
- `Scale` (0 to 10, default 1): size of the flag relative to the output height.

**Physics**
- `Wind Strength` (0 to 5, default 0.5): how far the flag swings. 0 = still. The left edge stays fixed, the free end swings most.
- `Waves` (0 to 20, default 1.5): waves along the flag.
- `Fold Shading` (0 to 2, default 0.5): light and shadow in the folds. 0 = plain colors.

**Flag**
- `Flag` (Rainbow / Straight Ally / Progress, default Rainbow).
- `Flag X`, `Flag Y` (-20 to 20, default 0): position of the center, in height units from the frame center. 1 = half the output height at `Scale` 1.
- `Flag Size` (0 to 20, default 1.2): flag height at `Scale` 1; 2 = full output height. The flag always keeps the format 3:2.

The colors are fixed, as they define the flag. They use common standard values, because the lexicon names colors only.

**Backdrop**
- `Backdrop On` (Off / On, default Off): solid color behind the flag, or transparent.
- `Backdrop Color`.

## Any surface

- Follows the surface model: height is the reference, nothing is stretched.
- `Scale`: 1 for any single screen, also 4K; ⅓ for three stacked to keep the size of one screen.
- Exceptions: the scene does not extend sideways. One flag is the idea of the patch, so wide surfaces keep empty sides. Use `Flag X`, `Flag Size` or several clips per setup.
- Anchor: center. On portrait surfaces lower `Flag Size`: the default flag is wider than a 9:16 frame and gets cut off.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.
- Size-dependent parameters: all sizes and the swing scale with the output height, `Scale` and `Flag Size`.

Tested sizes (in Wire):

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok |
| 3840×2160 | 16:9 | ok |
| 5760×1080 | 3 side by side | ok |
| 1920×3240 | 3 stacked | ok at `Scale` ⅓ |
| 1080×1920 | 9:16 | cut off at the default `Flag Size` |
| 4096×512 | 8:1 | ok |
| 512×512 | 1:1 | ok |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Apride_flag).

## Changelog

- 2026-11-06: First version, with Rainbow, Straight Ally and Progress flags.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
