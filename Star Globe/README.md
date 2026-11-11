# Star Globe

An effect: glowing points over the clip below. Fireflies that roam the whole frame, or stars caught in a vessel that fall, bounce, collide and simmer.

Last updated: 2026-11-11

## Vision

A snow globe of light. Stars lie in a glass, boil up and settle again; or they float through the whole picture like fireflies on a summer night. With the line of the vessel hidden, the points alone show the shape: a heart that only becomes visible when it fills.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: the clip below (this is an effect)
- Performance: one multipass ISF shader, up to 512 points with collision, four steps per frame. Not measured in Arena.

## Files

| File | Purpose |
|---|---|
| `Star Globe.wire` | The patch |
| `Resource/StarGlobe.fs` | ISF shader: time, point simulation (forces, collision, wall) and picture |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required. For points on black, put a black clip (e.g. Solid Color) below.

## Time

The points move in real time and do not depend on the frame rate; there is no `Clock`. The motion never repeats.

## Parameters

All ranges are deliberately wide; the extremes are safe.

- `Scale` (0 to 10, default 1): size of everything relative to the output height.

**Physics**
- `Gravity` (0 to 20, default 1.5), `Gravity Dir (°)` (0 to 360, default 270 = down).
- `Bounce` (0 to 2, default 0.4): how much speed a point keeps when it hits the wall or another point. Above 1 the wall adds speed.
- `Air Drag` (0 to 10, default 0.3).
- `Simmer` (0 to 10, default 0.3): random kicks. A pile starts to boil.
- `Wander` (0 to 10, default 0): every point flies about on its own and turns away from the wall, like a firefly.
- `Collide` (0 to 2, default 1): points push each other apart. 0 = they pass through each other.

**Container**
- `Shape`: Frame (the edges of the output are the wall), Circle, Polygon, Heart. Every shape is also limited by the frame, so no point leaves the picture.
- `Corners` (3 to 24, default 6): for Polygon.
- `Container X`, `Container Y` (-2 to 2, default 0): centre of the container. 0 = centre, -1 and 1 = edges of the frame.
- `Size` (0 to 5, default 0.85): radius in output heights.
- `Width` (0.1 to 10, default 1): 1 = round, larger = wider.
- `Rotation (°)` (-360 to 360, default 0).
- `Line Width` (0 to 0.2, default 0.004): 0 hides the line, and the points alone show the shape.
- `Line Color`.

**Stars**
- `Amount` (0 to 512, default 120): new points appear inside the container. The last point fades in with fractional values.
- `Star Size` (0 to 0.2, default 0.015): radius of the solid core, also the size for collisions.
- `Glow Size` (0 to 0.5, default 0.04).
- `Star Color`, `Glow Color`.
- `Twinkle` (0 to 1, default 0): every point flickers on its own.

Starting points:
- Fireflies: Shape Frame, Gravity 0, Wander 1.5, Collide 0, Twinkle 0.8, Line Width 0, small stars, green-yellow glow.
- Star globe: Shape Circle, Gravity 1.5, Collide 1, Simmer 2.
- Hidden heart: Shape Heart, Line Width 0, Amount 220, Star Size 0.022, Simmer 1.5.

## Any surface

- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched.
- `Scale`: 1 for any single screen; ⅓ for three stacked to keep the size of one screen.
- Shape Frame fills any surface; the other shapes are cut by the frame when they are larger than it.
- `Container X` / `Container Y` are relative to the frame.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.

Tested sizes:

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok (Arena) |
| 5760×1080 | 3 side by side | ok (Wire) |
| 1080×1920 | 9:16 | ok (Wire) |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Astar_globe).

## Changelog

- 2026-11-11: Rebuilt as an effect with one shader (#50, #35). Real time instead of `Physics Clock`; the motion no longer repeats. Shapes Frame, Circle, Polygon and Heart instead of a stepless morph; Trapezoid removed. New `Simmer`, `Wander`, `Twinkle`, `Scale`. Physics in one group; `Jolt` and `Jitter` removed (Simmer replaces Jitter). `Line Width` in output heights instead of pixels. `Opacity` and `Backdrop` removed. Shader in `Resource/` with a relative path (#8).

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
