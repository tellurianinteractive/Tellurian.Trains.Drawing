# Drawing file schemas

JSON Schemas (draft 2020-12) of the Tellurian Trains Drawing files. The **module** and **end profile**
files are public contracts (spec EX-7): the Module Registry will accept them for upload.

| File | Kind | File name |
|---|---|---|
| [endprofile.v2.json](endprofile.v2.json) | `EndProfile` | `*.endprofile.json` |
| [module.v2.json](module.v2.json) | `Module` | `*.module.json` |
| [catalogue.v2.json](catalogue.v2.json) | `Catalogue` | `*.catalogue.json` |
| [hall.v2.json](hall.v2.json) | `Hall` | `*.hall.json` |
| [hallplan.v2.json](hallplan.v2.json) | `HallPlan` | `*.hallplan.json` |

The schemas of the earlier version are kept for files that still have it: [endprofile.v1.json](endprofile.v1.json),
[module.v1.json](module.v1.json), [catalogue.v1.json](catalogue.v1.json), [hall.v1.json](hall.v1.json) and
[hallplan.v1.json](hallplan.v1.json). They are no longer generated or checked. This library reads version 1 files and
writes version 2.

Drawing files are plain JSON. The kind in the file name is a convention; readers identify a file by its `format`
(`tellurian-drawing`) and `kind`. Files saved up to version 0.4 used the extensions `.tdendprofile`, `.tdmodule`,
`.tdcatalogue`, `.tdhall` and `.tdhallplan`; the content is the same.

Examples are in [examples/](examples/). Changes are logged in [CHANGELOG.md](CHANGELOG.md).

## Envelope
Every file is UTF-8 JSON with the same envelope (spec §6):

```json
{
  "format": "tellurian-drawing",
  "kind": "Module",
  "schemaVersion": 2,
  "appVersion": "1.2.3",
  "content": { }
}
```

- `schemaVersion` is the **major** version of the content's schema. Within a version, changes are
  backward compatible: new properties are optional, and readers ignore properties they don't know.
- A reader refuses a file with a newer `schemaVersion` than it knows. This library upgrades older
  versions on load.
- `appVersion` is for information only.
- A writer that changes a file must **keep the properties it doesn't know**, on every object. This library does.

## Conventions
- Lengths in millimetres, angles in degrees, counter-clockwise from the positive X axis.
- Property names are camelCase. Enum values are written as in the code, e.g. `Left`, `SignalControlled`, `Module`
  (the `kind` of the envelope). This library also reads enum values in any other case, such as the camelCase of
  version 1 files; the schemas accept only what is written. Properties with no value are left out.
- A **point** is `{ "x", "y" }`. A **pose** is `{ "x", "y", "heading" }`; at an exit, the heading points out of the module.
- **Paths** (outlines, sides, ground lines, track centre lines) are a `start` point and `steps`, each starting where
  the previous one ends: `{ "type": "line", "end" }` or `{ "type": "arc", "center", "sweep" }` (sweep positive counter-clockwise).
  A closed path's last step ends at the start.
- Track elements have a `type` discriminator: `straight`, `curve`, `flex`, `turnout`, `crossing`, `doubleSlip`, `bufferStop`.
  The `type` of a track element and of a path step is not an enum value and is written in lower case, as before.
- Catalogue references (add-ons, end profiles) are stored by id **and** by name, e.g.
  `{ "id": "…", "name": "FREMO-H0-B96" }`, so that a receiver can map them to its own data.
- Heights are offsets from the standard's rail-top height (0 = standard, normally 1300 mm).

### External references
What another system knows a drawing as is kept apart from the drawing's own properties, in `externalReferences`:
an object with one entry per system. Modules, end profiles, standards, halls, hall plans and catalogues can have one.

```json
"externalReferences": {
  "moduleRegistry": {
    "stationId": 1234,
    "moduleId": 5678
  }
}
```

- **Each system owns its entry**: it alone writes it and decides what is in it. The schema only says that the section
  is an object. This library reads an entry as JSON and writes it back unchanged.
- `moduleRegistry` is the Module Registry's. In a module it has the Registry's module id, `moduleId` (a number); in an
  end profile `endProfileId`; in a standard `standardId`. These three are what the migration from version 1 writes;
  the Registry may add more, such as `stationId`.
