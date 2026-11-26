# Motion Bridge - Fabric Wind

Motion Bridge - Fabric Wind is an effect on the [Motion Bridge](../../tools/Motion%20Bridge/) field: ribbons of fabric blow in the wind, either over the floor in front of the projection or flat across the screen. They flow around whatever stands in the room and flutter where something moves. The background is transparent, so the ribbons lie over the layers below.

**It needs the Motion Bridge.** The patch reacts only to the bridge's Spout texture `Motion Bridge`: the bridge with a 2D lidar, set up for the room, must run on the same computer as Arena. Without it the ribbons blow calmly in the wind.

Formerly *Bulging Lines*; renamed because the look changed from lines to fabric and the prefix says that it needs the Motion Bridge.

Last updated: 2026-11-26

## Vision

Fabric in the wind. Long strips of silk, velvet or rags are fixed on one side and blow towards the other; waves run along them, they twist, catch the light and show their back. The room drives the wind. Someone standing parts the fabric like a post in a stream (or draws it in). Someone moving shakes it as if a gust had caught it, and it settles again.

The bridge always delivers the same field; how its sensor is set up decides what the field means. The patch therefore lets you choose separately where the ribbons blow and where the field lies, and any combination is allowed:

| Setup | Ribbon Plane | Wind Direction | Field Plane | Field Rotation |
|---|---|---|---|---|
| **Dance floor**: sensor scans the floor in front of the projection | Floor | 270 (towards the viewer) | Floor | 0 |
| **Curtain**: sensor scans a plane just in front of the projection surface, front at the top edge | Screen | 270 (from the top) | Screen | 180 |
| **Floor projection**: sensor scans just above the projected floor | Screen | any | Screen | as the base line lies in the picture; `Field Height` for true proportions (Parameters) |
| **Shadow from the room**: people on the floor, fabric falling from the top | Screen | 270 | Floor | 0 |

## Requirements

