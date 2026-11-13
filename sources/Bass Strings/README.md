# Bass Strings

A source patch with alpha: four round-wound double bass strings (E A D G) vibrate with the music, one frequency band per string.

Last updated: 2026-11-13

## Vision

Inspired by Len Lye's lines that move to the music. The strings stand for the instruments of the band: at silence they are straight and still, and they swing wider the louder their band gets. Built to be played live with few controls.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: audio via `Spectrum In`, source External (master out of the band or DJ).
- Audio: the band edges assume 48 kHz. At 44.1 kHz they shift slightly.
- Performance: two shaders. Each string is sampled 16 times per pixel near it, so `Motion Blur` above 0 with wide `Amplitude` costs the most. Not measured.

## Files

| File | Purpose |
|---|---|
| `Bass Strings.wire` | The patch |
| `Resource/BassStringsAnalysis.fs` | Sound analysis with feedback state: band levels, learned frequency range, swing phases |
| `Resource/BassStrings.fs` | Draws the strings |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. In Arena set `Spectrum In` to External and choose the audio input.

## Timing

There is no `Clock`: sound sets how wide the strings swing, `Vibe Speed (Hz)` how fast.

## Parameters

All ranges are deliberately wide; the extremes are safe.

- `Scale` (0 to 10, default 1): size of all strings relative to the output height.

**Physics**
- `Amplitude` (0 to 2, default 0.14): widest swing at full level.
- `Waves` (0.5 to 100, default 4): half waves along the string, stepless. 1 = one big bow. Good for FFT.
- `Vibe Speed (Hz)` (0 to 60, default 9): swing rate of the E string; the others are a fourth apart each. Changes never jump, so it can follow FFT.
- `Overtones` (0 to 1, default 0.5): extra modes at high levels.
- `Motion Blur` (0 to 1, default 0.6): shutter time. At 1 the string sweeps its whole swing per frame.

**Strings**
- `String Rotation` (-180 to 180, default 0): turns all strings, stepless and counter-clockwise. 0 = vertical, 90 = horizontal.
- `String Spacing` (0 to 2, default 0.18): distance between the strings, as a share of the frame across them.
- `Line Width` (0 to 1, default 0.033): width of the E string; the others get thinner.
- `Strings Opacity` (0 to 1, default 1).
- `E String Color`, `A String Color`, `D String Color`, `G String Color`.
- `Light Source` (0 to 360, default 135): where the light comes from, 0 = right, 90 = up; it stays fixed in the frame when the strings turn. `Shine` (0 to 1, default 0.7): give the round look, the highlight and the shadow.

**Sound Input**
- `Spectrum In`: the audio source, see above.
- `Boost` (0 to 20, default 1): raises all bands.
- `Gate` (0 to 1, default 0.05): band levels below this stay silent.
- `Fade` (0.01 to 5, default 0.3): time to fall back. G fades a bit faster.

The analysis learns the used frequency range on its own and splits it into the four bands.

**Backdrop**
- `Backdrop On` (default on): colored background or transparent.
- `Backdrop Color`.

## Any surface

- Follows the surface model: height is the reference, nothing is stretched.
- `Scale`: 1 for any single screen, also 4K; ⅓ for three stacked to keep the string sizes.
- Exceptions: none. The four strings spread with the frame through `String Spacing`, as a zone.
- Anchor: center. The string ends are fixed at the frame edges at any angle, so `Waves` spreads over the whole frame; a longer string shows longer waves.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip. On narrow surfaces lower `Amplitude`, or the strings overlap.
- Size-dependent parameters: line width, swing and winding scale with the output height and `Scale`. Winding fades out near pixel size.

Tested sizes (in Wire, with a test spectrum):

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok |
| 3840×2160 | 16:9 | ok |
| 5760×1080 | 3 side by side | ok |
| 1920×3240 | 3 stacked | ok at `Scale` ⅓ |
| 1080×1920 | 9:16 | ok, strings overlap at the default `Amplitude` |
| 4096×512 | 8:1 | ok |
| 512×512 | 1:1 | ok, strings overlap at the default `Amplitude` |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Abass_strings).

## Changelog

- 2026-11-13: Angle convention (#89): `String Angle` → `String Rotation`, now counter-clockwise. `Light Angle` → `Light Source`: a direction in the frame (default 135 = top left, as before), fixed when the strings turn.
- 2026-11-13: Glossary names: `Spacing` → `String Spacing`, `Fade (s)` → `Fade` (#102).
- 2026-11-13: New section `Timing` (#108). `licenseName` set to Unlicense (#91).
- 2026-11-06: Revision to the current rules (#41). From 39 to 22 controls; band, range and FFT settings are now internal, and `Boost`, `Gate` and `Fade (s)` remain. New `Waves` and stepless `String Angle (°)`. `Motion Blur` is a real shutter and replaces `Blur` and `Twist`. `Scale` is new, and sizes follow the output height. The swing phase runs in the analysis state: no jump at the top of the hour or when `Vibe Speed (Hz)` changes. Removed `Center X`, `Edge Soft (px)`, `Node Top`, `Node Bottom`, `Show Meter`, `Winding Pitch`, `Winding Depth`. Shaders move to `Resource/` with relative paths (#2). Metadata filled in, README added.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
