# Tellurian Trains Drawing – Specification

Status: **Draft 0.21** (2026-11-13) – for review. Implementation plan: [ImplementationPlan.md](ImplementationPlan.md).
Requirement IDs (e.g. `MD-12`) are stable and are referenced from the implementation plan.
Priority: **M** = must (first release), **S** = should (first release if time allows), **L** = later.

---

## 1. Purpose

A progressive web app (PWA) that anyone can use to:

1. **Draw end profiles**: the cross-section of a module end, with its end plate, track positions, screw holes and ground line.
2. **Draw modules**: the baseboard outline, the track plan and the module ends (exits) with their end profiles.
3. **Plan a layout in a hall**: put modules in order, so the app joins them end to end, place the result in a drawing of the hall, and check that the layout fits physically and technically.

**All drawings are 2D.** End profiles are drawn in *end view*. Modules, halls and hall plans are drawn in *plan view* (from above).

The app is a client-only **AOT-compiled Blazor WebAssembly standalone app**. It runs offline and stores data in the browser. Users share their work by exporting and importing files.

## 2. Scope

### In scope (first release)
- End profile drawing editor, with fit comparison between profiles.
- Module drawing editor (outline + track plan + exits).
- Local catalogue of scales, module standards and end profiles, bundled with the app and editable by the user.
- Hall editor (walls, pillars, doors, no-go zones).
- Hall planning: collecting module files (ZIP, shared folder), ordering modules into lines, flipping, branching, validating joints, clearance checks, loop closure checks, statistics.
- Local storage, file import/export, printing and export.

### Out of scope (first release)
- Integration with the Module Registry. A read-only import is planned for a later phase (§10).
- User accounts, rights/roles, servers, cloud sync and real-time collaboration.
- Submitting modules to meetings and managing meeting registrations (done in the Module Registry or as the meeting organiser decides).
- 3D of any kind, scenery details (buildings, roads, water), electrical/DCC wiring.
- Running operations or timetables on the layout.

## 3. Domain model

Terms and properties follow the **Module Registry** so that a later import maps one-to-one. Registry names are given in *italics* where they differ.

| Concept | Description | Key properties |
|---|---|---|
| **Scale** | Model scale. | Short name (H0, N, 0…), denominator (87, 160…). |
| **Module standard** | A standard that modules follow, e.g. FREMO H0-RE, MMM-2R or MMM-3R. | Short name, scale, track system, normal/narrow gauge (mm), couplings, electricity, wheelset. **New:** height of rail top above floor (Swedish *RÖK*, default **1300 mm**, which is the most common value), default minimum clearance (mm), default operator space depth (mm), **track colour** (default blue `#1f5fbf`), minimum radius for generated reverse curves, **default end profiles** (single and double track) and **default curve radius**, used when auto-draw has to infer missing Registry data (ModuleDrawing.md §8.6). |
| **Standard add-on** | Extra requirements on top of a base standard, e.g. **MMM-Summer** and **MMM-Modern**, which are both add-ons to MMM-2R. A module can meet zero or more add-ons of its standard. A hall plan can prefer add-ons, so that the planner sees which modules don't meet them. | Short name, base standard, description, optional PDF/URL reference. |
| **End profile** (*ModuleEndProfile*) | The shape of a module end, **drawn in end view** (§5.2). How well two ends fit is worked out from the drawings by the rules in §3.2. | Designation, scale, standard(s), optional PDF/URL reference, optional Registry end profile id. **Drawing:** end-plate outline, ground line, tracks (lateral position, gauge; rail top at Y = 0), **screw holes**, optional dowels and cable passages, dimensions and labels. **Derived from the drawing:** plate width and height, number of tracks, track offsets and c/c, screw-hole positions, symmetry (*Symmetric* / *Asymmetric*, with which elements are asymmetric). **Fallback:** a screw-hole pattern identifier, for when holes are not drawn. End profiles of the same standard may have different plate heights. That doesn't affect fit; the **screw-hole positions** (relative to rail top and centre line) are what matter. |
| **End profile compatibility override** | An optional catalogue entry that overrides the rule result for a specific pair of profiles, for known exceptions. | Pair (A, B), forced result (§3.2), note. |
| **Owner** | A person or group that owns (part of) a module. | Name, optional share (0–1), optional Registry person/group id. |
| **Module** (drawing) | One physical module, drawn as **one drawing**. It consists of **one or more sections** and has **one or more variants**. | Name (*FullName*), standard, **add-ons met** (of that standard), scale (from the standard), owners, theme, period (*RepresentsFromYear/UptoYear*), number of through tracks, flags (2R/3R, duck-under, turntable, end of line, end-profile switcher, signal module, stand-alone), functional state, landscape state, note, package label, optional Registry module id, variants. **Derived per variant:** number of sections (*NumberOfSections*), length, width, straight length, radius, angle (comparable with the Registry values); **ramp** (exits with different rail-top offsets) with height difference and gradient (‰). |
| **Module variant** (*ConfigurationLabel*) | An alternate drawing of the same module, e.g. a different section arrangement or track plan. Module properties are shared; geometry is per variant. Exactly one variant is used when the module is placed in a hall plan. | Name/configuration label, default flag, sections, internal joints, track plan, exits, operator spaces, public sides, labels. |
| **Operator space** | An area next to a module where operators stand, typically along a long side of a station module. A module can have operator spaces on **one or both sides**. It is **part of the module drawing**; conflicts between operator spaces are only checked between modules placed in the same hall plan (VA-11). | Polygon in module coordinates (normally a rectangle attached to an outline edge, with length and depth), label/note. |
| **Public side** | A side of the module that the owner intends to face the public, important for meetings open to the public. It is **part of the module drawing**. | One or more outline edges marked as public. |
| **Duck-under area** | The part of a module that people can pass under (Registry flag *IsDuckunder*). It is **part of the module drawing**. | Polygon in module coordinates (by default the whole outline of the marked sections), clear height under the baseboard (optional, mm). |
| **Section** | A physically separate baseboard part of a module. | Label, outline (closed path) in module coordinates. |
| **Internal joint** | The fixed joint between two sections of the same module. The owner designs the **internal end profile** freely; it does not have to follow any standard. | The two sections, position along their common edge, optional internal end profile drawing (user end profile, not tied to a standard). |
| **Exit** (*ModuleExit*) | An **outer** module end that can connect to another module. Its end profile **must conform to the module's standard**. | Label (e.g. "A", "East"), direction (*Eastwards/Westwards/Undefined*), end profile, **profile orientation** (*Normal* / *Mirrored*, only meaningful for asymmetric profiles), **rail-top offset** (mm relative to the standard's rail-top height; **0 = standard**, e.g. 1300 mm above floor; non-zero only on ramp modules), **pose** (position + outward heading in module coordinates). |
| **Hall** | A room where a hall plan is built. | Name, outline (polygon), obstacles, doors, no-go zones, notes. |
| **Hall plan** | A hall plus the modules placed in it. | Name, hall, primary standard, **accepted standards** (one or more, e.g. FREMO H0-RE and MMM-2R), **preferred add-ons** (e.g. MMM-Modern), **module collection**, **lines**, **public areas**, **walking paths**, acknowledgements, settings (tolerances, clearance, path width). |
| **Module collection** | The module files available to a hall plan, imported from a ZIP archive, a shared folder or single files (UC-7). | Modules (read-only copies with revision), source (ZIP / folder / files), import time. |
| **Line** | A sequence of modules joined **end to end**. This is how almost all layouts are built. The positions of its modules are **computed**, not placed one by one. | Ordered entries, **anchor** (one of its entries, by default the first, with a pose in hall coordinates), optional **start at** a free exit of a module in another line (a branch), **closed** flag (rare loop), name. |
| **Line entry** | One module in a line. | Module and variant, **entry exit** (joins the previous entry), **exit exit** (continues the line). For a 2-exit module these are swapped by a **flip**. |
| **Public area** | An area of the plan where visitors have access. | Polygon, label/note. |
| **Walking path** | A path for train drivers to walk along while following their trains. It may pass under duck-under parts of modules, but not through other modules. | Centre line (open path of line/arc segments), width (default 600 mm, configurable per plan), label. |
| **Placement** | One module, in one of its variants, placed in a hall plan, i.e. a line entry seen together with its computed pose. The module drawing is **read-only** in the plan; only the placement's own properties can be changed there. | Reference to an embedded (read-only) module drawing and its revision, chosen variant, **computed** pose in hall coordinates, **leg adjustment** (mm, default 0; raises or lowers the whole module), locked flag, note. |
| **Joint** | Two placed exits connected to each other. Joints between consecutive line entries are **exact by construction**; only loop closures and branch reconnections can have a gap. | Exit A, exit B, **fit** (§3.2: *Fits* / *Fits with mismatch* / *Fits with clamps* / *Incompatible*), **join method** (*Bolted* / *Clamped*), measured gap (mm) and angle error (°). |
| **Acknowledgement** | A planner's decision that a specific warning in a hall plan is OK. It is valid only as long as the situation it was given for is unchanged. | Finding type, involved placements, a **fingerprint** of their state when acknowledged (pose, variant, leg adjustment and module drawing revision), note, time. |
| **Clamped joint** (Swedish *tving*) | A joint whose tracks line up but whose screw holes don't, so the ends are held together with clamps. This is typical for single-track modules of different standards, e.g. FREMO H0-RE and MMM-2R. Shown with a clamp symbol at the junction. | Number of clamps (default 2), note. |