- Resolume Wire / Arena: 7.29
- Tool: the [Motion Bridge](../../tools/Motion%20Bridge/) with its sensor, running and set up for the room (area with its base line drawn, empty room learned). Its interface is described under [Output](../../tools/Motion%20Bridge/README.md#output).
- Inputs: the clip it sits on (`Texture In`), a Spout clip of `Motion Bridge`.
- Performance: one full-screen pass that evaluates seven ribbons per pixel and their shadows, three passes at half the output size for the flow around objects (only the columns of ribbons in view are computed), and three small passes at 160 × 120. At 1920 × 1080 on an RTX 5080 Laptop it adds about 10 % GPU load.

## Files

| File | Purpose |
|---|---|
| `Motion Bridge - Fabric Wind.wire` | The patch |
| `Resource/FabricWind.fs` | ISF shader: sign of life of the bridge, spread field, excitation and held shadows, the ribbons and their fabric |

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. Start the Motion Bridge and set it up (its README).
3. In Arena (bridge README, Usage, "In Arena"):
   1. Drag the Spout source `Motion Bridge` into a clip slot (Sources > Spout). The clip must have the texture's own size (*Width* / *Height*, e.g. 19 × 48); a fresh Spout clip takes it by itself.
   2. Set the layer's *Auto Size* (layer panel, Video) to *Stretch*.
   3. Drag `Motion Bridge - Fabric Wind` onto the clip (a clip effect, after the clip's Transform).
   4. Set the layer's blend mode to *Alpha*, so the ribbons lie over the layers below.
   5. Set the phase source of `Clock` to *Timeline*, *Duration* 1000 s (see Timing).
   6. Choose the setup (table under Vision). With `Show Field` *On*, move the field with `Field X`, `Field Y`, `Field Width`, `Field Height` and `Field Rotation` until it lies where the sensed area is seen; the white edge is the base line.
   7. More looks: duplicate the clip into other columns and change its settings. Each clip is an ordinary clip.

If anything looks wrong, check the field with a Spout clip carrying the example [Field Viewer](../../examples/Field%20Viewer/).

## Timing

`Clock` (0 to 1000, default 0) drives the wind: the waves run along the ribbons and the gusts move with it. If the clock stops, the fabric stands still. Start with phase source *Timeline*, *Duration* 1000 s, *In* 0, *Out* 1000. BPM Sync works as well.

The reaction to the room runs in real time and does not follow `Clock`: occupancy is smoothed over 0.15 s, the flutter fades over `Flutter Life`, held shadows over `Shadow Hold`. The bridge sends 30 frames per second. When its status band stops changing for more than 0.25 s, the field's influence fades out within a second and the ribbons blow calmly; no black frame, nothing frozen.

## Parameters

All ranges are deliberately wide; the extremes are safe (tested in Arena). `Show Field` and the group Field come first, because they are set first when the patch is set up for a room.

- `Scale` (0 to 10, default 1): size of all elements relative to the output height.
- `Clock` (0 to 1000, default 0): the wind's waves, see Timing.
- `Show Field` (Off / On, default Off): shows where the field lies and what of it is evaluated. Light grey = evaluated: a sensor sees it now, the ribbons react there; on it red = occupancy, green = excitation. Hatched = not evaluated, nothing excites the ribbons there: outside the field, outside the area and in a shadow (the bridge does not tell the two apart); what is held in a shadow still shows in red and green. Without a sensor (no visibility) the whole field is light grey; without the bridge everything is hatched, because nothing is evaluated. White = the base line, magenta = the other edges.

**Field**: where the bridge's field lies in the picture.
- `Field Plane` (Floor / Screen, default Floor): Floor = on the floor in perspective; Screen = flat on the frame.
- `Field X`, `Field Y` (-10 to 10, default 0.5): centre of the field, as a share of the reference area: on the screen the frame (0 = left / bottom edge, 1 = right / top edge); on the floor its width at the bottom edge and the depth from the bottom edge to four times that distance.
- `Field Width` (0 to 10, default 1): along the base line, as a share of the frame side the base line lies on.
- `Field Height` (0 to 10, default 1): into the area, as a share of the frame side the depth runs along. For true proportions (one metre the same both ways, e.g. a floor projection): `Field Height` = `Field Width` × field depth / field width (the bridge status shows both in metres) × frame width / frame height; with the field turned by 90 or -90 degrees, swap the frame width and height.
- `Field Size` (0 to 10, default 1): scales the field as it is set, around its centre (`Field X`, `Field Y`); proportions and rotation stay. It multiplies `Field Width` and `Field Height`.
- `Field Rotation` (-180 to 180, default 0): where the base line lies: 0 = bottom, 90 = right, 180 = top, -90 = left. Turns counter-clockwise.

**Fabric**
- `Fabric` (default Silk):
  - *Rags*: matte, uneven colour, holes, frayed edges and ends; light.
  - *Silk*: smooth, a sharp highlight runs along the folds; light and flowing.
  - *Velvet*: deep colour, rounded heavy ribbons with sheen at the edges; heavy, long slow folds.
  - *Chiffon*: sheer, denser where it is seen at a grazing angle; very light.
  - *Linen*: matte with a visible weave (fades out where it would be finer than a pixel); a little stiff.
  - *Lamé*: metallic highlight and glittering threads.
- `Fabric Color` (default deep red).
- `Fabric Sheen` (0 to 10, default 1): strength of highlight, sheen and glitter.
- `Fabric Weight` (0 to 10, default 1): multiplies the fabric's own weight. Heavier = longer, slower waves.
- `Tatter` (0 to 1, default 0): holes, frayed edges and ragged ends on top of the fabric's own (Rags has some already).
- `Ribbon Plane` (Floor / Screen, default Floor): Floor = the ribbons lie on the floor in perspective, up to the horizon; Screen = flat across the frame.
- `Ribbon Density` (0 to 200, default 5): ribbons per output height (on the floor at the bottom edge), at `Scale` 1. 0 = none. High values give narrow bands.
- `Ribbon Width` (0 to 0.5, default 0.22): width of a ribbon in output heights. Wider than the spacing (default slightly), the ribbons overlap and cast shadows on each other.
- `Ribbon Length` (0 to 100, default 100): length of a ribbon from where it is fixed, in output heights. 100 fills any frame; short ribbons end in a free, fluttering end.
- `Ribbons Opacity` (0 to 1, default 1).
- `Light Source` (0 to 360, default 135): where the light comes from; 0 = right, 90 = top, 135 = top left.

**View**
- `Horizon Y` (0 to 10, default 0.7): height of the floor's horizon above the bottom edge, in output heights, multiplied by `Scale`. Used where a plane is Floor.

**Physics**
- `Wind Direction` (0 to 360, default 270): where the ribbons blow to. The ribbons are fixed on the opposite side. On the screen 270 = from the top down; on the floor 270 = towards the viewer, 90 = into the depth.
- `Wind Strength` (0 to 10, default 1): size and speed of the waves. 0 = still.
- `Avoid Strength` (-10 to 10, default 1): how the ribbons react to what stands there. 1 = they part around it like a stream around a post, so that their edges just clear it; above 1 they behave as if it were that many times wider (with a big object and 10 the ribbons leave most of the frame). Negative = they are drawn towards it. 0 = no reaction.
- `Flutter Strength` (0 to 10, default 1): how strongly the ribbons flutter and twist where something moves.
- `Flutter Life` (0 to 30, default 1.5): seconds until the flutter has calmed down.
- `Shadow Hold` (0 to 60, default 5): seconds a spot keeps its state while no sensor can see it (behind a person, a barrier, outside the fans), fading out over this time. 0 = shadows count as empty.

## How it works

What the bridge sends is described in its README (Output; "From the room to the picture" under Usage). This patch takes the field and does the following, in one ISF shader with eight passes (`Resource/FabricWind.fs`):

1. **Sign of life** (pass 1, 2 × 1, kept from frame to frame): the patch reads the status band at the top of the texture at three points across the width. It counts as live when R = G = B > 0 there and the value changes. The time since the last change decides how much the field may act: full up to 0.25 s, nothing after 1 s. The clock of the computer (`System Time`) gives the frame time. It also checks 64 points of the visibility channel (B): only when some part of the field is seen does visibility count; otherwise (simulated objects only, no sensor) it is ignored.
2. **Spread** (passes 2 and 3, 160 × 120): occupancy (R) and motion (G) are spread over about 6 % of the field in x and in depth with a soft maximum, so single cells and sensor noise do not make the ribbons jitter and a person, only a thin arc to the sensor, keeps its full strength. Visibility goes along unspread.
3. **Memory** (pass 4, 160 × 120, kept from frame to frame):
   - *Excitation*: spread motion × 2.5, at most 1, falling off over `Flutter Life`.
   - *Occupancy*: follows the spread occupancy × 1.5 (at most 1) with a delay of 0.15 s.
   - *Shadow*: where no sensor sees the cell, there is no new motion and the occupancy fades out over `Shadow Hold` instead of following the bridge's 0 ("unknown").
   - Without the bridge both fall to 0.
4. **Flow around** (passes 5 to 7, half the output size; one column per ribbon, rows along the wind):
   - *Width* (pass 5): in each row the occupancy is read across the ribbon (9 points over about one output height). Faint traces are ignored; occupancy at both ends of that window (wider than a post, e.g. a wall or a crowd) does not count. This gives the object's centre and half width R, plus half a ribbon so that the ribbon's edge clears the object, times `Avoid Strength`.
   - *Cone* (pass 6): R spreads along the ribbon as a cone: before the object over about 1 × R (where the ribbon starts to bend), behind it over about 1.4 × R (the wake). Each row reads the raw width of up to 128 rows up- and downwind directly, so the cone is exact, has no memory and goes as soon as the object goes. Because `Avoid Strength` scales R before this, the cone grows with it and the ribbons keep their angle.
   - *Offset* (pass 7): like a streamline around a cylinder, a ribbon at the distance y from the centre moves to √(y² + R²). The ribbons part around the object and crowd at its sides; far away they hardly move. Each ribbon passes on one side for its whole length: the side it is on where the object is widest. Negative `Avoid Strength` draws the ribbons in instead. The offset follows with a delay of 0.15 s, so the fabric swings over instead of jumping. Motion where the ribbon now is gives its excitation.
   - Wire reads these float textures unfiltered; the field and the tables are interpolated by hand. Rows and columns are addressed only through texture coordinates (in Wire, `gl_FragCoord.y` runs the other way).
5. **Picture** (pass 8, output size):
   - *Planes*: a pixel is placed in the ribbon plane and, independently, in the field plane. Floor: the bottom edge is near, the floor recedes to the horizon at `Horizon Y`; Screen: the frame itself. The field is a rectangle in its plane (Field controls); at rest it fills the floor from the bottom edge to four times that distance, or the whole frame, with the base line at the bottom.
   - *Ribbons*: strips along `Wind Direction`, `Ribbon Density` per output height, fixed at the most upwind point of the visible plane. Per pixel a binary search over the moved ribbons finds the one that lands there; it and three on each side are evaluated, so they can overlap and crowd beside an object; they are drawn back to front in a random order.
   - *Surface*: each ribbon is a height field. Two to three folds run along it (depending on the fabric), unevenly spaced; they wander and fan out towards the free end, with soft creases between them (rounded ridges from a soft absolute value of a sine). The edges roll a little and wave, so the outline is not a straight line. Normals come from the slope of the height field.
   - *Wind*: waves travel along each ribbon with `Clock`; they ripple out of the plane (the light changes), the ribbon twists (it gets narrower and shows its back) and sways sideways. A slow noise adds gusts. The waves grow towards the free end. Heavy fabric makes long, slow waves.
   - *Flow around*: each ribbon moves sideways as a whole strip by its offset from pass 7, smoothed along its length. Its width and folds stay as they are, so the fabric bends around a person in a smooth arc instead of the picture being warped like through a lens. Nothing is cut out where someone stands.
   - *Flutter*: where excitation is high along a ribbon's centre line, the whole ribbon swings sideways in fast waves, ripples and twists, × `Flutter Strength`.
   - *Light*: physically based, in linear colour, then tone mapped like film (ACES), so highlights roll off softly instead of clipping. Silk and Lamé use anisotropic GGX with Fresnel (Lamé as a metal, coloured highlights); Rags, Velvet, Chiffon and Linen use the cloth model (Charlie sheen, wrapped diffuse with light scattered inside the threads). Formulas after Filament (Sources). Three lights: the key light from `Light Source`, a sky of ambient light that is darker in creases, and a light behind the fabric that shines through thin fabric. Glossy fabrics also reflect the key light as a large soft box. Small wrinkles sit on the folds; Rags, Chiffon and Linen have a weave where its threads are larger than a pixel; finer detail (threads, folds of a strip seen edge-on or far away) turns into roughness instead of flickering.
   - *Shadows*: a ribbon in front darkens the ribbons behind it, with a soft edge, shifted away from `Light Source`; right next to it, less ambient light reaches the ribbon behind (ambient occlusion).
   - Edges are smoothed by the size of a pixel on the plane; ribbons fade towards the horizon and where they would get finer than a few pixels.
   - The background stays transparent. The band and the raw field are never shown.

## Sources

How the fabric look is built follows what fabric shaders do elsewhere:
- [Drapery shader](https://www.shadcn.io/shaders/drapery-shader): folds as rounded ridges with creases, Ward highlight along the threads, Charlie sheen, darker valleys.
- [3D cloth material](https://www.ui-skills.com/skills/mengto/3d-cloth-material): fold geometry, sheen and translucency values per fabric, rolled hems.
- [Filament](https://google.github.io/filament/main/filament.html), Google's physically based renderer, sections 4.10 (anisotropic GGX) and 4.12 (cloth): the light model.
- Sadeghi et al., [A Practical Microcylinder Appearance Model for Cloth Rendering](https://escholarship.org/uc/item/6v11p5b0) (SIGGRAPH 2013): what dominates the look of silk, velvet and linen (diffuse, anisotropic highlight, shadowing, grazing sheen).

## Any surface

- Follows the surface model: height is the reference, width shows more or less, nothing is stretched. Ribbon spacing, width, length and wave length are in output heights, multiplied by `Scale`.
- `Scale`: 1 for any single screen; ⅓ for three stacked to keep the ribbon size and put the horizon low.
- Exception: the field is a zone that grows with the frame (docs/PATCH_RULES.md): at rest it spans the whole width, because the patch stretches the bridge texture, base line along the bottom, over the whole width. `Field Width` and `Field Height` are shares of the frame.
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. Here the clip must keep the texture's own size and the layer stretches it (Installation).

Tested sizes:

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | ok (Arena, live sensor) |
| 5760×1080 | 3 side by side | untested |
| 1920×3240 | 3 stacked | untested |
| 1080×1920 | 9:16 | untested |
| 4096×512 | 8:1 | untested |
| 512×512 | 1:1 | untested |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3Amotion_bridge_-_fabric_wind).

## Changelog

- 2026-11-26: The field is spread with a soft maximum instead of an average, as in Motion Bridge - Autumn Wind: the sensor sees a person only as a thin arc, which the average thinned out below the thresholds, so live the fabric barely parted around someone standing. Controls unchanged. Tested in Arena with the live sensor (the fabric parts around someone standing and flutters at motion) and a simulated object, at 1920×1080, 5760×1080, 1920×3240, 1080×1920, 4096×512 and 512×512.
- 2026-11-22: `Show Field` without the bridge hatches everything (before: the whole field light grey, as if it were evaluated). Tested in Arena (1920×1080): bridge stopped, all hatched; bridge started, the seen area light grey again.
- 2026-11-22: `Field Size` in the group Field scales the field as it is set, proportions and rotation kept (built in Wire: it multiplies `Field Width` and `Field Height`; the shader is unchanged). Tested in Arena (1920×1080) with the live sensor: 0.5 halves the field around its centre with rotation and proportions kept, 0 leaves no field (all hatched, ribbons calm, no NaN), 10 covers the frame; the patch loads in Wire.
- 2026-11-22: `Show Field` shows what is evaluated: the part a sensor sees is light grey, everything else (outside the field, outside the area, in a shadow) is hatched. Held shadows are no longer shown in blue. Tested in Arena (1920×1080) with the live sensor and a simulated object: Field Plane Screen and Floor, ribbons on and off, field smaller than the frame, `Show Field` Off unchanged, bridge stopped (whole field light grey, outside hatched, no NaN) and restarted.
- 2026-11-22: `Show Field` is the third control after `Scale` and `Clock`, the group Field follows it (agreed for all Motion Bridge patches). `Field Height` documents how to get true proportions. Tested in Arena (1920×1080) with the bridge after #135 (texture 38 × 64, area 3.80 × 6.00 m) in the floor projection setup (`Ribbon Plane` and `Field Plane` Screen): order of the controls in the clip panel; field, motion and held shadow arrive; the ribbons part around a standing simulated object and flutter where it walks; with `Field Width` 0.5 and `Field Height` 1.40 (formula) the standing object is round, with both at 1 it was about three times wider than high.
- 2026-11-22: Documentation: the Motion Bridge front is now its base line (#135); the wording here and in the patch description follows. Nothing else changed.
- 2026-11-20: Renamed from Bulging Lines to Motion Bridge - Fabric Wind (#124, #120). Ribbons are now fabric strips fixed on the upwind side: waves, twist, gusts, six fabrics with their own light (`Fabric`, `Fabric Color`, `Fabric Sheen`, `Fabric Weight`, `Tatter`, `Light Source`), `Ribbon Length`. `Ribbon Plane` and `Wind Direction` choose where the ribbons blow; `Field Plane`, `Field X`, `Field Y`, `Field Width`, `Field Height`, `Field Rotation` choose where the field lies (floor, curtain, floor projection). `Avoid Strength` may be negative. The visibility channel holds shadows (`Shadow Hold`). The flight of segments is gone; `Clock` now drives the wind. After a first round that still looked like coloured lines, the ribbons were rebuilt after research into fabric shaders (Sources): fewer, wider ribbons (`Ribbon Density` 5, `Ribbon Width` 0.22), folds along each ribbon with creases, rolled wavy edges, shadows between ribbons. A third round lifted the light to current game standards: physically based shading in linear colour with ACES tone mapping, anisotropic GGX for silk and lamé, the Filament cloth model for the others, sky light, light through thin fabric, ambient occlusion, wrinkles, weave, and specular anti-aliasing. A fourth round replaced the avoidance, which warped the picture like a glass ball: each ribbon now moves as a whole strip around what stands there, with a smooth arc and a wake; flutter is read along the ribbon too. Tested in Arena (1920×1080) with the live sensor: both planes, field rotation, all fabrics, slider extremes. Not tested live: flutter from someone moving. A fifth round: someone standing still parts the fabric like a post in a stream (passes 5 to 7, see How it works); the hole that used to be cut where someone stands is gone; the groups Ribbons and Fabric are one group Fabric. Tested in Arena (1920×1080) with the live sensor and a simulated object, dragged and with the field moving: screen and floor plane, ribbons on the screen with the field on the floor, `Avoid Strength` -10, -1, 0, 1, 5, 10, `Scale` 0.3, 1, 1.6, 5, `Ribbon Density` 5, 50, 200, `Wind Direction` 0, 135, 270, `Field Rotation` 90, bridge stopped and restarted. GPU load at 1920×1080 on an RTX 5080 Laptop: about 17 % without the effect, 26 to 30 % with it.
- 2026-11-17: Rebuilt as an effect on the Spout clip of the Motion Bridge field texture (#115), moved from `sources/` to `effects/`: ribbons fly towards the viewer over the floor in perspective, flow around occupancy and flutter where something moves; transparent background, no Backdrop. Replaces the OSC version with its 8 people slots and bulges. Tested in Arena (1920×1080) with the live sensor and simulated objects, slider extremes and bridge stop.
- 2026-11-16: Marked as not working: the Motion Bridge changed its interface from OSC to a Spout texture (#115); rebuild pending.
- 2026-11-14: First version (#73, PoC for #69): lines bulge per tracked person (8 slots), depth from the surface weakens the bulge, heartbeat fade-out without the bridge, `Show People` for setting up.

---

Author: Stefan Mazur, https://github.com/S-Mazu/WirePatches
