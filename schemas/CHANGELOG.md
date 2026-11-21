# Schema change log

Changes to the drawing file schemas (spec EX-7). Within a major schema version, only backward-compatible
changes are allowed: additions that older readers can ignore. Changes that aren't additions are collected in one
new version per file kind, and files of the older version are upgraded when they are opened.

## Version 2 (not yet released; more changes will be added before it is)
All five file kinds: end profile, module, catalogue, hall and hall plan files. Version 1 files are read and
upgraded; files are written as version 2. See `docs/OperationLocationsTasks.md` for what is still to come in this version.

### 2026-11-21 (the module in the shared model)
Modules are read and written by the Schedules packages `Importers.Drawing` and `Exporters.Drawing` (4.0.22): Module
Draw's module is the shared model's `Module`, which the planner uses too (survey step 6a).
- **A module has no `theme`**: the text is removed when an earlier file is read, until what a module's theme means is
  settled with the Module Registry.
- A module's `id`, `standardId` and `drawing` are optional in the schema, since the planner's modules have none. Module
  Draw still writes all three in a module file and in a hall plan. A drawing with nothing in it isn't written.
- The module's properties are written in another order: the shared model's first (name, owners, standard, location,
  tracks), then Module Draw's, with `id` still first and `drawing` still last.

### 2026-11-21 (the hall in the shared model)
Halls are read and written by the Schedules packages `Importers.Drawing` and `Exporters.Drawing` (4.0.19); decision 10
of `CrossAppReview.md` (an entity inside a document has an `int` id unique within it).
- **Obstacles, doors and no-go zones have whole numbers as `id`**, unique among their kind in the hall, in place of
  GUIDs. In hall files and in the hall embedded in a hall plan. Earlier files are numbered 1, 2 and so on in the order
  of each list.
- **A hall plan's `obstacleId`** (in the anchor dimensions, `first` and `second`) is the obstacle's new number.
- Files in the earlier shape are still read, also version 2 files written by development builds.

### 2026-11-21 (catalogue, standards and gauges in the shared model)
The catalogue, its standards and add-ons and the end profiles are read and written by the Schedules packages
`Importers.Drawing` and `Exporters.Drawing` (4.0.16); decisions 11 to 13 of `CrossAppReview.md`.
- **An end-profile track has `gauges`**, a list of widths in mm, in place of `gauge`: one, or more for a dual-gauge
  track. In end profile files and wherever end profiles are embedded (module files, catalogues, hall plans).
- **A standard has `gauges`**, a list with `width`, optional `name` (e.g. "H0m") and `prototypeWidth`, the normal gauge
  first, in place of `normalGauge` and `narrowGauge`.
- **A standard's `scale` is the shared model's scale** (`{ "shortName": "H0" }`) in place of its short name as text, as a
  module's `standard` already was.
- A standard's properties beyond `id`, `shortName` and `scale` are optional: `railSystem`, `railTopHeight`,
  `operatorSpaceDepth`, `trackColor` and `countries` are left out when not stated (an empty country list means every
  country, as before), and their defaults apply where they are used.
- A scale's `denominator` may have decimals (1:43.5); a scale has no gauge.
- Files in the earlier shape are still read, also version 2 files written by development builds.

### 2026-11-21 (the shared layout model 4.0)
- **The tracks of a module that is an operation location are written with the location**, as
  `operationLocation.tracks`; `module.tracks` is written only for a module of plain line. A file with the tracks on
  the module and none in its location, as development builds of version 2 wrote it, is still read: the tracks become
  the location's.
- `isSignal` is removed from every kind of location (the planner no longer has it).
- New, from the shared model, and not written in a module or hall plan file today: `controlledById` and
  `cargoServedFromId` on a location, `heldAtId` on a lock key, and `operationLocationId` on a module (used where a
  module refers to a location written elsewhere, as in a layout).
- An empty `regionIds` is no longer written.

### 2026-11-19 (the shared layout model 0.3: the kinds of location are types)
- **An operation location is one of four types**, `Station`, `SignalControlled`, `IndustrialArea` and `Other`, told
  apart by `kind`, the first property. Each has only what its kind can have: `isShadow`, `isManned`, `hasTurntable`,
  `staffPosts`, `shiftCount`, `startLabels`, `splitDispatchByDirection`, `isPassengerInterchange` and `regionIds` only
  a station, `trainsCanCross` only a signal controlled location. A `kind` written in another case (`station`), not
  first, or missing (a station) is still read.
- **What is decided for a location in a layout is part of it**, from the planner: `id`, `owner`, `phoneNumber`,
  `instructions`, `isSignal`, `hideMeets`, `hidePassings`, `lockKey` (with `name`), `timings`
  (`minimumStopMinutes`, `locoRunaroundRealMinutes`, `trainClearanceRealMinutes`), `tracks` and `externalReferences`.
  A module file leaves them as they are when a location is added; a module's tracks are `tracks` of the module.
