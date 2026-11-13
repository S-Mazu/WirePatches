# Deco Marquee

An effect: an Art Deco theatre frame with chasing marquee lamps, on top of the clip below.

Last updated: 2026-11-13

## Vision

The front of a 1930s picture palace: an ornament band around the screen and rows of lamps that run around it. For a portal around the picture, or on its own over a dark background.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: the clip below (this is an effect)
- Performance: one shader. Not measured in Arena.

## Files

| File | Purpose |
|---|---|
| `Deco Marquee.wire` | The patch |
| `Resource/DecoMarquee.fs` | ISF shader with the frame and the lamps |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required.

## Timing

`Clock` drives the chase: 0 to 1 is one step of the pattern and loops seamlessly. Start with BPM Sync over 1 beat, or 4 beats for a slower chase. Twinkle changes 16 times per cycle.

## Parameters

All ranges are deliberately wide; the extremes are safe.

- `Scale` (0 to 10, default 1): size of everything relative to the output height.
- `Clock` (0 to 1, default 0).

**Border**
- `Border Style` (default Steps): Zigzag, Chevron, Steps, Greek Key, Diamonds, Scallop, Beads, Sunburst.
- `Border Width` (0 to 2, default 0.06): 0 = no border.
- `Border Color`.

**Bulbs**
- `Bulb Layout` (default Rows): Off, Rows, Frame.
- `Bulb Spacing` (0.02 to 5, default 0.25): distance between lamps. A wider surface gets more lamps.
- `Bulb Size` (0 to 2, default 0.022).
- `Bulb Inset` (0 to 2, default 0.1): distance from the edge of the frame.
- `Bulb Glow` (0 to 5, default 0.3): halo of lit lamps.
- `Bulb On Color`, `Bulb Off Color`.

Lamps always stay clear of the corner elements of the border.

**Chase**
- `Chase Mode` (default Run): Run, Alternate, Fill, Bounce, Mirror (from the middle outward), Twinkle, Wave.
- `Chase Group` (1 to 64, default 3): lamps per step.
- `Chase Duty` (0 to 1, default 0.3): how much of each step is lit. In Run and Mirror the number of lit lamps, in Alternate the lit time, in Bounce and Wave the width, in Twinkle how many lamps sparkle. No effect in Fill.
- `Filament` (0 to 1, default 0.3): lit lamps leave a fading trail; at 1 it lasts one whole `Clock` cycle.

## Any surface

- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched.
- `Scale`: 1 for any single screen; ⅓ for three stacked to keep the size of one screen.
- The border and the lamps are anchored to the frame. Every edge holds a whole number of pattern periods, so the corners close the same way on any surface.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.

Tested sizes (in Wire):

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok |
| 5760×1080 | 3 side by side | ok |
| 1920×3240 | 3 stacked | ok at `Scale` ⅓ |
| 1080×1920 | 9:16 | ok |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Adeco_marquee).

## Changelog

- 2026-11-13: `Scale` is now the first parameter, before `Clock` (#92). Section `Clock` renamed to `Timing` (#108). `licenseName` set to Unlicense (#91).
- 2026-11-09: Split from Deco Marquee v2 and rebuilt as an effect with the frame and the lamps; the fan moved to Deco Fan (#42, #25). New border styles and chase modes, `Filament`, `Scale`. Lamps stay clear of the corners.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
