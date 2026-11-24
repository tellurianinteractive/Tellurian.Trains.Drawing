# Operation locations – tasks for Module Draw

Written 2026-11-18. These are the changes in this app that follow from the cross-system design in the Schedules repository: `Tellurian.Trains.Schedules/Documentation/OperationLocations.md` (called "the design" below, with its section numbers). Read against release 0.8.0.

Marks: **[free]** can be done now. **[model]** waits for the shared layout model (design 4.2).

## Changed direction, 2026-11-20

Read first: `Tellurian.Trains.Schedules/Documentation/CrossAppReview.md` (section 0 holds the owner's decisions) and `LayoutsLayerSurvey.md` (section 7 is the order of work). Where they differ from this document, they hold.

- **Module Draw's model moves into the shared model.** The geometry becomes its own assembly in Schedules (`Model.Geometry`); the drawing model, the catalogue and the reading and writing of every file kind go into `Model.Layouts`. Module Draw keeps `Editing`, `Components`, `App` and a few rendering helpers. No types of its own for a shared concept, no mapping (`ToShared()` goes), `ExitSide` replaces `ExitDirection`.
- **A hall plan is the shared model's `Layout` plus where it stands in a hall.** Module Draw's own `Layout` type goes.
- **So build new model, geometry and validation code in Schedules, not here.** Of the open tasks below, E1 to E4, F1 to F3 and G4 to G8 are now built in Schedules, and B2a, B3 to B5 and D3 have their model part there. What stays here is their user interface.
- **Decided 2026-11-21** (review decisions 10 to 17): aggregate roots with ids for every cross-reference; scale ids; the catalogue as a set merged by Guid; gauges as lists of widths on tracks (dual gauge); lines and joints in the hall plan; end plates as parts of the drawing with exits on them, the outline drawable before the exits (`ModuleDrawing.md` draft 0.3); exit directions read from how the drawing lies, which the user decides by turning it (`ModuleDrawing.md` §2.1). That replaces B2's inference by angle, and B3 to B5 become "turn the drawing": the viewing side is the bottom of the drawing, not derived from public or operator sides.
- The version numbers in the notes below (0.1.0 to 0.3.5) are of earlier shapes of the shared model. It is now 4.0, versioned with every package of the Schedules repository.

Done 2026-11-21: the survey's step 3 on Module Draw's side. Module Draw builds against `Model.Layouts` 4.0.4. A module's tracks and its location's tracks are one list, whichever property is set (4.0.4 fixed a loss of tracks when the editor set them separately). The tracks of a location module are written with the location. `isSignal` is gone from schemas and examples, and the change is logged in `schemas/CHANGELOG.md`. Done the same day: step 4. The geometry is the package `Tellurian.Trains.Schedules.Model.Geometry` (4.0.7), namespace `Tellurian.Trains.Schedules.Model.Geometry`; Module Draw's `Geometry` projects are gone. Next is step 5: the drawing files are read and written in Schedules.

Done 2026-11-21, step 5 sub-steps 1 and 2 (`CrossAppReview.md` decisions 18 to 20): reading and writing are separate packages, `Importers.Drawing` and `Exporters.Drawing` (4.0.11). The end profile with its parts and the drawing values moved to `Model.Layouts`; the end profile is a mutable class (values stay records). Module Draw's `DrawingFileSerializer` hands end profiles to the shared importer and exporter, so its callers didn't change; the end profile file is unchanged byte for byte. Next: the catalogue, then the hall.

Done 2026-11-21, sub-step 3 together with survey step 7 (review decisions 20 and 21): the catalogue (standards, add-ons, overrides, validation), the fit evaluator, comparison, analysis and templates are the shared model's; one `ModuleStandard` with a `Guid` id and optional properties; scales without gauges; gauges as lists on standards and end-profile tracks, with the 1->2 migration. `DrawingFileSerializer` hands catalogues to the shared importer and exporter too. The standard editor still edits a normal and a narrow gauge; names and prototype gauges, and a second gauge on an end-profile track (dual gauge), aren't editable yet. Next: the hall.

## Before starting: one new schema version

Several tasks change the file formats in ways that are not additions (renamed enum values, moved ids, removed owner properties, another shape of a hall plan line). The file formats are public and only additions are allowed within a major version.

- [ ] Collect **all** breaking changes of groups A, B, C and G in **one** schema version 2 per file kind, with one migration each in `DrawingFileMigrations`. Don't release them one by one as versions 2, 3, 4.
- [ ] Additions (the operation-location block, the layout block) can come later within version 2.

Phases 7 (track plan) and 9 (hall planning) of the implementation plan are not built yet. Groups C and G should be built this way from the start, not built first and changed after.

Done 2026-11-18 on branch `feature/schema-v2`: A1 to A7, B1 and B2, with the version 2 migrations in `SchemaVersion2`. Version 2 is not released until the breaking changes of C and G are in it too.
Done 2026-11-18, same branch: C1 to C7. Notes for what follows:
- The list is `stationTracks` on the variant; the track elements stay in `tracks`. Lengths in the list are in **metres** already (C8), since a change of unit later would not be an addition.
- The *Tracks* pane works with the track elements that exist. Drawing turnouts and sidings is the rest of phase 7; until then only the through tracks that auto-draw makes can be given to tracks in the app.
- C4: platform length is a property of the track. The `PlatformEdge` label is unchanged and not yet linked to a track.

Decided and done 2026-11-19, same branch. These change parts of the tasks below; where they differ, this is what holds:
- **No variants in Module Draw.** A drawing is one complete module, and optionally an operation location. A module that can be set up in several ways is several modules, each with its own file; only the Module Registry knows that they belong together. `module.drawing` replaces `module.variants`. Wherever a task below says "per variant", read "per module". D7 is withdrawn, and A4's list of Registry ids keyed by variant is one `moduleId`.
- **Every module has a track list** (`drawing.stationTracks`), at least one track: a through track or a track ending. The operation location is an optional block beside it, `module.operationLocation`, with the location's own facts only.
- **The shared model exists**: the package `Tellurian.Trains.Schedules.Model.Layouts` in the Schedules repository (project `Model.Layouts`), version 0.1.0 on the local feed. It has the operation location, its four kinds and their rules as checks (`OperationLocationRules`), the station track, and a first shape of the layout block (`Layout`, `LayoutLocation`, `LayoutTrack`, `TrackStretch`). Track names are `designation`, countries are ISO codes, lengths are metres. Module Draw's `StationTrack` is the shared one with `id`, `isLengthEntered` and `isStubEntered` added. This does C8, and the model and file format of D1. Built the same day: the *Operation location* pane of the module editor (D1, D2, D4, D8), with the centre as an entered distance. Not built: the centre as a marker in the drawing (D3), the station-name label showing the location's name (D5), and entry of the remaining module properties (D6).
- **G1 to G3 are done in the model and the file format**: a line entry has `isTurned` and, for three or more exits, `continuesFrom`; exits are referred to by side and number (`ExitPlace`, `ExitReference`). The hall plan editor is still phase 9.
- **Every module gets its track list** (done 2026-11-19; **changed 2026-11-21**: only an operation location has station tracks, and a module of plain line gets no list; see `ImplementationPlan.md`): auto-draw makes one main, scheduled track per generated track, called 1, 2 and so on (`StationTrackList`, generator version 1.2); a drawing without a list whose tracks are all generated gets one when it is opened in the module editor; a redrawn module with more tracks gets the new ones added; a module with an empty list is reported (`StationTrackProblemCode.NoTracks`).
- **The package label stays** (`module.packageLabel`): it is the box the module is transported in, not a variant concept. It can be entered in the *Properties* pane. Hall planning is to be package aware (spec HP-23).
- With this, **every change that isn't an addition is in version 2**. What remains (viewing side, operation location entry, assumed profiles, the layout block, schemas published) are additions.
- Before version 2 is released: publish `Tellurian.Trains.Schedules.Model.Layouts` to nuget.org (CI can't build without it), and remove `DrawingFileMigrations.UpgradeDraft`, which reads version 2 files written by development builds. *`UpgradeDraft` removed 2026-11-23 (survey step 11), with the draft step of `Importers.Drawing` (Schedules 4.0.27).*

Decided and done 2026-11-19, later the same day, same branch. Where these differ from the notes above, this is what holds:
- **The module is the shared model's module** (`Tellurian.Trains.Schedules.Model.Layouts` 0.2.0) with Module Draw's own parts added: `Module` derives from it. Name, prefix, number, configuration and package label, owners, standard, operation location and tracks are the shared contract's. Module Draw adds the GUID, `standardId` (the standard in its catalogue), its other properties and the drawing.
- **Owners are one text** with their names, separated by commas (`moduleOwners`), and the prefix is a property of the module. The `Owner` record and the list `owners` are gone. This replaces A5's list of prefix and name, and G9 shows the text and the prefix.
- **The track list is `module.tracks`**, plain shared tracks with whole numbers as ids, from 1. The drawing's track elements are `drawing.trackElements`, and the drawing lists the tracks whose length or stub mark the user has entered (`tracksWithEnteredLength`, `tracksWithEnteredStub`). Module Draw's own `StationTrack` is gone. Where the notes above say `drawing.stationTracks`, read `module.tracks`.
- **Enum values in files are written as in the code** (`Left`, `SignalControlled`), in every file kind: the form now follows the shared contract. Values in camelCase, as in version 1 files, are still read.
- **All five file kinds are at schema version 2**, hall files too; their shape is unchanged.
- **The shared model is `Model.Layouts` 0.3.5** (2026-11-19): mutable classes, the operation locations a closed hierarchy told apart by `kind`. Module Draw's `Module` is a class deriving from the shared one; the editor copies with `Copy()` and `With(change)` from the shared model in place of `with`, changes a kind with `OperationLocationRules.Conforming`, and reads earlier files through `OperationLocationJson.Normalise`.
- **The name of a module is the shared model's** (`Model.Layouts` 0.2.3, 2026-11-19): `Designation` (`SFjä 0012`, or `12` without a prefix), `Title` (designation and the name of the operation location, `SFjä 0012 Tellby`; a module that is no operation location is named by its designation; without a prefix the name and the number, `Curve 12`), `DisplayName` (standard and scale before the title, without repeated words) and `Scale` (that of the standard). Module Draw's own `FremoDesignation`, `Title`, `TitleOf` and `Scale` are removed; the name label, file titles and the new-module page use `Title`, and the end profile title uses `Wording.JoinWithoutRepeatedWords`.
- The rules of the shared model for tracks (`OperationLocationRules.Validate` for tracks) are not used: Module Draw's `StationTrackValidation` has its own codes, which also cover the drawing.

## A. File format: what belongs to other systems

Design 4.3, "External references" and "Owners".

- [x] **A1 [free]** An `externalReferences` section in every file kind: an object with one entry per system (`moduleRegistry`). The app reads each entry as JSON and writes it back unchanged. In the schema it is only an object.
- [x] **A2 [free]** Embedded copies carry the section: modules and end profiles embedded in a module file or a hall plan keep their `externalReferences`.
- [x] **A3 [free]** A copy loses the section: every function that makes a new module, variant-as-module, end profile or hall plan from an existing one gives new GUIDs and removes `externalReferences`. Test each such function.
- [x] **A4 [free]** Move the Registry ids of today into `externalReferences.moduleRegistry`, in the version 2 migration: `ModuleVariant.RegistryModuleId` (as a list keyed by the variant's GUID), `EndProfile.RegistryId`, `ModuleStandard.RegistryId`. Remove the properties from the model.
- [x] **A5 [free]** Owners hold the name and the FREMO prefix only. Remove `Owner.Share`, `RegistryPersonId` and `RegistryGroupId`, and drop them in the migration. Owners are a list, each entry with a prefix and a name (decided 2026-11-18). The first owner's prefix gives the FREMO designation, as today.
- [x] **A6 [free]** Preferably: keep every unknown JSON property when a file is saved (extension data on the model's records), so that a file from a newer app version, or with an addition from another system, isn't damaged by a save.
- [x] **A7 [free]** The revision counter stays the app's own. Nothing else writes it; the Registry keeps its mark in its own entry.

## B. Left and right

Design 4.3, "Left and right" and "Where track direction is assigned".

- [x] **B1 [free]** `ExitDirection` becomes Left, Right, Up and Down; Undefined stays for an exit not yet placed. West becomes Left and East becomes Right in the migration; the enum is written by name. Up is the side away from the viewer, Down the side towards the viewer.
- [x] **B2 [free]** Up and Down are inferred: when Left and Right are set, an exit whose track leaves the module across the left–right direction is Up or Down, from the angle of the track at the exit. The user can change it.
- [ ] **B2a [free]** Auto-draw labels new exits `L` and `R`, and `Up` and `Down` when needed, with a number where a side has several. Existing exit labels are not changed by the migration. (The compass letters came from some American modules; they are no longer used.)
- [ ] **B3 [free]** A **viewing side** on the module, the same for all its variants: the side the viewer stands on. It decides left and right, and from which side the end profiles are seen. Decided 2026-11-18, first match wins: (a) the non-public side, when the module has a public side on one side only; (b) the operation side, when it has an operator space on one side only; (c) the user decides. Until the user has decided in case (c), the side with the first exit on the left is used, which is what drawings mean today. Stored once derived, never derived again on load.
- [ ] **B4** A way for the user to choose the viewing side in case (c), and to see the viewing side in the drawing in every case.
- [ ] **B5** Changing the viewing side swaps left and right of the exits and turns the direction of every track, in one undoable step, and tells the user.
- [ ] **B6** Documentation: ModuleDrawing §4.1 and the help texts say left and right, with the viewing side.

## C. Tracks as a list (with phase 7)

Design 4.3 and 6.9, "Properties of a track".

- [x] **C1 [free]** A list of tracks per variant. Each track: designation, display order, scheduled, lockable, main track, stub, length, platform length, direction (left to right, right to left, both), speed limit, usage.
- [x] **C2 [free]** Track elements refer to a track of the list, in place of `TrackElement.Designation` and `IsMainTrack`. Migration: one track per distinct designation of a variant, with its elements pointing to it.
- [x] **C3** Length worked out from the elements of a track and written in the list. At a turnout the track ends where a train on the other track can still pass freely, the fouling point, not at the points (decided 2026-11-18). This needs the clearance between two tracks, from the standard. The user can enter another value.
- [x] **C4** Platform length as a property of the track. The `PlatformEdge` label becomes how it is drawn, not where the value is kept.
- [x] **C5** Stub suggested from the drawing (a buffer stop, or no connection at one end), and changeable.
- [x] **C6** A table of the tracks beside the drawing, where all properties are entered. Direction is chosen in terms of the exits, by name, with the drawing in view.
- [x] **C7** Validation: designations unique within a variant; elements without a track; a scheduled track that reaches no exit; a turnout shared by two tracks belongs to one of them.
- [x] **C8 [model]** The names, units and meaning of the track properties follow the shared model. Lengths in the list are in the model's unit, not millimetres.

## D. The operation location

Design 4.3, 4.4 and 6.9. Mostly **[model]**: the block is the shared model's location type, so its shape must be settled there first. D1 to D3 can be prepared in the user interface.

- [x] **D1 [model]** An operation-location block per variant: name, signature, country of operation, kind, centre, and the location's own facts (trains can cross, direction change possible, turntable, terminus, harbour, cargo exchange, passenger exchange, key required), with the track list of group C.
- [x] **D2 [model]** The four kinds (station, signal controlled, industrial area, other) and their rules as validation, the same checks as the planner and the Registry run (design 6.9, table of kinds). Changing kind clears what the new kind can't have, after asking.
- [ ] **D3** The **centre**: a distance along the main path from the left exit, per variant, by default half the main path. Shown as a marker that can be moved along the path. For a module with three or more exits, the distance from the centre to a branch exit is measured along the branch track.
- [x] **D4** A module without the block is plain line. A way to make a module an operation location and to remove it again. A signal module or junction module can be given a location of kind signal controlled.
- [ ] **D5** The station-name label shows the location's name, not a text of its own.
- [ ] **D6** Entry of all module properties, so that the file can be the source of truth (design 4.1). Go through the Registry's module properties and add what the app lacks; candidates seen so far: two-rail and three-rail, integrated LocoNet, signal feature, overhead line feature, speed limit, landscape season.
- [x] **D7** (withdrawn 2026-11-19: no variants) Variants: the default variant is the main one and should be the largest. A warning when another variant is longer. A new variant made from another copies its block and tracks; the user then removes what the shorter variant lacks.
- [x] **D8** Signature and country are entered here. Uniqueness within a country can't be checked by the app; within a hall plan it can (G5).

## E. Generated and assumed content

Design 6.5 and 4.1, condition 4.

- [ ] **E1 [free]** A flat default end profile for a standard and a width, as a function of the model library without user interface, so that the Registry can use it.
- [ ] **E2 [free]** A mark on an exit that its end profile is assumed, and on an exit that it was inferred. Shown in the drawing.
- [ ] **E3 [free]** Assumed profiles and inferred exits are left out of the fit check in hall planning, and reported as "not confirmed" instead of as fit or misfit.
- [ ] **E4 [free]** The notes that auto-draw makes about what it assumed are kept in the file and shown to the user. Confirming or replacing a profile clears its mark.

## F. Schemas

Design 6.8 and 6.2.

- [ ] **F1 [free]** Publish the JSON Schemas as static files with the app, at a fixed address, with kind and major version in the name (`module.v2.json`). The deploy workflow does it. Change Specification §6, which says the schemas are not deployed.
- [ ] **F2 [free]** Send an ETag, so a reader can ask whether a schema has changed.
- [ ] **F3 [free]** In `schemas/README.md`: the list of properties that the Module Registry reads and writes (design 6.2), marked as a contract that isn't moved within a major version. A test that fails when one of them moves.

## G. Hall plan (with phase 9)

Design 4.5 and 4.6.

- [x] **G1 [free]** A line is a list of modules in order, each with the variant used and a flag for being turned 180°. Not turned: the left exit joins the previous module. This replaces `LineEntry.EntryExit` and `ExitExit`.
- [x] **G2 [free]** For a module with three or more exits, the entry says which exit the line continues from, and a branch line says at which exit it starts, by side and number (Left 1, Right 2), not by label. `ExitReference` and `Joint` refer to a place in the plan in the same way.
- [x] **G3 [free]** Exit labels in module drawings are then free text for the reader only. Nothing in a plan depends on them.
- [ ] **G4** Updating an embedded module from a newer file keeps its place and flip, and tells the user what changed that matters to the plan (length, exits, tracks, owner).
- [ ] **G5** Validation: a signature occurs once in a plan, together with its country; a location without tracks; a location without a centre.
- [ ] **G6 [model]** The **layout block**, written at every save and ignored at load: the operation locations with their tracks; the track stretches (the two locations, the side each leaves from, the distance from centre to centre, the number of tracks from the end profiles); for each track its previous and next location; the plan's GUID and revision. In the shared model's types.
- [ ] **G7 [model]** Each location in the layout block carries its external reference, copied from the embedded module without interpreting it; without one, the module's GUID and variant.
- [x] **G8 [model]** ~~Only the layout block is a public contract of the hall plan file.~~ *Changed 2026-11-22 (survey step 8): the whole hall plan is the shared model's `HallPlan`, and its `layout` the contract's `Layout`; both are in the published schema.*
- [ ] **G9** The hall plan shows the owners of a module (name and prefix), for the one who has a question about it.

## H. Working with the Module Registry (later)

Design 6.3, 6.7 and 6.8. Waits for the Registry.

- [ ] **H1** Settings: the Registry's address and the user's API key, sent as `Authorization: Bearer`.
- [ ] **H2** Open a module from the Registry and save it back, so that local copies are the exception. A save that the Registry refuses because the module was changed there shows the differences.
- [ ] **H3** Import standards and end profiles with their drawings from the Registry into the catalogue (CA-5).
- [ ] **H4** Open a layout's hall plan from the Registry and save it back.
- [ ] **H5** Change Specification §10 to the design: the file is the source of truth; the Registry maps a few properties and reads no geometry.

## Suggested order

1. A1 to A7, B1 and B2, with the version 2 migration. Small, and everything else rests on it.
2. C1 to C7, as phase 7.
3. B3 to B6.
4. E1 to E4.
5. F1 to F3.
6. G1 to G5, as phase 9.
7. When the shared model is settled: C8, D1 to D8, G6 to G8.
8. H, when the Registry is ready.

## Open questions

None at the moment. Answered 2026-11-18: owners are a list of prefix and name; the viewing side is the same for all variants; the viewing side is the non-public side, else the operation side, else the user's choice; a track's length ends at the fouling point; exits are Left and Right, with Up and Down when needed.