### 3.1 Geometry conventions
- Units are **millimetres** and **degrees**, and all values are stored at full model size (no scaling to the real world).
- **Plan view (modules, halls, hall plans).**
  - Each module variant has a **local coordinate system**. The X axis runs along the module's main direction, Y is perpendicular to it, and the origin is at the "first" exit's centre point. All sections of the variant are drawn in this system, assembled as they are when the module is set up.
  - An **exit pose** is a point (the centre of the end plate at the joint face) plus a heading that points **out of** the module.
  - Two exits are **joined** when their positions coincide and their headings are opposite (180°).
  - Modules are rigid bodies. A placement may be **translated and rotated, never mirrored**, because a physical module can't be mirrored.
  - **Height** is not part of the 2D geometry but carried as data, always as an **offset from the standard's rail-top height** (0 = standard, normally 1300 mm above floor). Each exit has a rail-top offset (ramp modules) and each placement a leg adjustment. The **effective rail-top offset** of an exit in a hall plan = exit offset + placement leg adjustment + (standard's rail-top height − hall plan primary standard's rail-top height).
- **End view (end profiles).**
  - The end face is drawn **as seen from outside the module, looking at it**.
  - X is lateral (positive to the right), Y is vertical (positive up).
  - The origin is on the end-plate centre line, and **Y = 0 is the top of rail** (Swedish *RÖK*, *rälsens överkant*). Everything else (plate edges, screw holes, ground line) is measured from it, so profiles with different plate heights are compared correctly.
  - The height of rail top above the floor comes from the standard (default 1300 mm). It is used to show floor-related heights, but it plays no part in comparing profiles.
  - This view links to the plan view through the exit pose: the end view's X axis is the exit's lateral axis.
- **Facing ends.** When two module ends are joined, each end is seen from outside its own module, so A's right side meets B's left side. To compare two profiles, A's drawing is **mirrored about its centre line** (X → −X) and laid over B's drawing.

### 3.2 Joint fit rules
Each joint is classified by laying the **mirrored drawing of end A over the drawing of end B**, taking the exit orientations into account (§3.1). The rules are evaluated top-down and the first one that matches decides the result. A catalogue override (§3) for the pair, if present, replaces the result.

