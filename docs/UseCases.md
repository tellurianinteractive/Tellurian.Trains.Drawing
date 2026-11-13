# Use Cases

Status: **Draft 0.1** (2026-11-13). Part of the [specification](Specification.md).

## Principles

1. **One drawing at a time.** A user works on one end profile, one module or one hall plan at a time.
2. **Files are the result.** The primary result of every drawing is its **JSON file**: an end profile, module, hall or hall plan file. Files are what users keep, share and upload to the Module Registry.
3. **IndexedDB is a workbench.** It holds work in progress: autosaved drafts, recently used files, and collections of imported modules for a hall plan. It is not the place where finished work is kept. The user is reminded to save a file when a draft has unsaved changes.
4. **Hall plans are built by ordering modules.** Almost all module layouts are **end to end**. So a hall plan is built by putting modules in a **sequence**, not by placing each module freely. The app computes every module's position from the previous one. **Branches are common**: layouts often contain junction modules (three or more end profiles) where a branch line starts, and branches often **rejoin** another line. Closed loops built from several modules are rare.

## Actors
There are no roles in the app (spec §4). The actors below are only typical situations.

| Actor | Typical situation |
|---|---|
| Module owner | Draws their own modules and uploads them to the Module Registry or another shared location. The owner can also provide the data to someone else who draws them (Registry data, measures, a sketch or photos). |
| Module drawer | Draws modules on behalf of one or more owners, from the data they provide, and uploads them to the Module Registry or another shared location. |
| End profile author | One of a **few selected persons**, e.g. appointed by FREMO or MMM, who draw the end profiles from the standards' specifications. The end profiles are published in the Module Registry or other shared location and then **used** in module drawings by everyone else. |
| Planner | Usually the meeting organiser. Collects module files and builds the hall plan. |
| Participant | Looks at a hall plan to see where their modules go. |

## Use case overview

| ID | Use case | Primary result |
|---|---|---|
| UC-1 | Draw an end profile (selected persons) | End profile file |
| UC-2 | Import end profiles from the Module Registry | End profiles in the local catalogue |
| UC-3 | Draw a module | Module file |
| UC-4 | Auto-draw a module from parameters | Module file |
| UC-5 | Resume work in progress | Continued draft |
| UC-6 | Draw a hall | Hall file |
| UC-7 | Collect modules for a hall plan | Module collection of the hall plan |
| UC-8 | Build a hall plan by ordering modules | Hall plan file |
| UC-9 | Branch at a junction module (common) | Branch line in the hall plan |
| UC-10 | Rejoin a line or close a loop | Joint with its closure gap reported |
| UC-11 | Validate and adjust a hall plan | A hall plan without errors |
| UC-12 | Print and share a hall plan | Printout, SVG/PNG or hall plan file |
| UC-13 | Upload a module drawing to the Module Registry or another shared location | Module data in the Registry (done in the Registry) |

---

### UC-1 Draw an end profile
**Actor:** end profile author. Most users never draw end profiles; they import them (UC-2).
1. The user creates a new end profile, from the template (plate width, track positions) or as a copy of an existing one.
2. They draw or adjust the plate outline, the ground line, the tracks and the screw holes. A PDF page of the standard can be used as a background for tracing (EP-11).
3. The app shows the derived properties and the symmetry live. The user can compare the profile with another one (EP-8).
4. The user **saves the end profile file**. The draft is autosaved in IndexedDB in the meantime.

### UC-2 Import end profiles from the Module Registry
**Actor:** module owner or module drawer, before drawing modules.
1. The user downloads end profile files from the Registry, as single files or a ZIP archive, and opens them in the app.
2. The app adds them to the local catalogue. If a profile already exists, the app shows the differences and the user chooses which to keep.
3. *Later phase:* the app fetches them directly from the Registry's API (CA-5).

### UC-3 Draw a module
**Actor:** module owner, or a module drawer working from data the owner has provided. The owners entered in the module are the real owners, not the person who draws.
**Precondition:** the end profiles that the module's exits need exist as drawings. If not, the user does UC-1 or UC-2 first.
1. The user creates a new module from a template, from parameters (UC-4), or from scratch.
2. They enter the module properties: name, standard, owners and so on.
3. They place the exits and choose their end profiles. The end plates appear.
4. The sides, through tracks and operator space are generated. The user draws by hand only where needed: a custom side, station track work, an operator space or a public side.
5. If needed, they add variants.
6. The user **saves the module file**. Drafts are autosaved in IndexedDB.

### UC-4 Auto-draw a module from parameters
1. The user enters the Registry-style parameters (straight, radius, angle, width, through tracks, sections, exits), or pastes them from the Registry.
2. The app generates the drawing and lists its **assumptions**, e.g. inferred exits or a default radius.
3. The user accepts, adjusts the parameters, or continues drawing by hand (UC-3).
4. The user **saves the module file**.

