# Drawing file schemas

JSON Schemas (draft 2020-12) of the Tellurian Trains Drawing files. The **module** and **end profile**
files are public contracts (spec EX-7): the Module Registry will accept them for upload.

| File | Kind | Extension |
|---|---|---|
| [endprofile.v1.json](endprofile.v1.json) | `endProfile` | `.tdendprofile` |
| [module.v1.json](module.v1.json) | `module` | `.tdmodule` |
| [catalogue.v1.json](catalogue.v1.json) | `catalogue` | `.tdcatalogue` |
| [hall.v1.json](hall.v1.json) | `hall` | `.tdhall` |
| [hallplan.v1.json](hallplan.v1.json) | `hallPlan` | `.tdhallplan` |

Examples are in [examples/](examples/). Changes are logged in [CHANGELOG.md](CHANGELOG.md).

## Envelope
Every file is UTF-8 JSON with the same envelope (spec §6):

```json
{
  "format": "tellurian-drawing",
  "kind": "module",
  "schemaVersion": 1,
  "appVersion": "0.1.0",
  "content": { }
}
```

- `schemaVersion` is the **major** version of the content's schema. Within a version, changes are
  backward compatible: new properties are optional, and readers ignore properties they don't know.
- A reader refuses a file with a newer `schemaVersion` than it knows. This library upgrades older
  versions on load.
- `appVersion` is for information only.

## Conventions
- Lengths in millimetres, angles in degrees, counter-clockwise from the positive X axis.
- Property names and enum values are camelCase. Properties with no value are left out.
- A **point** is `{ "x", "y" }`. A **pose** is `{ "x", "y", "heading" }`; at an exit, the heading points out of the module.
- **Paths** (outlines, sides, ground lines, track centre lines) are a `start` point and `steps`, each starting where
  the previous one ends: `{ "type": "line", "end" }` or `{ "type": "arc", "center", "sweep" }` (sweep positive counter-clockwise).
  A closed path's last step ends at the start.
- Track elements have a `type` discriminator: `straight`, `curve`, `flex`, `turnout`, `crossing`, `doubleSlip`, `bufferStop`.
- Catalogue references (standard, add-ons, end profiles) are stored by id **and** by name, e.g.
  `{ "id": "…", "name": "FREMO-H0-B96" }`, so that a receiver can map them to its own data.
- Heights are offsets from the standard's rail-top height (0 = standard, normally 1300 mm).

### End profiles
Drawn in end view as seen from outside the module: X to the right, Y up, origin on the centre line at
rail top. Whether two ends fit is worked out from the drawings, not stored.

### Modules
Module coordinates have their origin at the first exit's end-plate centre, with X pointing into the module.
A module file contains:
- `module`: the module with all its variants;
- `endProfiles`: every end profile its exits and internal joints use, so a receiver can show and check the ends;
- `measures`: the derived measures per variant (length, straight, radius, angle, width, track length,
  number of sections, ramp height difference and gradient), which a receiver can compare with or take over
  as its own values (spec §10). They are written for receivers and derived again when the file is opened.

An auto-drawn variant has an `autoDraw` record with the generator version and the parameters it was drawn from.

### Halls
Hall coordinates are millimetres with X to the right and Y up. The `outline` is the walls as a closed path;
wall `i` is step `i`. Obstacles (pillars, furniture) and no-go zones are closed paths. A door has a `position`
(the centre of the opening on the wall, heading into the hall), a `width` and a `keepFreeDepth`.

### Hall plans
A hall plan file is self-contained: it embeds the `hall`, the placed `modules`, their `endProfiles`, the
`standards` and any compatibility `overrides`. Embedded modules are read-only copies with their `revision`.
- `layouts` hold the modules in `lines`. A line's `entries` are joined end to end: each entry's `entryExit`
  joins the previous entry's `exitExit`. A branch line has `startAt`, the free exit of a module in another line
  where it starts; `endAt` joins the last module to a free exit elsewhere (a rejoining branch or a loop).
- Module poses are **not stored**; they are computed from the layout's `anchorModuleId` and `anchorPose`
  (the anchor module's local origin and X axis in hall coordinates).
- Exits are referred to by module id and exit label. A module is placed at most once per plan.
- `joints` only lists joints whose properties the planner has set (join-method override, number of clamps,
  note). Joint fit is evaluated from the end profiles, not stored.

## Keeping the schemas up to date
The schemas are generated from the code (`DrawingFileSchema.Create`), and a test fails when the code and
the published files differ. After a deliberate change, check that it is backward compatible, then run

```bash
TELLURIAN_UPDATE_SCHEMAS=1 dotnet test --project Tellurian.Trains.Drawing.Model.Tests
```

to rewrite the schemas and examples, and add an entry to the change log.