- **A track** has `previousLocationId`, `nextLocationId` and `appliesInBothDirections`, the route through its location
  that the planner decides, and `usage` is an empty text when not given. A track made from designated track elements of
  a version 1 file is written with `isScheduled: false`, as before: a track is scheduled unless it says otherwise.
- **The types of the shared model are described once in `$defs`**, as this library's own: `OperationLocation`,
  `StationTrack`, and `SharedModuleStandard` and `SharedScale` where the name is also this library's.

### 2026-11-19 (the module of the shared layout model)
- **A module is the module of the shared layout model**, the package `Tellurian.Trains.Schedules.Model.Layouts`,
  with this library's own parts added. In a module, in its own file or embedded in a hall plan:
  - `fremoNumber` is renamed `number`.
  - `owners` (a list with `name` and `prefix`) is removed. `prefix` is a property of the module: the prefix of the
    first owner. `moduleOwners` is one text with the names of all owners, separated by a comma and a space.
  - `standard` was a reference with `id` and `name`. Its `id` is moved to `standardId`, the id of the standard in
    this library's catalogue. `standard` is now the shared model's: `shortName` (the earlier `name`) and `scale`
    with `shortName`. Its own `id`, and the `id` and `denominator` of the scale, are the Module Registry's and are
    not written by this library.
  - `scale` (a text) is removed from the module. The scale is `standard.scale.shortName`.
  - `hasFremoDesignation` and `configurationLabel` are new, from the shared model. `packageLabel` and
    `operationLocation` are where they were.
  - `name` is required, as in the shared model. It is an empty text when the module is identified by prefix and
    number only.
  - `id` is written first and `drawing` last.
- **The list of tracks is `tracks` of the module**, not `stationTracks` of the drawing. A track's `id` is a whole
  number, unique within the module, from 1; it was a GUID. `isLengthEntered` and `isStubEntered` are removed from the
  track: the drawing has `tracksWithEnteredLength` and `tracksWithEnteredStub`, two lists of track ids. A track is the
  shared model's track without additions, so it also has `keyName`.
- **The track elements of a drawing are `trackElements`**, not `tracks`. Their `stationTrackId` is the whole number
  of a track. An element that referred to a track that isn't in the list refers to none.
- **Auto-draw `parameters`:** `fremoNumber` is renamed `number`; `owners` is replaced by `prefix` and `moduleOwners`,
  as in the module; `configurationLabel` is carried again and copied to the module.
- **Enum values are written as in the code**, e.g. `Left`, `SignalControlled`, `LeftToRight`, and `Module` as the
  `kind` of the envelope, in every file kind. This follows the shared layout model. Property names are camelCase as
  before, and the `type` of a track element or a path step is unchanged (`flex`, `line`). Values in the earlier
  form, e.g. `left`, are still read, in any case; the version 2 schemas list only the values as they are written.
- **Hall files are at version 2.** Their shape is unchanged; only their enum values are written in the new form.
  `hall.v2.json` replaces `hall.v1.json`, which is kept for files that still have version 1.
- When a version 1 file is upgraded, each distinct designation of its track elements becomes a track numbered from 1
  in the order they occur. Version 2 files written by development builds before this change are read and brought to
  this shape: their tracks are numbered from 1 in the order of the list.

### 2026-11-19 (one drawing per module, operation location, hall plan lines)
- **A module has one drawing.** `module.drawing` (an object) replaces `module.variants` (a list). The drawing has no
  `id`, `name` or `isDefault`. A module that can be set up in several ways is several modules, each with a file of
  its own; only the Module Registry knows that they belong together. When a version 1 file is upgraded, the default
  variant, or else the first, becomes the drawing, and other variants are left out.
  - `measures` of a module file is one object, not a list with one item per variant.
  - The Registry's entry of a module's `externalReferences` has `moduleId` directly: the `registryModuleId` of the
    variant that became the drawing. The `variants` list of the entry, written by development builds, is gone.
  - `configurationLabel` is removed from the auto-draw `parameters`.
  - A line entry of a hall plan has no `variantId`.
- **Operation location.** A module has an optional `operationLocation`: `name`, `signature`, `countryCode` (ISO 3166-1
  alpha-2), `kind` (`station`, `signalControlled`, `industrialArea`, `other`), `isShadow`, `trainsCanCross`,
  `isChangingTrainDirectionPossible`, `hasTurntable`, `isTerminus`, `isHarbour`, `hasCargoExchange`,
  `hasPassengerExchange`, `isKeyRequired` and `centre` (metres along the main track from the left end; missing means
  the middle). A module without it is plain line. This is an addition.
- **Shared layout model.** The operation location and the properties of a track in `stationTracks` (all but `id`,
  `isLengthEntered` and `isStubEntered`) are the types of the package `Tellurian.Trains.Schedules.Model.Layouts`, which the planner,
  the Module Registry and Dispatch use too. Their names, units (metres) and meaning are that contract's, and are not
  changed here. Every module has the list of tracks, whether it is an operation location or not.