| # | Condition | Result | Colour | Join method |
|---|---|---|---|---|
| 1 | **Track geometry differs**: number of tracks, track positions (i.e. c/c between double tracks), gauge or 2R/3R don't match within the joint tolerance. | **Incompatible** | Red | – |
| 2 | Tracks match, but the **screw holes don't line up** (e.g. single-track modules of different standards). | **Fits with clamps** | Yellow + clamp symbol | Clamped |
| 3 | Tracks and screw holes match, but the **ground line doesn't match**, e.g. asymmetric profiles turned the wrong way. | **Fits with mismatch** | Yellow | Bolted |
| 4 | Tracks, screw holes and ground line match. | **Fits** | Green | Bolted |

Notes:
- **Screw holes decide how ends are joined.** If the screw holes don't match, the ends are either incompatible (the tracks don't line up, rule 1) or joined with clamps (rule 2).
- **Plate size is not a fit criterion.** Plates of different height (or width) are compared only through their screw holes, tracks and ground line. The plate outline is drawn and shown in the compare view for information only.
- **Tolerances.** Track positions and screw holes use the joint tolerance (default 0.5 mm). The ground line uses a looser landscape tolerance (default 2 mm). Both are configurable.
- **Screw holes match** when every hole in the mirrored set A lies on a hole in set B, and vice versa. Positions are relative to rail top and the centre line. If either profile has no drawn holes, the screw-hole pattern identifiers are compared instead. If neither is available, the result is reported as "screw holes unknown" (warning) and the joint is assumed to be bolted.
- **Symmetry is derived automatically.** A profile is symmetric when its drawing equals its own mirror image within tolerance. The orientation setting on an exit only matters for asymmetric profiles.
- **Colour is never the only signal.** Each fit has its own symbol as well (✓, !, clamp, ✕), for colour-blind users and black-and-white prints.

## 4. Users and main use cases

The use cases are described in detail in [UseCases.md](UseCases.md).

**Anyone can use the app** to draw end profiles, modules, halls and hall plans. The app has **no accounts, rights or roles**: every user can do everything with the data on their own device.
Roles and access rights exist only **outside** the app. For example, the Module Registry defines which module data a user may see or change, and a meeting organiser decides how modules are submitted to a meeting.

Typical scenarios (not roles):

| Scenario | Description |
|---|---|
| Draw an end profile | One of a few selected persons (e.g. appointed by FREMO or MMM) draws an end profile from a standard's specification sheet, and publishes it with the end profile in the Module Registry (§10). Everyone else imports it and uses it in module drawings. |
| Check end profile fit | Someone compares two end profiles to see whether and how they fit, e.g. before joining modules of different standards. |
| Draw a module | The module owner, or someone drawing on the owner's behalf from data the owner provides, draws a module and saves it as a module file. The owner uploads it to the Registry. They can share it in any way they like, and in a coming release they can upload it to the Module Registry (§10). |
| Draw a hall | Someone draws a hall and shares the hall file. |
| Plan a layout in a hall | Someone collects module files and builds and validates a hall plan in a hall. They then print it or share the hall plan file. Typically this person is a meeting organiser. |
| View a hall plan | Someone opens a shared hall plan file, for example to see where their modules go. |
| Maintain catalogue data | Someone edits standards, end profiles and overrides and shares a catalogue file. |

**Submitting modules to a meeting is not part of the app.** It is done through the Module Registry or in whatever other way the meeting organiser decides, such as e-mailing module files. The app only reads and writes the files.

## 5. Functional requirements

All three editors (end profile, module, hall) share the same drawing canvas: pan/zoom, grid, snapping, selection, measurement, undo/redo and autosave (MD-11, MD-12, AP-3, AP-4 apply to all of them).

### 5.1 Catalogue (CA)
| ID | Pri | Requirement |
|---|---|---|
| CA-1 | M | The app ships with a built-in catalogue (JSON, versioned) of scales, module standards, **end profile drawings** for the supported standards, and any compatibility overrides. The end profiles are drawn from the standardisation bodies' published PDF drawings: **FREMO** for FREMO end profiles in all scales, and **MMM** (MälarModulMöte) for its standards MMM-2R and MMM-3R. The catalogue also contains the add-ons MMM-Summer and MMM-Modern. Each built-in profile references its source document and revision. |
| CA-2 | M | Users can add and edit their own standards, standard add-ons, end profiles and overrides. User entries are kept separately from the built-in ones and survive app updates. A built-in end profile can be copied and then edited as a user profile. |
| CA-3 | M | The catalogue can be exported and imported as a file. |
| CA-4 | – | *Replaced by EP-1…EP-9.* |
| CA-5 | L | The catalogue can be imported from the Module Registry (§10). |

