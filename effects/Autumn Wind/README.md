# Autumn Wind

An effect: 16 kinds of items drift through a real space in front of the viewer, over the clip below, with depth, gusts, tumble and eddies.

Last updated: 2026-11-25

## Vision

Leaves, hats and newspapers blown through the picture, from a light autumn breeze to a storm: across the picture, into the distance or straight towards the audience. Made to lie over other material, for example the skyline of Rolling Landscape.

## Requirements

- Resolume Wire / Arena: 7.29.0
- Inputs: the clip below (this is an effect)
- Performance: up to 1024 instanced items, native Wire nodes, no shader. Measured in Arena at 1920×1080 on an RTX 5080 Laptop with every Density at 64 (1024 items): about 51 % GPU load, against 40 % for the previous 2D version.
- `Performance Cap` (default 1024): on very large surfaces or at a small `Scale` it can limit the density.

## Files

| File | Purpose |
|---|---|
| `Autumn Wind.wire` | The patch |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. No further setup required.

## Timing

No `Clock`: the items move in real time (the computer's clock). Their speed follows `Wind Strength`, `Fall Strength` and `Gust Sway`; to slow everything down, lower these.

## The space

The items fly in a real space in front of the viewer, not on a flat picture:

- The picture plane lies one picture height in front of the viewer. An item there has the size set by `Item Size`.
- Behind it the space reaches as deep as `Depth Spread` sets; far items are smaller, slower on the screen and fade with `Depth Haze`.
- In front of it items come up to a quarter of a picture height to the viewer and fade out on the way.
- Depth converges at the vanishing point (`Vanishing Point X`, `Vanishing Point Y`). Items that come closer spread out from it and leave the picture at the sides.
- An item that leaves the space comes back where the wind brings new ones in: across the picture from the edge the wind comes from, from the distance for a headwind, from the sides and from the front for a tailwind. Items entering from the distance or from the front fade in.
- Near items are drawn over far ones.

At `Wind Direction Z` 90 the wind blows only in the picture plane, as in the previous version.

## Parameters

All ranges are deliberately wide; the extremes are safe (tested in Arena).

- `Scale` (0 to 10, default 1): size of everything relative to the output height.

**Physics**
- `Wind Strength` (0 to 10, default 0.3), `Wind Direction` (0 to 360, default 0): where the wind blows to in the picture plane; 0 = right, 90 = up.
- `Wind Direction Z` (0 to 360, default 90): where the wind blows to in depth. 0 = away from the viewer (tailwind), 180 = towards the viewer (headwind), 90 = in the picture plane along `Wind Direction`, 270 = in the picture plane against it.
- `Fall Strength` (0 to 10, default 0.15), `Fall Direction` (0 to 360, default 270): where the items fall to; 270 = down.
- `Gust Sway` (0 to 5, default 0.3): the swing of gusts across the wind. Gusts run through the items as fronts, so neighbours sway together.
- `Tumble` (0 to 5, default 0.25): how much items spin and flip; the back of a flipped item is darker.
- `Speed Variation` (0 to 1, default 0.3).
- `Eddies` (0 to 5, default 0.5): how often and how strongly eddies whirl the items around and lift them. 0 = no eddies. An eddy is a volume of air in the space, as deep as it is wide, with its own axis; the wind carries it, and it only catches items near it, also in depth.
- `Eddy Size` (0 to 5, default 0.6): small and large eddies alternate around this size.

Every kind has its own weight: chestnuts drop, feathers drift, bubbles rise, paper planes and raindrops point along their path.

**Items**
- `Performance Cap` (0 to 1024, default 1024): upper limit of items in flight, for performance only. When the densities ask for more, all kinds are scaled down together.
- `Item Size` (0 to 2, default 0.2): size of an item in the picture plane.
- `Depth Spread` (0 to 1, default 0.5): how deep the space is. 0 = everything in the picture plane; 1 = the space reaches 50 times as far.
- `Depth Haze` (0 to 1, default 0.35): distant items fade, so near items stand out.
- `Vanishing Point X` (-10 to 10, default 0), `Vanishing Point Y` (-10 to 10, default 1): where depth converges. X from the centre, Y from the bottom edge, in output heights × `Scale`; the default lies in the middle of the picture at `Scale` 1.
- `Color Variation` (0 to 1, default 0.4), `Variation Color`: some items take on this second color.

**Autumn**, **Street**, **Party**, **Sky**

Each item has a `Density` (0 to 64) and a `Color`, next to each other. A Density is the number of items per reference area (output height × output height at `Scale` 1): a 16:9 frame holds about 1.8 times the value, three screens side by side 5.3 times. Items in flight stay until they leave the space; raising a Density releases more at once. Defaults: `Leaf Density` 6.75 and `Newspaper Density` 2.25.
- Autumn: Leaf, Maple Seed, Chestnut, Twig, Feather.
- Street: Bowler, Cap, Paper Plane, Newspaper.
- Party: Confetti, Bubble, Heart, Note.
- Sky: Raindrop, Snowflake, Star, and `Star Twinkle` (0 to 1).

Typical: `Wind Strength` on the bass, `Leaf Density` on the kick, `Star Twinkle` on the hats.

## Any surface

- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched.
- `Scale`: 1 for any single screen; ⅓ for three stacked to keep the size of one screen.
- The vanishing point is anchored: X at the centre, Y at the bottom edge, its offset in output heights × `Scale`. With `Scale` ⅓ it stays one third of the height above the bottom.
- The space follows the aspect ratio of the output, so wide surfaces get items across the whole width. The densities keep the same number of items per screen height, so a wider surface gets proportionally more items.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.

Tested sizes (Arena, composition 1920×1080, `Wind Direction Z` 90, 0 and 180):

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok |
| 5760×1080 | 3 side by side | ok |
| 1920×3240 | 3 stacked | ok |
| 1080×1920 | 9:16 | ok |
| 4096×512 | 8:1 | ok |
| 512×512 | 1:1 | ok |

When the clip's aspect ratio differs from the composition's, Arena also shows items outside the clip's area; the previous version does the same.

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Aautumn_wind).

## Changelog

- 2026-11-25: Real depth (#142). The items fly in a space in front of the viewer; new `Wind Direction Z` (towards and away from the viewer), `Vanishing Point X`, `Vanishing Point Y`. Real time instead of `Clock`. Items leaving the space re-enter where the wind brings them in. Gusts travel as fronts, `Tumble` also flips the items (darker back), eddies are volumes with their own axis and are carried by the wind. Near items are drawn over far ones.
- 2026-11-13: Glossary (#95, #89): `<Item> Amount` → `<Item> Density` (items per reference area, so wider surfaces and a smaller `Scale` get more items; defaults adjusted to keep the 16:9 look); `Max Items` → `Performance Cap` (limit only); `Object Size` → `Item Size`; no degree unit in `Wind Direction` and `Fall Direction`. `Variation Color` stays.
- 2026-11-13: `Scale` is now the first parameter, before `Clock` (#92). Section `Clock` renamed to `Timing` (#108). `licenseName` set to Unlicense (#91).
- 2026-11-10: Revised to the current rules (#40). Now an effect over the clip below. Items grouped in themes with Amount and Color side by side; new item Newspaper. New `Eddies`, `Eddy Size`, `Depth Haze` and `Scale`; the area follows the aspect ratio. `Wind Clock (x)` is now `Clock`; `Overlay Opacity` and `Backdrop` removed.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
