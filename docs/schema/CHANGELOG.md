# Schema change log

Changes to the drawing file schemas (spec EX-7). Within a major schema version, only backward-compatible
changes are allowed. Schema version 1 is a draft until it is frozen (implementation plan, Phase 12);
until then it may still change.

## Version 1 (draft)
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
