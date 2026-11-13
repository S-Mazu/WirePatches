<!--
TEMPLATE FOR A PATCH README: EFFECT

For patches that process the clip below (Texture In).
Keep in sync with docs/templates/Source.md (docs/PATCH_RULES.md, "Patch README").

How to use:
1. Create a new patch in Wire and save it as `effects/<Patch Name>/<Patch Name>.wire`.
2. Copy this file into the patch folder as README.md.
3. Replace every <placeholder>, then delete all HTML comments (including this one).
4. Before committing, work through the checklist at the end of this file.

The folder name, the patch name and the .wire file name must be identical.
No version suffixes (v2, final, …) and no status; use git history instead.
-->

# <Patch Name>

<Patch Name> is an effect over the clip below: <one or two sentences on what it does to the clip.>

Last updated: <YYYY-MM-DD>

## Vision

<The idea behind the patch: mood, use case, artistic reference.>

## Requirements

- Resolume Wire / Arena: <minimum tested version, e.g. 7.28.1>
- Inputs: the clip below (`Texture In`)<, plus audio via Spectrum In (source: …) | MIDI | OSC | … if used>
- Performance: <rough GPU load, any heavy settings>
- `Performance Cap`: <default; on very large surfaces it can limit the density. Delete this line if the patch has none.>

## Files

| File | Purpose |
|---|---|
| `<Patch Name>.wire` | The patch |
| `Resource/<Shader>.fs` | <What this ISF shader does> |
<!-- List every file in the folder. Every resource the patch loads must be in Resource/, referenced by a relative path. -->

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. <Any further step, e.g. "Set Spectrum In to External". If none, write: "No further setup required.">

## Timing

<!-- Required. See docs/GLOSSARY.md, "Clock". -->
<Explain the use of `Clock`: what it drives, its range, the default, and a sensible start value in Arena (e.g. phase source Timeline with Duration = 1000 s); alternatives such as BPM Sync or FFT.
Or explain why the patch has no `Clock` and how another timing control is used (e.g. real time with `Speed`).>

## Parameters

<!-- Order as in Arena: `Scale`, `Clock` (if used), `Texture In`, then the groups. No `Backdrop`.
Group as in the Arena UI. Give the range and the effect. Ranges are deliberately wide; state that extremes are safe.
Use only the glossary terms and naming patterns (docs/GLOSSARY.md). Time in seconds and angles in degrees, without unit in the label. -->

- `Scale` (0 to <max>, default 1): size of all elements relative to the output height.

**<Group>**
- `<Parameter>` (<min> to <max>, default <value>): <effect>

## Any surface

<!-- Required. See docs/PATCH_RULES.md, "Surface model". -->
- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched.
- `Scale`: <recommended values, e.g. 1 for one projector, ⅓ for three stacked to keep element sizes>
- Exceptions: <anything that does not follow the model, and why>
- Size-dependent parameters: <how line widths, blur, density etc. scale with the output size>
- Notes: a clip takes the composition size when it is loaded and keeps its size when the composition changes. To change it, set `Width`/`Height` on the clip in Arena or reload the clip.

Tested sizes:

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | <ok / issue #…> |
| 5760×1080 | 3 side by side | |
| 1920×3240 | 3 stacked | |
| 1080×1920 | 9:16 | |
| 4096×512 | 8:1 | |
| 512×512 | 1:1 | |

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Apatch%3A<patch_name>).
<!-- Replace <patch_name> with the label name after "patch:" (lowercase, no version suffix, spaces as "_"). See CLAUDE.md, "Issues and ideas". -->

## Changelog

- <YYYY-MM-DD>: <change> (<#issue>)

---

Author: <Name>, https://github.com/S-Mazu/WirePatches

<!--
CHECKLIST BEFORE COMMIT (delete this block)

[ ] Folder name = patch name = .wire file name, no version suffix
[ ] All required files are in this folder; no references to other folders
[ ] No absolute paths:
      grep -alE '(^|[^A-Za-z])[A-Za-z]:[\\/]|/Users/|/home/|OneDrive' *.wire Resource/*.fs Resource/*.py 2>/dev/null
    returns nothing
[ ] .wire is valid JSON:
      python3 -m json.tool "<Patch Name>.wire" > /dev/null
[ ] Patch "description" in the .wire matches this README
[ ] patch.meta: author, mail, url filled; licenseName "Unlicense"; identifier unique
[ ] No controls duplicating Arena (global opacity/position/scale/rotation)
[ ] `Scale` is the first parameter; `Clock` follows only if used (docs/GLOSSARY.md)
[ ] `Texture In` directly after `Scale` / `Clock`; no `Backdrop`
[ ] patch.meta.category is "effect"
[ ] Control names follow the glossary terms and naming patterns
[ ] Every control label has at most 20 characters
[ ] Every slider tested at both extremes: no NaN, no black/frozen output
[ ] README updated: Last updated, Changelog
[ ] Tested on the show machine at the sizes listed above
    (the commit states "Tested in <Wire/Arena> (<sizes>)" or "Untested")
[ ] Folder is in effects/; root README.md lists the patch under "Effects"
[ ] Label patch:<patch_name> exists (ask the repo owner)
-->
