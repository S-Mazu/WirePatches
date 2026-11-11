# Water Drops

An effect: a view straight down onto water over the clip below. Objects fall in, rings spread, interfere and reflect at the edges of the frame.

Last updated: 2026-11-11

## Vision

A still pond that the picture lies under. A drop, a stone or a heart falls in, its shadow comes towards it, it hits, a crown splashes up, and a train of rings runs out and bends the picture below. On the kick, on a button or on a MIDI note.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: the clip below (this is an effect)
- Performance: one multipass ISF shader; the water is simulated at up to 300 rows with 16 steps per frame. Not measured in Arena.

## Files

| File | Purpose |
|---|---|
| `Water Drops.wire` | The patch |
| `Resource/WaterDrops.fs` | ISF shader: time, objects in flight, wave simulation and picture |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required. For drops on black, put a black clip (e.g. Solid Color) below.

## Time

The water runs in real time and does not depend on the frame rate; there is no `Clock`. Without new drops the water calms down and stands still.

## Parameters

All ranges are deliberately wide; the extremes are safe.

- `Scale` (0 to 10, default 1): size of everything relative to the output height.

**Drop**
- `Drop` (0 to 1): every rising edge over 0.5 releases one object. Map it to a button, a MIDI note, the FFT kick or a BPM ramp. Up to 8 objects can be in the air at once.
- `Object`: Drop, Stone, Heart or Mixed (each object picks one at random).
- `Drop X`, `Drop Y` (-2 to 2, default 0): impact point. 0 = centre, -1 and 1 = edges of the frame.
- `Spread` (0 to 5, default 0.3): random scatter around `Drop X` / `Drop Y`, relative to the frame. 0 = always the same point.
- `Drop Size` (0 to 0.5, default 0.03): size of the object and its crater.
- `Drop Strength` (0 to 5, default 1): how hard it hits: height of the waves and of the splash.
- `Fall Time (s)` (0 to 5, default 0.4): seconds from release to impact. 0 = instant.
- `Heart Color`.

A drop is a small lens that shows the picture below upside down. Stones and hearts tumble while they fall. Each object casts a shadow that meets it at the impact.

**Physics**
- `Wave Speed` (0 to 5, default 0.4): how fast the rings run, in output heights per second at `Scale` 1.
- `Wave Life (s)` (0.05 to 60, default 6): seconds until the rings have faded.

**Water**
- `Refraction` (0 to 5, default 0.4): how much the waves bend the clip below.
- `Shading` (0 to 5, default 1): how plastic the waves look.
- `Glints` (0 to 5, default 1): sun glints on the crests.
- `Light Color`: sky reflection and light on the waves.
- `Light Angle (°)` (0 to 360, default 135): direction the light comes from; it also sets where the shadows fall.

## Any surface

- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched; the rings stay round.
- `Scale`: 1 for any single screen; ⅓ for three stacked to keep the size of one screen.
- Exception: `Drop X`, `Drop Y` and `Spread` are relative to the frame, so on a wide surface the drops land across the whole width.
- The waves reflect at the edges of the frame.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.

Tested sizes:

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok (Arena) |
| 5760×1080 | 3 side by side | ok (Wire) |
| 1080×1920 | 9:16 | ok (Wire) |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Awater_drops).

## Changelog

- 2026-11-11: Rebuilt as an effect (#51). The waves bend the clip below (`Refraction`, #81). Real time instead of frames, several objects in the air, objects Drop, Stone, Heart and Mixed with shadow, splash crown and rebound, a softer, more realistic impact. Controls renamed (`Wave Life (s)`, `Wave Speed`, `Shading`, `Glints`, `Light Color`, `Light Angle (°)`), new `Scale`. One shader in `Resource/` with a relative path (#9). `Backdrop` removed.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
