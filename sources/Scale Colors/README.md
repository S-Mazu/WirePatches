# Scale Colors

A source patch with alpha: an overlay that shows which of the 12 tones of the octave are sounding right now, as keys, pie, ring, grid or circles. It does not detect a key.

Last updated: 2026-11-13

## Vision

Makes harmony visible during a show: the tones of the music light up in their own colors, on top of any other content.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: audio via `Spectrum In`, source External (master out of the band or DJ). Never use Composition, although Arena preselects it.
- Audio: Arena at 48 kHz (Arena preferences). At 44.1 kHz all tones shift by about 1.5 semitones; Wire offers no way to read the sample rate.
- Performance: node based, no shader. Not measured.

## Files

| File | Purpose |
|---|---|
| `Scale Colors.wire` | The patch |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. In Arena set `Spectrum In` to External and choose the audio input.

## Timing

There is no `Clock`: the display follows the sound in real time. `Fade` sets how fast a tone fades after it stops sounding.

## Parameters

All ranges are deliberately wide; the extremes are safe.

- `Scale` (0 to 10, default 1): size of the whole layout, lines included, relative to the output height.

**Sound Input**
- `Spectrum In`: the audio source, see above.
- `Boost` (0 to 20, default 1): input gain.
- `Gate` (0 to 1, default 0.05): keeps everything dark on quiet noise; 1 = only very loud tones.
- `Fade` (0.01 to 3, default 0.4): release time.
- `Focus` (0.5 to 8, default 2): higher values let only the main tones light up.
- `Min Freq (Hz)` (100 to 2000, default 400): lower limit of the analysis. Below about 400 Hz the FFT cannot separate semitones; low notes are caught through their overtones.
- `Max Freq (Hz)` (1000 to 12000, default 5000): upper limit, always kept at least one octave above `Min Freq (Hz)`.

**Layout**
- `Layout` (Keys / Pie / Ring / Grid / Circles, default Keys).
- `Layout Width`, `Layout Height` (0.05 to 5, defaults 0.9, 0.7): box the layout fills, as a share of the frame at `Scale` 1. Keys and Grid fill it exactly, Circles spread over it and stay round, Pie and Ring stay round and fit inside.
- `Line Width` (0 to 0.5, default 0.008): outline width, independent of the layout size.
- `Line Color`.
- `Lines Opacity` (0 to 1, default 1).

**Colors**
- `Cells Opacity` (0 to 1, default 1).
- `Rest Natural`, `Rest Accidental`: color at rest of C D E F G A B and of C# D# F# G# A#, including their alpha. A sounding tone replaces this with its own color at full opacity.
- `Hue Start (°)` (0 to 360, default 200), `Hue Range (°)` (0 to 360, default 80): 0 = one color for all, 360 = full color wheel.
- `Fifths Order` (default off): spreads the colors along the circle of fifths, so the tones of one key get neighbouring colors.
- `Tone Saturation` (0 to 1, default 0.55), `Tone Brightness` (0 to 1, default 0.95).

**Backdrop**
- `Backdrop On` (Off / On, default Off): solid color behind everything, or transparent.
- `Backdrop Color`.

## Any surface

- Follows the surface model: height is the reference, nothing is stretched by the surface itself.
- `Scale`: 1 for any single screen, also 4K. On three stacked projectors ⅓ keeps the size of one screen, centered.
- Exceptions: scene does not extend sideways. Scale Colors is one instrument and does not repeat, so set `Layout Width` per setup. Keys and Grid get wider with it, by design.
- Anchor: center. `Layout Width` and `Layout Height` follow the frame, as they define a zone.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.
- Size-dependent parameters: `Line Width` scales with the output height and `Scale`.

Tested sizes (in Wire, with test spectra):

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok, also in Arena |
| 3840×2160 | 16:9 | ok |
| 5760×1080 | 3 side by side | ok |
| 1920×3240 | 3 stacked | ok at `Scale` ⅓ |
| 1080×1920 | 9:16 | ok, also in Arena |
| 4096×512 | 8:1 | ok |
| 512×512 | 1:1 | ok |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Ascale_colors).

## Changelog

- 2026-11-13: Glossary names: `Fade (s)` → `Fade`; `Fade` moved before `Focus` in `Sound Input` (#94).
- 2026-11-13: New section `Timing` (#108). `licenseName` set to Unlicense (#91).
- 2026-11-06: Revision to the current rules (#49). The layout box reads the aspect ratio at runtime instead of a fixed 16:9; `Scale` and a camera follow the surface model. `Backdrop On` becomes Off / On, `Line Width` and `Layout Width`/`Layout Height` get wider ranges, the group `Mix` is dissolved into `Colors` and `Layout`, and `Sound Input` comes first. Metadata filled in, README added.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
