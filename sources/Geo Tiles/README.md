# Geo Tiles

A source patch with alpha: a tile collage of 67 flat geometric motifs in three colors. Each tile has its own motif, zoom, placement, rotation, mirror and color order.

Last updated: 2026-11-13

## Vision

A graphic, poster-like surface in the spirit of geometric pattern collages. Tiles can appear one by one and start moving one by one, so the picture can grow with the music.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: none
- Performance: one full-screen shader. Moving tiles are sampled four times, so high `Animate` costs the most. Not measured.

## Files

| File | Purpose |
|---|---|
| `Geo Tiles.wire` | The patch |
| `Resource/GeoTiles.fs` | ISF shader that draws all tiles and motifs |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required.

## Timing

The patch has no internal motion. Motion comes from the parameter `Clock` (0 to 1000, default 0): one clock unit moves patterns by one element. Figures turn once, or pulse or bob 4 times, per 8 clock units.

- Start value: in Arena set the phase source of `Clock` to `Timeline` with `Duration` = 1000 s (about 1 clock unit per second).
- The loop from 1000 back to 0 is seamless. Play the timeline backwards to reverse all motion; BPM Sync works as well.

## Parameters

All ranges are deliberately wide; the extremes are safe.

- `Scale` (0 to 10, default 1): size of all tiles relative to the output height.
- `Clock` (0 to 1000, default 0): see above.

**Physics**
- `Animate` (0 to 1, default 0.6): share of tiles that follow `Clock`. Each tile has a fixed rank, and it crossfades when it starts or stops following.

**Grid**
- `Rows` (1 to 200, default 6): tiles per output height at `Scale` 1.
- `Tile Aspect` (0.1 to 10, default 1.333): tile width / height. The number of columns follows from the surface width.
- `Seed` (0 to 999, default 1): whole numbers, each value is a new layout. Changes cut hard.
- `Fill` (0 to 1, default 0.85): share of tiles shown. Each tile has a fixed rank, so raising `Fill` only adds tiles (FFT friendly).
- `Big Tiles` (0 to 1, default 0.25): share of 2x2 blocks merged into 2x2, 2x1 or 1x2 tiles. Changes cut hard for the affected tiles.

**Colors**
- `Color 1`, `Color 2`, `Color 3`: each tile uses all three as background, shape and accent.

**Backdrop**
- `Backdrop On` (Off / On, default Off): empty tiles show the backdrop color or stay transparent.
- `Backdrop Color`.

## Any surface

- Follows the surface model: height is the reference, width shows more or fewer tiles, nothing is stretched.
- `Scale`: 1 for any single screen, also 4K; ⅓ for three stacked to keep the tile size and show more tiles.
- Anchors: the grid starts at the bottom left edge. A tile keeps its motif when the surface grows; the top and right edges may cut tiles.
- Exceptions: none.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.
- Size-dependent parameters: patterns or figures close to pixel size blend into their average color instead of shimmering.

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

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Ageo_tiles).

## Changelog

- 2026-11-13: `Scale` is now the first parameter, before `Clock` (#92). Section `Clock` renamed to `Timing` (#108). `licenseName` set to Unlicense (#91).
- 2026-11-06: Revision to the current rules (#45). `Clock` replaces `Pattern Shift`, with a seamless loop. `Scale` and `Tile Aspect` are new, `Columns` and `Opacity` are gone, and `Rows` now reaches 200 and counts tiles per output height. `Fill`, `Big Tiles` and `Animate` run from 0 to 1. Motion rework (#29): `Animate` crossfades instead of jumping, moving tiles are sampled four times to soften hard cell edges, very fine patterns no longer shimmer, the accent of motifs 1, 15, 32, 39 and 49 no longer steps or splits a band. Shader moves to `Resource/` with a relative path (#5). Metadata filled in.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
