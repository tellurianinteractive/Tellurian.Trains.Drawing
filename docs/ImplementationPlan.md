# Tellurian Trains Drawing – Implementation Plan

Status: **Draft 0.16** (2026-11-13). Based on [Specification.md](Specification.md) draft 0.23, [UseCases.md](UseCases.md) draft 0.1 and [ModuleDrawing.md](ModuleDrawing.md) draft 0.1.
Requirement IDs (e.g. `MD-6`) refer to the specification.

---

## 1. Architecture

### 1.1 Solution layout
Follows the solution conventions: `.slnx` file, shared `Directory.Build.props`, MSTest.Sdk tests on Microsoft Testing Platform.

```
Tellurian.Trains.Drawing/
├── Tellurian.Trains.Drawing.slnx
├── Directory.Build.props                 # net11.0, Version, Nullable, LangVersion latest
├── global.json                           # sdk 11.0.100, test runner = Microsoft.Testing.Platform
├── .github/workflows/build-and-deploy.yml
├── docs/                                 # Specification, ImplementationPlan, ADRs, schema/
├── Tellurian.Trains.Drawing.Geometry/    # Pure 2D math, no domain knowledge
├── Tellurian.Trains.Drawing.Model/       # Domain model, catalogue, fit evaluator, validation, layout graph, file format
├── Tellurian.Trains.Drawing.Editing/     # UI-independent editor state: documents, commands, undo/redo, snapping, tools
├── Tellurian.Trains.Drawing.Components/  # Razor class library: ALL view and editor components + their JS modules (reused by the Module Registry)
├── Tellurian.Trains.Drawing.App/         # Blazor WASM standalone PWA (AOT): shell, pages, storage, files, service worker
├── Tellurian.Trains.Drawing.Geometry.Tests/
├── Tellurian.Trains.Drawing.Model.Tests/
├── Tellurian.Trains.Drawing.Editing.Tests/
├── Tellurian.Trains.Drawing.Components.Tests/ # bUnit component tests (MSTest)
└── Tellurian.Trains.Drawing.App.Tests/   # bUnit tests of app pages
```

Dependencies run in one direction: `App → Components → Editing → Model → Geometry`. Only `Components` and `App` reference Blazor. Geometry, Model and Editing are plain `net11.0` class libraries, testable without a browser (NF-5).

**Reuse by the Module Registry (NF-10).** Geometry, Model, Editing and Components are packable libraries (NuGet). The Registry, which runs on Blazor Server, can:
- embed the **display components** to show end profile, module and hall plan drawings (EX-8);
- call the **auto-draw API** in Model on its server to generate module drawings from its own module data and store the JSON (MD-20).

The App holds only what is specific to the PWA: shell and pages, IndexedDB storage, file access, service worker and print views.

### 1.2 Key technical decisions
| Topic | Decision | Rationale |
|---|---|---|
| Hosting model | Blazor WebAssembly **standalone**, created from the template with `--pwa`. | No server (NF-1), offline (AP-1). |
| AOT | `RunAOTCompilation=true` **only in Release**. Debug builds use the interpreter for fast inner-loop builds. Requires the `wasm-tools` workload (installed locally; installed in CI). | AOT builds take minutes. |
| Trimming/size | Full trimming, Brotli pre-compression, `InvariantGlobalization=false` with only the 5 UI cultures loaded. | NF-3 first-load target. |
| Rendering | **SVG** rendered by Razor components. All coordinates are in mm, and pan/zoom uses the SVG `viewBox`/group transform. Dragged items render in a separate overlay layer. Static items use `@key` and `ShouldRender` to avoid re-rendering. | Crisp at any zoom, printable and exportable as-is (EX-1/2). Easy to hit-test via the DOM. |
| Reusable components | All view and editor components live in the `Components` Razor class library. They must work in **any render mode**: WebAssembly here, Interactive Server in the Module Registry. So there are no WebAssembly-only APIs (`IJSInProcessRuntime`, synchronous interop) and JS interop starts only after the first render. Each component's JS is a collocated module (`*.razor.js`) loaded on demand; read-only display needs no JS at all. Storage and file access stay in the App behind interfaces. | The Registry can embed the drawings (NF-10, EX-8). |
| One canvas, three editors | End profile, module and hall editors share the same canvas, tools, snapping and undo framework. They differ only in their tool sets and document types. The end view uses Y-up like the plan view, so there is no special case. | Less code. The editors behave the same way. |
| Pointer input | Small JS module (`canvas.js`) owns pointer capture, wheel and pinch-zoom. It updates `viewBox` directly during pan/zoom and sends drag deltas to .NET at most once per animation frame. | Keeps pan/zoom smooth without a .NET round trip per event (NF-2). |
| Serialization | `System.Text.Json` with **source generation** (`JsonSerializerContext`) and polymorphism via `[JsonDerivedType]` (track elements, segments, profile elements). | Trim/AOT safe. |
| Local storage | Own minimal IndexedDB JS module (put/get/delete/list per store, JSON strings). Call `navigator.storage.persist()` on first save. | No third-party dependency. Reduces Safari eviction risk. |
| Files | File System Access API (open/save in place) when available, otherwise `<InputFile>` plus a download. Manifest `file_handlers` for `.tdendprofile/.tdmodule/.tdhall/.tdhallplan/.tdcatalogue` (Chromium). | EX-4. |
| Localization | **Tellurian.Localization** (RESX + Markdown providers), plus **Tellurian.Utilities**. Changing culture stores the choice and reloads. | AP-2, AP-6. Matches the Module Registry. |
| Arcs | Outlines, ground lines and track centre lines are stored exactly (line + arc segments). Tessellation (chord tolerance 0.05 mm) is used only for overlap/clearance/shape comparison and hit-testing. | NF-8 precision, simple robust algorithms. |
| Spatial queries | Axis-aligned bounding boxes plus a uniform grid as the broad phase for snapping, overlap and clearance. | NF-2 with 300 modules. |
| Undo/redo | Command pattern in `Editing`. Each command operates on immutable model records and returns a new document version. | Simple undo, safe autosave. |
| Hosting | **Azure Static Web Apps** (Free or Standard tier). `staticwebapp.config.json` for navigation fallback, MIME types (`.wasm`, `.dat`, `.webmanifest`) and cache headers (`index.html`/`service-worker.js` no-cache, fingerprinted assets immutable). | Decided. |

