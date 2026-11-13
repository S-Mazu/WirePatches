<!--
TEMPLATE FOR AN EXAMPLE README

For Wire patches without show content that demonstrate a technique for building patches (e.g. Scaler).

How to use:
1. Create the patch in Wire and save it as `examples/<Example Name>/<Example Name>.wire`.
2. Copy this file into the folder as README.md.
3. Replace every <placeholder>, then delete all HTML comments (including this one).
4. Before committing, work through the checklist at the end of this file.

The folder name, the patch name and the .wire file name must be identical.
No version suffixes (v2, final, …) and no status; use git history instead.
-->

# <Example Name>

An example <source | effect> patch that shows <the technique, in one or two sentences>. It has no show content. You load it to see the idea working, and you copy its nodes into your own patch.

Last updated: <YYYY-MM-DD>

## Vision

<The problem the technique solves, and why it matters for our patches.>

## Requirements

- Resolume Wire / Arena: <minimum tested version, e.g. 7.28.1>
- Inputs: <none | the clip below (`Texture In`) | …>
- Performance: <rough GPU load>

## Files

| File | Purpose |
|---|---|
| `<Example Name>.wire` | The example patch |
<!-- List every file in the folder. Every resource the patch loads must be in Resource/, referenced by a relative path. -->

## Installation

1. Copy this whole folder into your Resolume Wire patches folder (`Documents/Resolume Wire/Patches/`).
2. <Any further step. If none, write: "No further setup required.">

## The idea in plain words

<Explain the technique without Wire terms.>

## Try it

<Steps in Arena or Wire that make the technique visible, e.g. change the composition size, move a slider.>

## How the logic works

<The node chain, step by step. Link the rule it demonstrates in docs/PATCH_RULES.md or docs/GLOSSARY.md.>

## Using it in your own patch

<Which nodes to copy, how to connect them, what to adapt.>

## Parameters

<!-- Order as in Arena. Use the glossary terms and naming patterns (docs/GLOSSARY.md) where they fit the purpose. -->

- `<Parameter>` (<min> to <max>, default <value>): <effect>

## Any surface

<!-- See docs/PATCH_RULES.md, "Surface model". -->
- <How the example behaves on other surfaces.>

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

See [open issues for this example](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Aexample%3A<example_name>).
<!-- Replace <example_name> with the label name after "example:" (lowercase, spaces as "_"). See CLAUDE.md, "Issues and ideas". -->

## Changelog

- <YYYY-MM-DD>: <change> (<#issue>)

---

Author: <Name>, https://github.com/S-Mazu/WirePatches

<!--
CHECKLIST BEFORE COMMIT (delete this block)

[ ] Folder is in examples/; folder name = patch name = .wire file name
[ ] All required files are in this folder; no references to other folders
[ ] No absolute paths:
      grep -alE '(^|[^A-Za-z])[A-Za-z]:[\\/]|/Users/|/home/|OneDrive' *.wire Resource/*.fs Resource/*.py 2>/dev/null
    returns nothing
[ ] .wire is valid JSON:
      python3 -m json.tool "<Example Name>.wire" > /dev/null
[ ] Patch "description" in the .wire matches this README
[ ] patch.meta: author, mail, url filled; licenseName "Unlicense"; identifier unique
[ ] Follows the surface model
[ ] Every slider tested at both extremes: no NaN, no black/frozen output
[ ] README updated: Last updated, Changelog
[ ] Tested on the show machine (the commit states "Tested in <Wire/Arena> (<sizes>)" or "Untested")
[ ] Root README.md lists the example under "Examples"
[ ] Label example:<example_name> exists (ask the repo owner)
-->
