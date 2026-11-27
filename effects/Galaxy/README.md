# Galaxy

Galaxy is an effect over the clip below: a glowing, twinkling galaxy of light points, nebulae and dust lanes that turns, morphs from one form into another and lets supernovae flash on a trigger.

Last updated: 2026-11-27

## Vision

Not a photograph of a galaxy, but one that lives: it sparkles, glows in colour and moves. All shape controls are continuous, so an Arena timeline, a beat or the sound can let the galaxy dance: a spiral grows a new arm, winds tighter, grows a bar, dissolves into chaos or shrinks to an elliptical ball of old stars. Supernovae flash on a button, MIDI note or beat. The shapes follow real galaxies (spiral, barred spiral, lenticular, elliptical, irregular), the colours and the motion are free.

Made to lie over other material, for example a night sky. Its round shape suits domes, ceilings and round stage elements; tilted, it lies on a floor or a wall in perspective.

## Requirements

- Resolume Wire / Arena: 7.29.0
- Inputs: the clip below (`Texture In`)
- Performance: two small ISF shaders plus instanced sprites in Wire. Measured in Arena at 1920×1080 on an RTX 5080 Laptop: about 18 % GPU load at the default 12 000 stars, about 30 % at 30 000; the processor load of Arena stays the same.
- Distant galaxies: at most 256; on very large surfaces or at a small `Scale` this can limit `Galaxy Density`.

## Files

| File | Purpose |
|---|---|
| `Galaxy.wire` | The patch |
| `Resource/GalaxyData.fs` | Computes position, size and colour of every star, nebula, core glow, supernova and distant galaxy (one pixel each, read back by the patch); also draws the soft light point and the small galaxy picture used for the distant galaxies |
| `Resource/GalaxyComposite.fs` | Dust lanes: dims the clip below and the light of the galaxy along the inner edge of the arms |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required. Put the effect on any clip; on its own, use a black Solid Color clip.

## Timing

Everything moves with `Clock` (0 to 1000, default 0): the galaxy turns (about one turn per 2 minutes of clock), the stars move along their orbits through the arms, nebulae breathe, stars twinkle and distant galaxies turn slowly. If the clock stops, the galaxy stops.

- Start value: in Arena set the phase source of `Clock` to `Timeline` with `Duration` = 1000 s (about 1 clock unit per second).
- Faster or slower: a shorter or longer duration. BPM Sync works as well.
- Supernovae run in real time: `Supernova Life` is in seconds.

## Parameters

- `Scale` (0 to 10, default 1): size of all elements relative to the output height.
- `Clock` (0 to 1000, default 0): see Timing.
- `Texture In`: the clip below.

**Galaxy**
- `Galaxy X`, `Galaxy Y` (-10 to 10, default 0): position of the galaxy in galaxy radii. Because they are in galaxy radii, zooming with `Galaxy Size` keeps the same point of the galaxy in place.
- `Galaxy Size` (0 to 10, default 0.8): diameter of the galaxy relative to the output height. Large values zoom into the galaxy; the light points stay points.
- `Galaxy Rotation` (-180 to 180, default 0): orientation in the picture.
- `Galaxy Tilt` (-180 to 180, default 50): tilt of the disc out of the picture plane; 0 = seen from above, 90 = seen edge-on: a glowing band with thickness, the round core in the middle and a dark dust band through it. Negative values turn the other side towards the viewer.

**Shape**
- `Core Size` (0 to 2, default 0.15): size of the core as a share of the galaxy.
- `Core Aspect` (0 to 4, default 1): proportions of the core; with `Disc Opacity` 0 it gives an elliptical galaxy.
- `Disc Opacity` (0 to 1, default 1): the disc with its arms, nebulae and dust; 0 = only the core.
- `Arm Count` (0 to 12, default 2): number of arms; between two whole numbers a new arm grows smoothly. 0 = a smooth disc (lenticular).
- `Arm Twist` (-1440 to 1440, default 300): how tightly the arms wind, in degrees from the centre to the rim; negative values mirror the spiral.
- `Arm Width` (0 to 1, default 0.3): 0 = sharp arms, 1 = a smooth disc.
- `Bar Size` (0 to 1, default 0): a straight bar through the core as a share of the galaxy; the arms start at its ends.
- `Scatter` (0 to 10, default 0): from order to chaos (an irregular galaxy).

**Stars**
- `Star Count` (0 to 30 000, default 12 000): number of stars, about 30 % of them in the core. For performance only; the look comes from the other controls.
- `Star Size` (0 to 0.5, default 0.007): size of a star relative to the output height; a few stars are larger and brighter.
- `Star Twinkle` (0 to 1, default 0.5): the stars fade in and out smoothly, about 0.8 times per clock unit, from dark up to almost twice as bright; 0 = steady. They twinkle in three random groups, each with its own rhythm, plus a little variation of their own. The core twinkles with a third of the strength, so the dense centre stays calm.
- `Hue Variation` (0 to 180, default 30): random shift of hue per star and nebula, in degrees.

**Colors**
- `Core Color`, `Arm Color`, `Nebula Color`, `Dust Color`: the core (old stars), the arms (young stars), the glowing gas clouds in the arms, and the dust (the colour the dust lets through).