### 1.3 Core geometry concepts (Geometry project)
- `Vector`, `Point`, `Angle` (degrees, normalised), `Pose(Point Position, Angle Heading)`.
- `Transform`: rotation + translation, composition, inverse. It has **no mirroring**, and it is used for placements.
- `Mirror`: a separate operation, used only to compare end profiles (X → −X about the centre line, spec §3.1). It is kept separate so that a module placement can never be mirrored by mistake.
- Segments: `LineSegment`, `ArcSegment` (centre, radius, start angle, sweep) with length, point-at, tangent-at and bounding box.
- `SegmentPath` (open/closed sequence of segments), `Polyline` (tessellated), polygon area/orientation, point-in-polygon, polyline–polyline intersection and minimum distance.
- **Shape comparison**: the Hausdorff-style maximum deviation between two tessellated paths. It is used for comparing ground lines (spec §3.2 rule 3) and for detecting symmetry.
- **Point-set matching**: matches two sets of points (screw holes, track positions) within a tolerance and reports the unmatched points.
- **Join math**: given a target exit pose `T` (world) and the moving module's exit pose `E` (local), the placement transform is `T ∘ Rot(180°) ∘ E⁻¹`.
- **Closure gap**: compare the two exit poses of a non-tree joint and return `(dx, dy, dθ)` in the frame of one exit.

## 2. Phases

Each phase ends with something working, tested and deployed to the staging environment. Estimates assume one developer working part-time with AI assistance and are only indicative.

