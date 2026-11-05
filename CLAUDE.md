# CLAUDE.md

Guidance for Claude when working in this repository.

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
- Wire stores ISF shader references in `.wire` files as `"type": "resourceIsf"` → `"value": { "path": { "main": "…" } }`. Whether Wire resolves relative paths there is **not yet verified**; see #1.
- Wire's own sharing features (see [Saving & Sharing](https://resolume.com/support/en/wire-saving-consolidating)):
  - **Consolidate** copies the patch and all resources into a new folder and redirects the resource nodes to the copies.
  - **Copy For Sharing** does *not* copy shaders, images or videos.
  - **Compile** stores resources, presets and patch data in one file.
  - Which of these yields a portable, path-independent result is part of #1.
- Check before every commit:
  ```bash
  grep -alE '[A-Za-z]:[\\/]|/Users/|/home/' */*.wire */*.fs */*.py 2>/dev/null
  ```
  This must return nothing.

### Surface independence
Every patch must work on any surface:
- a single projector
- three projectors side by side or stacked
- an LED wall of any size and aspect ratio

**How to achieve this is still under discussion** (#11, placement principles #13). Do not treat any approach as settled.

Until then:
- In ISF shaders, derive everything from `RENDERSIZE`; no hard-coded resolutions.
- Document the patch's current behavior in the README ("Any surface").
- `meta.resolution` is fixed to 1920×1080 in all patches. This is a known bug (#14); do not change it before #11 is settled.

### Controls
- **No duplication of Arena**: no controls for what Arena already does for the whole clip (global opacity, position, scale, rotation). Controls that act on individual elements inside the patch are fine (e.g. `Strings Opacity`). Audit: #17.
- **Consistent names**: the same thing has the same name in every patch (e.g. `Clock`, `Backdrop`). A glossary is added here only after the names have been extracted from the patches and reconciled; never guessed.
- **Grouping**: controls that belong together are grouped.
- **Wide ranges**: ranges are always wider than needed, to allow creative use.
- **NaN-safe**: invalid values (NaN, Inf, division by zero, `log`/`sqrt`/`pow` of invalid input, `normalize` of a zero vector) are caught inside the patch, never by narrowing a slider range. Every patch must stay stable at all slider extremes.

### Metadata
Every `.wire` file has these fields filled in `patch.meta` (tracked in #15):

| Field | Value |
|---|---|
| `author` | `Stefan Mazur` |
| `mail` | `S.Mazur@cati-co.de` |
| `url` | `https://github.com/S-Mazu/WirePatches` |

`identifier` must be unique per patch (see #16).

### Naming and versions
- Folder name = patch name = `.wire` file name (`Patch Name/Patch Name.wire`).
- **No version suffixes** (`v2`) in folder or file names. Git history is the version history; tag releases if needed.
- If two variants really must coexist, give them names that say how they differ.
- Existing `v2` folders are tracked as a bug: #10.

### Patch description inside Wire
- The patch-level `"description"` field in the `.wire` file is the in-app documentation. Keep it consistent with the patch README.

## Patch README

Every patch folder has a `README.md` based on [`_template/README.md`](_template/README.md). The template is the single source of truth for the sections and the pre-commit checklist.

Required sections:
- Summary
- Vision
- Requirements
- Files
- Installation
- Clock: required if the patch is driven by a clock. Explain it and give a sensible start value.
- Parameters
- Any surface (surface behavior, tested sizes)
- Known issues
- Changelog

The README carries a `Last updated: YYYY-MM-DD` line.

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

- **Labels** (lowercase; names are case-sensitive):
  - Type: `bug`, `enhancement`, `documentation`, `idea`, `question`, `invalid`.
  - Patch: `patch:<name>`. `<name>` is the patch name without version suffix, lowercase, with spaces replaced by `_`. Examples: `Autumn Wind` → `patch:autumn_wind`, `Deco Marquee v2` → `patch:deco_marquee`. Description: "Related to Patch <Patch Name>".
  - Workflow: `claude` marks work assigned to Claude (see below).
  - Not every patch has a label yet. Claude cannot create labels; if one is missing, ask the user.
- **Label `claude`**: the user assigns an issue to Claude by adding this label.
  - At the start of every planning or implementation phase, list the open issues labeled `claude` and bring them up with the user:
    ```
    list_issues(owner="S-Mazu", repo="WirePatches", labels=["claude"], state="OPEN")
    ```
  - The label is a request to discuss, not permission to act. Agree on scope with the user before starting work.
  - Only the user adds the label. Claude removes it when the issue is closed, or when the user hands the work back.
- **Ideas**: issue with label `idea`. When work starts, replace `idea` with `enhancement` (or `bug`) in the same issue; do not open a new one.
- **Cross-patch problems**: one parent issue with one sub-issue per patch.
- **Closing**: always with a reason (`completed`, `not_planned`, `duplicate`) and a reference to the fixing commit.
- GitHub Projects (board view) and Discussions are maintained by the user; Claude has no tooling for them.

## Commits

- English, imperative mood, prefixed with the patch name where applicable: `Twirl: fix aspect handling`.
- One patch per commit where possible.
- Reference issues (`Fixes #7`).
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
