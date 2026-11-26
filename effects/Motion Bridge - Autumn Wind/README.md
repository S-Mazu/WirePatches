# Motion Bridge - Autumn Wind

Motion Bridge - Autumn Wind is an effect on the [Motion Bridge](../../tools/Motion%20Bridge/) field: [Autumn Wind](../Autumn%20Wind/) over a transparent background, the same 16 kinds of items in the same space in front of the viewer. Motion in the field stirs the items up; where something stands still, they slow down and gather.

**It needs the Motion Bridge.** The patch reacts only to the bridge's Spout texture `Motion Bridge`: the bridge with a 2D lidar, set up for the room, must run on the same computer as Arena. Without it the items just drift, exactly as in Autumn Wind.

Last updated: 2026-11-26

## Vision

Autumn in the room. The wind carries leaves, hats, rain or confetti through the picture; whoever stands still collects them, and a cluster grows where they stand. When they move, the cluster bursts and the items whirl off again.

The bridge always delivers the same field; where it lies in the picture is a free choice, as in [Motion Bridge - Fabric Wind](../Motion%20Bridge%20-%20Fabric%20Wind/) and [Motion Bridge - Fireflies](../Motion%20Bridge%20-%20Fireflies/). Every item reads the field where it is seen in the picture, so any combination works:

| Setup | Field Plane | Field Rotation | Example |
|---|---|---|---|
| **Curtain**: sensor scans a plane just in front of the projection surface, base line at the top edge | Screen | 180 | rain that someone reaching through the curtain whirls up |
| **Street scene**: sensor scans the floor in front of the projection | Floor | 0 | leaves on a street that gather where someone stands |
| **Floor projection**: sensor scans just above the projected floor | Screen | as the base line lies in the picture; `Field Height` for true proportions | confetti on the dance floor |

## Requirements

- Resolume Wire / Arena: 7.29.0
- Inputs: the clip below (`Texture In`), which must be the Spout source `Motion Bridge` (see Installation)
- Tool: [Motion Bridge](../../tools/Motion%20Bridge/), Output "Spout texture" (R occupancy, G motion, B visibility, status band)
- Performance: the items of Autumn Wind (native Wire nodes, up to 1024) plus one small ISF shader that prepares the field (three passes at 160 × 120) and places it in the picture, at a quarter of the output size for the items and at full size for `Show Field`.
- `Performance Cap` (default 1024): on very large surfaces or at a small `Scale` it can limit the density.

## Files

| File | Purpose |
|---|---|
| `Motion Bridge - Autumn Wind.wire` | The patch |
| `Resource/MotionField.fs` | ISF shader: the bridge's sign of life, the field (blur, memory of motion, held shadows), the field placed in the picture; output Map (what an item reads) or View (`Show Field`) |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. Start the [Motion Bridge](../../tools/Motion%20Bridge/) and set up the room there.
3. In Arena, load the Spout source `Motion Bridge` as a clip. It loads at the texture's own size; keep it.
4. Set the layer's *Auto Size* to *Stretch* and its blend mode to *Alpha*.
5. Drag this patch onto the clip, after its *Transform*.
6. Choose the setup (Vision): with `Show Field` *On*, move the field with `Field Plane`, `Field X`, `Field Y`, `Field Width`, `Field Height`, `Field Size` and `Field Rotation` until it lies where the sensed area is seen; the white edge is the base line.

## Timing

No `Clock`: the items move in real time, as in Autumn Wind. The reaction to the room runs in real time too: occupancy is smoothed over 0.15 s, motion fades out over 1.5 s, held shadows over `Shadow Hold`. When the bridge's status band stops changing for more than 0.25 s, the field's influence fades out within a second: stuck items let go, the items drift on. No black frame, nothing frozen.

## Parameters

All ranges are deliberately wide; the extremes are safe (tested in Arena). `Show Field` and the group Field come first, because they are set first when the patch is set up for a room (docs/PATCH_RULES.md).