### 5.2 End profile drawing (EP)
| ID | Pri | Requirement |
|---|---|---|
| EP-1 | M | Create, open, duplicate, rename and delete end profile drawings. Show them in a library tab, searchable by designation, scale and standard. Each profile has a designation, scale, standard(s), optional PDF/URL reference and optional Registry id. |
| EP-2 | M | **Template**: a rectangular end plate (width, plus the distances from rail top to the plate's top and bottom edges) with N tracks at given lateral positions or c/c, the gauge taken from the standard, and the standard's screw-hole positions if known. The rail top (Y = 0) is always shown as a reference line labelled *RÖK* / *Top of rail*. |
| EP-3 | M | **End-plate outline**: a closed path of line and arc segments (the cross-section of the baseboard end), edited like the module outline (MD-4). |
| EP-4 | M | **Ground line**: an open path showing the landscape surface across the end, e.g. track bed, ballast shoulders, embankments and ditches. |
| EP-5 | M | **Tracks**: positioned by lateral offset, with gauge and rail top level. Rails and an optional track bed/ballast profile are drawn. Tracks snap to the grid and to c/c values. |
| EP-6 | M | **Screw holes**: circles with diameter, positioned numerically relative to rail top and centre line, or by snapping. Optional **dowels/alignment pins** and **cable passages** (S). |
| EP-7 | M | **Derived properties** are shown live: plate width/height, number of tracks, track offsets and c/c, screw-hole positions, and symmetry. If the profile is asymmetric, the asymmetric parts are highlighted by showing the mirror image as a ghost. |
| EP-8 | M | **Compare view**: pick two end profiles (and orientations). Profile A is shown mirrored over profile B as they meet at a joint. Both are aligned on rail top and the centre line. Differences in tracks, screw holes and ground line are highlighted (plate outlines are shown for information), and the §3.2 result is shown. |
| EP-9 | S | **Fit matrix**: a table with the §3.2 result for every pair of end profiles in a scale (both orientations for asymmetric profiles). Selecting a cell opens the compare view. |
| EP-10 | M | Dimensions (horizontal/vertical/aligned) and text labels on the drawing. |
| EP-11 | S | A background image can be placed and calibrated by two points, for tracing (same mechanism as MD-14). A page of a **PDF** (such as a FREMO or MMM end profile drawing) can be used directly as the background. |

### 5.3 Module drawing (MD)
The structure of a module drawing (exits → end plates → sides → sections → tracks → zones), the auto-draw rules and the visual style are defined in detail in [ModuleDrawing.md](ModuleDrawing.md).

| ID | Pri | Requirement |
|---|---|---|
| MD-1 | M | Create, open, duplicate, rename and delete module drawings. Show them in a searchable library (by name, owner, standard, flags), with their variants. |
| MD-2 | M | Edit module properties (§3, Module), including owners. |
| MD-3 | M | **Quick-create templates**: a straight module (length × width), a curved module (radius, angle, width) and a straight+curve module. Each template creates the outline, exits and through tracks according to the chosen end profile. Templates use the auto-draw function (MD-20). |
| MD-4 | M | **Sections and outlines**: a module has one or more sections. Each section's outline is a closed path of line and arc segments, edited by vertex handles and numeric input. A section can be split in two along a line, and adjacent sections can be merged. Sections are labelled and visually separated. |
| MD-4a | M | **Internal joints**: where two sections meet, there is an internal joint. Its internal end profile can optionally be drawn as a user end profile, freely designed by the owner and not checked against any standard. Internal joints are fixed and never offered for snapping in a hall plan. |
| MD-5 | M | **Exits** (outer ends): every exit must reference a **drawn** end profile, so end profiles are drawn or imported before modules are drawn. Add exits to section edges that are not internal joints, and edit them. Each has a label, direction, end profile and, for asymmetric profiles, the profile orientation. The end profile **must be one of the module standard's end profiles**; otherwise the editor shows a warning. An exit snaps to an outline edge and is perpendicular to it by default, and the end plate is drawn at the exit with its profile width. Its pose can also be entered numerically. The exit's end profile drawing can be shown in a side panel. |
| MD-6 | M | **Track plan** with track elements: straight, arc, turnout (left, right, curved, wye, three-way, with parametric length/angle/radius), crossing, double slip, buffer stop. Each element has connection points, and elements snap to each other and to exit track positions. |
| MD-7a | M | **Ramp modules**: the rail-top offset of each exit can be set (default 0 = standard height). If the exits differ, the module is shown as a ramp with its height difference and gradient along the main track. |
| MD-7 | M | The track positions at an exit come from its end profile drawing. The editor warns if a track ends at an exit off a profile position (tolerance configurable, default 0.5 mm) or if a profile track position has no track. |
| MD-8 | S | Track attributes: designation, main/siding, and normal or narrow gauge. Optional station track data (max train length, platform length), following the Registry *StationTrack*. |
| MD-9 | M | Text labels and simple markers (platform edge, station name, operator side) on the drawing. |
| MD-20 | M | **Auto-draw from parameters**: many simple straight and curved modules can be drawn from the data the Module Registry already has. A complete module drawing (one variant) is generated from:<br>• standard, length/straight length, radius, angle, width;<br>• number of through tracks and number of sections;<br>• exits (label, direction, end profile);<br>• flags such as duck-under.<br>The function is a **UI-independent public API** in the model library, so the Module Registry can run it on its server and store the resulting module JSON (EX-7). In the app, "New module from parameters" uses the same function, and the generated JSON can be copied or saved. An auto-drawn module records the generator version and parameters. It can be re-generated from changed parameters until it is edited by hand, after which it is an ordinary drawing. |
| MD-10 | M | Derived measures (length, width, angle, radius, total track length) are computed and shown next to the user-entered Registry values. Differences are flagged. |
| MD-11 | M | Undo/redo for all edits. Autosave. |
| MD-12 | M | Grid, snapping (grid, endpoints, outline edges, exits) with an on/off toggle. Measurement tool for distance and angle. |
| MD-13 | M | **Variants**: a module has one or more variants (alternate drawings, *ConfigurationLabel*), e.g. a station that can be set up with or without a middle section, or with different track plans. Create a variant from scratch or as a copy of another, rename it, delete it and mark one as the default. The editor shows one variant at a time with a variant switcher. Module properties are shared by all variants. *(Replaces the earlier "module sets" concept.)* |
| MD-14 | S | A background image (scanned sketch, photo, exported DWG as PNG/SVG) can be placed, scaled and calibrated by two points, for tracing. |
| MD-15 | L | Import of DXF outlines and track centre lines. |
| MD-17 | M | **Operator spaces**: add operator spaces to a module variant. The quick way is "add operator space along this edge" with a depth (default from the standard, e.g. 800 mm) and the edge length; it can then be edited as a polygon. Spaces can be added on both sides. They are drawn as hatched areas, outside the baseboard. |
| MD-18 | M | **Public side**: mark one or more outline edges as the public side. It is shown with a distinct edge style and symbol, both in the module drawing and in hall plans. |
| MD-19 | M | **Duck-under**: the module's duck-under flag (as in the Module Registry) is set in the module properties. In the drawing, the duck-under area is shown with its own fill and symbol. By default it is the whole module; it can be limited to selected sections or to a drawn polygon. Optionally the clear height under it can be given. A module with a duck-under area has its duck-under flag set automatically, and the other way round. |
| MD-21 | M | **Track colour by standard**: tracks are drawn in their module standard's track colour (default blue), with a lighter tint for narrow gauge. In a hall plan the colours tell the standards apart. |
| MD-22 | M | **Sides between end plates**: the outline consists of the end plates (one per exit, as wide as its end profile) and the sides between them. Each side is generated (*Auto*) or drawn by hand (*Custom*), with its ends attached to the end-plate corners. Operator spaces and public sides attach to sides and are likewise generated or drawn by hand. The kind of module (end of line, pass-through, junction) is derived from its number of exits. |
| MD-16 | L | **Adjustable modules**: modules with variable length or angle, for closing loops. No such modules are known today. |

### 5.4 Hall drawing (HA)
| ID | Pri | Requirement |
|---|---|---|
| HA-1 | M | Create a hall from a rectangle (width × depth) or as a free polygon of walls. |
| HA-2 | M | Add **obstacles** (pillars: rectangular or round, and fixed furniture), **doors / emergency exits** with a keep-free zone, and **no-go zones** (polygons with a reason, e.g. a fire hose cabinet). |
| HA-3 | M | Hall properties: name, address/notes, default minimum aisle width. |
| HA-4 | S | A background floor plan image with two-point calibration. |
| HA-5 | M | Halls are stored separately and can be reused by several hall plans. A hall plan keeps an embedded copy (§6). |
| HA-6 | – | *Replaced by HP-17 (public areas belong to the hall plan, since they depend on how the modules are placed).* |

### 5.5 Hall planning (HP)
| ID | Pri | Requirement |
|---|---|---|
| HP-1 | M | Create a hall plan for a hall with a primary module standard and **any number of additional accepted standards** of the same scale (e.g. FREMO H0-RE + MMM-2R). Accepted standards can be changed at any time. The plan can also have **preferred add-ons** (e.g. MMM-Modern). |
| HP-2 | M | **Module collection** (UC-7): import module files into the plan's collection **from a ZIP archive** (e.g. exported from the Module Registry), **from a shared folder** (directory picker; re-reading the folder picks up new and changed files where the browser allows), or as single files. The collection lists the modules with owner, standard, length and number of exits, and filters by accepted standards, preferred add-ons, owner and "not used yet". It flags modules with an unaccepted standard or missing end profiles. |
| HP-3 | M | **Ordering** (UC-8): a line is shown as an **ordered list** next to the drawing. Branch lines are nested under the junction module where they start, so the whole layout is shown as a **tree** (HP-19). Add modules from the collection at the end or at any position, remove them, **move them up or down**, or drag them in the list. The plan is **redrawn immediately** after every change. Selecting a module in the list highlights it in the drawing, and the other way round. |
| HP-4 | M | **Flip** (UC-8): turn a module around, so that its other end connects to the previous module, e.g. a curve that bends the wrong way. For a module with three or more exits, choose the entry exit and the exit that continues the line. The plan is redrawn immediately. |
| HP-5 | M | **Positioning lines** (UC-8): each line has an **anchor module** (by default the first; any module can be chosen, e.g. a station that must stand in a particular place). Move and rotate a line by dragging it in the drawing (rotation in 15°/configurable steps) or with numeric input. The rest of the line is computed in both directions from the anchor. A line can be locked. A plan can have several lines. |
| HP-6 | M | Rubber-band and multi-select. Undo/redo. Autosave. |
| HP-7 | M | The same module can only be placed once per hall plan, in one variant (it is one physical object). The variant of a placed module can be switched. The module stays joined at an exit with the same label in the new variant, if there is one; otherwise it keeps its pose. |
| HP-8 | S | Colour modules by owner, standard, add-on compliance, functional state or validation status. Show a legend. (Joint fit markers, HP-11, are always shown.) |
| HP-9 | S | Show module names, owners and exit labels as optional layers. |
| HP-14 | M | Operator spaces, public sides and duck-under areas of placed modules are shown in the hall plan, with layers to turn each on and off. |
| HP-17 | M | **Public areas**: draw areas where visitors have access as polygons in the plan, with a label. |
| HP-18 | M | **Walking paths**: draw walking paths for train drivers as centre lines with a width (plan default 600 mm; can be set per path). They snap to module edges and to each other. A path may pass under duck-under areas; a passage under a duck-under module is shown with a duck-under symbol. |
| HP-15 | M | **Acknowledge warnings**: the planner can mark an acknowledgeable warning (see VA-11) as OK, with an optional note. It then appears as "acknowledged" in the problems list and in the drawing instead of as a warning. If any involved placement changes (moved, rotated, variant switched, leg adjustment changed, or module updated to a newer revision, HP-16) and the warning still applies, the acknowledgement is **invalidated**: the warning is shown again (marked "previously acknowledged") and must be acknowledged again. If the warning no longer applies, the acknowledgement is removed. |
| HP-10 | L | Drag an arrangement into another plan and use a plan as a template. |
| HP-19 | M | **Branches** (UC-9). Branches are common in module layouts.<br>• Every free exit of a module with three or more exits is shown as a **branch point**, in the drawing and in the list.<br>• One click **starts a branch line** there. The branch is anchored at that exit and moves with it.<br>• Branches can branch again.<br>• Changing which exit continues the line swaps the main line and the branch.<br>• Validation treats branch lines exactly like lines (open ends, joint fit, overlap, clearance). |
| HP-20 | S | **Closing a loop** (UC-10, rare): mark a line as closed, so that its last module should join its first, or join a chosen free exit elsewhere. The closure gap is reported (VA-5). |
| HP-11 | M | **Joint fit feedback**: when a module is added, moved or flipped (HP-3, HP-4), the joint with its neighbours gets its fit colour and symbol immediately (§3.2); the ordered list shows them too. Each joint is drawn with its fit marker: green, yellow, yellow + **clamp (tving) symbol** across the junction, or red. The join method follows the rules automatically. The user can override it per joint (e.g. "Join with clamps" when the screw-hole data is missing), and can edit the number of clamps. An incompatible (red) joint can be placed but stays an error. |
| HP-13 | M | **Leg adjustment**: set a placement's leg adjustment numerically (0 = standard height). A "Level from here" command (S) propagates adjustments along a chain of joined modules from a chosen module, so heights continue after a ramp. Modules with a non-zero adjustment are marked in the drawing. |
| HP-16 | M | **Modules are read-only in a hall plan.** The plan can't change a module's drawing or properties: outline, tracks, exits, operator spaces, public sides, owners and so on. In the plan you can only change placement and joint properties: pose, variant, leg adjustment, lock, note, join-method override and number of clamps. "Open in module editor" opens the module from the library, if it is there. When the library has a newer revision of a placed module, the plan shows it. The planner can then **update** the embedded copy after seeing the differences, which re-validates the plan. |
| HP-12 | M | **Inspect joint**: selecting a joint opens the end profile compare view (EP-8) for its two exits, in their actual orientations. |

### 5.6 Validation (VA)
Validation runs continuously and its results appear in a **problems list** and as markers in the drawing. Each finding has a severity (error / warning / info) and can be selected to zoom to it.

| ID | Pri | Requirement |
|---|---|---|
| VA-1 | M | **Joint fit** according to §3.2:<br>• *Incompatible* is an **error**.<br>• *Fits with mismatch* is a **warning** ("end profiles don't match", with the differing elements).<br>• *Fits with clamps* is a **warning** ("clamps needed").<br>• *Fits* gives no finding.<br>A joint whose method the user overrode against the rule result is reported as a warning with the reason. Modules flagged as end-profile switcher are adapters and are validated per exit. |
| VA-2 | M | **Standard/scale consistency**: modules of another scale are an error. Modules whose standard is not among the hall plan's **accepted standards** are a warning. |
| VA-14 | M | **Preferred add-ons**: a module that doesn't meet a preferred add-on of the hall plan (e.g. MMM-Modern) is a **warning** ("doesn't meet MMM-Modern"). The warning is acknowledgeable (HP-15). Modules of standards the add-on doesn't apply to are not checked. |
| VA-3 | M | **Joint geometry** (loop closures and branch reconnections only; joints within a line are exact): for two exits that should join but don't align exactly, show the position gap (mm) and angle error (°). Within the tolerances (default 2 mm, 0.5°, configurable per plan) the joint is accepted with a warning. Outside them it is not a joint. |
| VA-4 | M | **Open ends**: list every exit without a joint, except exits of modules flagged as end of line. |
| VA-5 | M | **Loop closure**: for a closed line (HP-20), report the closure gap (dx, dy, angle and height) at the closing joint. Highlight the modules in the loop. |
| VA-6 | M | **Overlap**: module outlines may not overlap each other (except at joints), walls, obstacles, door zones or no-go zones. |
| VA-7 | M | **Clearance**: check the minimum distance between non-joined module outlines, and between modules and walls/obstacles. Default from the standard, can be overridden per plan. Show violations as dimension markers. Operator spaces count as part of the module for this check, so an aisle is measured from the edge of the operator space. This can be switched off per plan. |
| VA-11 | M | **Operator space conflicts**: an operator space that overlaps another module's operator space is a **warning**. The same applies if it overlaps another module's baseboard, a wall, an obstacle, a door zone, a no-go zone or a **public area**. These warnings are **acknowledgeable** (HP-15). |
| VA-12 | S | **Public side**: a public side that faces another module's operator space, or (if public areas are defined, HP-17) doesn't border a public area, is an acknowledgeable warning. |
| VA-8 | S | **Enclosed areas**: an area completely surrounded by modules is reported as info if it can be reached under a duck-under area, and as a warning if it can't be reached at all. |
| VA-13 | M | **Walking paths**: a walking path (with its width) that crosses a module baseboard **outside a duck-under area** is an **error**. If it passes a wall, obstacle, door zone or no-go zone, or is narrower than its width because of a module or obstacle, it's a **warning**. Passages under duck-under areas are listed as **info** ("duck-under passage"), with the clear height if known. Warnings are acknowledgeable (HP-15). |
| VA-10 | M | **Height at joints**: the effective rail-top offsets of two joined exits must be equal within the height tolerance (default 1 mm). Otherwise it is an **error** ("height step N mm") with the suggested leg adjustment. Covers ramp modules and standards with different rail-top heights. |
| VA-9 | S | Track continuity across joints: track counts at both sides match. A track ending at an exit without a counterpart on the other side is a warning. |

### 5.7 Statistics and reports (RP)
| ID | Pri | Requirement |
|---|---|---|
| RP-1 | M | Hall plan summary: number of modules (total, per standard, and meeting/not meeting each preferred add-on), total module length (m), main line length, number of open ends, joints with warnings or errors, **number of clamped joints and clamps needed**. |
| RP-2 | M | Module list grouped by owner: name, length, standard, position label/number. CSV export. |
| RP-3 | S | Joint list: module A/exit – module B/exit, end profiles, join method (bolted/clamped, number of clamps), status. |
| RP-4 | S | Placement numbering (automatic or manual) used in printouts, e.g. for the set-up crew. |

### 5.8 Export, print and files (EX)
| ID | Pri | Requirement |
|---|---|---|
| EX-1 | M | Export an end profile drawing, a module drawing and a hall plan as **SVG** and **PNG**. |
| EX-2 | M | **Print** at a chosen scale (e.g. 1:1 for end profiles, 1:10, 1:50, 1:100) or fit to page. Large drawings are printed on tiled A4/A3 pages with overlap marks. Includes a title block (name, date, scale bar, legend). |
| EX-3 | S | PDF export (may be "print to PDF" in the browser for the first release). |
| EX-4 | M | Save and open **end profile files**, **module files**, **hall files**, **hall plan files** and **catalogue files** (§6). **Files are the primary result** of all drawing work (UseCases.md). |
| EX-9 | M | **Import from ZIP archives and folders**: read module and end profile files from a ZIP archive (e.g. a Module Registry export) or from a folder chosen by the user, in one go (UC-2, UC-7). |
| EX-5 | M | Opening a hall plan or module file that contains modules or end profiles that already exist in the local library shows any differences (by revision). For the **library**, the user chooses whether to keep the local version or import the file's version. For a **hall plan**, the embedded modules stay as they are until the planner updates them (HP-16). |
| EX-6 | S | Export and import of the whole local workspace (backup). |
| EX-7 | M | The **module and end profile file formats are public contracts**. The Module Registry will accept module files and end profile drawings for upload in a coming release. The formats are documented, have published JSON Schemas, and are versioned with backward-compatible changes only within a major schema version (§6). |
| EX-8 | M | **Reusable display components**: read-only components that show an end profile, a module (a chosen variant) or a hall plan, from the model or directly from the file JSON, with layer options (e.g. operator spaces, public sides, duck-under areas). They are meant to be embedded in other apps, first the Module Registry (NF-10). |

### 5.9 Application (AP)
| ID | Pri | Requirement |
|---|---|---|
| AP-1 | M | Installable PWA that works fully offline after the first load. Shows an "update available" prompt when a new version is deployed. |
| AP-2 | M | UI languages: English, German, Danish, Swedish and Norwegian (the same as the Module Registry), using Tellurian.Localization. |
| AP-3 | M | **Editing is done on a desktop or laptop** with mouse and keyboard, preferably on a large screen. Hall plans need a lot of screen space, much like CAD work. **Tablets and phones are for viewing** end profiles, modules and hall plans, e.g. at the meeting, with touch pan and pinch-zoom. Editing on a touch device is not a goal. |
| AP-4 | M | Pan/zoom canvas with zoom-to-fit, zoom-to-selection, and a scale bar. |
| AP-5 | S | Light/dark theme. |
| AP-6 | M | Context help (Markdown, localised) for the editors. |

## 6. Data storage and file formats

- **Files are primary.** The JSON file is the result of every drawing. Users keep, share and upload files.
- **IndexedDB is the workbench** for work in progress: autosaved **drafts** (one per open drawing, with an "unsaved changes" flag), **recent files** (with file handles where the File System Access API allows re-opening), the **module collections** of hall plans, the user part of the catalogue, and settings. Each stored drawing is one JSON document with `id` (GUID), `schemaVersion`, `revision` (incremented per save) and `modified` (UTC). The app reminds the user to save a file when a draft has unsaved changes.
- **File format**: UTF-8 JSON with an envelope `{ "format": "tellurian-drawing", "kind": "endProfile|module|hall|hallPlan|catalogue", "schemaVersion": n, "appVersion": "...", "content": {...} }`.
  File extensions: `.tdendprofile`, `.tdmodule`, `.tdhall`, `.tdhallplan`, `.tdcatalogue` (JSON content; the extension is what the OS uses to associate files).
- **Self-contained files**:
  - a **module file** contains all variants and embeds the end profile drawings its exits and internal joints use, so the receiver (another user or the Module Registry) can show and check the ends;
  - a **hall plan file** embeds copies of the hall, every module drawing it uses and their end profiles, so anyone can open it.
- **Identity**: drawings keep their GUID when exported and imported, so the same physical module or the same end profile is recognised again. Optional Registry ids (module, end profile) are kept as extra keys.
- **Migration**: older schema versions are upgraded on load. Newer versions are refused with a clear message.
- **Published schema**: JSON Schemas for all file kinds are kept in the repository (`docs/schema/`) and published with the app. The **module** and **end profile** schemas are consumed by the Module Registry (EX-7), so changes to them must be coordinated with the Registry.
- **Self-describing references**: a module file includes its catalogue references by value (standard short name, scale, end profile designations) as well as by id. A receiver can then map them to its own data without access to the app's catalogue.
- **Sharing**: through the File System Access API where available (save in place), otherwise download/upload. The Web Share API is used on mobile (S).

## 7. Non-functional requirements (NF)
| ID | Requirement |
|---|---|
| NF-1 | Blazor WebAssembly **standalone** on **.NET 11**, **AOT compiled**, deployed as static files to **Azure Static Web Apps**. No server code. |
| NF-2 | Performance: a hall plan with 300 modules pans and zooms smoothly (≥ 30 fps on a mid-range laptop). Reordering, flipping and dragging a line redraw within 100 ms. Validation of 300 modules finishes in under 200 ms. |
| NF-3 | The first load on broadband is under ~10 s. Later starts work offline from the cache. |
| NF-4 | Supported browsers for editing: current Chrome, Edge, Firefox and Safari on desktop. For viewing: also Safari on iPadOS/iOS and Chrome on Android. |
| NF-5 | Geometry and validation logic sit in a UI-independent .NET library with unit tests (MSTest). |
| NF-6 | Keep JavaScript interop to a minimum and isolate it (pointer/gesture handling, IndexedDB, file access, printing). |
| NF-7 | No personal data leaves the device. Owner names are stored only locally and in files the user chooses to share. |
| NF-8 | Precision: geometry in `double`. Exit poses and end profile positions are reproducible to 0.01 mm after save/load. |
| NF-9 | Basic accessibility: keyboard operation of menus and property panels, plus sufficient contrast. |
| NF-10 | **Reusable libraries**:<br>• All view and editor components are in a **separate Razor class library**, independent of the hosting model. They must work both in this WebAssembly app and in the Module Registry (Blazor Server, Interactive Server render mode); read-only display needs no JavaScript.<br>• The geometry, model, editing and component libraries are published as **NuGet packages**.<br>• Auto-draw (MD-20) and JSON (de)serialization need only the model library, with no UI. |

## 8. User interface outline

- **Start page**: recent items, plus New end profile / New module / New hall / New hall plan / Open file.
- **Library**: tabs for End profiles, Modules, Halls, Hall plans and Catalogue (scales, standards, overrides).
- **End profile editor**:
  - toolbar: select, outline, ground line, track, screw hole, dimension, label, measure;
  - canvas (end view), with an optional mirror ghost;
  - property panel with the derived properties;
  - a compare button that opens the compare view.
- **End profile compare view**: two profile pickers with orientation, the overlay canvas, a difference list and the fit result.
- **Module editor**: variant switcher, toolbar (select, section outline, split/merge section, exit, track tools, label, measure), canvas, property panel (selected object or module properties), problems list.
- **Hall editor**: like the module editor, with tools for walls, pillars, doors and no-go zones.
- **Hall plan editor** (designed for large screens; panels can be collapsed to give the drawing more room, and the drawing can be shown full screen): tools for public areas and walking paths, module collection and ordered list (left), canvas (centre), properties and problems (right, with "Mark as OK" on acknowledgeable warnings), summary bar (bottom).

## 9. Assumptions
- The track geometry needs to be accurate enough for planning (±0.5 mm), not for building track.
- End profile drawings are not in the Registry today. They are drawn in the app from the published PDF drawings of FREMO (all scales) and MMM (MMM-2R, MMM-3R), starting with FREMO H0, MMM-2R, MMM-3R and FREMO N.
- One module drawing corresponds to one physical module, which may consist of several sections and be set up in several variants.

## 10. Module Registry integration
The app never talks to the Registry as a user with rights. Access control is entirely the Registry's concern.

**Coming Registry release (Registry-side work, enabled by EX-7):**
- A user uploads a module file to a module in the Module Registry. The Registry validates it against the published module schema, stores it, and can show it (replacing or complementing the DWG/SKP document links).
- An end profile drawing is stored with the Registry's **EndProfile** entity (complementing its PDF document). End profile drawings are maintained there and **imported into the app**, so modules are drawn with the current profiles.
- **Data flows back:** when a module drawing is uploaded, the Registry can set missing properties and update measures from the drawing: exits, straight/radius/angle/length, width, sections, through tracks and flags (ModuleDrawing.md §8.8). The module file therefore includes the derived values per variant. This improves the Registry data, where 71 % of the modules have no exits today.
- Meeting organisers can then get module files of registered modules from the Registry, or collect them in any other way.

**Reuse in the Registry (enabled by NF-10):**
- The Registry embeds the display components (EX-8) to show module and end profile drawings on its pages.
- The Registry **auto-draws** simple straight and curved modules from its own module data with the model library (MD-20) and stores the module JSON, so most modules get a drawing without anyone drawing them by hand.

**Later phase in this app (not in first release):**
- A read-only Registry API for scales, standards and end profiles, including end profile drawings (catalogue import, CA-5), and possibly for downloading module files the user has access to in the Registry.
- The Registry has no concept of standard add-ons today. Until it has, add-ons are a drawing-app concept; the module file carries them by short name.
- Import maps Registry *Module*, *ModuleExit*, *ModuleEndProfile*, *ModuleStandard* and *ModuleOwnership* to the drawing model and generates a template outline from length/radius/angle (MD-3).

**Variant mapping.** The Registry stores the variants of a module as separate *Module* rows with the same *PackageLabel* and different *ConfigurationLabel*. One drawing with N variants therefore corresponds to N Registry modules. The module file keeps the optional Registry module id **per variant**, so that upload and import can map each variant to its Registry row.

## 11. Open questions
*None at the moment.*

### Resolved
- **Adjustable modules**: none are known, so loop closure is only reported. Adjustable modules are a later item (MD-16) (decided 2026-11-13).
- **Authoritative end profiles**: FREMO standardises FREMO end profiles in all scales and MMM standardises MMM-2R and MMM-3R, with the add-ons MMM-Summer and MMM-Modern to MMM-2R. Both publish PDF drawings, which are the sources for the built-in catalogue (CA-1) (decided 2026-11-13).
- **Multi-section modules**: a module of one or more sections is **one drawing**, with alternate drawings (variants) for different set-ups. Internal end profiles are free; outer end profiles must follow the standard (MD-4, MD-4a, MD-5, MD-13) (decided 2026-11-13).
- **Operator space and public side**: modules can have operator spaces on one or both sides, and marked public sides. Overlapping operator spaces are a warning that the planner can acknowledge. The acknowledgement is invalidated when an involved module changes and the overlap still exists (MD-17, MD-18, HP-15, VA-11). Operator spaces and public sides belong to the module drawing; modules are read-only in a hall plan (HP-16) (decided 2026-11-13).
- **Public areas, walking paths and duck-under**: the hall plan has public areas and walking paths for train drivers. The duck-under area is part of the module drawing, and walking paths may only cross modules under duck-under areas (MD-19, HP-17, HP-18, VA-13) (decided 2026-11-13).
- **Standard add-ons**: a standard can have add-ons (e.g. MMM-Summer and MMM-Modern on MMM-2R). Modules state which add-ons they meet, and hall plans can prefer add-ons; modules not meeting them get a warning (VA-14) (decided 2026-11-13).
- **Reuse in the Module Registry**: all view components are in a separate component library (NuGet), usable in Blazor Server; simple modules can be auto-drawn from Registry data, producing module JSON for storage in the Registry (MD-20, EX-8, NF-10) (decided 2026-11-13).
- **Hosting**: Azure Static Web Apps (decided 2026-11-13).
- **Target framework**: .NET 11 (decided 2026-11-13).
- **End profiles are drawn in the app** (2D end view) and compared geometrically (decided 2026-11-13).
- **Heights are offsets**: rail-top offset 0 = the standard's height (normally 1300 mm above floor). Rare ramp modules have exits with non-zero offsets; hall plans check heights at joints (MD-7a, HP-13, VA-10) (decided 2026-11-13).
- **Vertical datum** is the top of rail, RÖK (typically 1300 mm above the floor). Plate height is not a fit criterion; screw-hole positions decide whether ends are bolted, clamped or incompatible (decided 2026-11-13).
