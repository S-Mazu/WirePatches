# Lye Swing

A source patch: a painted big-band score after Len Lye. Five shapes follow the music, one per sound band, with or without a transparent background.

Last updated: 2026-11-13

## Vision

Len Lye scratched and painted directly on film. In *Swinging the Lambeth Walk* and *Free Radicals* every instrument becomes a line, a ring or a stroke that dances to the music. This patch plays that score live: shapes appear with their part of the music and tremble like paint from frame to frame.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: audio via `Spectrum In` (Composition or External)
- Performance: five shape renders and one small shader. Not measured in Arena.

## Files

| File | Purpose |
|---|---|
| `Lye Swing.wire` | The patch |
| `Resource/Boil.fs` | ISF shader that redraws the shapes 12 times per second |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. In Arena, set `Spectrum In` to External for a live band or a DJ, or to Composition for clips in the composition.

## Timing

There is no `Clock`: the patch has no motion of its own besides the music. Automate position, size and rotation in Arena for movement, and store your looks with Arena's preset button.

## Parameters

All ranges are deliberately wide; the extremes are safe.

- `Scale` (0 to 10, default 1): size of everything relative to the output height.

**Sound Input**
- `Spectrum In` (Composition / External): where the sound comes from.
- `Boost` (0 to 20, default 1): input gain; higher values let the shapes appear more readily.
- `Fade` (0 to 5, default 0.3): how long a shape takes to fall back.
- `Show Meter` (Off / On, default Off): level bars and range readout, for setting up.

Each shape measures its own part of the music against its own peaks of the last seconds, so there is nothing to adjust per shape. A shape reacts to what stands out from its own average; Wave most selectively, Zigzag most readily.

**Physics**
- `Boil` (0 to 5, default 0.5): redraws the shapes 12 times per second with slightly different outlines and uneven paint. 0 = clean shapes.
- `Punch` (0 to 5, default 0.6): a short push in size on the attack of a note. 0 = no push.

**Backdrop**
- `Backdrop On` (Off / On, default On): off makes the background transparent.
- `Backdrop Color`.

**Ring** (low), **Strokes** (low-mid), **Wave** (mid), **Cross** (mid-high), **Zigzag** (high)

Every group has the same controls, named after its shape, for example `Strokes X`:
- `X`, `Y` (-2 to 2): position. -1 and 1 are the edges of the frame.
- `Size` (0 to 5).
- `Rotation` (-180 to 180): positive values turn counter-clockwise.
- `Count` (0 to 8): copies in a row; values in between fade the last copy in.
- `Spacing` (0 to 5): distance between the copies.
- `Scatter` (0 to 1): fixed random offset per copy.
- `Color`.
- One control for the form: `Ring Fill`, `Strokes Width`, `Wave Height`, `Cross Arm Width`, `Zigzag Height` (0 to 1).

## Any surface

- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched.
- `Scale`: 1 for any single screen; ⅓ for three stacked to keep the size of one screen.
- Exception: `X` and `Y` are relative to the frame on purpose, so the shapes spread out over a wide surface.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.

Tested sizes (in Arena, with music):

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok |
| 5760×1080 | 3 side by side | ok |
| 1920×3240 | 3 stacked | ok at `Scale` ⅓ |
| 1080×1920 | 9:16 | ok |
| 4096×512 | 8:1 | ok |
| 512×512 | 1:1 | ok |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Alye_swing).

## Changelog

- 2026-11-13: Glossary (#93, #89): group `Sound Input` with `Spectrum In` first; `Sensitivity` → `Boost` (same role as in Bass Strings and Scale Colors, range 0 to 20); `Fade (s)` → `Fade`; `Strokes Thickness` → `Strokes Width`; rotations -180 to 180 without degree unit.
- 2026-11-13: New section `Timing` (#108). `licenseName` set to Unlicense (#91).
- 2026-11-09: Revised to the current rules (#47). Automatic level per shape replaces 23 sound controls. Controls are named after the shapes. New `Scale`, `Boil` and `Punch`. Removed `Z`.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