- `Scale` (0 to 10, default 1): size of everything relative to the output height.
- `Show Field` (Off / On, default Off): shows where the field lies and what of it is evaluated. Light grey = evaluated: a sensor sees it now, the items react there; on it red = occupancy, green = motion. Hatched = not evaluated: outside the field, outside the area and in a shadow; what is held in a shadow still shows in red and green. Without the bridge everything is hatched. White = the base line, magenta = the other edges. The items stay visible over it.

**Field**: where the bridge's field lies in the picture, as in Motion Bridge - Fabric Wind and Fireflies.
- `Field Plane` (Floor / Screen, default Screen): Floor = on the floor in perspective, up to the horizon at `Horizon Y`; Screen = flat on the frame.
- `Field X`, `Field Y` (-10 to 10, default 0.5): centre of the field, as a share of the frame (screen) or of the floor in view (floor: its width at the bottom edge and the depth from the bottom edge to four times that distance).
- `Field Width`, `Field Height` (0 to 10, default 1): along the base line and into the area, as shares of the frame side. For true proportions (one metre the same both ways): `Field Height` = `Field Width` × field depth / field width (the bridge status shows both in metres) × frame width / frame height; with the field turned by 90 or -90 degrees, swap the frame width and height.
- `Field Size` (0 to 10, default 1): scales the field as it is set, around its centre; proportions and rotation stay. It multiplies `Field Width` and `Field Height`.
- `Field Rotation` (-180 to 180, default 0): where the base line lies: 0 = bottom, 90 = right, 180 = top, -90 = left.
- `Shadow Hold` (0 to 60, default 5): seconds a spot keeps its state while no sensor can see it, fading out over this time. 0 = shadows count as empty.

**Physics**: as in [Autumn Wind](../Autumn%20Wind/) (`Wind Strength`, `Wind Direction`, `Wind Direction Z`, `Fall Strength`, `Fall Direction`, `Gust Sway`, `Tumble`, `Speed Variation`, `Eddies`, `Eddy Size`), plus:
- `Stir Strength` (0 to 10, default 1): how hard motion kicks the items (mostly upwards in the picture), makes them spin faster and tears stuck items off. 0 = motion has no effect.
- `Stick Strength` (0 to 10, default 1): how fast items get stuck where something stands still: they slow down until they stop and stop turning; they let go within about a second when it has gone. 0 = they drift through.

**Items**, **Autumn**, **Street**, **Party**, **Sky**: as in [Autumn Wind](../Autumn%20Wind/) (`Performance Cap`, `Item Size`, `Depth Spread`, `Depth Haze`, `Vanishing Point X`, `Vanishing Point Y`, `Hue Variation`; Density and Color for all 16 kinds, `Star Twinkle`).

**View**
- `Horizon Y` (0 to 10, default 0.7): height of the floor's horizon above the bottom edge, in output heights, multiplied by `Scale`. Used where `Field Plane` is Floor; independent of the vanishing point of the items.

## How it works

1. **Field** (`Resource/MotionField.fs`, passes as in Motion Bridge - Fabric Wind and Fireflies): the bridge's sign of life from the status band, occupancy and motion blurred, motion fading out over 1.5 s, occupancy smoothed over 0.15 s, held shadows. The last pass places the field in the picture (`Field Plane` and the Field controls). Two nodes use the shader: Map (a quarter of the output size: motion, occupancy, seen) for the items, View for `Show Field`.
2. **Items**: the whole of Autumn Wind (space, wind, gusts, eddies, re-entry, glyphs). Each item samples the Map where it is seen in the picture. Occupancy without motion raises its *stuck* value (`Stick Strength`), motion lowers it (`Stir Strength`) and gives a kick that fades out over about half a second; the item's velocity is multiplied by 1 − stuck² and the kick is added. Wire inserts a one-frame delay where the velocity feeds back through the field (the five Delay nodes after "Velocity With Field").
3. **Picture**: the items over a transparent background, `Show Field` over them when it is on.

## Any surface

- Follows the surface model exactly as Autumn Wind: height is the reference, width shows more or less scene, nothing is stretched.
- `Scale`: 1 for any single screen; ⅓ for three stacked to keep the size of the items.
- Exceptions: the field is a zone that grows with the frame (docs/PATCH_RULES.md).
- Notes: here the clip must keep the texture's own size and the layer stretches it (Installation); the patch then renders at the composition size.

