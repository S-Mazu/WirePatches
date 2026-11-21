# Motion Bridge - Autumn Wind

Motion Bridge - Autumn Wind is an effect on the [Motion Bridge](../../tools/Motion%20Bridge/) field: leaves, maple seeds, chestnuts, twigs and feathers drift in the wind over a transparent background, across the screen with depth or over the floor in perspective. Blown against someone standing still, they stick to them and gather there; motion stirs them up again.

**It needs the Motion Bridge.** The patch reacts only to the bridge's Spout texture `Motion Bridge`: the bridge with a 2D lidar, set up for the room, must run on the same computer as Arena. Without it the items just drift in the wind.

Last updated: 2026-11-21

## Vision

Autumn in the room. The wind carries leaves across the surface; whoever stands still collects them like a fence post, and a little pile grows at their feet. When they move, the pile bursts and the leaves whirl off again. Based on the source patch [Autumn Wind](../Autumn%20Wind/) and issue #70.

## Requirements

- Resolume Wire / Arena: 7.29.0
- Inputs: the clip below (`Texture In`), which must be the Spout source `Motion Bridge` (see Installation)
- Tool: [Motion Bridge](../../tools/Motion%20Bridge/), Output "Spout texture" (R occupancy, G motion, B visibility, status band)
- Performance: one ISF shader with seven passes: a tiny one for time and the bridge's sign of life, three at 160 × 120 for the field, two simulation steps for up to 1024 items, and the picture, which looks at every item for every pixel. At 1920 × 1080 on an RTX 5080 Laptop it adds about 12 % GPU load at the default densities; with every density, size and the cap at maximum (1024 large items) the load reaches 90 to 98 %, the picture stays stable.
- `Performance Cap` (default 1024): at most this many items. On very large surfaces it can limit the density.

## Files

| File | Purpose |
|---|---|
| `Motion Bridge - Autumn Wind.wire` | The patch |
| `Resource/AutumnWind.fs` | ISF shader: the bridge's sign of life, the field (blur, memory, held shadows), the item simulation (wind, sticking, stirring) and the picture (the item shapes) |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. Start the [Motion Bridge](../../tools/Motion%20Bridge/) and set up the room there.
3. In Arena, load the Spout source `Motion Bridge` as a clip. It loads at the texture's own size; keep it.
4. Set the layer's *Auto Size* to *Stretch* and its blend mode to *Alpha*.
5. Drag this patch onto the clip, after its *Transform*.
6. With `Show Field` *On*, move the field with `Field Plane`, `Field X`, `Field Y`, `Field Width`, `Field Height` and `Field Rotation` until it lies where the sensed area is seen.

## Timing

