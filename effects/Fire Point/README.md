# Fire Point

An effect that makes flames and smoke rise from one burning shape, on top of the clip below.

Last updated: 2026-11-08

## Vision

A single, freely placed fire: a point, a square or a heart. Built on a real-time fluid simulation, so that flames lick, smoke rolls, and wind carries everything along together.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: the clip below (this is an effect)
- Performance: one shader with a real-time fluid simulation of up to 540 rows and 16 pressure steps per frame. Not measured in Arena.

## Files

| File | Purpose |
|---|---|
| `Fire Point.wire` | The patch |
| `Resource/FirePoint.fs` | ISF shader with the fluid simulation and the picture |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required.

## How to use it in Arena

Add it as an effect to a clip, a layer or a group. On a layer or group the fire stays when you switch clips, and you need no extra layer.

## Parameters

All ranges are deliberately wide; the extremes are safe. There is no `Clock`: the fire burns as soon as it is loaded, in real time and independent of the frame rate.

- `Scale` (0 to 10, default 1): size of everything relative to the output height.

**Physics**
- `Speed` (0 to 10, default 1): 0 freezes the image, below 1 is slow motion (a lava lamp at low values). Above 1 the flow gets stronger; the fluid does not break up.
- `Rise` (0 to 5, default 1): how strongly hot gas climbs. 0 lets the smoke hang in the air.
- `Wind Direction (°)` (0 to 360, default 0): where the wind blows to; 0 = right, 90 = up.
- `Wind Strength` (0 to 10, default 0): carries all flames and all smoke along together.
- `Swirl` (0 to 5, default 1): the large rolling eddies in flames and smoke. The fine eddies stay the same at every value.

**Fire**
- `Fire Strength` (0 to 10, default 1): how strongly the source burns. 0 = no flames. Above 1 the flames feed themselves and spread from the source; at 10 the screen is an inferno. Nothing ignites in clean air.
- `Flame Reach` (0 to 10, default 1): how far flames reach before they burn out.
- `Fire Burst` (0 to 1, default 0): a short flare-up while it is high, good for FFT or a button.
- `Flame Core`, `Flame Mid`, `Flame Tip`: colors from the hottest to the coolest part.

**Smoke**
- `Smoke Amount` (0 to 10, default 0.5): how much smoke rises. Above 2 it stays longer and collects under the top of the frame; at 10 the source fills the frame with a dense fog.
- `Smoke Life (s)` (0.1 to 120, default 4): seconds until smoke dissolves.
- `Smoke Color`.

**Shape**
- `Shape` (Point / Square / Heart, default Point).
- `Shape X`, `Shape Y` (-20 to 20, defaults 0, -0.45): position, 0 = centre of the frame, in height units.
- `Shape Z` (-2 to 20, default 0): depth. Higher values are further away and smaller.
- `Shape Size` (0 to 10, default 0.16).
- `Shape Rotation (°)` (-360 to 360, default 0).
- `Shape Outline` (Off / On, default Off): only the edge of the shape burns.
- `Outline Width` (0 to 2, default 0.03).

## Any surface

- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched.
- `Scale`: 1 for any single screen, also 4K; ⅓ for three stacked to keep the size of one screen.
- The simulation grid depends on the output height, up to 540 rows, so large surfaces do not cost more per row. A `Scale` above 1 uses a smaller part of the grid, so the flames stay smooth at any `Scale`.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.

Tested sizes (in Wire):

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok, with Point, Square and Heart |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Afire_point).

## Changelog

- 2026-11-08: Wind now carries all of the smoke: none stays behind or moves against it. Flames stay smooth at a `Scale` above 1. The controls `Burn X`, `Burn Y`, `Burn Z`, `Burn Size` and `Burn Rotation (°)` are now named `Shape …`.
- 2026-11-08: Speed and Swirl no longer tear the fluid apart. Fire and fog now only grow from the source. Smoke gets depth at high density. Renamed `Fire Amount` to `Fire Strength` and `Flame Length` to `Flame Reach`.
- 2026-11-08: First version. Split from Fire and Smoke, with a new fluid simulation (#43, #26, #27).

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
