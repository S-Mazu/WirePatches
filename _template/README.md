<!--
TEMPLATE FOR A PATCH README

How to use:
1. Create a new patch in Wire and save it as `<Patch Name>/<Patch Name>.wire`.
2. Copy this file into the patch folder.
3. Replace every <placeholder>, then delete all HTML comments (including this one).
4. Remove optional sections that do not apply. They are marked "(optional)".
5. Before committing, work through the checklist at the end of this file.

The folder name, the patch name and the .wire file name must be identical.
No version suffixes (v2, final, …); use git history instead.
-->

# <Patch Name>

<One or two sentences: what the patch does and what kind it is (source / effect, with or without alpha).>

Last updated: <YYYY-MM-DD>

## Vision (optional)

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

## Driving the patch (optional)

<!-- For patches without internal motion, e.g. driven by a Clock parameter. -->
<Which parameter drives the motion, its range, and a recommended setup in Arena (e.g. Timeline with Duration = 1000 s, BPM Sync, FFT).>

## Parameters

<!-- Group as in the Arena UI. Give the range and the effect. Mention safe extremes. -->

**<Group>**
- `<Parameter>` (<min> to <max>, default <value>): <effect>

## Any surface

<!-- Required. See CLAUDE.md, "Resolution and aspect-ratio independence", and issue #11. -->
- Aspect handling: <fit | fill | stretch | extends scene>, <what happens on very wide or very tall surfaces>
- Size-dependent parameters: <how line widths, blur, density etc. scale with the output size>
- Notes: <e.g. "set the composition to the full surface size before loading; reload the clip after changing it">

Tested sizes:

| Size | Aspect | Result |
|---|---|---|
| 1920×1080 | 16:9 | <ok / issue #…> |
| 1080×1920 | 9:16 | |
| 4096×512 | 8:1 | |
| 512×512 | 1:1 | |
<!-- Test matrix to be finalized in #11. -->

## Known issues

See [open issues for this patch](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3A%22patch%3A<Patch+Name>%22).
<!-- Replace <Patch+Name> with the folder name, spaces as "+". -->

## Changelog

- <YYYY-MM-DD>: <change> (<#issue>)

---

Author: <Name>, https://github.com/S-Mazu/WirePatches

<!--
CHECKLIST BEFORE COMMIT (delete this block)

[ ] Folder name = patch name = .wire file name, no version suffix
[ ] All required files are in this folder; no references to other folders
[ ] No absolute paths:
      grep -alE '[A-Za-z]:[\\/]|/Users/|/home/' *.wire *.fs *.py 2>/dev/null
    returns nothing
[ ] .wire is valid JSON:
      python3 -m json.tool "<Patch Name>.wire" > /dev/null
[ ] Patch "description" in the .wire matches this README
[ ] Tested on the show machine at the sizes listed above
    (changes made in the cloud session are marked "untested" in the commit message)
[ ] Root README.md lists the patch
[ ] Label patch:<Patch Name> exists (ask the repo owner)
-->