- Embedded copies carry their sections: the end profiles in a module file, and the modules, end profiles and
  standards in a hall plan.
- A **copy** of a drawing, with a new id, has no external references.

### End profiles
Drawn in end view as seen from outside the module: X to the right, Y up, origin on the centre line at
rail top. Whether two ends fit is worked out from the drawings, not stored.

### Modules
Module coordinates have their origin at the first exit's end-plate centre, with X pointing into the module.
A module file contains:
- `module`: the module with its properties, its `tracks`, its one `drawing` (an object) and, when it is an operation
  location, its `operationLocation`;
- `endProfiles`: every end profile its exits and internal joints use, so a receiver can show and check the ends;
- `module.packageLabel`: the package the module is transported in, usually a box; the modules of one box have the
  same label. The Module Registry sets it from its own data when it has a package for the module.
- `measures`: the derived measures of the drawing, one object (length, straight, radius, angle, width, track length,
  number of sections, ramp height difference and gradient), which a receiver can compare with or take over
  as its own values (spec §10). They are written for receivers and derived again when the file is opened.

A module is one complete module with one drawing. The drawing has no id, name or default flag. A module that can be
set up in several ways (a station with or without a middle section) is several modules, each with its own file and
`id`; only the Module Registry knows that they belong together. When a version 1 file is opened, the default variant,
or else the first, becomes the drawing, and other variants are left out.

An auto-drawn drawing has an `autoDraw` record with the generator version and the parameters it was drawn from.

An exit's `direction` is `Left`, `Right`, `Up`, `Down` or `Undefined`, as seen by a viewer standing at the operation
place. `Up` is the side away from the viewer and `Down` the side towards the viewer; they are for exits whose track
leaves across the left–right direction. An exit's `label` is free text for the reader only.

#### The module of the shared layout model
A module is the module of the shared layout model, the NuGet package `Tellurian.Trains.Schedules.Model.Layouts`, which
the planner, the Module Registry and Dispatch use too, with what this library adds. The names, units (metres) and
meaning of the shared properties are that contract's.

```json
"module": {
  "id": "32fa0819-9185-4b49-9ab2-a6cc7b19fe1f",
  "standardId": "5b6f0c1e-2a41-4d8e-9c3a-0f1d2e3a4b01",
  "name": "Station",
  "prefix": "SFjä",
  "number": 12,
  "hasFremoDesignation": false,
  "moduleOwners": "Module Club",
  "standard": { "shortName": "FREMO/H0-RE", "scale": { "shortName": "H0" } },
  "operationLocation": { "name": "Tellby", "signature": "Tby", "kind": "Station" },
  "tracks": [ { "id": 1, "designation": "1", "isMain": true, "isScheduled": true, "length": 3, "direction": "Both" } ],
  "drawing": { "trackElements": [ { "type": "flex", "stationTrackId": 1 } ], "tracksWithEnteredLength": [], "tracksWithEnteredStub": [] }
}
```

- **From the shared model:** `name`, `prefix`, `number`, `hasFremoDesignation`, `configurationLabel`, `packageLabel`,
  `moduleOwners`, `standard`, `operationLocation` and `tracks`.