**Nebula**
- `Nebula Opacity` (0 to 1, default 0.6): glowing clouds that light up in the arms and the soft glow of the disc.
- `Nebula Size` (0 to 2, default 0.12): size of the clouds as a share of the galaxy.

**Dust**
- `Dust Opacity` (0 to 1, default 0.5): how much the dust lanes darken the light of the galaxy and the clip below.
- `Dust Width` (0 to 2, default 0.06): width of the dust lanes as a share of the galaxy.

**Supernova**
- `Supernova`: trigger (button, MIDI, beat). A star of the disc flashes and a ring of light spreads in the plane of the disc. Up to four overlap.
- `Supernova Size` (0 to 2, default 0.15): final diameter of the ring relative to the output height.
- `Supernova Life` (0 to 30, default 4): duration in seconds; 0 = no supernova.
- `Supernova Color`.

**Sky**
- `Galaxy Density` (0 to 50, default 0): small distant galaxies per area (output height × output height at `Scale` 1), spread over the whole surface. They take the shape and colours of the main galaxy.

- `Seed` (0 to 1000, default 0): another value gives another galaxy with the same settings.

All sliders are safe at their extremes: no NaN, no frozen picture; `Galaxy Size` 0 simply shows the clip below.

## How it works

1. **Orbits** (`GalaxyData.fs`): each star of the disc runs on a loop with as many lobes as arms. The loops are turned a little more the further out they lie (`Arm Twist`). Where neighbouring loops crowd, arms appear, and the stars move along their loops through them, so the arms never wind up while the galaxy turns (the density wave picture of real spiral galaxies). Stars on the arm crests are brighter and bluer; nebulae only light up there.
2. **Sprites** (Wire): one instance per star, nebula, core glow and supernova point is drawn as a soft light point, added up in one render. The data for all of them come from the shader, so the processor load hardly depends on `Star Count`.
3. **Depth**: the disc has a thickness, larger towards the core; a quarter of the stars form a thicker disc around the thin one, nebulae lie flatter. `Galaxy Tilt` turns the disc in space; the core is a ball and stays round.
4. **Dust** (`GalaxyComposite.fs`): a thin layer in the disc plane, densest at the inner edge of each arm and broken up by noise. Per pixel the line of sight through the tilted layer is followed and the dust along it is added up: lanes along the arms seen from above, a dark band through the middle seen edge-on. The picture behind is dimmed accordingly, never to black.
5. **Distant galaxies**: the shader draws a small picture of the galaxy; up to 256 copies, turned, tilted and tinted, are spread over the surface.

## Any surface

- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched.
- `Scale`: 1 for one projector or any single screen; ⅓ for three stacked to keep the size of the galaxy and the stars.
- The galaxy is one object, anchored at the centre and placed with `Galaxy X` / `Galaxy Y`. Wide surfaces get content across the whole width through `Galaxy Density`. On portrait surfaces the galaxy is wider than the screen at the default size; lower `Galaxy Size` or `Scale`.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.

Tested sizes:

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok |
| 5760×1080 | 3 side by side | ok; distant galaxies across the width |
| 1920×3240 | 3 stacked | ok; galaxy wider than the screen at `Galaxy Size` 0.8, as by the model |
| 1080×1920 | 9:16 | ok; as above |
| 4096×512 | 8:1 | ok |
| 512×512 | 1:1 | ok |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Agalaxy).

## Changelog

- 2026-11-27: `Star Twinkle` calmer: about 0.8 instead of 2.5 changes per clock unit, a smooth curve that fades in and out instead of a sharpened one, and the core twinkles with a third of the strength; before, the twinkling looked erratic, above all in the core. Tested in Arena (1920×1080) with the timeline on `Clock`: `Star Twinkle` 1, core and arms. Patch loads in Wire.
- 2026-11-27: `Star Twinkle` rebuilt: three random groups of stars with their own restless rhythm (noise instead of a slow sine) and some individual flicker; stars now also brighten, up to almost twice as bright. Before, each star dimmed on its own slow sine, which evened out and barely showed. Tested in Arena (1920×1080) with the timeline on `Clock`: `Star Twinkle` 0, 0.5 and 1, core and arms. Patch loads in Wire.
- 2026-11-27: `Galaxy Aspect` replaced by `Galaxy Tilt`, an angle (new glossary pattern `<Element> Tilt`). The disc has a real thickness and is tilted in space instead of squashed; the dust follows the line of sight, so edge-on shows a glowing band with a dark dust band. Tested in Arena (1920×1080, 5760×1080, 1920×3240, 1080×1920, 4096×512, 512×512): tilts 0, 50, -60, 78, 80, 90, ±180, edge-on at `Galaxy Size` 3 and 10, `Scale` 0. Patch loads in Wire.
- 2026-11-27: First version (#80). Tested in Arena (1920×1080, 5760×1080, 1920×3240, 1080×1920, 4096×512, 512×512): forms (bar, 4.5 arms, elliptical, irregular, edge-on), `Clock` 0 to 1000, supernova, distant galaxies, `Star Count` 12 000 and 30 000, every slider at both extremes. Patch loads in Wire.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