- **Hall plan lines are lists of modules that are turned or not.** A line entry has `isTurned` in place of
  `entryExit` and `exitExit`: not turned, the module's left exit joins the previous module and its right exit the
  next. For a module with three or more exits, `continuesFrom` (`side`, `number`) says which exit the line continues
  from. When a version 1 plan is upgraded, an entry is turned when it was entered at a right exit.
- **A hall plan refers to an exit by side and number**, e.g. right 2: the second exit with direction `right`, in the
  order of the module's exits. `side` and `number` replace `exitLabel` in `startAt` and `endAt` of a line and in
  `first` and `second` of a joint, and the anchor's reference point has `exit` (`side`, `number`) in place of
  `exitLabel`. Exit labels are text for the reader only; nothing in a plan depends on them.
- Auto-drawn modules have their tracks in `stationTracks` (generator version 1.2): one main, scheduled track per
  generated track, called 1, 2 and so on, with its length. This is content, not a change of the schema.
- Version 2 files written by development builds before this date are read and brought to this shape.

### 2026-11-18 (external references, owners, left and right)
- **External references.** Modules, end profiles, standards, hall plans and catalogues have an optional
  `externalReferences`: an object with one entry per system, each owned by its system and written back unchanged
  (README, "External references"). It replaces the Registry ids of version 1:
  - a variant's `registryModuleId` is moved to the module's `externalReferences.moduleRegistry.moduleId`
    (2026-11-19: of the variant that becomes the module's drawing);
  - an end profile's `registryId` is moved to `externalReferences.moduleRegistry.endProfileId`;
  - a standard's `registryId` is moved to `externalReferences.moduleRegistry.standardId`;
  - `registryModuleId` is removed from the auto-draw `parameters`.
- **Owners** have `name` and `prefix` only. `share`, `registryPersonId` and `registryGroupId` are removed, also in
  the auto-draw `parameters`, and are dropped when a version 1 file is upgraded.
- **Exit directions** are `left`, `right`, `up`, `down` and `undefined`. `west` becomes `left` and `east` becomes
  `right`, also in the auto-draw `parameters`. `up` and `down` are new. Exit labels are not changed.
- **Unknown properties are kept.** This library now writes back the properties it doesn't know, on every object
  except points and poses. This is not a change of the schemas.

### 2026-11-18 (tracks as a list)
- **A list of tracks per module.** A module's drawing has an optional `stationTracks`: the tracks as train planning
  sees them, each with `id`, `designation`, `displayOrder`, `isScheduled`, `isLockable`, `isMain`, `isStub`, `length`,
  `platformLength`, `direction` (`both`, `leftToRight`, `rightToLeft`), `speedLimit` (km/h) and `usage`.
  **`length` and `platformLength` are in metres**, as in the Module Registry and in train planning; everything else in
  a module file is still in millimetres. `isLengthEntered` and `isStubEntered` say that the user has entered the value;
  otherwise the app works `length` and `isStub` out from the drawing at every save. A module without the list is
  plain line.
- **Track elements refer to a track of the list** by `stationTrackId`, in place of their own `designation` and
  `isMainTrack`, which are removed. When a version 1 file is upgraded, each distinct designation becomes
  a track, a main track if one of its elements was, and its elements point to it. Elements without a designation get
  no track, and their `isMainTrack` is dropped.
- A turnout, crossing or double slip is shared by the tracks that meet in it and refers to one of them.
- Module standards have an optional `foulingDistance` (mm): the centre distance at which trains on two tracks pass
  each other freely. A track's `length` ends there, at the fouling point, not at the points of a turnout. When it is
  missing, the track distance of the standard's default double-track end profile is used. This is an addition.

## Version 1
Version 1 was a draft while these changes were made.

### 2026-11-18 (external references in halls)
- Halls (and the hall embedded in a hall plan) have an optional `externalReferences`, as the other kinds have in
  version 2. Readers that don't know it ignore it, so hall files stayed at version 1 with this change. (They went to
  version 2 on 2026-11-19, with the enum values written as in the code.)

### 2026-11-17 (section lines with bends)
- Internal joints (in modules, and in the modules embedded in a hall plan) have an optional `bends`: the points where
  the section line bends between `start` and `end`, in order. Missing or empty means a straight line, as before. The
  sections are cut from the module's outline by these polylines. Readers that don't know it ignore it.
- Internal joints always fit: their end profiles are never compared (spec MD-4a, EP-8).

### 2026-11-17 (module entrances)
- Doors in halls (and in the hall embedded in a hall plan) have an optional `isModuleEntrance`: true when modules can
  be carried in and out of the hall through the door (spec HA-7). Missing means false. Readers that don't know it
  ignore it.

### 2026-11-16 (module identification)
- Modules have an optional `fremoNumber` (1–9999, Registry `FremoNumber`), and owners an optional `prefix`
  (the owner's FREMO prefix, e.g. `SFjä`, Registry `FremoOwnerSignature`). The first owner's prefix and the number
  form the FREMO designation, e.g. `SFjä 0012`. Readers that don't know them ignore them.
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
