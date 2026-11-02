# Rolling Landscape

Stand: 02.11.2026

A Resolume Wire source patch. An Art Deco night skyline of the 1930s scrolls past as up to six parallax silhouette layers: towers in the back, city buildings in the middle, street level in front, with lit windows, a moon, searchlights and airships.

## Vision

A calm backdrop in the style of the 1930s: the city rolls by like a stage set. Every element can be added step by step (layers, beams, airships), so the picture can grow with the music.

## Any surface

The same patch works on one projector, on three projectors side by side (e.g. 5760 x 1080) or stacked (e.g. 1920 x 3240) and on LED walls of any size:

- Sizes are in scene units set by `Scale`: 1 = sized for a projector 1080 px high. For 2160 px per projector (4K) use 2. On a stacked surface the city keeps its size and the sky grows.
- Positions (`Horizon`, `Moon X`, `Moon Y`) are relative to the frame: -1 = left / bottom edge, 1 = right / top edge.
- Searchlights and airships are densities, not counts: a wider or taller surface simply gets more of them, with no upper limit.
- Set the Arena composition to the full surface size before loading the patch; after changing the composition size, reload the clip.

## Clock (required)

The patch has no internal motion. Everything moves with the first parameter, `Clock` (range 0 to 1000). Its rate is the scroll speed; if the clock stops, the city stops.

- Default is 0: nothing moves.
- Start value: in Arena set the phase source of `Clock` to `Timeline` with `Duration` = 1000 s (about 1 clock unit per second) for a slow, calm roll.
- Play the timeline backwards to scroll the other way. BPM Sync works as well.

## Parameters

- **Scale** (0 to 10): size of the whole scene, 1 = a 1080 px high projector.
- **Physics:** `Parallax` (-2 to 2): 0 = all layers equal, positive = back layers slower, negative = back layers faster.
- **Skyline:** `Layer Count` (0 to 6, fractional values fade the next layer in, good for FFT), `Density`, `Horizon` (-2 to 2, -1 = bottom edge), `Seed` (0 to 1000, a new city per value), `Layer 1 Color` to `Layer 6 Color` (1 = back), `Window Lights`, `Window Color`.
- **Moon:** `Moon X`, `Moon Y` (-3 to 3, -1 / 1 = frame edges), `Moon Opacity`, `Moon Size` (0 to 2, 0 hides it), `Moon Phase` (0 = new moon, 0.25 = first quarter, 0.5 = full, 0.75 = last quarter), `Moon Ray Length` (0 to 10, 1 = classic, 0 = no rays), `Moon Color`.
- **Searchlights:** `Beam Density` (0 to 1, share of ground spots with a light), `Beam Length` (0 to 20), `Beam Color`.
- **Airships:** `Airship Density` (0 to 1, rows over the whole sky), `Airship Dir` (Left / Right / Random per row), `Airship Color`.
- **Backdrop:** `Backdrop On`, `Backdrop Color`.

## Install

Copy this folder into your Resolume Wire `Patches` folder (Documents/Resolume Wire/Patches/). The shader `RollingLandscape.fs` must stay next to the patch.

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