### Phase 0 – Foundations and technical spikes (≈1–2 weeks) — ✅ done 2026-11-13
Results: ADR 0001 (SVG canvas), ADR 0002 (files first, per-browser fallback), ADR 0003 (localization). Production runs on Azure Static Web Apps, deployed with OIDC.
**Goal:** de-risk the platform before building features.
1. Initialise the git repository and create the GitHub repository (owner's decision on organisation/visibility). Add `.gitignore`, `README.md`, `LICENSE`, `CLAUDE.md`.
2. Create the solution, `Directory.Build.props` (net11.0, Version 0.1.0, NuGet metadata), `global.json`, all projects and test projects (empty), and the package references (Tellurian.Utilities, Tellurian.Localization). Geometry, Model, Editing and Components are packable (NF-10).
3. Create the App from `dotnet new blazorwasm --pwa`. Configure AOT for Release, trimming, compression, manifest (name, icons, theme colour), and the service-worker update prompt.
4. Create the Azure Static Web App resource and add `staticwebapp.config.json`.
5. **CI/CD** (`build-and-deploy.yml`):
   - on push and pull request: `dotnet workload install wasm-tools`, build, test;
   - on push to `master` with a new `<Version>`: `dotnet publish -c Release` and deploy `wwwroot` with `Azure/static-web-apps-deploy` (`skip_app_build: true`), then tag and create a release (same release model as the Module Registry);
   - pull requests get SWA preview environments.
6. **Spike A – canvas performance:** render 300 module-like SVG groups (arcs + tracks), pan/zoom/drag in the AOT Release build. Measure fps and drag latency in Chrome and Firefox (desktop is the editing platform, AP-3). Write an ADR with the results (confirm SVG, or fall back to canvas for the hall view).
7. **Spike B – localization in WASM:** verify Tellurian.Localization RESX + Markdown under trimming/AOT and culture switching.
8. **Spike C – files first, IndexedDB as workbench** (UseCases.md):
   - open and save a JSON file in place with the File System Access API, with a download/upload fallback (Firefox, Safari);
   - pick a **folder** and read all `*.tdmodule` files in it, then re-read it later with the stored handle (Chromium); multi-file fallback;
   - read module files from a **ZIP archive** with `System.IO.Compression` in WebAssembly;
   - drafts and recent file handles in IndexedDB, and the persistent-storage request;
   - manifest file handlers.

**Exit criteria:** an installable, offline-capable "hello" app deployed to Azure SWA from CI. Spike results recorded in `docs/adr/`.
**Requirements:** NF-1, NF-3, NF-4 (partly), AP-1 (skeleton).

### Phase 1 – Geometry library (≈1–2 weeks) — ✅ done 2026-11-13
Results: 89 unit tests, 98 % line coverage. The NF-2 distance check on 300 tessellated outlines runs as a timed unit test (< 200 ms in Debug); a separate benchmark project is still to be added.
1. Primitives, `Pose`, `Transform`, `Mirror` and the join math (§1.3).
2. Line/arc segments, paths, tessellation, bounding boxes.
3. Intersection, minimum distance, point-in-polygon, polygon offset for clearance visualisation.
4. Shape comparison (maximum deviation) and point-set matching.
5. Uniform-grid spatial index.
6. Thorough unit tests, including round-trip precision (NF-8) and degenerate cases (zero-length, 360° arcs, collinear).

**Exit criteria:** ≥ 90 % line coverage on Geometry. Benchmarks for the distance checks on 300 tessellated outlines are under the NF-2 budget.
**Requirements:** foundation for EP-7/8, MD-4..7, VA-1, VA-3, VA-5..7, NF-2, NF-5, NF-8.

### Phase 2 – Domain model, fit evaluator, catalogue and file format (≈2–3 weeks) — ✅ done 2026-11-14
1. Model records:
   - Scale, ModuleStandard, **StandardAddOn** (base standard), EndProfileCompatibilityOverride;
   - **EndProfile drawing**: outline, ground line, tracks (offset, gauge, rail top), screw holes, dowels, dimensions, labels, and the fallback screw-hole pattern identifier;
   - Owner, Module (shared properties, incl. add-ons met) → **ModuleVariant** (configuration label, default flag, optional Registry id) → **Section** (outline), **InternalJoint** (optional free internal end profile), Exit (outer ends only, with profile orientation and rail-top offset, 0 = standard height), TrackElement hierarchy (straight, arc, turnout variants, crossing, double slip, buffer stop), **OperatorSpace** (polygon), **PublicSide** (edge references), **DuckUnderArea** (polygon, optional clear height), Label;
   - Hall, Obstacle, Door, NoGoZone;
   - HallPlan (primary + accepted standards, preferred add-ons), Placement (with leg adjustment), Joint (fit, join method Bolted/Clamped, override flag, number of clamps), **PublicArea** (polygon), **WalkingPath** (centre line + width), **Acknowledgement** (finding type, involved placements, state fingerprint, note, time).

   *As built:* the stored placement properties (variant, entry/exit exit, leg adjustment, note) are on `LineEntry`, inside `Layout` → `Line`, because poses are computed from the layout's anchor (spec §3). A computed `Placement` comes with the layout engine in Phase 9. `Joint` stores only the planner's choices (join-method override, number of clamps, note); the fit is evaluated, not stored. The anchor lock is on `Layout`.
2. **End profile analysis**: derive plate width/height, track offsets and c/c, screw-hole positions and symmetry (drawing vs its own mirror image, listing the asymmetric elements) (EP-7 logic).
3. **Joint fit evaluator** (spec §3.2), a pure function `(endA + orientation, endB + orientation, overrides, tolerances) → (Fit, JoinMethod, differences)`.
   - Rules are evaluated in order: mirror A → tracks → screw holes (with identifier fallback) → ground line → fits (plate outline/height is ignored; all positions relative to rail top). Overrides are applied last.
   - It is used by the compare view and fit matrix (Phase 5), snapping and joints (Phase 9) and validation (Phase 10).
   - It has a table-driven test suite covering each rule and edge case, for example:
     - symmetric/symmetric;
     - asymmetric correctly and wrongly oriented;
     - symmetric/asymmetric;
     - single track across standards (clamps);
     - different double-track c/c;
     - missing screw holes.
4. Parametric track element geometry: each element produces centre-line paths and connection points (poses).
5. **Templates**:
   - end profile: rectangular plate with N tracks (EP-2 logic);
   - module structure per [ModuleDrawing.md](ModuleDrawing.md): exits → end plates → sides (Auto/Custom) → sections → tracks → zones; the auto side and through-track rules (MD-22);
   - module **auto-draw** (MD-20): a public, UI-independent API, `ModuleAutoDraw.Create(parameters, catalogue) → Module`. Its parameters mirror the Module Registry's module data: standard; length/straight, radius, angle and width; number of through tracks; number of sections; exits (label, direction, end profile); flags such as duck-under. It generates the sections, outline, exits, through tracks (on the end profile track positions) and, when flagged, the duck-under area. The result records the generator version and parameters. It covers straight, curved and straight+curve modules; the MD-3 templates use the same API. There are table-driven tests on Registry-like data, and the generated JSON is validated against the module schema.
6. Derived module measures (length, width, radius, angle, track length, ramp height difference and gradient) and comparison with the entered Registry-style values (MD-10 logic).
7. File envelope, `JsonSerializerContext`, schema version + migration pipeline (no-op v1 → v1), refusal of newer versions.
   **The module and end profile files are public contracts (EX-7)** for upload to the Module Registry:
   - JSON Schemas in `docs/schema/` (end profile and module first);
   - module files embed their end profiles;
   - catalogue references stored by value as well as by id;
   - a test that validates serialized samples against the schemas;
   - a schema change log;
   - sample files in `docs/schema/examples/` for the Registry developers.
8. **Built-in catalogue** JSON (embedded resource): scales and standards, plus end profile drawings for FREMO H0 (incl. H0-RE), MMM-2R, MMM-3R and FREMO N, so that the H0-RE ↔ MMM-2R clamp case and the 2R ↔ 3R incompatibility can be tested. Add-ons MMM-Summer and MMM-Modern on MMM-2R. Add overrides only where the rules give the wrong answer.
   Sources: the published PDF drawings from **FREMO** (all FREMO scales) and **MMM** (MMM-2R, MMM-3R and its add-ons). Each profile records its source document and revision. Phase 2 can use hand-entered test profiles; the real ones are drawn and checked in Phase 5.
9. Unit tests: serialization round trips, templates, derived measures, catalogue loading/merging (built-in + user, CA-2).

**Exit criteria:** end profiles and a module can be created from templates in code, saved to a file and loaded back identically. The fit evaluator classifies the built-in profile pairs as expected.
**Requirements:** CA-1, CA-2 (logic), EP-2/EP-7 (logic), MD-3 (logic), MD-10 (logic), §6 file format, EX-4 (logic), EX-7.

### Phase 3 – Files, workbench storage and app shell (≈1–2 weeks) — code complete 2026-11-14; manual exit check pending
**Files are the primary result; IndexedDB is the workbench** (UseCases.md, spec §6).
1. File service: open, save, save as for all file kinds, using the File System Access API with a fallback. Import from **ZIP archives** and **folders** (EX-9).
2. Workbench over IndexedDB:
   - **drafts**, autosaved per open drawing, with an "unsaved changes" flag;
   - **recent files**, with handles where possible;
   - the user catalogue and settings.

   Each stored item has a revision and modified time.
3. App shell: navigation. The start page shows the drafts with unsaved changes, recent files, and New / Open (UC-5).
4. The catalogue of end profiles and standards, including importing end profiles from Registry files or ZIP archives (UC-2). Copy a built-in end profile to a user profile.
4. Catalogue pages: list and edit scales, standards, standard add-ons and overrides (user layer).
5. Localization infrastructure in place: English master resources, language selector.
6. Update-available prompt (AP-1).
7. **Safe reload (AP-7)**: one `SafeReload` service that is used for every reload the app starts itself (language change, update). It flushes all open drafts to IndexedDB, waits for confirmation, stores the editor state (open documents, variant, selection, view), then reloads. On start it restores the editor state. If flushing fails, it cancels the reload and offers Save. Tests cover: a language change with unsaved edits in two open drawings, an update with an unsaved hall plan, and blocked storage.

**Exit criteria:** create a module from a template via the UI and save it as a file. Close the app with unsaved changes and resume the draft. Re-open the saved file from the recent files. Import end profiles from a ZIP archive.

*As built:* the file service has two implementations chosen at start-up (File System Access API; upload/download). ZIP and folder import, drawing identity, module templates (MD-3), end profile import with a choice per profile (UC-2) and catalogue validation live in `Model`. The app has the start page, a placeholder drawing page, the new-module page, the catalogue page with editors for standards, add-ons and overrides, export of the user catalogue (CA-3), the update prompt and `SafeReload`, and texts in all five languages. Restoring the editor state after a reload (AP-7) comes with the editors from Phase 4: until then, drafts are kept and offered on the start page. The exit criteria are covered by unit and bUnit tests with fake files; the real file pickers still need a manual check in Chromium and in Firefox or Safari.
**Requirements:** EP-1 (library part), MD-1, CA-2, CA-3, EX-4, EX-9, AP-1, AP-2 (infrastructure), AP-7.

### Phase 4 – Shared drawing canvas and editor framework (≈2–3 weeks)
1. `DrawingCanvas` component plus `canvas.js`: pan, wheel/pinch zoom, zoom to fit/selection, scale bar, grid (AP-3, AP-4).
2. Coordinate mapping screen ↔ world. Layered rendering (background, static, ghost, selection, overlay/drag, markers).
3. `Editing` project:
   - `EditorDocument<T>`, `ICommand`, undo/redo stack (MD-11, HP-6);
   - `ITool` state machine interface (pointer down/move/up, key) and selection model (single, multi, rubber band);
   - snapping engine (grid, endpoints, edges, connection points, exits), with candidates from the spatial index (MD-12);
   - generic **path tool** for closed and open line/arc paths, later reused for end-plate outlines, ground lines, module outlines and hall walls;
   - dimension tool and measure tool (EP-10, MD-12).
4. Autosave (debounced, per document) and recovery of unsaved work on start.
5. Property panel framework (forms bound to the selected object, numeric input with units).

6. All components live in the `Components` library. **Read-only display components** for end profiles, modules and hall plans (EX-8), taking the model or the file JSON, with layer options. The library stays render-mode agnostic so that this app (static WebAssembly) and the Module Registry (Blazor Server) can both use it (NF-10): no WebAssembly-only APIs, only asynchronous JS interop and only after first render, and read-only display without JS. A static-rendering test in the Components tests (`HtmlRenderer`, a JS runtime that fails on any call) checks every display component.

**Exit criteria:** a test document of simple shapes can be panned, zoomed, selected, moved with snapping and undone, with the mouse; pan and pinch-zoom also work with touch, for viewing. A module drawing renders read-only in the app and in the static-rendering test, without JS.
**Requirements:** AP-3, AP-4, MD-11, MD-12, EP-10 (dimensions), HP-6 (framework), EX-8 (framework), NF-10.

### Phase 5 – End profile editor and compare view (≈2 weeks)
This comes first among the editors because module exits (Phase 6) depend on end profiles.
1. End profile properties: designation, scale, standards, PDF/URL reference, Registry id (EP-1).
2. Template dialog: plate size and N tracks at c/c (EP-2).
3. Tools:
   - outline tool for the end-plate cross-section (EP-3);
   - ground line tool (EP-4);
   - track tool with rails and optional track bed (EP-5);
   - screw hole and dowel tools (EP-6);
   - dimensions and labels (EP-10).
4. Live derived-properties panel, symmetry indicator and mirror ghost highlighting the asymmetric parts (EP-7).
5. **Compare view** (EP-8):
   - pick two profiles and their orientations;
   - A is shown mirrored over B;
   - a coloured difference overlay with a difference list;
   - the fit result with colour and symbol.
6. Draw the built-in end profiles in the editor from the FREMO and MMM PDF drawings and verify them (dimensions, screw holes, track positions). This is easier with EP-11 (PDF page as background); consider pulling EP-11 forward into this phase.

**Milestone M1a:** a user can draw an end profile and see whether and how it fits another.
**Requirements:** EP-1..EP-8, EP-10.

### Phase 6 – Module editor: properties, sections, variants, exits (≈3 weeks)
1. Module properties panel, including the owners list (MD-2).
2. Template dialogs and "New module from parameters" (auto-draw, MD-3, MD-20). Auto-drawn modules can be re-generated from changed parameters until they are edited by hand. "Copy JSON" / "Save module file" make the result available for the Module Registry.
3. Section outline tool: add/move/delete vertices, convert an edge to an arc and back, numeric edit. Split and merge sections (MD-4).
4. Internal joints between sections, with an optional free internal end profile (MD-4a).
5. Variants: switcher; create, copy, rename, delete, set default (MD-13).
6. Exit tool (MD-5):
   - place on an outer section edge (perpendicular snap), with label, direction, end profile (warning if not one of the standard's profiles), profile orientation for asymmetric profiles, and numeric pose;
   - draw the end plate at the exit with the profile's width and track positions;
   - side panel showing the exit's end profile drawing.
7. Exit rail-top offsets and ramp indication (MD-7a).
8. Operator space tool ("add along edge" with depth, then polygon editing, on one or both sides), public side marking, and the duck-under area (whole module, selected sections or a polygon; kept in sync with the duck-under flag) (MD-17, MD-18, MD-19).
9. Labels and markers (MD-9).
10. Derived measures panel per variant with difference flags (MD-10).
11. Module problems list (precursor of VA).

**Milestone M1b:** a user can draw a multi-section module with variants and exits, and save, export and import it, with its end profiles embedded.
**Requirements:** MD-2, MD-3, MD-4, MD-4a, MD-5, MD-7a, MD-9, MD-10, MD-13, MD-17, MD-18, MD-19.

### Phase 7 – Track plan (≈2–3 weeks)
1. Track element palette and placement. Connection-point snapping between elements and to exit track positions (MD-6).
2. Parametric editing (length, radius, angle, turnout type/hand/angle) and rotate/flip of elements within the module. (Mirroring a track *element* is allowed; mirroring a *module* is not.)
3. Exit/profile track checks with tolerance (MD-7).
4. Track attributes and station track data (MD-8).
5. Total track length in derived measures.

**Exit criteria:** a two-track station module with turnouts can be drawn whose tracks land exactly on the end-profile positions.
**Requirements:** MD-6, MD-7, MD-8.

### Phase 8 – Hall editor (≈1–2 weeks)
1. Hall from a rectangle or polygon walls, reusing the path tool (HA-1).
2. Pillars (rectangle/round), fixed furniture, doors/emergency exits with keep-free zones, no-go zones (HA-2).
3. Hall properties and default aisle width (HA-3).
4. Hall library and reuse. Embedded copy in hall plans (HA-5).

**Requirements:** HA-1, HA-2, HA-3, HA-5.

### Phase 9 – Hall planning (≈3 weeks)
Hall plans are built by **ordering modules into lines** (UseCases.md UC-7…UC-10). There is no free placement of single modules.
1. Create a hall plan (hall + primary standard + additional accepted standards, editable later). Settings for tolerances and clearance (HP-1).
2. **Module collection** (HP-2, UC-7): import from a ZIP archive, a shared folder (re-read for changes) or single files. A list with filters (standard, add-ons, owner, not used yet) and flags (unaccepted standard, missing end profiles). Cached in IndexedDB.
3. **Line model and layout engine** in `Model`, unit-tested without UI:
   - `Layout` (anchor module, anchor pose, anchor dimensions, lines), `Line` (ordered entries, optional start at the free exit of another line's module, optional end at a free exit elsewhere) and `LineEntry` (module, variant, entry exit, exit exit);
   - `LayoutEngine.Compute(layout, …) → poses`: starts at the **anchor module** and works outwards through all lines (in both directions along the anchor's line, then into branches from their parent exits), joining each entry's entry exit to its neighbour's exit exit with the join math (§1.3). Rejoining lines and loops report their closure gap (VA-5);
   - tests: straight and curve chains, flips, anchors in the middle, branches (including branches of branches, swapping the main line and a branch, and moving or flipping a junction module with its branches), loops that close and loops that don't.
4. **Ordered list UI** (HP-3, HP-4, HP-19): a **tree**, with branch lines nested under their junction module and collapsible. Add from the collection, remove, move up/down, drag in the list, flip (2 exits), choose entry/exit exits (≥ 3 exits), start a branch at a branch point, switch variant (HP-7). The plan is redrawn immediately (NF-2: < 100 ms). Selecting in the list and selecting in the drawing are kept in sync.
5. **Anchor and anchor dimensions** (HP-5, HP-21): choose the anchor module; drag or rotate it in the drawing (step rotation) or enter its pose, and the whole layout follows; lock. **Anchor dimension tool**: pick a reference point on the anchor and two walls or obstacles. It shows the distances and the angle as dimension lines, and typing a distance or angle moves the anchor. Uses the minimum-distance functions from Phase 1. Several layouts per plan. Check dimensions (HP-22, should). Branch lines (HP-19). **Joining a line's end to another exit** (HP-20): rejoining branches and closed loops, with the closure gap (VA-5).
6. Leg adjustment per placement with a marker in the drawing (HP-13).
7. **Joint fit feedback (HP-11):**
   - the fit is evaluated for every joint after each change;
   - markers in green ✓, yellow !, yellow + clamp (*tving*) symbol, or red ✕, in the drawing, in the list, and in print/export;
   - per-joint override of the join method;
   - editable number of clamps.

   **Inspect joint (HP-12):** opens the compare view for the joint's two exits in their actual orientations.
8. Embedding of module drawings (with revision), end profiles and catalogue entries in the hall plan file. Difference resolution on open (EX-5).
9. **Read-only modules (HP-16)**:
   - placements reference an immutable embedded module snapshot;
   - the hall plan editor exposes only placement and joint commands, so there are no module-editing commands in the plan;
   - "Open in module editor" goes to the library copy;
   - a "newer revision available" indicator, with a diff and an "Update" command that replaces the snapshot and re-validates the plan.
10. Operator spaces, public sides and duck-under areas (from the module drawings) rendered for placed modules, with layer toggles (HP-14).
11. Public area tool (polygon, reusing the path tool) and walking path tool (centre line + width, snapping to module edges, duck-under passage symbol) (HP-17, HP-18).
12. Performance check against NF-2 with a generated 300-module plan.

**Milestone M2:** modules from a Registry ZIP archive can be ordered into an end-to-end layout with a branch, placed in a hall, saved and shared as one file.
**Requirements:** HP-1..HP-7, HP-11, HP-12, HP-13, HP-14, HP-16, HP-17, HP-18, HP-19, HP-20, HP-21, EX-5, NF-2.
Loop closure is *report only* (VA-5). Adjustable modules (MD-16) are a later item. They can be added later without breaking the file format, by adding optional parameters to the module.

### Phase 10 – Validation engine and problems list (≈2–3 weeks)
Validation logic lives in `Model` and is unit-tested without UI. Parts of it can start in parallel with Phases 6–9.
1. `IValidationRule` pipeline, incremental where cheap (re-validate the changed placements and their neighbours), with a full run under 200 ms (NF-2). Fit results are cached per profile pair and orientation.
2. Rules:
   - VA-1 joint fit (via the evaluator) and join-method overrides; end-profile switcher modules;
   - VA-2 scale consistency and accepted standards;
   - VA-14 preferred add-ons;
   - VA-3 joint geometry tolerance;
   - VA-4 open ends;
   - VA-5 loop closure gap, including height;
   - VA-6 overlaps (modules, walls, obstacles, door zones, no-go zones);
   - VA-7 clearance;
   - VA-10 height at joints, with suggested leg adjustment;
   - VA-11 operator space conflicts (overlap with other operator spaces, baseboards, walls, obstacles, door and no-go zones, public areas);
   - VA-13 walking paths: the corridor (centre line buffered by half the width) against baseboards minus duck-under areas (error), obstacles and zones (warning); duck-under passages as info; VA-7 clearance measured from operator space edges (switchable).
3. **Acknowledgements (HP-15)**: findings carry a stable key (type + involved placement ids). "Mark as OK" stores an acknowledgement with a fingerprint (hash of pose, variant, leg adjustment and module revision of each involved placement). On each validation run:
   - finding present and fingerprint equal → acknowledged;
   - finding present and fingerprint differs → invalidated, shown again as "previously acknowledged";
   - finding gone → the acknowledgement is removed.

   Unit tests cover move-but-still-overlapping, move-apart, variant switch and module revision change.
4. Problems list UI with severity filter, zoom-to and "Mark as OK". Markers in the canvas (joint badges, gap arrows, clearance dimensions, overlap hatching, acknowledged state).
4. A test suite of small hall plan fixtures, one or more per rule.

**Requirements:** VA-1..VA-7, VA-10, VA-11, VA-13, VA-14, HP-15.

### Phase 11 – Reports, export and print (≈2 weeks)
1. Summary bar and hall plan summary, including modules per standard and the number of clamped joints and clamps needed (RP-1).
2. Module list per owner with CSV export (RP-2).
3. SVG export (serialize the rendered drawing with print styles) and PNG export (SVG → canvas → blob in JS) for end profiles, modules and hall plans (EX-1).
4. Print view (EX-2):
   - chosen scale (1:1 for end profiles) or fit-to-page;
   - A4/A3 tiling with overlap and registration marks;
   - title block with scale bar and legend;
   - the hall plan's **set-up plan** with the anchor dimensions (HP-21) and check dimensions (HP-22);
   - CSS `@page`.

**Requirements:** RP-1, RP-2, EX-1, EX-2.

### Phase 12 – Localization, help, hardening, release 1.0 (≈2 weeks)
1. Translations: de, da, sv, nb resources and help Markdown (AP-2, AP-6).
2. Accessibility pass: keyboard navigation, focus, contrast (NF-9).
3. Cross-browser test round (NF-4): editing in Chrome, Edge, Firefox and Safari on desktop; viewing on iPadOS/iOS and Android.
4. Performance and size review against NF-2/NF-3, including the first-load time from Azure SWA.
5. Privacy statement (NF-7), about page, release notes. Set the version to 1.0.0 and deploy.
6. Freeze **end profile and module schemas v1** (EX-7). Publish them with the app (e.g. `/schema/endprofile.v1.json`, `/schema/module.v1.json`) and hand them over to the Module Registry for its upload feature.
7. Publish the libraries (Geometry, Model, Editing, Components) as NuGet packages, version 1.0.0, for the Module Registry (NF-10). Iterate them first through the local NuGet feed while integrating with the Registry.

**Milestone M3:** release 1.0 with all **M** requirements.

### Phase 13 – "Should" requirements (release 1.x, ≈3–5 weeks)
In priority order (to be confirmed):
1. EP-9 fit matrix; "Level from here" (HP-13).
2. VA-12 public side check and VA-8 enclosed areas (reachability, with duck-under passages).
3. HP-8/HP-9 colouring by owner/state/status, plus layers.
4. RP-3 joint list, RP-4 placement numbering.
5. VA-9 track continuity across joints.
6. EX-3 PDF (browser print-to-PDF first), EX-6 workspace backup.
7. EP-11/MD-14/HA-4 background images with calibration, including PDF pages (one shared mechanism; EP-11 possibly already in Phase 5).
8. EP-6 dowels (if not done in Phase 5), AP-5 dark theme.

### Later (separate plan)
- **Module Registry upload of module files and end profile drawings** (spec §10). This is Registry-side work in its own repository, based on the published schemas. End profile drawings are stored with the Registry's EndProfile entity. This app needs no changes beyond keeping the schemas stable.
- **Registry import in this app** (spec §10): a read-only Registry API for the catalogue, including end profile drawings (CA-5), and possibly for downloading module files. Access control stays in the Registry; the app has no roles of its own. CORS for the SWA origin is required.
- MD-15 DXF import, HP-10 plan templates.
- MD-16 adjustable modules.

## 3. Traceability

| Requirement | Phase |
|---|---|
| CA-1 | 2 |
| CA-2, CA-3 | 2 (logic), 3 (UI) |
| CA-4 | replaced by EP-* |
| CA-5 | Later |
| EP-1 | 3 (library), 5 |
| EP-2, EP-7 | 2 (logic), 5 (UI) |
| EP-3 … EP-6, EP-8 | 5 |
| EP-9 | 13 |
| EP-10 | 4 (tool), 5 |
| EP-11 | 13 |
| MD-1 | 3 |
| MD-2, MD-4, MD-4a, MD-5, MD-9, MD-13, MD-17, MD-18, MD-19 | 6 |
| MD-3, MD-10, MD-20, MD-22 | 2 (logic), 6 (UI) |
| MD-21 | 2 (standard colour), 4 (rendering) |
| MD-6, MD-7, MD-8 | 7 |
| MD-7a | 2 (logic), 6 (UI) |
| MD-11, MD-12 | 4 |
| MD-14 | 13 |
| MD-16 | Later |
| MD-15 | Later |
| HA-1, HA-2, HA-3, HA-5 | 8 |
| HA-4 | 13 |
| HA-6 | replaced by HP-17 |
| HP-1 … HP-7, HP-11, HP-12, HP-13 | 9 (HP-6 framework in 4; "Level from here" in 13) |
| HP-14, HP-16, HP-17, HP-18, HP-19, HP-20, HP-21 | 9 (printed in 11) |
| HP-22 | 9 (should) |
| HP-15 | 10 |
| HP-8, HP-9 | 13 |
| HP-10 | Later |
| VA-1 … VA-7, VA-10, VA-11, VA-13, VA-14 | 10 (fit evaluator in 2, VA-3 snapping in 9) |
| VA-8, VA-9, VA-12 | 13 |
| RP-1, RP-2 | 11 |
| RP-3, RP-4 | 13 |
| EX-1, EX-2 | 11 |
| EX-3, EX-6 | 13 |
| EX-4 | 2 (logic), 3 (UI) |
| EX-9 | 0 (spike C), 3, 9 |
| EX-5 | 9 |
| EX-7 | 2 (schemas), 12 (freeze v1) |
| EX-8 | 4 (framework), 5, 6, 9 (per drawing kind) |
| AP-1 | 0 (skeleton), 3 |
| AP-2 | 3 (infrastructure), 12 |
| AP-3, AP-4 | 4 |
| AP-5 | 13 |
| AP-6 | 12 |
| AP-7 | 3 (safe reload), 12 (language switcher uses it) |
| NF-1, NF-3 | 0, 12 |
| NF-2 | 0 (spike), 1, 9, 10, 12 |
| NF-4 | 0, 12 |
| NF-5 | 1, 2, 4, 10 |
| NF-6 | 0, 4 |
| NF-10 | 0 (packaging), 4 (Server host check), 12 (NuGet publish) |
| NF-7 | 12 |
| NF-8 | 1, 2 |
| NF-9 | 12 |

## 4. Risks and mitigations
| Risk | Impact | Mitigation |
|---|---|---|
| SVG rendering too slow for large hall plans | Hall plan editor sluggish | Spike A in Phase 0: 1000 modules without problems on desktop (ADR 0001). Overlay layer for dragging, `ShouldRender` discipline. Canvas fallback for the hall plan view if needed. |
| AOT build time and download size | Slow CI, slow first load | AOT only in Release. Trimming + Brotli. Measure size in CI and fail on large regressions. |
| Built-in end profiles drawn incorrectly from the FREMO/MMM PDFs | Joint fit colours are wrong | Record the source document and revision per profile. Dimension check against the PDF, ideally reviewed by someone from FREMO/MMM. Users can override (CA-2). |
| Drawn end profiles too imprecise for the automatic comparison (e.g. ground lines drawn roughly) | False yellow results | Separate, looser landscape tolerance (spec §3.2). Templates and snapping to exact values. Differences are explained in the compare view. |
| Safari IndexedDB eviction / no File System Access | Data loss, clumsy saving | Persistent storage request. Prompt users to export regularly. Download/upload fallback. Workspace backup (EX-6). |
| Viewing on tablets and phones is poor | Participants can't read the plan at the meeting | Pointer Events for pan and pinch-zoom; a check on a tablet and a phone in Phase 12. Editing on touch devices is not a goal (AP-3). |
| Schema changes break Registry uploads | Uploaded files rejected or misread | Schema test in CI, change log, only backward-compatible changes within a major version, coordinated releases with the Registry. |
| Scope creep in track editing (CAD-like) | Delays | Keep to parametric elements (MD-6). No flex-track/easement in release 1. |
| Localization library behaviour under trimming | Missing strings at runtime | Spike B in Phase 0. Trimmer roots for resource assemblies if needed. |

## 5. Definition of done (per feature)
- Logic is in Geometry/Model/Editing with unit tests. The UI is verified in a browser (Debug and the AOT Release build before each release).
- English resources added. Strings for other languages are added in Phase 12, or immediately if available.
- No new warnings. CI green. Deployed to the SWA preview environment for review.
- Release notes updated for user-visible changes.

## 6. Next steps
1. Review this plan and the open questions in the specification (none open at the moment).
2. Decide the GitHub repository location (organisation, name, visibility).
3. Start Phase 0.
