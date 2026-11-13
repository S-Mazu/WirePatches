<!--
TEMPLATE FOR A TOOL README

For programs that run outside Wire and feed patches (e.g. a sensor bridge).

How to use:
1. Copy this file into the tool folder as README.md.
2. Replace every <placeholder>, then delete all HTML comments (including this one).
3. Before committing, work through the checklist at the end of this file.
-->

# <Tool Name>

<Tool Name> is a tool: <one or two sentences on what it does and what it sends to Wire/Arena.>

Last updated: <YYYY-MM-DD>

## Purpose

<Why the tool exists; which patches use it.>

## Hardware

- <Device, model, connection (e.g. USB), mounting position>

## Requirements

- OS: <e.g. Windows 11>
- Runtime: <e.g. Python 3.12>
- Packages: <list, or "see requirements.txt">
- Resolume Wire / Arena: <minimum tested version>

## Files

| File | Purpose |
|---|---|
| `<script>.py` | <what it does> |

## Installation

1. Copy this whole folder to any location.
2. <Install dependencies, e.g. `pip install -r requirements.txt`>
3. <Driver or device setup>

## Usage

<How to start and stop it; what you see when it works.>

## Output

<!-- The interface to the patches. -->
- Transport: <Spout | OSC | MIDI | …>, name/address/port: <…>
- Data: <format, units, rate (e.g. 10 Hz)>
- Patches using it: <links>

## Configuration

- `<option>` (default <value>): <effect>

## Troubleshooting

- <symptom>: <cause and fix>

## Known issues

See [open issues for this tool](https://github.com/S-Mazu/WirePatches/issues?q=is%3Aissue+is%3Aopen+label%3Atool%3A<tool_name>).

## Changelog

- <YYYY-MM-DD>: <change> (<#issue>)

---

Author: <Name>, https://github.com/S-Mazu/WirePatches

<!--
CHECKLIST BEFORE COMMIT (delete this block)

[ ] All required files are in this folder; no references to other folders
[ ] No absolute paths, no hard-coded user folders
[ ] All dependencies listed with versions
[ ] Starts on a fresh machine with the installation steps above
[ ] Output section matches what the patches expect
[ ] README updated: Last updated, Changelog
[ ] Tested with the hardware; the commit states "Tested with <device>" or "Untested"
[ ] Root README.md lists the tool
-->
