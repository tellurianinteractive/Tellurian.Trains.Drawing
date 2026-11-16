# Schema change log

Changes to the drawing file schemas (spec EX-7). Within a major schema version, only backward-compatible
changes are allowed. Schema version 1 is a draft until it is frozen (implementation plan, Phase 12);
until then it may still change.

## Version 1 (draft)
### 2026-11-16 (module identification)
- Modules have an optional `fremoNumber` (1–9999, Registry `FremoNumber`), and owners an optional `prefix`
  (the owner's FREMO prefix, e.g. `SFjä`, Registry `FremoOwnerSignature`). The first owner's prefix and the number
  form the FREMO designation, e.g. `SFjä 012`. Readers that don't know them ignore them.
- A module's `name` and an owner's `name` are no longer required; they may be empty. A module needs a name unless it
  has a FREMO number with the first owner's prefix. Writers still write both, as empty strings when not given.
- The auto-draw `parameters` have `fremoNumber` and `owners`.

### 2026-11-16 (end profile short name)
- End profiles have an optional `shortName`, e.g. `B96`, printed along the end plate in module drawings.
  When it is missing, the `designation` is used. Readers that don't know it ignore it.

### 2026-11-16 (exit orientation, Endewender)
- The auto-draw `parameters` of a module variant have `isEndProfileSwitcher` (the module is an *Endewender*,
  Registry flag). Readers that don't know it ignore it.
- Auto-draw generator version 1.1: an exit at the far end of the main path gets `orientation: mirrored` when its
  end profile is asymmetric, unless the module is an Endewender. The meaning of `orientation` is unchanged.
- An exit's `endProfile` may belong to another standard of the module's scale than the module's own standard.

### 2026-11-15 (file names)
- Drawing files are plain JSON files. Suggested names end with the kind and `.json`: `.endprofile.json`,
  `.module.json`, `.hall.json`, `.hallplan.json`, `.catalogue.json`. Readers identify a file by `format` and `kind`,
  not by its name. The earlier extensions (`.tdendprofile`, `.tdmodule`, `.tdhall`, `.tdhallplan`, `.tdcatalogue`)
  are still read. The content is unchanged; only the schemas' descriptions name the new file endings.

### 2026-11-15 (revision log, countries)
- End profiles, modules, halls and hall plans have a `revisionLog`: one entry per person who saved the drawing,
  with `name` and the last `date` (`yyyy-MM-dd`) they saved it, newest first. Readers that don't know it ignore it.
- Module standards have `countries`: the ISO 3166 two-letter codes of the countries the standard applies to
  (e.g. `["SE"]` for MMM-2R and MMM-3R); empty when it applies everywhere.

### 2026-11-14 (end profile plate)
- End profiles have an optional `plate`: the end plate as entered, a `start` point, `segments` of kind `line`
  (`end`), `arc` (`center`, `end`) or `fillet` (`radius`,
  optional `side`: `above` or `below`, the side of the edge its centre is on), and the `bottom`. When present, `outline` and
  `groundLine` are generated from it and still written, so readers that don't know `plate` are unaffected.

### 2026-11-14
- First version of the end profile, module and catalogue schemas, with the file envelope.
- Module files embed their end profiles and include the derived measures per variant.
- First version of the hall and hall plan schemas. Hall plans embed the hall, modules, end profiles,
  standards and overrides; module poses are computed from each layout's anchor and not stored.