No `Clock`: the items are a simulation in real time (the computer's clock), like Motion Bridge - Fireflies. Their speed follows `Wind Strength`, `Fall Strength` and `Gust Sway`.

The reaction to the room runs in real time too: occupancy is smoothed over 0.15 s, motion fades out over 1.5 s, held shadows over `Shadow Hold`. When the bridge's status band stops changing for more than 0.25 s, the field's influence fades out within a second and the items drift on; stuck items let go. No black frame, nothing frozen.

## Parameters

All ranges are deliberately wide; the extremes are safe (tested in Arena).

- `Scale` (0 to 10, default 1): size of all elements relative to the output height, also their speed.

**Physics**
- `Wind Strength` (0 to 10, default 0.3), `Wind Direction` (0 to 360, default 0 = to the right).
- `Fall Strength` (0 to 10, default 0.15), `Fall Direction` (0 to 360, default 270 = down). On the floor, falling means drifting along the floor in that direction.
- `Gust Sway` (0 to 5, default 0.3): gusts that change the wind over time.
- `Tumble` (0 to 5, default 0.25): how fast the items turn and flip; the back of a leaf is darker.
- `Speed Variation` (0 to 1, default 0.3): each item a little faster or slower.
- `Eddies` (0 to 5, default 0.5), `Eddy Size` (0 to 5, default 0.6): swirls in the wind.
- `Stir Strength` (0 to 10, default 1): how hard motion stirs the items up and tears stuck items off. On the screen they are flung upwards, on the floor away to the sides.
- `Stick Strength` (0 to 10, default 1): how fast items stick to what stands still. 0 = they drift through.

**Items**
- `Item Plane` (Floor / Screen, default Screen): Screen = across the frame, each item at its own depth; Floor = over the floor in perspective, from the bottom edge to four times that distance (or to the top edge when the horizon lies above the frame). Independent of `Field Plane`.
- `Performance Cap` (0 to 1024, default 1024): at most this many items.
- `Item Size` (0 to 2, default 0.2): size of an item, in output heights.
- `Depth Spread` (0 to 1, default 0.5): on the screen, how much smaller and slower far items are.
- `Depth Haze` (0 to 1, default 0.35): how much far items fade.
- `Color Variation` (0 to 1, default 0.4): share of each item's colour mixed towards `Variation Color` (default dark red), differently per item.

**Autumn**
- `Leaf Density` (0 to 64, default 16), `Maple Seed Density` (default 4), `Chestnut Density` (default 0), `Twig Density` (default 2), `Feather Density` (default 0): items per output height × output height at `Scale` 1, so a wider surface gets more. On the floor they count per floor area (measured at the bottom edge).
- `Leaf Color`, `Maple Seed Color`, `Chestnut Color`, `Twig Color`, `Feather Color`.

**View**
- `Horizon Y` (0 to 10, default 0.7): height of the floor's horizon above the bottom edge, in output heights, multiplied by `Scale`. Used where the items or the field lie on the floor.

**Field**: where the bridge's field lies in the picture.
- `Field Plane` (Floor / Screen, default Screen): Floor = on the floor in perspective, as in Motion Bridge - Fabric Wind; Screen = the field is read as seen from the front (issue #70): the position along the front is the picture's x, the distance from the surface is the item's depth. So motion near the surface stirs near items, far away the far ones, and someone standing still is an invisible column where items of their depth gather.
- `Field X` (-10 to 10, default 0.5), `Field Width` (0 to 10, default 1): centre and width of the front, as shares of the frame width.
- `Field Y` (-10 to 10, default 0.5), `Field Height` (0 to 10, default 1): on the floor the centre and depth of the field; on the screen `Field Height` is the share of the field's depth that the items' depths cover: 1 = the farthest items match the far edge; 2 = they match the middle, and the far half of the field acts on nothing; 0.5 = items of medium depth already match the far edge, farther ones lie beyond the field.
- `Field Rotation` (-180 to 180, default 0): on the floor, where the front lies: 0 = bottom, 90 = right, 180 = top, -90 = left.
- `Shadow Hold` (0 to 60, default 5): seconds a spot keeps its state while no sensor can see it, fading out over this time. 0 = shadows count as empty.

- `Show Field` (Off / On, default Off): shows the field as the items see it: red = occupancy, green = motion, blue = held shadow. On the screen it is drawn as seen from the front.

## How it works

What the bridge sends is described in its README (Output). This patch does the following in one ISF shader (`Resource/AutumnWind.fs`):

1. **Field** (passes 1 to 4, as in Motion Bridge - Fireflies): the bridge's sign of life from the status band, occupancy and motion blurred, motion fading out over 1.5 s, occupancy smoothed over 0.15 s, held shadows.
2. **Items** (passes 5 and 6, two steps per frame): position, velocity, rotation, flip and a *stuck* value per item, in the coordinates of its plane. Each item reads the field where it is (on the screen: at its x and its depth).
   - *Wind*: wind, fall, gusts and eddies; items that leave the frame come back on the other side.
   - *Stick*: occupancy without motion raises *stuck* (`Stick Strength`); a stuck item slows down and stops turning. When the object goes, it lets go within about a second.
   - *Stir*: motion gives a kick in a random direction (upwards on the screen), makes the items spin faster and tears stuck items off (`Stir Strength`).
3. **Picture** (pass 7): every item as a drawn shape (leaf with vein and stalk, maple seed with wing, chestnut, twig with a side branch, feather), turned and flipped, the back darker; near items over far ones; `Depth Haze` by depth.
   - The output is premultiplied (colour × alpha), so the patch needs no Premultiply Alpha node; Arena's *Alpha* blend adds it over what is below.

## Any surface

- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched. Sizes and speeds are in output heights × `Scale`.
- `Scale`: 1 for any single screen; ⅓ for three stacked to keep the size of the items.
- The densities grow with the surface: a wider surface gets more items (up to `Performance Cap`).
- Exceptions: the field is a zone that grows with the frame (docs/PATCH_RULES.md).
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. Here the clip must keep the texture's own size and the layer stretches it (Installation).

Tested sizes:

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok (Arena, live sensor, moving simulated object) |
| 5760×1080 | 3 side by side | untested |
| 1920×3240 | 3 stacked | untested |
| 1080×1920 | 9:16 | untested |
| 4096×512 | 8:1 | untested |
| 512×512 | 1:1 | untested |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Amotion_bridge_-_autumn_wind).

## Changelog

- 2026-11-21: First version (#70): leaves, maple seeds, chestnuts, twigs and feathers in the wind, on the screen with depth or on the floor (`Item Plane`); they stick to what stands still (`Stick Strength`) and are stirred up by motion (`Stir Strength`); on the screen the field is read as seen from the front. Tested in Arena (1920×1080):
  - Floor with the field on the floor, with a simulated object walking between two points (bridge `--sim-path`, 30 s pauses): a pile gathers at the standing object within about 10 s and scatters when it walks off.
  - Screen with the field on the screen: items of the object's depth stop at its column on the windward side and come loose when it walks off.
  - Live sensor: someone walking through the field scatters the leaves.
  - Every slider at its extremes (all densities, sizes and the cap at maximum; all zero; `Item Size` 0; `Scale` 0.3 and 5); bridge stopped (the items drift on).
  - Not tested: other sizes.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
