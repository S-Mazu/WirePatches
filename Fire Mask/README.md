# Fire Mask

An effect in which the brightness of the clip below decides what burns: logos, text or silhouettes.

Last updated: 2026-11-08

## Vision

Set anything on fire by its brightness: white shapes, black shapes, or one exact tone in between. Built on a real-time fluid simulation, so that flames lick, smoke rolls, and wind carries everything along together.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: the clip below (this is an effect)
- Performance: one shader with a real-time fluid simulation of up to 540 rows and 16 pressure steps per frame. Not measured in Arena.

## Files

| File | Purpose |
|---|---|
| `Fire Mask.wire` | The patch |
| `Resource/FireMask.fs` | ISF shader with the fluid simulation and the picture |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required.

## How to use it in Arena

Add it as an effect to a clip, a layer or a group. On a layer or group the fire stays when you switch clips, and you need no extra layer.

## Parameters

All ranges are deliberately wide; the extremes are safe. There is no `Clock`: the fire burns as soon as it is loaded, in real time and independent of the frame rate.

- `Scale` (0 to 10, default 1): size of everything relative to the output height.

**Physics**
- `Speed` (0 to 10, default 1): tempo of the fire and smoke. 0 freezes the image.
- `Rise` (0 to 5, default 1): how strongly hot gas climbs. 0 lets the smoke hang in the air.
- `Wind Direction (°)` (0 to 360, default 0): where the wind blows to; 0 = right, 90 = up.
- `Wind Strength` (0 to 10, default 0): carries all flames and all smoke along together.
- `Swirl` (0 to 5, default 1): the rolling eddies in flames and smoke.

**Fire**
- `Fire Amount` (0 to 10, default 1): how much burns. 0 = no flames. Above 1 flames take over more and more of the frame; at 10 the screen is an inferno.
- `Flame Length` (0 to 10, default 1): how far flames reach before they burn out.
- `Fire Burst` (0 to 1, default 0): a short flare-up while it is high, good for FFT or a button.
- `Flame Core`, `Flame Mid`, `Flame Tip`: colors from the hottest to the coolest part.

**Smoke**
- `Smoke Amount` (0 to 10, default 0.5): how much smoke rises. Above 2 a fog builds over the whole frame; at 10 with a long `Smoke Life (s)` it is a dense fog.
- `Smoke Life (s)` (0.1 to 120, default 4): seconds until smoke dissolves.
- `Smoke Color`.

**Burn**
- `Burn Level` (0 to 1, default 1): the brightness that burns. 0 = black, 1 = white.
- `Burn Range` (0 to 2, default 0.5): the width of the burning band around that brightness.
- `Burn Softness` (0 to 1, default 0.08): soft edge of that band.
- `Show Input` (Off / On, default On): keeps the clip visible under the fire. Off shows only flames and smoke.

## Any surface

- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched.
- `Scale`: 1 for any single screen, also 4K; ⅓ for three stacked to keep the size of one screen.
- The simulation grid depends on the output height, up to 540 rows, so large surfaces do not cost more per row.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.

Tested sizes (in Wire):

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok, at levels 0 and 1 |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Afire_mask).

## Changelog

- 2026-11-08: First version. Split from Fire and Smoke, with a new fluid simulation (#43, #26, #27).

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
