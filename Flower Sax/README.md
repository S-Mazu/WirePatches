# Flower Sax

Stand: 31.10.2026

A Resolume Wire source patch (category: source, with alpha). A flat, contour-drawn saxophone in the style of Len Lye blows seven kinds of flowers (rose, daisy, tulip, blossom, poppy, lily, violet) out of its bell. The flowers grow, then fly on by themselves and fade.

## Vision

A calm, playful overlay for music visuals: the saxophone is the source, the flowers are the music. Move or rotate the saxophone and only the starting point of new flowers changes. Set `Sax Size` to 0 to hide the saxophone and let the flowers start from a single point.

## Clock (required)

The patch has no internal motion. Everything is driven by the first parameter, `Clock` (range 0 to 1000). Its rate is the flight speed; if the clock stops, all flowers stop.

- Default is 0: nothing moves.
- Start value: in Arena set the phase source of `Clock` to `Timeline` with `Duration` = 1000 s (about 1 clock unit per second). This gave a calm flight in our test; a shorter duration flies faster.
- Alternatives: BPM Sync or FFT, to let the music push the flowers.

## Parameters

- **Flowers:** `Max Items`, then per kind `Amount` and `Color`; `Color Var (°)`, `Hue Shift (°)`, `Flower Size` (0 to 3), `Flowers Opacity`.
- **Physics:** `Reach` (0 to 10), `Spread (°)` (0 to 360), `Fall`, `Fall Angle (°)` (270° = down), `Tumble`, `Speed Variation`, `Grow Distance` (0 to 5), `Depth Spread`.
- **Saxophone:** `Sax X/Y/Z`, `Sax Rotation (°)`, `Sax Size` (0 to 10), `Sax Color`, `Sax Line Color`, `Sax Opacity`.
- **Backdrop:** `Backdrop On`, `Backdrop Color`.

## Install

Copy this folder into your Resolume Wire `Patches` folder (Documents/Resolume Wire/Patches/). The patch needs no further files.

All ranges are deliberately wide and safe at their extremes (e.g. `Reach` 0, `Grow Distance` 0, `Flower Size` 0, `Sax Z` -3 never break the patch).

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
