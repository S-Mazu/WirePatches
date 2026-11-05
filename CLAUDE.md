# CLAUDE.md

Guidance for Claude when working in this repository.

**Change this file only when the user explicitly instructs it.**

## Purpose

A collection of Resolume Wire patches, shared with colleagues for use in Resolume Arena/Avenue.
The repo serves three goals:

1. **Distribution**: colleagues copy a patch folder and use it without further setup.
2. **Bug tracking**: via GitHub Issues.
3. **Development**: new patches are designed and built together with Claude.

## Language

- Everything in the repo is **English**: code, comments, patch descriptions, READMEs, issues, commit messages.
- In chat, reply in the language the user writes in.

## Working environments

| Environment | Wire/Arena access | What Claude can do |
|---|---|---|
| Cloud session (claude.ai/code) | none | Edit files (`.wire` is JSON, `.fs` is GLSL), docs, issues. **Cannot load, run or visually verify a patch.** |
| Show machine | Resolume Arena and Wire via MCP server | Build, modify and test patches in Wire and Arena. |

Rules:
- In the cloud session, mark every change to a `.wire` or `.fs` file as **untested** (commit message and issue) until it has been verified on the show machine.
- Never claim that a patch works without testing it in Wire or Arena.

## Repository layout

```
/
├── CLAUDE.md          this file
├── README.md          overview of all patches (one line each, link to patch folder)
├── LICENSE
├── _template/         README template and checklist for new patches
└── <Patch Name>/      one folder per patch, self-contained
    ├── <Patch Name>.wire
    ├── README.md
    └── *.fs, *.py, …  any other required files
```

## Patch rules

### Self-contained
- A patch folder contains **everything** required to use the patch: `.wire`, ISF shaders (`.fs`), scripts, images, other resources.
- No dependencies on files in other patch folders or anywhere outside the folder.
- Copying the folder to any location on any machine must be enough.

### Paths
- **No absolute paths**, anywhere: not in `.wire` files, shaders, scripts or docs.
- References to resources must be relative to the patch folder.
- Wire stores ISF shader references in `.wire` files as `"type": "resourceIsf"` → `"value": { "path": { "main": "…" } }`.
- `main` is resolved relative to the `.wire` file. Wire stores a shader set by absolute path as absolute, so run the check below after every rebuild.
- Check before every commit:
  ```bash
  grep -alE '(^|[^A-Za-z])[A-Za-z]:[\\/]|/Users/|/home/|OneDrive' */*.wire */*.fs */*.py 2>/dev/null
  ```
  This must return nothing.

### Surface independence
Every patch must work on any surface:
- a single projector
- three projectors side by side or stacked
- an LED wall of any size and aspect ratio

#### Surface model
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

Rules:
- Read the output size at runtime (`Patch Info` node in Wire, `RENDERSIZE` in ISF); no hard-coded resolutions.
- No pixel units in controls (`(px)`). Sizes are relative to the height, multiplied by `Scale`.
- A clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip. `meta.resolution` has no effect in Arena. Document this in the README.

### Controls
- **No duplication of Arena**: no controls for what Arena already does for the whole clip (global opacity, position, scale, rotation). Controls that act on individual elements inside the patch are fine (e.g. `Strings Opacity`).
- **Consistent names**: the same thing has the same name in every patch. Use only the terms in the glossary below. New terms are added only after the names have been extracted from the patches and reconciled; never guessed.
- **Label length**: at most 20 characters. Arena scales labels and cuts them off at about 19 characters; one clipped letter is accepted.
- **Grouping**: controls that belong together are grouped.
- **Wide ranges**: ranges are always wider than needed, to allow creative use.
- **NaN-safe**: invalid values (NaN, Inf, division by zero, `log`/`sqrt`/`pow` of invalid input, `normalize` of a zero vector) are caught inside the patch, never by narrowing a slider range. Every patch must stay stable at all slider extremes.

### Glossary
Fixed terms. Every revision uses only these.

| Term | Meaning | Rule |
|---|---|---|
| `Clock` | Controls the speed of the patch | Label exactly `Clock`. First parameter of the patch, not in any group. |
| `Backdrop` | Background behind the source | Group `Backdrop` containing `Backdrop On` and `Backdrop Color`. |
| `Physics` | All physics and motion settings of a patch | One group `Physics` holding all of them. |
| `Scale` | Size of all elements relative to the output height (surface model) | Label exactly `Scale`. Default 1, wide range. Second parameter, directly after `Clock`, not in any group. |

### Metadata
Every `.wire` file has these fields filled in `patch.meta`:

| Field | Value |
|---|---|
| `author` | `Stefan Mazur` |
| `mail` | `S.Mazur@cati-co.de` |
| `url` | `https://github.com/S-Mazu/WirePatches` |

`identifier` must be unique per patch.

