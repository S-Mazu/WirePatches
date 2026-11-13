# Rolling Landscape

A source patch with alpha: an Art Deco night skyline of the 1930s scrolls past as up to six parallax silhouette layers, with lit windows, stars, a moon, searchlights and airships.

Last updated: 2026-11-13

## Vision

A calm backdrop in the style of the 1930s: the city rolls by like a stage set. Every element can be added step by step (layers, stars, beams, airships), so the picture can grow with the music.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: none
- Performance: one full-screen shader. High `Star Size`, `Beam Density` and `Beam Length` cost the most, especially at a low `Scale`. Not measured.

## Files

| File | Purpose |
|---|---|
| `Rolling Landscape.wire` | The patch |
| `Resource/RollingLandscape.fs` | ISF shader that draws the whole scene |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required.

## Timing

The patch has no internal motion. Everything moves with the parameter `Clock` (0 to 1000, default 0). Its rate is the scroll speed; if the clock stops, the city stops.

- Start value: in Arena set the phase source of `Clock` to `Timeline` with `Duration` = 1000 s (about 1 clock unit per second).
- Play the timeline backwards to scroll the other way. BPM Sync works as well.
- With play mode `Loop` the city jumps to a different position when the timeline restarts, every 1000 s with the start value. `Bounce` avoids the jump, but reverses the direction at each end.

## Parameters

All ranges are deliberately wide; the extremes are safe.

- `Scale` (0 to 10, default 1): size of all elements relative to the output height.
- `Clock` (0 to 1000, default 0): see above.

**Physics**
- `Parallax` (-2 to 2, default 0.6): 0 = all layers equal, positive = back layers slower, negative = back layers faster.
- `Airship Dir` (Left / Right / Random, default Left): Random picks a direction per row and changes with `Seed`.
- `Star Scroll` (Off / On, default Off): Off = stars stay fixed, On = stars scroll behind Layer 1.

**Skyline**
- `Layer Count` (0 to 6, default 3): adds layers from back to front; fractional values fade the next layer in.
- `Density` (0 to 1, default 0.85): share of filled plots.
- `Horizon` (-2 to 10, default 0.05): distance of the base line above the bottom edge.
- `Seed` (0 to 1000, default 0): a different value gives a different city, sky and traffic.
- `Layer 1 Color` to `Layer 6 Color`: one color per layer, 1 = back.
- `Window Lights` (0 to 1, default 0.35): share of lit windows.
- `Window Color`: color of all windows.

**Sky**
- `Moon X`, `Moon Y` (-3 to 3, defaults 0.703, 0.62): position relative to the frame, -1 / 1 = edges. The moon sits behind the skyline, so raising `Moon Y` gives a moonrise.
- `Moon Opacity` (0 to 1, default 1).
- `Moon Size` (0 to 2, default 0.09): 0 hides it.
- `Moon Phase` (0 to 1, default 0.5): 0 = new moon, 0.25 = first quarter, 0.5 = full, 0.75 = last quarter.
- `Moon Ray Length` (0 to 10, default 1): 0 = no rays.
- `Moon Color`.
- `Star Density` (0 to 1, default 0): share of possible stars. New stars fade in and existing ones keep their position, so it can follow FFT.
- `Star Size` (0 to 20, default 1): 0 hides them. Stars keep their position, so it can follow FFT.
- `Star Color`.

**Searchlights**
- `Beam Density` (0 to 1, default 0): share of ground spots with a light. 1 is four times as many as before; the old maximum is now about 0.25.
- `Beam Length` (0 to 20, default 2.4).
- `Beam Color`.

**Airships**
- `Airship Density` (0 to 1, default 0): rows over the whole sky, drifting with `Clock`.
- `Airship Color`.

**Backdrop**
- `Backdrop On` (Off / On, default On): colored sky or transparent.
- `Backdrop Color`.

## Any surface

- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched.
- `Scale`: 1 for any single screen, also 4K; ⅓ for three stacked to keep element sizes and show more sky.
- Anchors: the horizon is anchored to the bottom edge, the moon's position follows the frame, stars, searchlights and airships fill the visible area.
- Exceptions: none.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.
- Size-dependent parameters: all sizes, distances and speeds are relative to the height and multiplied by `Scale`.

Tested sizes (in Wire):

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok, also in Arena |
| 3840×2160 | 16:9 | ok |
| 5760×1080 | 3 side by side | ok |
| 1920×3240 | 3 stacked | ok at `Scale` ⅓ |
| 1080×1920 | 9:16 | ok |
| 4096×512 | 8:1 | ok |
| 512×512 | 1:1 | ok |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Arolling_landscape).

## Changelog

- 2026-11-13: `Scale` is now the first parameter, before `Clock` (#92). Section `Clock` renamed to `Timing` (#108). `licenseName` set to Unlicense (#91).
- 2026-11-06: Revision to the current rules (#48). Add stars with `Star Density`, `Star Size`, `Star Color`, `Star Scroll`; group `Moon` becomes `Sky`. `Beam Density` reaches four times as many lights. `Scale` and `Horizon` follow the surface model: 4K now uses `Scale` 1, and `Horizon` is the distance above the bottom edge (old -0.95 = new 0.05). `Airship Dir` moves to `Physics`. Shader moves to `Resource/` with a relative path (#6). Undefined `smoothstep` and `atan` calls replaced.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
