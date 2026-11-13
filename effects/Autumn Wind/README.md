# Autumn Wind

An effect: 16 kinds of items drift across the clip below with depth, gusts, tumble and eddies.

Last updated: 2026-11-13

## Vision

Leaves, hats and newspapers blown through the picture, from a light autumn breeze to a storm. Made to lie over other material, for example the skyline of Rolling Landscape.

## Requirements

- Resolume Wire / Arena: 7.28.1
- Inputs: the clip below (this is an effect)
- Performance: up to 1024 instanced items, native Wire nodes, no shader. Not measured in Arena.
- `Performance Cap` (default 1024): on very large surfaces or at a small `Scale` it can limit the density.

## Files

| File | Purpose |
|---|---|
| `Autumn Wind.wire` | The patch |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required.

## Timing

Nothing moves by itself: `Clock` drives all motion. If the clock stops, everything stops, eddies included. Start with phase source Timeline, Duration 1000 s, In 0, Out 1000. BPM Sync or FFT work as well.

## Parameters

All ranges are deliberately wide; the extremes are safe.

- `Scale` (0 to 10, default 1): size of everything relative to the output height.
- `Clock` (0 to 1000, default 0).

**Physics**
- `Wind Strength` (0 to 10, default 0.3), `Wind Direction` (0 to 360, default 0): where the wind blows to; 0 = right, 90 = up.
- `Fall Strength` (0 to 10, default 0.15), `Fall Direction` (0 to 360, default 270): where the items fall to; 270 = down.
- `Gust Sway` (0 to 5, default 0.3): the swing of gusts across the wind.
- `Tumble` (0 to 5, default 0.25): how much items spin.
- `Speed Variation` (0 to 1, default 0.3).
- `Eddies` (0 to 5, default 0.5): how often and how strongly eddies whirl the items around and lift them. 0 = no eddies.
- `Eddy Size` (0 to 5, default 0.6): small and large eddies alternate around this size.

Every kind has its own weight: chestnuts drop, feathers drift, bubbles rise, paper planes and raindrops point along their path.

**Items**
- `Performance Cap` (0 to 1024, default 1024): upper limit of items in flight, for performance only. When the densities ask for more, all kinds are scaled down together.
- `Item Size` (0 to 2, default 0.2).
- `Depth Spread` (0 to 1, default 0.5): how far apart near and far items are in size and speed.
- `Depth Haze` (0 to 1, default 0.35): distant items fade, so near items stand out.
- `Color Variation` (0 to 1, default 0.4), `Variation Color`: some items take on this second color.

**Autumn**, **Street**, **Party**, **Sky**

Each item has a `Density` (0 to 64) and a `Color`, next to each other. A Density is the number of items per reference area (output height × output height at `Scale` 1): a 16:9 frame holds about 1.8 times the value, three screens side by side 5.3 times. Items in flight stay until they leave the frame; raising a Density releases more at once. Defaults: `Leaf Density` 6.75 and `Newspaper Density` 2.25, which give the same picture on 16:9 as before.
- Autumn: Leaf, Maple Seed, Chestnut, Twig, Feather.
- Street: Bowler, Cap, Paper Plane, Newspaper.
- Party: Confetti, Bubble, Heart, Note.
- Sky: Raindrop, Snowflake, Star, and `Star Twinkle` (0 to 1).

Typical: `Wind Strength` on the bass, `Leaf Density` on the kick, `Star Twinkle` on the hats.

## Any surface

- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched.
- `Scale`: 1 for any single screen; ⅓ for three stacked to keep the size of one screen.
- The area the items travel through follows the aspect ratio of the output, so wide surfaces get items across the whole width. The densities keep the same number of items per screen height, so a wider surface gets proportionally more items.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.

Tested sizes:

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok (Arena) |
| 5760×1080 | 3 side by side | ok (Arena) |
| 1080×1920 | 9:16 | ok (Wire) |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Aautumn_wind).

## Changelog

- 2026-11-13: Glossary (#95, #89): `<Item> Amount` → `<Item> Density` (items per reference area, so wider surfaces and a smaller `Scale` get more items; defaults adjusted to keep the 16:9 look); `Max Items` → `Performance Cap` (limit only); `Object Size` → `Item Size`; no degree unit in `Wind Direction` and `Fall Direction`. `Variation Color` stays.
- 2026-11-13: `Scale` is now the first parameter, before `Clock` (#92). Section `Clock` renamed to `Timing` (#108). `licenseName` set to Unlicense (#91).
- 2026-11-10: Revised to the current rules (#40). Now an effect over the clip below. Items grouped in themes with Amount and Color side by side; new item Newspaper. New `Eddies`, `Eddy Size`, `Depth Haze` and `Scale`; the area follows the aspect ratio. `Wind Clock (x)` is now `Clock`; `Overlay Opacity` and `Backdrop` removed.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
