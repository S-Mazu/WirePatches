<!--
TEMPLATE FOR A PATCH README

How to use:
1. Create a new patch in Wire and save it as `<Patch Name>/<Patch Name>.wire`.
2. Copy this file into the patch folder.
3. Replace every <placeholder>, then delete all HTML comments (including this one).
4. Delete the "Clock" section if the patch has no clock.
5. Before committing, work through the checklist at the end of this file.

The folder name, the patch name and the .wire file name must be identical.
No version suffixes (v2, final, …); use git history instead.
-->

# <Patch Name>

<One or two sentences: what the patch does and what kind it is (source / effect, with or without alpha).>

Last updated: <YYYY-MM-DD>

## Vision

<The idea behind the patch: mood, use case, artistic reference.>

## Requirements

- Resolume Wire / Arena: <minimum tested version, e.g. 7.28.1>
- Inputs: <none | audio via Spectrum In (source: …) | external texture | MIDI | OSC | …>
- Performance: <rough GPU load, any heavy settings>

## Files

| File | Purpose |
|---|---|
| `<Patch Name>.wire` | The patch |
| `<Shader>.fs` | <What this ISF shader does> |
<!-- List every file in the folder. Every resource the patch loads must be here, referenced by a relative path. -->

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. <Any further step, e.g. "Set Spectrum In to External". If none, write: "No further setup required.">

## Clock (required)

<!-- Required if the patch is driven by a clock; otherwise delete this section. -->
<What the Clock drives, its range, the default, and a sensible start value in Arena (e.g. phase source Timeline with Duration = 1000 s). Alternatives such as BPM Sync or FFT.>

## Parameters

<!-- Group as in the Arena UI. Give the range and the effect. Ranges are deliberately wide; state that extremes are safe. Use the shared control names (CLAUDE.md, "Controls"). -->

**<Group>**
- `<Parameter>` (<min> to <max>, default <value>): <effect>

## Any surface

<!-- Required. See CLAUDE.md, "Surface model". -->
- Follows the surface model: height is the reference, width shows more or less scene, nothing is stretched.
- `Scale`: <recommended values, e.g. 1 for one projector, ⅓ for three stacked to keep element sizes>
- Exceptions: <anything that does not follow the model, and why>
- Notes: <e.g. "set the clip size to the full surface; reload the clip after changing the composition size">
- Size-dependent parameters: <how line widths, blur, density etc. scale with the output size>
- Notes: <e.g. "set the composition to the full surface size before loading; reload the clip after changing it">

Tested sizes:

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | <ok / issue #…> |
| 5760×1080 | 3 side by side | |
| 1920×3240 | 3 stacked | |
| 1080×1920 | 9:16 | |
| 4096×512 | 8:1 | |
| 512×512 | 1:1 | |
<!-- Test matrix to be finalized in #11. -->

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
      grep -alE '(^|[^A-Za-z])[A-Za-z]:[\\/]|/Users/|/home/|OneDrive' *.wire *.fs *.py 2>/dev/null
    returns nothing
[ ] .wire is valid JSON:
      python3 -m json.tool "<Patch Name>.wire" > /dev/null
[ ] Patch "description" in the .wire matches this README
[ ] patch.meta: author, mail, url filled; identifier unique
[ ] No controls duplicating Arena (global opacity/position/scale/rotation)
[ ] Control names match the other patches
[ ] Every control label has at most 20 characters
[ ] Every slider tested at both extremes: no NaN, no black/frozen output
[ ] README updated: Last updated, Changelog
[ ] Tested on the show machine at the sizes listed above
    (changes made in the cloud session are marked "untested" in the commit message)
[ ] Root README.md lists the patch
[ ] Label patch:<patch_name> exists (ask the repo owner)
-->
