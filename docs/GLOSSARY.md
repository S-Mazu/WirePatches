# Glossary

Fixed terms for control names. Every patch and every revision uses only these.

- **Consistent names**: the same thing has the same name in every patch.
- New terms are added only after the names have been extracted from the patches and reconciled; never guessed.
- Label length: at most 20 characters (see [PATCH_RULES.md](PATCH_RULES.md), "Controls").

## Terms

| Term | Meaning | Rule |
|---|---|---|
| `Clock` | Controls the speed of the patch | Label exactly `Clock`. Directly after `Scale`, not in any group. Only for patches with a sequence (e.g. Rolling Landscape). Physical effects whose look breaks when slowed down or sped up (e.g. fire) have no `Clock`. Decide case by case for effects where slow motion just shows less action (e.g. Autumn Wind). |
| `Backdrop` | Background behind the source | Group `Backdrop` containing `Backdrop On` and `Backdrop Color`. |
| `Physics` | All physics and motion settings of a patch | One group `Physics` holding all of them. |
| `Scale` | Size of all elements relative to the output height (surface model) | Label exactly `Scale`. Default 1, wide range. First parameter of every patch, not in any group. |
| `Sound Input` | Audio input and its conditioning | Group `Sound Input`. `Spectrum In` is its first entry, followed by `Boost`, `Gate`, `Fade`, then patch-specific controls. |
| `Performance Cap` | Upper limit of simultaneous items, for performance only | Default high. The README states that on very large surfaces it can limit the density. |
| `Seed` | Random start value | |

## Naming patterns

`<Element>` is the thing the control acts on (e.g. `Fan`, `Leaf`, `Ring`). If the prefixed label exceeds 20 characters, drop the prefix; the group name identifies the element.

| Pattern | Meaning | Rule |
|---|---|---|
| `<Element> X`, `<Element> Y` | Position of an element | |
| `<Element> Z` | Depth | Only for depth in a scene with parallax. |
| `<Element> Size` | Size of an element | Relative to the output height, multiplied by `Scale`. |
| `<Element> Count` | Explicit number of parts of one object (rays, blades, layers, copies) | Does not grow with the surface. |
| `<Element> Density` | Population: amount per reference area (output height × output height at `Scale` 1) | Grows with the surface. |
| `<Element> Width` | Thickness of a line (line, edge, outline, border, ray, ring) | Relative to the output height, multiplied by `Scale`; never pixels. |
| `<Element> Width`, `<Element> Height` | Dimensions of an element, used as a pair | Relative to the output height, multiplied by `Scale`; never pixels. |
| `<Element> Aspect` | Proportions of a shape | |
| `<Element> Spacing` | Distance between copies | Relative to the output height, multiplied by `Scale`; never pixels. |
| `<Element> Opacity` | Opacity of an element | Plural for several elements (`Strings Opacity`), singular for one (`Moon Opacity`). |
| `<Element> Color` | Color of an element | |
| `<Element> Life` | Lifetime of emitted elements | |
| `<Element> On` | Switches an element on or off | |
| `Show <X>` | Helper view or overlay (`Show Input`, `Show Meter`) | |
| `Scatter` | Random offset per copy | Stable over time, in reference units multiplied by `Scale`. |
| `Spread` | Random range around a point or direction | |
| `<Property> Variation` | Random deviation per item | Stable over the item's life, as a share of the base value. Hue in degrees. |

## Units

- Time is in seconds; labels carry no unit for it.
- Other units are named in the label: `(°)`, `(Hz)`.
