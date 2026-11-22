# Motion Bridge - Fireflies

Motion Bridge - Fireflies is an effect on the [Motion Bridge](../../tools/Motion%20Bridge/) field: fireflies fly about over a transparent background, flat over the screen or over the floor in perspective, blink on and off, swarm around whatever stands still in the room and flee from motion. Fleeing always wins.

**It needs the Motion Bridge.** The patch reacts only to the bridge's Spout texture `Motion Bridge`: the bridge with a 2D lidar, set up for the room, must run on the same computer as Arena. Without it the fireflies just fly about.

Last updated: 2026-11-22

## Vision

A summer night. Small fireflies drift over the whole surface, light up for a few seconds, go out and light up again somewhere else. When they come near someone standing still, they begin to circle around them, and a swarm gathers. Movement scares them: they keep away from it, and when they could circle someone or flee from someone moving, they always flee.

`Reverse` turns it around: they circle movement and flee from whoever stands still. Automated in Arena, the room changes its mood every few seconds.

## Requirements

- Resolume Wire / Arena: 7.29.0
- Inputs: the clip below (`Texture In`), which must be the Spout source `Motion Bridge` (see Installation)
- Tool: [Motion Bridge](../../tools/Motion%20Bridge/), Output "Spout texture" (R occupancy, G motion, B visibility, status band)
- Performance: one ISF shader with seven passes: a tiny one for time and the bridge's sign of life, three at 160 × 120 for the field, two simulation steps for up to 1024 fireflies, and the picture, which looks at every firefly for every pixel. At 1920 × 1080 on an RTX 5080 Laptop it adds about 5 % GPU load at the default density, about 50 % with 1024 fireflies.
- `Performance Cap` (default 512): at most this many fireflies. On very large surfaces it can limit the density.

## Files

| File | Purpose |
|---|---|
| `Motion Bridge - Fireflies.wire` | The patch |
| `Resource/Fireflies.fs` | ISF shader: the bridge's sign of life, the field (blur, memory, held shadows), the firefly simulation and the picture |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. Start the [Motion Bridge](../../tools/Motion%20Bridge/) and set up the room there.
3. In Arena, load the Spout source `Motion Bridge` as a clip. It loads at the texture's own size; keep it.
4. Set the layer's *Auto Size* to *Stretch* and its blend mode to *Alpha*.
5. Drag this patch onto the clip, after its *Transform*.
6. Choose the setup like in [Motion Bridge - Fabric Wind](../Motion%20Bridge%20-%20Fabric%20Wind/): with `Show Field` *On*, move the field with `Field Plane`, `Field X`, `Field Y`, `Field Width`, `Field Height` and `Field Rotation` until it lies where the sensed area is seen; the white edge is the base line.

## Timing