The Module Registry runs the same auto-draw on its server, without this app (MD-20).

### UC-5 Resume work in progress
1. On start, the app lists **drafts** with unsaved changes and **recent files**.
2. The user opens a draft and continues. When it's saved to its file, the draft is cleared.
3. A draft can also be discarded, after a confirmation.

### UC-6 Draw a hall
1. The user creates a hall from a rectangle or as a polygon. They add pillars, doors and no-go zones.
2. The user **saves the hall file**.

### UC-7 Collect modules for a hall plan
1. The planner gets the module files in one of three ways:
   - **as a ZIP archive** exported from the Module Registry, e.g. all modules registered for a meeting layout;
   - **from a shared folder**, e.g. a cloud-synced folder that the participants put their files in. The planner picks the folder, and the app reads all module files in it. Where the browser allows it, the app can re-read the folder later to pick up new or changed files.
   - as **single files**.
2. The app adds the modules to the hall plan's **module collection**. The collection is cached in IndexedDB.
3. The collection lists the modules with their owner, standard, length and number of exits. It shows which modules aren't used in the plan yet, and flags modules whose standard isn't accepted (HP-1) or whose end profiles are missing.
4. If a module file has a newer revision than the copy used in the plan, the app says so, and the planner can update it (HP-16).

### UC-8 Build a hall plan by ordering modules
**Precondition:** a hall (UC-6) and a module collection (UC-7).
1. The planner creates a hall plan for the hall, with the accepted standards.
2. They create a **line**: a sequence of modules joined end to end.
3. They add modules to the line from the collection, at the end or at any position.
4. The app computes the line: each module is joined to the previous one, at its *entry exit*, and the line continues from its *exit exit*. **The hall plan is redrawn immediately.**
5. The planner **moves modules up or down** in the line, or drags them in the list. The plan is redrawn after every change.
6. If a module is turned the wrong way, for example a curve that bends the wrong way, the planner **flips** it: its other end now connects to the previous module. The plan is redrawn.
7. The planner **positions the line in the hall**. They move and rotate it by its **anchor module**, which by default is the first module but can be any module of the line, e.g. a station that must stand in a particular place. The rest of the line is computed in both directions from the anchor.
8. A plan can have several lines, e.g. separate layouts in the same hall.
9. The planner **saves the hall plan file**. It embeds the hall, the used modules and their end profiles (spec §6). Drafts are autosaved.

### UC-9 Branch at a junction module
Branches are a normal part of most layouts.
1. A line contains a junction module with three or more exits. For that module the planner chooses which exit is the entry and which continues the line.
2. Each remaining free exit is shown as a **branch point**. With one click the planner **starts a branch line** there. The branch is anchored at that exit, so it moves with the junction module.
3. The planner builds the branch like any line (UC-8): adding, moving up or down, and flipping modules. Branches can have branches of their own.
4. The ordered list shows the layout as a **tree**: each branch is nested under the junction module where it starts, and can be collapsed.
5. Moving the junction module within its line, or flipping it, redraws all its branches. Choosing a different exit to continue the line swaps which way is the "main" line and which is the branch.

### UC-10 Rejoin a line or close a loop
Branches that **rejoin** another line are common, e.g. a triangle junction or two branches meeting at a station. A line joining its own start (a closed loop) is rare.
1. The planner joins the last module of a line to a chosen **free exit** elsewhere in the plan, in another line or in the same line.
2. The geometry rarely matches exactly. The app reports the **closure gap**: distance, angle and height (VA-5).
3. The planner adjusts the order, flips modules or swaps modules until the gap is within tolerance. Otherwise the gap stays as a reported problem.

A **loop module** at the end of a line (a return loop) is something else: it's a single, complete module with one exit, not a loop built from several modules (ModuleDrawing.md §3).

### UC-11 Validate and adjust a hall plan
1. The app validates continuously: joint fit, overlaps, clearance, operator spaces, walking paths, heights and add-ons (VA-…).
2. The planner works through the problems list: reorders, flips, moves lines, adjusts legs, or marks acceptable warnings as OK (HP-15).
3. The planner draws the public areas and walking paths (HP-17, HP-18).

### UC-12 Print and share a hall plan
1. The planner prints the plan at a scale or tiled on A4/A3 pages, or exports it as SVG or PNG. The module list per owner can be exported as CSV.
2. They share the **hall plan file**. Participants open it in the app to view it (UC-8 is not needed to view).

### UC-13 Upload a module drawing to the Module Registry or another shared location
**Actor:** module owner or module drawer. This use case is done in the Registry, not in the app; the Registry decides who may upload. A file can also be put in another shared location, e.g. the shared folder a planner reads (UC-7).
1. The owner uploads the module file to their module in the Registry.
2. The Registry validates it, stores it, and offers to update missing or different module data from the drawing (ModuleDrawing.md §8.8).
