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
- Every patch commit states `Tested in <Wire/Arena> (<sizes>)` or `Untested`.
- Never claim that a patch works without testing it in Wire or Arena.

## Repository layout

```
/
├── CLAUDE.md                 this file
├── README.md                 overview: sources, effects, examples, tools (one line each, link to the folder)
├── LICENSE
├── .gitignore
├── sources/<Patch Name>/     source patches, one folder per patch, self-contained
│   ├── <Patch Name>.wire
│   ├── README.md
│   └── Resource/             all other required files (*.fs, *.py, images, …), as Wire's Consolidate lays them out
├── effects/<Patch Name>/     effect patches, same layout as sources
├── examples/<Example Name>/  patches that demonstrate a technique (e.g. Scaler)
├── tools/<Tool Name>/        programs outside Wire that feed patches (e.g. a sensor bridge)
└── docs/
    ├── PATCH_RULES.md        rules for patches, examples and tools
    ├── GLOSSARY.md           fixed control names, naming patterns, units
    └── templates/            README templates and checklists: Source.md, Effect.md, Example.md, Tool.md
```

Top-level folders are lowercase. Patch, example and tool folders keep their names with spaces.

## Patch rules and glossary

The rules live in `docs/` so that colleagues can read them too. They are binding for Claude:

@docs/PATCH_RULES.md

@docs/GLOSSARY.md

## Editing `.wire` files directly

- `.wire` is JSON (`formatVersion` 1.1.0 at the time of writing), tab-indented. Keep the formatting to keep diffs readable.
- Validate after every edit:
  ```bash
  python3 -m json.tool "<sources|effects|examples>/<Patch>/<Patch>.wire" > /dev/null
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
  - Example: `example:<name>`, Tool: `tool:<name>`, built the same way. Descriptions: "Related to Example <Name>", "Related to Tool <Name>".
  - Workflow: `claude` marks work assigned to Claude (see below). `stefan` marks work for the user; Claude adds it, and also assigns the issue to `S-Mazu`.
  - Not every patch, example or tool has a label yet. Claude cannot create labels; if one is missing, ask the user.
- **Label `claude`**: the user assigns an issue to Claude by adding this label.
  - At the start of every planning or implementation phase, list the open issues labeled `claude` and bring them up with the user.
  - The label is a request to discuss, not permission to act. Agree on scope with the user before starting work.
  - Only the user adds the label. Claude removes it when the issue is closed, or when the user hands the work back.
- **Ideas**: issue with label `idea`. When work starts, replace `idea` with `enhancement` (or `bug`) in the same issue; do not open a new one.
- **Closing**: always with a reason (`completed`, `not_planned`, `duplicate`) and a reference to the fixing commit.

## Commits

- English, imperative mood, prefixed with the patch name where applicable: `Twirl: fix aspect handling`.
- Keep `README.md` (root) in sync when adding, renaming, moving or removing a patch, example or tool.
- Change `docs/PATCH_RULES.md` and `docs/GLOSSARY.md` only when the user explicitly instructs it, like this file.
- One branch per task (issue or instruction), named `claude/<topic>`. In a cloud session, use the branch the session assigns.
- One commit per patch, referencing its issue with `Refs #<n>`.
- One PR per branch, closing its issues with `Fixes #<n>`.
- Claude merges only when the user explicitly says so.

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
