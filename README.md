# WirePatches

A collection of [Resolume Wire](https://resolume.com/software/wire) patches for Resolume Arena/Avenue, mostly built with Claude via MCP.

## Patches

| Patch | Type | Description |
|---|---|---|
| [Autumn Wind](Autumn%20Wind/) | source | 15 kinds of items (leaves, feathers, paper planes, …) drift across the frame with depth, gusts and tumble. |
| [Bass Strings](Bass%20Strings/) | source | Four double bass strings vibrate with the music, one frequency band per string. |
| [Deco Marquee v2](Deco%20Marquee%20v2/) | source | Art Deco theatre portal: fans of rays and rings, chasing marquee bulbs, ornament border. |
| [Fire and Smoke](Fire%20and%20Smoke/) | effect | Flames and smoke from a fluid simulation; burning edges, a fire point, or the clip as burn mask. |
| [Flower Sax](Flower%20Sax/) | source | A Len Lye–style instrument of your choice blows seven kinds of flowers into the room. |
| [Geo Tiles](Geo%20Tiles/) | source | Tile collage of 67 flat geometric motifs in three colors. |
| [Iris Diaphragm](Iris%20Diaphragm/) | source | Mechanical blade iris that closes over the layers below. |
| [Lye Swing](Lye%20Swing/) | source | Painted big-band score after Len Lye; one shape per frequency band. |
| [Pride Flag](Pride%20Flag/) | source | A flag moves lightly in the wind: Rainbow, Straight Ally or Progress. |
| [Rolling Landscape](Rolling%20Landscape/) | source | Art Deco night skyline of the 1930s as scrolling parallax silhouettes. |
| [Scale Colors](Scale%20Colors/) | source | Shows which of the 12 tones of the octave are sounding right now. |
| [Star Globe](Star%20Globe/) | source | Glowing stars in a snow-globe container, with gravity, bounce and collision. |
| [Water Drops](Water%20Drops/) | source | View onto a water surface; drops send out interfering, reflecting rings. |

## Examples

| Example | Description |
|---|---|
| [Scaler](Scaler/) | Shows live how our patches handle oversized surfaces: height as the reference, `Scale`, anchors. |

## Tools

| Tool | Description |
|---|---|
| [Kinect - Discontinued](Kinect%20-%20Discontinued/) | **Discontinued, no longer developed.** Sent the outlines and skeletons of people from a Kinect v2 to Arena and Wire via Spout. We will rebuild this with a LIDAR. |

## Installation

Copy a patch folder into your Wire patches folder (`Documents/Resolume Wire/Patches/`). Keep all files of the folder together.

## Known limitations

- Patches with ISF shaders (`.fs`) still reference them by absolute path and may not find them on your machine ([#1](https://github.com/S-Mazu/WirePatches/issues/1)).
- Not all patches have their own README yet.

## Bugs and ideas

Use the [issues](https://github.com/S-Mazu/WirePatches/issues). Each patch has a label `patch:<name>`.

## License

Public domain ([Unlicense](LICENSE)).
