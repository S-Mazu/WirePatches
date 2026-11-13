# WirePatches

A collection of [Resolume Wire](https://resolume.com/software/wire) patches for Resolume Arena/Avenue, mostly built with Claude via MCP.

## Sources

Patches that render their own image. In Arena they fill a clip slot.

| Patch | Description |
|---|---|
| [Bass Strings](sources/Bass%20Strings/) | Four double bass strings vibrate with the music, one frequency band per string. |
| [Flower Sax](sources/Flower%20Sax/) | A Len Lye–style instrument of your choice blows seven kinds of flowers into the room. |
| [Geo Tiles](sources/Geo%20Tiles/) | Tile collage of 67 flat geometric motifs in three colors. |
| [Lye Swing](sources/Lye%20Swing/) | Painted big-band score after Len Lye; five shapes follow the music, one per sound band. |
| [Pride Flag](sources/Pride%20Flag/) | A flag moves lightly in the wind: Rainbow, Straight Ally or Progress. |
| [Rolling Landscape](sources/Rolling%20Landscape/) | Art Deco night skyline of the 1930s as scrolling parallax silhouettes. |
| [Scale Colors](sources/Scale%20Colors/) | Shows which of the 12 tones of the octave are sounding right now. |

## Effects

Patches that process the clip below. In Arena they go onto a clip, layer, group or the composition.

| Patch | Description |
|---|---|
| [Autumn Wind](effects/Autumn%20Wind/) | 16 kinds of items (leaves, hats, newspapers, …) drift across the clip with depth, gusts, tumble and eddies. |
| [Deco Fan](effects/Deco%20Fan/) | One Art Deco fan of rays and rings; add it several times for several fans. |
| [Deco Marquee](effects/Deco%20Marquee/) | Art Deco theatre frame with chasing marquee lamps. |
| [Fire Edges](effects/Fire%20Edges/) | Flames and smoke grow from the edges of the frame, from a glow to an inferno. |
| [Fire Mask](effects/Fire%20Mask/) | The brightness of the clip decides what burns: logos, text, silhouettes. |
| [Fire Point](effects/Fire%20Point/) | Flames and smoke rise from one burning shape: point, square or heart. |
| [Iris Diaphragm](effects/Iris%20Diaphragm/) | Mechanical blade iris with overlapping blades that closes over the clip. |
| [Star Globe](effects/Star%20Globe/) | Glowing points over the clip: fireflies across the frame, or stars in a vessel that fall, collide and simmer. |
| [Water Drops](effects/Water%20Drops/) | View onto water over the clip: drops, stones or hearts fall in and send out rings that bend the picture. |

## Examples

Patches without show content that demonstrate a technique for building patches.

| Example | Description |
|---|---|
| [Scaler](examples/Scaler/) | Shows live how our patches handle oversized surfaces: height as the reference, `Scale`, anchors. |

## Tools

Programs that run outside Wire and feed patches.

| Tool | Description |
|---|---|
| | None yet. A LIDAR tool for motion-based projections is in development ([#69](https://github.com/S-Mazu/WirePatches/issues/69)). |

## Archive

No longer developed, kept for reference. Not maintained and not tested with current versions.

| Name | Kind | Description |
|---|---|---|
| [Kinect](archive/Kinect/) | tool | Discontinued 2026-11-08. Sent the outlines and skeletons of people from a Kinect v2 to Arena and Wire via Spout. Replaced by the LIDAR tool ([#69](https://github.com/S-Mazu/WirePatches/issues/69)). |

## Installation

Copy a patch folder (e.g. `effects/Fire Mask/`) into your Wire patches folder (`Documents/Resolume Wire/Patches/`). Keep all files of the folder together. Tools have their own installation steps in their README.

## Bugs and ideas

Use the [issues](https://github.com/S-Mazu/WirePatches/issues). Each patch has a label `patch:<name>`, each example `example:<name>`, each tool `tool:<name>`.

## Building patches

- [Patch rules](docs/PATCH_RULES.md): self-contained folders, paths, surface model, controls
- [Glossary](docs/GLOSSARY.md): fixed control names and naming patterns
- [Templates](docs/templates/): READMEs and checklists for sources, effects, examples and tools

## License

Public domain ([Unlicense](LICENSE)).
