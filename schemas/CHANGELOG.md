# Schema change log

Changes to the drawing file schemas (spec EX-7). Within a major schema version, only backward-compatible
changes are allowed. Schema version 1 is a draft until it is frozen (implementation plan, Phase 12);
until then it may still change.

## Version 1 (draft)
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