Tested sizes:

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok |
| 5760×1080 | 3 side by side | ok |
| 1920×3240 | 3 stacked | ok |
| 1080×1920 | 9:16 | ok |
| 4096×512 | 8:1 | ok |
| 512×512 | 1:1 | ok |

Tested in Arena at each composition size, with the Spout clip reloaded and stretched by the layer, `Show Field` on and the bridge's simulated object walking and standing: the field covers the frame as placed, motion and occupancy arrive, the items gather at the standing object. Do not change the size of the Spout clip itself: the clip then shows the small texture unstretched inside a larger canvas and the patch loses the field (everything hatched).

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Amotion_bridge_-_autumn_wind).

## Changelog

- 2026-11-26: Rebuilt on the new [Autumn Wind](../Autumn%20Wind/) (#142), so both look and behave the same: the 16 kinds of items in the real space, `Wind Direction Z`, vanishing point, re-entry where the wind brings items in (fixes the poor spawning at `Wind Direction` 270, #134), `Hue Variation`, Oak Leaf. The field is placed freely in the picture as in Motion Bridge - Fabric Wind and Fireflies (`Field Plane` Floor / Screen); every item reads it where it is seen. The former Screen reading (distance from the surface = item depth) and `Item Plane` are gone; `Color Variation`, `Variation Color` and Chestnut are replaced as in Autumn Wind. The old shader `Resource/AutumnWind.fs` is replaced by `Resource/MotionField.fs` (field only). Tested in Arena (1920×1080) with the bridge's simulated object (walking, and standing for 30 s) and the live sensor: leaves gather at the standing object and let go when it walks off, motion kicks them up; `Show Field` Off and On, `Field Plane` Floor and Screen, `Horizon Y` 0.3, `Field Rotation` 90 and 180, `Field Size` 0.5; every field control, `Stir Strength`, `Stick Strength`, `Shadow Hold` and `Horizon Y` at both extremes; bridge stopped (everything hatched, items drift on, no NaN) and restarted (field back within seconds); all six sizes (Any surface). Patch loads in Wire.
- 2026-11-22: `Scale` follows the surface model with the scene coordinates of the example Scaler, as in Rolling Landscape: one transform, everything in scene units; before, single quantities were multiplied by `Scale` by hand (#133). The floor plane is measured in scene units both ways, so at the default horizon about a third fewer items lie on the floor. `Show Field` is the second control after `Scale`, the group Field follows it; `Show Field` shows what is evaluated (light grey) and hatches the rest (all of it without the bridge); `Field Size` scales the field as set; `Field Height` documents true proportions on the floor. Controls and their ranges are unchanged. Tested in Arena (1920×1080) with the live sensor and a simulated object, next to the previous version: at `Scale` 1 items of the same size and number on the screen; `Scale` 0.33 (more, smaller) and 3 (few, big); floor plane; `Show Field` with the field on the screen and on the floor, `Field Size` 0.5; bridge stopped (all hatched, no NaN) and restarted (an item sticks to the standing object). Patch loads in Wire.
- 2026-11-22: Documentation: the Motion Bridge front is now its base line (#135); the wording here and in the patch description follows. Nothing else changed.
- 2026-11-21: First version (#70): leaves, maple seeds, chestnuts, twigs and feathers in the wind, on the screen with depth or on the floor (`Item Plane`); they stick to what stands still (`Stick Strength`) and are stirred up by motion (`Stir Strength`); on the screen the field is read as seen from the front. Tested in Arena (1920×1080):
  - Floor with the field on the floor, with a simulated object walking between two points (bridge `--sim-path`, 30 s pauses): a pile gathers at the standing object within about 10 s and scatters when it walks off.
  - Screen with the field on the screen: items of the object's depth stop at its column on the windward side and come loose when it walks off.
  - Live sensor: someone walking through the field scatters the leaves.
  - Every slider at its extremes (all densities, sizes and the cap at maximum; all zero; `Item Size` 0; `Scale` 0.3 and 5); bridge stopped (the items drift on).
  - Not tested: other sizes.
---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
