# Flower Sax

A source patch with alpha: a flat, contour-drawn instrument in the style of Len Lye blows seven kinds of flowers (rose, daisy, tulip, blossom, poppy, lily, violet) into the room. The flowers grow, then fly on by themselves and fade.

Last updated: 2026-11-06

## Vision

A calm, playful overlay for music visuals: the instrument is the source, the flowers are the music. Choose the instrument that plays right now. Move or rotate it and only the starting point of new flowers changes. Set `Instrument Size` to 0 to hide it and let the flowers start from a single point.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: none
- Performance: node based, no shader. Flowers come from fixed 512 × 512 textures. Not measured.

## Files

| File | Purpose |
|---|---|
| `Flower Sax.wire` | The patch |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required.

## Clock (required)

The patch has no internal motion. Everything is driven by the first parameter, `Clock` (0 to 1000, default 0). Its rate is the flight speed; if the clock stops, all flowers stop.

- Start value: in Arena set the phase source of `Clock` to `Timeline` with `Duration` = 1000 s (about 1 clock unit per second). A shorter duration flies faster.
- The wrap from 1000 back to 0 does not move any flower. BPM Sync or FFT work as well, to let the music push the flowers.

## Parameters

All ranges are deliberately wide; the extremes are safe (for example `Reach` 0, `Grow Distance` 0, `Flower Size` 0 or `Instrument Z` -3).

- `Clock` (0 to 1000, default 0): see above.
- `Scale` (0 to 10, default 1): size of the whole scene relative to the output height.

**Flowers**
- `Max Items` (0 to 960, default 960): scales all amounts down.
- `<Flower> Amount` (0 to 137, defaults 2 to 6): spawn maximum per kind. Flowers in flight finish their way; raising it sends new ones out.
- `<Flower> Color`: color of each kind.
- `Color Variation` (0 to 180, default 15): fixed random hue offset per flower, in degrees.
- `Hue Shift (°)` (0 to 360, default 0): turns all flower colors.
- `Flower Size` (0 to 10, default 0.2).
- `Flowers Opacity` (0 to 1, default 1).

**Physics**
- `Reach` (0 to 50, default 2): flight distance before the flowers fade.
- `Spread (°)` (0 to 360, default 40): cone around the instrument's direction; 360 = all directions.
- `Fall Strength` (0 to 5, default 0.3), `Fall Direction (°)` (0 to 360, default 270 = down): bends the path.
- `Tumble` (0 to 4, default 0.25): spins the flowers.
- `Speed Variation` (0 to 1, default 0.3): each flower gets its own speed.
- `Grow Distance` (0 to 5, default 0.15): flight distance over which a flower grows to full size.
- `Depth Spread` (0 to 1, default 0.5).

**Instrument**
- `Instrument` (default Saxophone): where the flowers come from.

| Instrument | Flowers start at | Direction |
|---|---|---|
| Saxophone | Bell | Up and to the right |
| Trumpet | Bell | Right |
| Snare Drum | Top head | Up |
| Bass Drum | Front head | Up |
| Double Bass | F-holes | Up |
| Piano | Under the open lid | Up and to the right |

- `Instrument X`, `Instrument Y` (-20 to 20, defaults -0.9, -0.3): position, in height units from the center.
- `Instrument Z` (-3 to 10, default 0): pushes instrument and new flowers back; negative values bring them closer.
- `Rotation (°)` (-360 to 360, default 0).
- `Instrument Size` (0 to 10, default 0.85): 0 hides the instrument.
- `Instrument Color`, `Line Color`.
- `Instrument Opacity` (0 to 1, default 1).

**Backdrop**
- `Backdrop On` (Off / On, default Off): solid color behind everything, or transparent.
- `Backdrop Color`.

## Any surface

- Follows the surface model: height is the reference, nothing is stretched.
- `Scale`: 1 for any single screen, also 4K; ⅓ for three stacked to keep the size of one screen.
- Exceptions: the scene does not extend sideways. One instrument is the idea of the patch, so wide surfaces keep empty sides. Adjust `Instrument X` and `Reach` per setup.
- Anchor: center. The default `Instrument X` of -0.9 is outside the frame on portrait surfaces: set `Instrument X` per setup.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.
- Size-dependent parameters: all sizes and distances scale with the output height and `Scale`.

Tested sizes (in Wire, instrument centered):

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok |
| 3840×2160 | 16:9 | ok |
| 5760×1080 | 3 side by side | ok, also in Arena at 6000×1080 |
| 1920×3240 | 3 stacked | ok at `Scale` ⅓ |
| 1080×1920 | 9:16 | ok |
| 4096×512 | 8:1 | ok |
| 512×512 | 1:1 | ok |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Aflower_sax).

## Changelog

- 2026-11-06: Revision to the current rules (#44, #28). New `Instrument` selector with Trumpet, Snare Drum, Bass Drum, Double Bass and Piano; each has its own starting point and direction. The group `Saxophone` becomes `Instrument`, with its controls renamed. `Scale` is new; a camera and `Patch Info` follow the surface model, and flowers are no longer distorted outside 16:9. `Color Var (°)`, `Fall` and `Fall Angle (°)` become `Color Variation`, `Fall Strength` and `Fall Direction (°)`, matching Autumn Wind. Wider ranges for `Instrument X`, `Instrument Y`, `Reach` and `Flower Size`.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
