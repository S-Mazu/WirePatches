# Patch rules

Rules for every patch in `sources/` and `effects/`, and for examples and tools as listed in "Examples and tools". Control names follow the [glossary](GLOSSARY.md).

## Self-contained

- A patch folder contains **everything** required to use the patch: `.wire`, ISF shaders (`.fs`), scripts, images, other resources.
- No dependencies on files in other patch folders or anywhere outside the folder.
- Copying the folder to any location on any machine must be enough.

## Paths

- **No absolute paths**, anywhere: not in `.wire` files, shaders, scripts or docs.
- References to resources must be relative to the patch folder.
- Wire stores ISF shader references in `.wire` files as `"type": "resourceIsf"` → `"value": { "path": { "main": "…" } }`.
- `main` is resolved relative to the `.wire` file and reads `Resource/<Shader>.fs`. Wire stores a shader set by absolute path as absolute, so run the check below after every rebuild.
- Check before every commit, from the repository root:
  ```bash
  grep -alE '(^|[^A-Za-z])[A-Za-z]:[\\/]|/Users/|/home/|OneDrive' */*/*.wire */*/*.fs */*/*.py */*/Resource/*.fs */*/Resource/*.py 2>/dev/null
  ```
  This must return nothing.

## Surface independence

Every patch must work on any surface:
- a single projector
- three projectors side by side or stacked
- an LED wall of any size and aspect ratio

### Surface model

1. **Height is the reference.**
   - The image is designed for one screen of any height.
   - On another surface, everything scales with the output height. The width only decides how much of the scene is visible; nothing is ever stretched.
   - Wire's shape and mesh coordinates follow the shorter side (portrait: x ±1, y ±1/aspect). To make height the reference and apply `Scale`, render through one `Camera 2D` with `zoom = clamp(1/aspect, 1, 1000000) × safeScale`, where `safeScale = clamp(Scale, 0.001, 1000)`. The visible area is then y ±1/`safeScale`, x ±aspect/`safeScale`.
2. **`Scale` turns "bigger" into "more scene".**
   - `Scale` multiplies all element sizes, distances and speeds; default 1.
   - The choice is made per setup in Arena, not per patch.
3. **The scene must extend sideways.** Wide surfaces need content across the whole width (generated, repeating or distributed), not one centered picture with empty sides.
4. **Elements are anchored.** Each element has an anchor (bottom, top, left, right or center). Its offset from the anchor is in reference units, multiplied by `Scale`. Example: the horizon sits at a fixed offset above the bottom edge, so with `Scale` ⅓ the city stays at the bottom and the sky grows.
   - Percent of the actual frame only for zones and paths that are meant to grow with the frame (e.g. edge zones, the moon's path across the surface).

The example patch [Scaler](../examples/Scaler/) shows the model live.

Rules:
- Read the output size at runtime (`Patch Info` node in Wire, `RENDERSIZE` in ISF); no hard-coded resolutions.
- No pixel units in controls (`(px)`). Sizes are relative to the height, multiplied by `Scale`.
- A clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip. `meta.resolution` has no effect in Arena. Document this in the README.

## Controls

- **No duplication of Arena**: no controls for what Arena already does for the whole clip (global opacity, position, scale, rotation). Controls that act on individual elements inside the patch are fine (e.g. `Strings Opacity`).
- **Consistent names**: use only the terms and naming patterns in the [glossary](GLOSSARY.md).
- **Label length**: at most 20 characters. Arena scales labels and cuts them off at about 19 characters; one clipped letter is accepted.
- **Grouping**: controls that belong together are grouped.
- **Wide ranges**: ranges are always wider than needed, to allow creative use.
- **NaN-safe**: invalid values (NaN, Inf, division by zero, `log`/`sqrt`/`pow` of invalid input, `normalize` of a zero vector) are caught inside the patch, never by narrowing a slider range. Every patch must stay stable at all slider extremes.

## Source and effect

- Source: renders its own image. Lives in `sources/`, `patch.meta.category` is `source`.
- Effect: processes the clip below. `Texture In` directly after `Scale` and `Clock`. No `Backdrop`. Lives in `effects/`, `patch.meta.category` is `effect`.
- If the type of a patch changes, move its folder with `git mv`.

## Patches that use a tool

- A patch that receives data from a tool (e.g. a sensor via Spout or OSC) stays stable when the tool is not running: no motion, but no black output, no frozen output and no NaN.
- The interface (Spout name, OSC address, data format) is defined in the tool README under `Output`. The patch README links to it.

## Metadata

Every `.wire` file has these fields filled in `patch.meta`:

| Field | Value |
|---|---|
| `author` | `Stefan Mazur` |
| `mail` | `S.Mazur@cati-co.de` |
| `url` | `https://github.com/S-Mazu/WirePatches` |

`identifier` must be unique per patch.

## Naming and versions

- Folder name = patch name = `.wire` file name (`effects/Patch Name/Patch Name.wire`). Patch names keep their spaces.
- **No version suffixes** (`v2`) and no status (`Discontinued`) in folder or file names. Git history is the version history; tag releases if needed. A status goes into the README.
- If two variants really must coexist, give them names that say how they differ.

## Patch description inside Wire

- The patch-level `"description"` field in the `.wire` file is the in-app documentation. Keep it consistent with the patch README.

## Patch README

Every patch folder has a `README.md` based on [`templates/Source.md`](templates/Source.md) or [`templates/Effect.md`](templates/Effect.md). The templates define the sections and the pre-commit checklist.

Changes to one of the two templates are applied to the other in the same commit.

**Whenever a patch is changed, update its README in the same commit**, including `Last updated` and the changelog.

## Examples and tools

| | Patches | Examples | Tools |
|---|---|---|---|
| Folder | `sources/`, `effects/` | `examples/` | `tools/` |
| Self-contained, no absolute paths | yes | yes | yes |
| Metadata, surface model | yes | yes | no |
| Glossary, control rules | yes | where they fit the purpose | no |
| README template | [Source](templates/Source.md) / [Effect](templates/Effect.md) | [Example](templates/Example.md) | [Tool](templates/Tool.md) |
| Label | `patch:<name>` | `example:<name>` | `tool:<name>` |

- **Example**: a Wire patch without show content that demonstrates a technique for building patches (e.g. Scaler for the surface model).
- **Tool**: a program that runs outside Wire and feeds patches (e.g. a sensor bridge).
  - Name it by its function, not by the device: the patches depend on the interface, not on the hardware. The device goes into the README under `Hardware`.
  - Scripts sit directly in the tool folder (`Resource/` is a Wire concept).
  - Dependencies with versions in `requirements.txt` in the tool folder.
  - Runtime data (captured rooms, saved settings) is not committed; add it to the root `.gitignore`.