No `Clock`: the fireflies are a simulation in real time (the computer's clock), like Star Globe. Their speed follows `Wander`, `Swarm Strength` and `Flee Strength`.

The reaction to the room runs in real time too: occupancy is smoothed over 0.15 s, motion fades out over 1.5 s, held shadows over `Shadow Hold`. When the bridge's status band stops changing for more than 0.25 s, the field's influence fades out within a second and the fireflies just fly about; no black frame, nothing frozen.

**Swapping every 15 s:** give `Reverse` the phase source *Timeline*, *Duration* 30 s, *Play Mode* *Loop*. The ramp passes 0.5 after 15 s (reversed) and jumps back at 30 s (normal). *Bounce* swaps back and forth, BPM Sync follows the beat.

## Parameters

All ranges are deliberately wide; the extremes are safe (tested in Arena).

- `Scale` (0 to 10, default 1): size of all elements relative to the output height, also the fireflies' speed and how far they sense.

**Fireflies**
- `Firefly Plane` (Floor / Screen, default Screen): Screen = flat over the whole frame; Floor = hovering over the floor in perspective, from the bottom edge to four times that distance (or to the top edge when the horizon lies above the frame), as round lights that get smaller with the distance and fade out towards the far end. Independent of `Field Plane`.
- `Firefly Density` (0 to 500, default 40): fireflies per output height × output height at `Scale` 1, so a wider surface gets more (16:9 on the screen: about 71). On the floor it counts per floor area (measured at the bottom edge), and the floor in view is larger: at the default horizon about 7.5 times, so up to `Performance Cap`.
- `Performance Cap` (0 to 1024, default 512): at most this many fireflies.
- `Firefly Size` (0 to 0.2, default 0.006): radius of the bright core, in output heights.
- `Glow Size` (0 to 0.5, default 0.02): radius of the glow around it.
- `Firefly Color` (default warm white), `Glow Color` (default yellow-green).
- `Firefly Twinkle` (0 to 1, default 0.4): every firefly flickers on its own while it is lit.
- `Firefly Life` (0 to 30, default 3): seconds a firefly stays lit, each a little different. 0 = they never light up.
- `Dark Time` (0 to 30, default 1.5): seconds it stays dark before it lights up again, each a little different. 0 = always lit.

**Physics**
- `Wander` (0 to 10, default 1): every firefly flies about on its own heading, which meanders, as in Star Globe.
- `Air Drag` (0 to 10, default 1).
- `Swarm Strength` (0 to 10, default 1): how strongly what stands still attracts them; they circle its soft edge, each in its own direction.
- `Flee Strength` (0 to 10, default 1): how hard motion drives them away. Near motion the attraction is switched off, so fleeing always wins.
- `Reverse` (0 to 1, default 0): below 0.5 they swarm around what stands still and flee from motion; above 0.5 they swarm around motion and flee from what stands still; a soft change around 0.5. See Timing for swapping it automatically.

**Halo**
- `Halo On` (Off / On, default Off): shows what stands still as a soft light that follows the blurred field.
- `Halo Color` (default warm white), `Halo Opacity` (0 to 1, default 0.35).

**View**
- `Horizon Y` (0 to 10, default 0.7): height of the floor's horizon above the bottom edge, in output heights, multiplied by `Scale`. Used where the field lies on the floor.

**Field**: where the bridge's field lies in the picture, as in Motion Bridge - Fabric Wind.
- `Field Plane` (Floor / Screen, default Screen): Floor = on the floor in perspective; Screen = flat on the frame.
- `Field X`, `Field Y` (-10 to 10, default 0.5): centre of the field, as a share of the frame (screen) or of the floor in view (floor).
- `Field Width`, `Field Height` (0 to 10, default 1): along the base line and into the area, as shares of the frame side.
- `Field Rotation` (-180 to 180, default 0): where the base line lies: 0 = bottom, 90 = right, 180 = top, -90 = left.
- `Shadow Hold` (0 to 60, default 5): seconds a spot keeps its state while no sensor can see it, fading out over this time. 0 = shadows count as empty.

- `Show Field` (Off / On, default Off): shows the field: red = occupancy, green = motion, blue = held shadow, white = the base line, magenta = the other edges.

## How it works

What the bridge sends is described in its README (Output). This patch does the following in one ISF shader (`Resource/Fireflies.fs`):

1. **Field** (passes 1 to 4, as in Motion Bridge - Fabric Wind): the bridge's sign of life from the status band, occupancy and motion blurred over about 6 % of the field, motion fading out over 1.5 s, occupancy smoothed over 0.15 s, held shadows.
2. **Fireflies** (passes 5 and 6, two steps per frame, each reading the buffer the other one wrote): position, velocity, heading and turning sense of every firefly, in the coordinates of its plane (on the floor: across and distance, in output heights at the bottom edge), so on the floor everything far away is smaller and slower in the picture.
   - *Wander*: each keeps its own heading, which turns at random per distance flown; the frame is the wall.
   - *Sensing*: each reads the field where it is and in 16 points around it, at 0.15 and 0.4 output heights × `Scale`. *Still* is occupancy without motion; *motion* is the motion channel. `Reverse` swaps the two.
   - *Flee*: away from motion, the closer the harder, with a higher top speed.
   - *Swarm*: towards what stands still, held on its soft edge (where the blurred occupancy is about a third) and circling it in its own direction; while swarming, a firefly follows its own heading much less.
   - *Fleeing wins*: as soon as there is motion nearby, the attraction is switched off.
3. **Picture** (pass 7): the halo from the blurred field, then every lit firefly as a bright core with a soft glow (on the floor at its place in perspective, its size divided by its distance). Each firefly has its own durations and phase for being lit and dark, with a short fade in and out, and twinkles while lit.
   - The output is premultiplied (colour × alpha), so the patch needs no Premultiply Alpha node; Arena's *Alpha* blend adds it over what is below.

## Any surface

- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched. Sizes, speeds and the sensing distance are in output heights × `Scale`.
- `Scale`: 1 for any single screen; ⅓ for three stacked to keep the size of the fireflies.
- `Firefly Density` grows with the surface: a wider surface gets more fireflies (up to `Performance Cap`).
- Exceptions: the field is a zone that grows with the frame (docs/PATCH_RULES.md); the halo's blur is in frame heights, not × `Scale`.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. Here the clip must keep the texture's own size and the layer stretches it (Installation).

Tested sizes:

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok (Arena, live sensor, simulated object) |
| 5760×1080 | 3 side by side | untested |
| 1920×3240 | 3 stacked | untested |
| 1080×1920 | 9:16 | untested |
| 4096×512 | 8:1 | untested |
| 512×512 | 1:1 | untested |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Amotion_bridge_-_fireflies).

## Changelog

- 2026-11-22: Documentation: the Motion Bridge front is now its base line (#135); the wording here and in the patch description follows. Nothing else changed.
- 2026-11-21: `Firefly Plane` (#127): the fireflies hover over the floor in perspective as well, independent of `Field Plane`, like `Ribbon Plane` in Motion Bridge - Fabric Wind. Tested in Arena (1920×1080): Floor with the field on the floor and on the screen, swarm around a standing object and `Reverse` on the floor, `Scale` 0.3 and 5, `Horizon Y` 0 and 10, the screen plane unchanged.
- 2026-11-21: First version (#127): fireflies that wander, blink (`Firefly Life`, `Dark Time`, `Firefly Twinkle`), swarm around what stands still (`Swarm Strength`) and flee from motion (`Flee Strength`), fleeing always wins; `Reverse` swaps the roles; `Halo On` shows what stands still as a soft light; field controls as in Motion Bridge - Fabric Wind. Tested in Arena (1920×1080) with the live sensor and a simulated object: swarming, `Reverse` 0 and 1, an Arena Timeline on `Reverse`, halo, screen and floor field, `Field Rotation` 90, bridge stopped and restarted, every slider at its extremes (density 500 with cap 1024, sizes 0 and maximum, `Firefly Life` 0 and 30, `Dark Time` 0 and 30, `Wander`, `Air Drag`, `Swarm Strength`, `Flee Strength` 0 and 10, `Scale` 0.3 and 5). Fleeing from someone walking through the field checked live (the area around the movement stays clear, the swarm at the standing object stays). Not tested: other sizes.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