- **Added by this library:** `id` (a GUID), `standardId` (the id of the standard in this library's catalogue, a GUID),
  `addOns`, the other properties of the module, `revision`, `revisionLog`, `externalReferences` and `drawing`.
- `prefix` and `number` form the designation, which this app writes with a space, e.g. `SFjä 0012`. A number needs
  the prefix; a module without a number needs a `name`. `name` is always written, as an empty text when not given.
- `moduleOwners` is one text with the names of all owners, separated by commas. The Module Registry keeps who owns a
  module and sets or overwrites the text, the prefix and `hasFremoDesignation` when a drawing is uploaded.
- `standard` names the standard by `shortName`, with its `scale` by `shortName`. Its `id` and the scale's `id` and
  `denominator` are the Module Registry's and are not written by this library. The scale is not a property of the module.

There are two lists about tracks:
- `drawing.trackElements` is the geometry: the track elements of the drawing, in millimetres.
- `module.tracks` is what train planning needs, without geometry: one entry per track with its `id` (a whole number,
  unique within the module, from 1), `designation`, `displayOrder`, `isScheduled`, `isLockable`, `keyName`, `isMain`,
  `isStub`, `length`, `platformLength`, `direction` (`Both`, `LeftToRight`, `RightToLeft`), `speedLimit` and `usage`.
  `length` and `platformLength` are in **metres**. A track element refers to its track by `stationTrackId`. `length`
  is the length a train can use, between the fouling points of the turnouts, and is written by the app at every save
  unless the track's id is in `drawing.tracksWithEnteredLength`; a reader never has to work it out from the geometry.
  `isStub` is likewise worked out from the drawing unless the track's id is in `drawing.tracksWithEnteredStub`.

Every module has the list of tracks, whether it is an operation location or not, with at least one track: a through
track or a track ending. The schema does not enforce this; the app reports a module with an empty list. Auto-draw
(generator version 1.2) writes the list: one main, scheduled track per generated track, called 1, 2 and so on.

#### Operation location
A module that is an operation location has `module.operationLocation`; a module without it is plain line. Only `name`
and `signature` are required.
- `name`, `signature` and `countryCode` (ISO 3166-1 alpha-2);
- `kind`: `Station`, `SignalControlled`, `IndustrialArea` or `Other`;
- `isShadow`, `trainsCanCross`, `isChangingTrainDirectionPossible`, `hasTurntable`, `isTerminus`, `isHarbour`,
  `hasCargoExchange`, `hasPassengerExchange`, `isKeyRequired`;
- `centre`: metres along the main track from the left end; missing means the middle.

The operation location is a type of the shared layout model, as the module and its tracks are.

### Halls
Hall coordinates are millimetres with X to the right and Y up. The `outline` is the walls as a closed path;
wall `i` is step `i`. Obstacles (pillars, furniture) and no-go zones are closed paths. A door has a `position`
(the centre of the opening on the wall, heading into the hall), a `width` and a `keepFreeDepth`.

### Hall plans
A hall plan file is self-contained: it embeds the `hall`, the placed `modules`, their `endProfiles`, the
`standards` and any compatibility `overrides`. Embedded modules are read-only copies with their `revision`.
- `layouts` hold the modules in `lines`. A line's `entries` are a list of modules joined end to end, each with
  `moduleId` and `isTurned`. Not turned: the module's left exit joins the previous module and its right exit the next.
  Turned 180°: the other way round. Only an entry of a module with three or more exits has `continuesFrom`, the exit
  the line continues from. A branch line has `startAt`, the free exit of a module in another line where it starts;
  `endAt` joins the last module to a free exit elsewhere (a rejoining branch or a loop).
- Module poses are **not stored**; they are computed from the layout's `anchorModuleId` and `anchorPose`
  (the anchor module's local origin and X axis in hall coordinates).
- A module is placed at most once per plan.
- `joints` only lists joints whose properties the planner has set (join-method override, number of clamps,
  note). Joint fit is evaluated from the end profiles, not stored.

#### How a hall plan refers to exits
A hall plan refers to an exit by its `side` and its `number` on that side, never by its label: `side` is the exit's
`direction` (`Left`, `Right`, `Up`, `Down` or `Undefined`) and `number` counts the exits with that direction in the
order of the drawing's `exits`, from 1. `{ "side": "Right", "number": 2 }` is the second right exit. `number` is 1
when left out.
- `continuesFrom` of a line entry and `exit` of the anchor's `referencePoint` have `side` and `number`; the module
  is the entry's or the anchor module.
- `startAt` and `endAt` of a line, and `first` and `second` of a joint, have `moduleId`, `side` and `number`.

Exit labels in a module drawing are text for the reader; nothing in a plan depends on them. Version 1 hall plans,
which used exit labels, are migrated when opened.

## Keeping the schemas up to date
The schemas are generated from the code (`DrawingFileSchema.Create`), and a test fails when the code and
the published files differ. After a deliberate change, check that it is backward compatible, then run

```bash
TELLURIAN_UPDATE_SCHEMAS=1 dotnet test --project Tellurian.Trains.Drawing.Model.Tests
```

to rewrite the schemas and examples, and add an entry to the change log.