### Naming and versions
- Folder name = patch name = `.wire` file name (`Patch Name/Patch Name.wire`).
- **No version suffixes** (`v2`) in folder or file names. Git history is the version history; tag releases if needed.
- If two variants really must coexist, give them names that say how they differ.

### Patch description inside Wire
- The patch-level `"description"` field in the `.wire` file is the in-app documentation. Keep it consistent with the patch README.

## Patch README

Every patch folder has a `README.md` based on [`_template/README.md`](_template/README.md). The template defines the sections and the pre-commit checklist.

**Whenever a patch is changed, update its README in the same commit**, including `Last updated` and the changelog.

## Editing `.wire` files directly

- `.wire` is JSON (`formatVersion` 1.1.0 at the time of writing), tab-indented. Keep the formatting to keep diffs readable.
- Validate after every edit:
  ```bash
  python3 -m json.tool "<Patch>/<Patch>.wire" > /dev/null
  ```
- Node IDs and `connections` (`"from": [id, "outlet"]`, `"to": [id, "inlet"]`) must stay consistent. Do not renumber nodes.
- Prefer building and modifying patches in Wire via MCP. Edit JSON by hand only for small, mechanical changes (paths, descriptions, default values).

## Issues and ideas

All bugs, tasks and ideas live in GitHub Issues in `S-Mazu/WirePatches`. There is no TODO file.

**Keep issues lean.** Fixing beats documenting.
- Report findings in chat first. Create an issue only when the user agrees, or when the work clearly cannot be done right away.
- No parent issues or sub-issue trees unless the user asks for them.
- Do not document problems in patches that may still be deleted.

- **Labels** (lowercase; names are case-sensitive):
  - Type: `bug`, `enhancement`, `documentation`, `idea`, `question`, `invalid`.
  - Patch: `patch:<name>`. `<name>` is the patch name without version suffix, lowercase, with spaces replaced by `_`. Examples: `Autumn Wind` → `patch:autumn_wind`, `Deco Marquee v2` → `patch:deco_marquee`. Description: "Related to Patch <Patch Name>".
  - Workflow: `claude` marks work assigned to Claude (see below). `stefan` marks work for the user; Claude adds it, and also assigns the issue to `S-Mazu`.
  - Not every patch has a label yet. Claude cannot create labels; if one is missing, ask the user.
- **Label `claude`**: the user assigns an issue to Claude by adding this label.
  - At the start of every planning or implementation phase, list the open issues labeled `claude` and bring them up with the user.
  - The label is a request to discuss, not permission to act. Agree on scope with the user before starting work.
  - Only the user adds the label. Claude removes it when the issue is closed, or when the user hands the work back.
- **Ideas**: issue with label `idea`. When work starts, replace `idea` with `enhancement` (or `bug`) in the same issue; do not open a new one.
- **Closing**: always with a reason (`completed`, `not_planned`, `duplicate`) and a reference to the fixing commit.

## Commits

- English, imperative mood, prefixed with the patch name where applicable: `Twirl: fix aspect handling`.
- One patch per commit where possible.
- Reference issues (`Fixes #<n>`).
- Keep `README.md` (root) in sync when adding, renaming or removing a patch.

## Reference documentation

Resolume publishes no PDF manual since version 7; the documentation is web-based.

**Wire** ([index](https://resolume.com/support/en/wire)):
- [Introduction](https://resolume.com/support/en/wire-introduction)
- [User Interface](https://resolume.com/support/en/wire-user-interface)
- [Nodes](https://resolume.com/support/en/wire-node-anatomy): node types; signal, event and attribute flow
- [Integration with Arena/Avenue](https://resolume.com/support/en/wire-resolume-integration): exposed controls, bit depth, resolution
- [Saving & Sharing](https://resolume.com/support/en/wire-saving-consolidating): resource handling when sharing patches
- [Fast Patching](https://resolume.com/support/en/fast-patching)
- [Slices](https://resolume.com/support/en/wire-slices)
- [MIDI](https://resolume.com/support/en/wire-midi)
- [OSC](https://resolume.com/support/en/wire-osc)
- [FFT](https://resolume.com/support/en/wire-fft)
- [Syphon and Spout](https://resolume.com/support/en/wire-syphon-and-spout)
- [ISF](https://resolume.com/support/en/isf)
- [Release notes](https://resolume.com/update/releaseNotes_updates_wire.html): check for new nodes and behavior changes

Each Wire node also ships with an example patch, opened from the node's context menu or the node panel.

**Arena/Avenue**:
- [Support index](https://resolume.com/support/en)
- [Arena/Avenue articles](https://resolume.com/support/avenue-arena)

**ISF**:
- [ISF specification](https://github.com/mrRay/ISF_Spec): `RENDERSIZE`, passes, imported images (paths relative to the `.fs` file)
- [isf.video](https://isf.video/): editor, examples
