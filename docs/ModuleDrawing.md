# Module Drawing – Detailed Definition

Status: **Draft 0.3** (2026-11-21): end plates are parts of the drawing that exits sit on, and the outline can be drawn before the exits (§1, §4, §5); the drawing lies the way the module is seen, and the exits' directions are read from it (§2, §4.1). Draft 0.2 (2026-11-13) checked the defaults against Module Registry data (§12). Part of the [specification](Specification.md); refines MD-1…MD-20.
It defines what a module drawing consists of, how its parts are built from each other, how it is
auto-drawn from Module Registry data, and how it looks.

---

## 1. Overview

A module drawing is a 2D plan view (from above) of one physical module. A module has **one drawing**.
A module that can be set up in several ways, e.g. a station with or without a middle section, is
several modules, each with its own drawing, file and id (spec §10; decided 2026-11-19).
The drawing is built in layers, where each layer depends on the ones before it:

1. **End plates** – the straight parts of the outline where the module meets other modules. Each has a pose, a width and its own height in end view (§4.2).
2. **Exits** – where tracks can join another module. Each exit sits on an end plate, at an offset along it, and has its own end profile. A plate can carry several exits (§4.1, §4.4).
3. **Sides** – the outline between the end plates. They are generated automatically or drawn by hand.
4. **Sections** – how the outline is divided into physical parts, with internal joints between them.
5. **Tracks** – the track centre lines. Through tracks are generated between exits; other track work is drawn by hand.
6. **Zones attached to sides** – operator spaces and public sides. Generated or drawn by hand.
7. **Other content** – the duck-under area, labels and markers.

The first three layers can be made **either way round**, and both give the same kind of drawing (decided 2026-11-21):
- **Outline first**, as a module is built: draw the outline of the frame, mark straight stretches of it as end plates, then place exits with their end profiles on the plates.
- **Exits first**, as auto-draw and the quick templates do: place exits with their end profiles; an end plate is generated at each, as wide as its profile, and the sides are generated between the plates.

The idea is that **a simple module is fully described by a few parameters**, such as length, radius,
angle and exits, so that it can be **auto-drawn** (§8). Anything the parameters can't describe is
drawn by hand on top of the generated parts, and only where needed: one side, one operator space,
or the track work of a station.

## 2. Coordinate system

- Plan view, millimetres, Y up, angles counter-clockwise from the X axis (spec §3.1).
- **The drawing lies the way the module is seen** (decided 2026-11-21). The viewer stands below the drawing, on the module's **viewing side**: X points to the viewer's right and Y away from the viewer. The exits' directions are read from how the drawing lies (§2.1, §4.1).
- The origin is at the **first Left exit's** position on its end plate; a module without a Left exit has it at its first exit. So a straight
  pass-through module laid horizontally runs from Left at the origin to Right at (L, 0).
- Exit **headings point out of** the module. The heading of a Left exit on a horizontal module is therefore 180°.

### 2.1 Turning the drawing
The user decides how the module lies, by one of:
- choosing a **track** that is to lie horizontally;
- **turning the drawing 180°**;
- for a curve, laying it **symmetrically**: the line between the first Left and the first Right exit horizontal, so that the plates lean by half the angle each way (+15° and −15° on a 30° curve), or the other way round when turned 180°; or laying it **as auto-draw does**, with the first exit's plate vertical (§8.2).

Turning **rotates the stored geometry** once, then moves it so that the origin is again at the first Left exit, all in one undoable step. No display rotation is stored: the drawing in the file always lies the way the module is seen, for every reader. When a turn changes the side of any exit, the **directions of the tracks** in the track list (`LeftToRight`, `RightToLeft`) are swapped in the same step, and the user is told. The sides refer to their exits by side and number, so the turn changes the exits through `ModuleDrawing.ChangeExits`, which keeps every side at its exits. Turning 180° swaps Left and Right and the viewing side together.

## 3. Topology: classification by number of exits

| Exits | Kind | Typical modules | Auto-draw from parameters |
|---|---|---|---|
| 0 | *Invalid* (warning) | A diorama without connections | No |
| 1 | **End of line** | Either a **terminus** or staging track with buffer stops, or a **loop module**: a single, complete module whose track forms a return loop back to its only exit. Registry flag *IsEndOfLine*. | Terminus: yes (§8.4). Loop module: its track is drawn by hand. |
| 2 | **Pass-through** | Straight, curve, straight+curve and transition modules, simple stations | Yes (§8.2, §8.3) |
| ≥ 3 | **Junction** | Branch-line junctions and stations with several exits per direction | Partly: the exits and main path are generated, the rest is drawn by hand (§8.5) |

The kind is **derived** from the number of exits in the drawing; it isn't entered.
Two warnings help the user:
- the Registry flag *IsEndOfLine* is set but the module has more than one exit;
- the module has one exit but the flag isn't set.

## 4. Exits and end plates

### 4.1 Exit
| Property | Description |
|---|---|
| Label | E.g. "A", "B", or "Left 1" (Registry *ModuleExit.Label*). Free text for the reader only: a hall plan refers to an exit by its side and number on that side (Left 1, Right 2, Up 1; the n:th exit with that direction in the order of the drawing's exits), never by its label. |
| Direction | *Left*, *Right*, *Up* or *Down*, **worked out from how the drawing lies** (§2.1), never entered (decided 2026-11-21). An exit whose heading is **within 15° of straight up** is *Up* (the side away from the viewer), **within 15° of straight down** *Down* (the side towards the viewer); otherwise it is *Left* when its heading points to the left and *Right* when it points to the right. So a branch at 30° or 60° from the left end of a horizontal station is Left, and only a branch near a right angle is Up or Down. To put an exit on another side, the user turns the drawing. *Undefined* is only for an exit not yet placed. The direction is written in the file for readers such as the Module Registry. Up to schema version 1 Left was called *West* and Right *East*; they were never geography. |
| End plate and offset | The end plate the exit sits on, and the offset of the end profile's centre line along the plate from the plate's centre. The exit's **pose** (position on the joint face and a heading pointing out of the module) follows from the plate, so exits on one plate are always in line and parallel. |
| End profile | Each exit has its own. It must be of the module's scale, but may belong to another standard than the module's, e.g. FREMO at one end and MMM at the other, also two exits on the same plate (MD-5). |
| Profile orientation | *Normal* (as drawn) / *Mirrored*. The end plate can be fitted either side out; the orientation only matters for asymmetric profiles (§4.3). |
| Rail-top offset | 0 = the standard's rail-top height (ramp modules only). |

### 4.2 End plate
- An **end plate** is a part of the drawing in its own right: a straight stretch of the outline where the module meets other modules. It has an id (a whole number, unique in the drawing), a **pose** (the centre of the plate on its joint face and a heading pointing out of the module) and a **width**, the physical width of the plate.
- It carries **one or more exits** (§4.1). Made exits first, a plate is generated at each exit, as wide as its end profile, which is what every drawing had up to draft 0.2; made outline first, the user marks a straight stretch of the outline as a plate and places exits on it. Every exit lies within its plate.
- It has two **corners**: the *left corner* and the *right corner*, as seen standing outside the module and looking at the end plate. This matches the end view in the profile drawing.
- It has its **own shape in end view**. By default it is the plate of its (first) exit's end profile, since the frame's height is usually taken from the chosen profile. It can differ: **cut lower** to meet a lower neighbour, or of a **non-standard height** of its own. The screw holes and dowels still come from the end profiles of its exits, at their offsets, so a plate of non-standard height keeps the standard's holes. A plate with two exits whose profiles differ in height has one shape of its own, chosen by the user.
- The **track positions** at the end plate come from the end profiles of its exits: each profile's track offsets, moved by the exit's offset along the plate (spec §3.1, end view X axis = the plate's lateral axis).
- End plates are drawn with a thicker line than the sides, and the end profile's short name is written along them, inside the module, once per exit (§9).
- Joint fit compares the **end profiles** of two exits that meet (tracks, screw holes, dowels; spec §3.2). A difference in the **plates'** actual heights is reported as a visible step, not as a misfit.

### 4.3 Profile orientation and the Endewender
An end profile is drawn in end view, seen from outside the module. An exit builds it either **as drawn**
(*Normal*) or **mirrored**: the end plate, often 10–12 mm plywood, can be fitted either side out.
For a symmetric profile the two are the same.

For an **asymmetric** profile, such as the high side of FREMO-H0-B96, the orientation decides on which side
of the track the asymmetric feature is. The two ends of a module are seen from opposite directions, so the
feature stays on the **same side of the track** when one end is *Normal* and the other *Mirrored*. Then two
such modules join without mismatch (spec §3.2).

An **Endewender** (Registry flag *IsEndProfileSwitcher*, Swedish *ändevändare*) turns the profile over: the
feature is on one side of the track at one end and on the other side at the other end, so both ends have the
same orientation. Endewenders are needed to make a line of asymmetric modules continue the other way round.

Auto-draw sets the orientation accordingly: the first exit is *Normal*; exits at the far end of the main path
are *Mirrored* when their profile is asymmetric, unless the module is an Endewender. The user can change the
orientation of any exit.

### 4.4 Several exits on one side
A side can have several exits: on one plate, e.g. a 546 mm plate where a 296 mm FREMO single-track module and an
MMM 337 double-track module join side by side, or on separate plates, e.g. a branch line at 30° that is Left like
the main line. Each exit is joined and checked for fit on its own. The end profiles may reach beyond the plate
(296 + 337 > 546); only their tracks must meet the module's tracks.

A hall plan refers to an exit by side and number (Left 1, Left 2). The **numbering on a side** follows the exits'
**positions in the drawing as it lies**, not the order they were drawn in: Left and Right exits are numbered **from
top to bottom**, Up and Down exits **from left to right** (decided 2026-11-21). When a module embedded in a hall plan
is updated from a newer drawing, or the drawing has been turned, a change of an exit's side or number is reported,
since it would move what the plan joins there.

### 4.5 Exits that can't be used together
Exits on the **same plate whose end profiles overlap** along it can't both be joined: e.g. the 546 mm plate
above, if it can also be joined to a single module with a 546 mm profile, has that as a third exit, which
overlaps the other two. This is worked out from the drawing; nothing is marked by hand. A hall plan refuses to
join two such exits.

## 5. Outline: end plates and sides

### 5.1 Construction
The outline of a module is a closed path. Walk around the module **counter-clockwise** and you
meet the end plates in that order. Between two consecutive end plates there is a **side**:
it runs from the corner where one plate ends to the corner where the next plate begins.

```
           side 2 (left side, seen travelling Left → Right)
     ┌───────────────────────────────────────────┐
     │                                           │
 W ══╡  ────────────── track ──────────────────  ╞══ E
     │                                           │
     └───────────────────────────────────────────┘
           side 1 (right side = operator side)
```

A side is between **plates**, not exits, and refers to them by their ids. A pass-through module has 2 sides. An end-of-line module has 1 side, which runs from one corner of
its only end plate, around the far end, and back to the other corner. A module with n ≥ 3 plates has n sides; exits that share a plate add no side.

Made outline first, the outline is drawn as custom sides (§5.2) and the end plates are marked on it; the stretches between the plates become the sides.

### 5.2 Side modes
Each side has a **mode**:

| Mode | Geometry | Use |
|---|---|---|
| **Auto** | Generated from the module's main path and width (§5.3). | Almost all line modules. |
| **Custom** | A path of line and arc segments drawn by the user. Its end points stay attached to the two end plate corners, so moving a plate moves the ends of the side. | Bays, cut-outs, irregular stations, junctions, and every outline drawn before its exits. |

Switching a side from Auto to Custom starts from the generated geometry. Switching back discards
the custom path, after a confirmation.

### 5.3 Auto side rules
Auto sides follow the **main path** (§6.1), the module's centre line from the first to the second exit.

| Situation | Auto side |
|---|---|
| Straight | Straight lines parallel to the main path, offset by ± plate width / 2. |
| Curve | Arcs concentric with the main path arc (radius R ± plate width / 2). |
| Straight + curve | Line + arc, tangent at the junction, offset as above. |
| Plate widths differ at the two exits | Straight lines (or tangent arcs on curves) that change linearly from one half-width to the other. |
| **Widened body** (Width > plate width) | The side runs out from the plate corner at 45° (default; the taper length is a parameter), keeps the body width, and comes back at the other end. By default it widens **on the non-operator side**: station modules usually have their track work towards the back and the operator at the front. *To be checked against Registry data.* |
| End of line | The two long sides follow the main path to its end, joined by a straight back edge perpendicular to the main path. |
| Junction (≥ 3 exits) | Straight lines between corners, as a starting point for drawing by hand. |

### 5.4 Width and derived measures
- **Width** (Registry: "the widest part of the module") is derived from the outline as its maximum extent perpendicular to the main path. It is compared with the entered value (MD-10).
- **Length** is derived from the main path: arc length + straight length (Registry *TotalLength*: R·angle·π/180, rounded to whole mm, plus *Straight*).

## 6. Tracks

### 6.1 Main path
The **main path** is the centre line between the first exit and the second. In auto-draw it is built
from the parameters: Straight, Radius, Angle and curve direction (§8.2). It is the reference for:
- auto sides (§5.3);
- through tracks (§6.2);
- sections (§7.1);
- the derived length.

A junction can have one main path per pair of exits, with the first pair as the primary one.

### 6.2 Through tracks (auto)
The module has *NumberOfThroughTracks* through tracks. Each one connects **track position i** at the
first exit with **track position i** at the second exit.

| Situation | Generated track |
|---|---|
| Both exits have the same track offset | A curve parallel to the main path at that offset: concentric arcs, so on a curve the inner track has radius R − c/c/2. |
| The offsets differ (e.g. a transition module that widens the double-track c/c) | Reverse curves (S-curve) with a minimum radius taken from the standard, centred on the module. If they don't fit, the track is flagged and has to be drawn by hand. |
| End of line | The track runs from the exit along the main path and ends at a **buffer stop**, a given margin (default 50 mm) before the back edge. |
| More than one through track at an end-of-line module | Parallel tracks, each with a buffer stop. |

A generated track can be **detached** and becomes an ordinary editable track element (MD-6).

### 6.3 Other track work (custom)
Station track work, sidings and junction tracks are drawn by hand with track elements: straights,
arcs, turnouts, crossings and buffer stops (MD-6). They connect to through tracks and exit track
positions with snapping. Snapping a track to an exit checks it against the end profile (MD-7).

### 6.4 Track appearance
- **Track colour by standard.** Every module standard has a **track colour**. The default is **blue**, `#1f5fbf`. All tracks of a module are drawn in its standard's colour, which tells the standards apart in a mixed hall plan such as FREMO H0-RE and MMM-2R.
- **Narrow-gauge** tracks use a lighter tint of the same colour.
- **Level of detail by zoom.** When the gauge is less than about 6 px on screen, a track is drawn as a band as wide as the gauge. When it is larger, the track is drawn as two rails, with no sleepers in the first release.
- Turnouts are drawn with all their routes. The frog point is marked.
- Buffer stops are drawn as a short bar across the track.

### 6.5 Track list and operation location
- **Every module has a list of its tracks** as train planning sees them (spec MD-8), with at least one track: a through
  track or a track ending. The list is `tracks` of the module; the track elements (§6.2, §6.3), `trackElements` of the drawing, refer to its tracks by their id, a whole number from 1.
  A track whose length or stub mark the user has entered says so itself (`isLengthEntered`, `isStubEntered`); the others follow the drawing.
- A module that is an **operation location** also has `operationLocation` (spec MD-24): name, signature, country,
  kind (station, signal controlled, industrial area, other) and what can be done there, with the centre as a distance
  in metres along the main track from the left end (missing means the middle). A module without it is plain line.
- The module, its location and its tracks are the types of the shared layout model, the package
  `Tellurian.Trains.Schedules.Model.Layouts` (spec §10). Lengths in the shared model are in metres.
- It is entered in the *Operation location* pane of the module editor (`ModuleLocationPane`).

## 7. Sections, zones and other content

### 7.1 Sections
- The outline is divided into sections by **section lines**, which are cuts across the outline. In auto-draw they are perpendicular to the main path.
- *NumberOfSections* from the Registry gives equal-length sections by default. The positions can then be moved.
- Each section line is an **internal joint**. Its internal end profile is optional and free; it doesn't have to follow any standard (MD-4a).

### 7.2 Operator space
- **Auto**: an operator space is **attached to a side** (or part of it, from a start offset to an end offset along the side). It is the area between the side and a line offset by the space's depth (default from the standard, e.g. 800 mm).
- **Default side**: from the exit direction convention, the operator stands on the viewer's side of the module, which is the **right-hand side when travelling from a Left exit to a Right exit**: the bottom of the drawing (§2).
- **Default content**: auto-draw creates an operator space only for modules that are the operation place of a station (Registry: the module is the station's *primary module*). Its default length is the full side.
- **Custom**: a polygon drawn by the user, e.g. for a recess or a space on both sides.
- Spaces can be on **both sides** (spec MD-17).

### 7.3 Public side
- Attached to a side, or to part of one with start and end offsets.
- There is no default; the owner marks it (MD-18).

### 7.4 Duck-under area
- By default it is the whole outline of the module, or of the sections marked as duck-under. Auto-draw creates it when the Registry flag *IsDuckunder* is set.
- It can be limited to a drawn polygon. The clear height is optional (MD-19).

### 7.5 Labels and markers
- The module name label (FullName) is placed automatically at the centre of the outline. It can be moved.
- Exit labels and direction arrows (W/E) are placed automatically at the end plates.
- Free text labels and markers: platform edges, station name, signals (simple symbols).

## 8. Auto-draw (MD-20)

### 8.1 Parameters
The parameters map one-to-one to Module Registry data. The ones marked *new* are not in the Registry today.

| Parameter | Registry source | Default |
|---|---|---|
| Standard (→ scale, gauge, colour, operator depth) | *Module.StandardId* | – |
| Straight length | *Module.Straight* | 0 |
| Radius, angle | *Module.Radius*, *Module.Angle* | none (no curve) |
| Width | *Module.Width* | the end profile's plate width |
| Number of through tracks | *Module.NumberOfThroughTracks* | from the end profile |
| Number of sections | *Module.NumberOfSections* | 1 |
| Exits (label, direction, end profile) | *ModuleExit* rows; exits with the *Internal* profile become internal joints | inferred when missing (§8.6) |
| End of line | *Module.IsEndOfLine* | from the number of exits |
| Duck-under | *Module.IsDuckunder* | false |
| Endewender | *Module.IsEndProfileSwitcher* | false; decides the orientation of asymmetric profiles at the far exits (§4.3) |
| Operation place | the module is the *Station.PrimaryModule* | false |
| *new* **Curve direction** | – | **Left**, seen travelling from the first exit to the second |
| *new* **Straight position** | – | **Before** the curve, seen from the first exit (alternatives: *after*, *split equally*) |
| *new* Widening side, taper length | – | the non-operator side; 45° |

### 8.2 Pass-through: straight, curve, straight + curve
1. Place exit 1 (the first Left exit, otherwise the first exit) at the origin with heading 180°.
2. Build the main path from the straight length, radius, angle, curve direction and straight position.
3. Place exit 2 at the end of the main path, with its heading pointing out along the path's tangent.
4. Create the end plates, the auto sides (§5.3), the through tracks (§6.2) and the section lines (§7.1).
5. Add the operator space and the duck-under area if their flags are set (§7.2, §7.4).
6. Check: the derived length, width and angle must match the parameters (MD-10).
7. The module lies with exit 1's plate vertical; the user can then turn it, e.g. lay a curve symmetrically (§2.1). The exit directions are worked out from how it lies (§4.1); a registered direction that differs is listed as an assumption (§8.6).

### 8.3 Transition modules
When the two exits have different end profiles, for example a single-track and a double-track
profile, or different c/c, the same steps apply. The through tracks follow §6.2: one track
continues, and any extra track ends in a buffer stop or is flagged for drawing by hand.

### 8.4 End of line
The single exit goes at the origin. The main path has the straight length (and curve, if given).
Then come the sides with a back edge, and the track(s) ending in buffer stops (§6.2).

### 8.5 Junction (≥ 3 exits)
The main path is generated between the first Left exit and the first Right exit, as for a
pass-through module. The **remaining exits are placed provisionally** beside the main path,
evenly spaced, and marked "position not set". Their sides start in Auto mode as straight lines.
The user then places the exits and draws the track work. A junction auto-draw always carries the
note "needs manual completion".

### 8.6 Inference and data sanity rules
Registry data is often incomplete (§12), so auto-draw fills gaps by these rules. **Every inferred value
is listed in the result as an "assumption"** and shown to the user, e.g. "exits inferred: 2 × FREMO-H0-B96".

| Gap or anomaly | Rule |
|---|---|
| **No exits registered** | Infer them: **1 exit** if *IsEndOfLine*, otherwise **2 exits** labelled W and E. Their end profile is the standard's **default end profile** for the number of through tracks: one default for single track and one for double track, in the catalogue, e.g. FREMO/H0-RE → FREMO-H0-B96 (single). |
| Exit with the *Internal* end profile | Not an exit. It becomes an **internal joint**. The number of sections is at least the number of internal joints + 1. |
| Exit directions undefined | Use the registered order: the first exit becomes exit 1. |
| Angle without radius | Use the standard's **default radius**, from the catalogue (e.g. the median of its scale, H0: 2 000 mm), with a warning. |
| Width smaller than the plate width | Ignore it and use the plate width, with a warning. |
| Number of through tracks 0 | Draw no through track; the module is drawn as baseboard only. |
| Number of through tracks doesn't match the end profile | The end profile wins. Warning. |
| Standard *MMM-2R/SSM* or *FREMO/SSM* | Standard MMM-2R with the add-on MMM-Summer, and default end profile MMM480. |
| No geometry (no straight length, no curve) | Not auto-drawable. The result is "needs manual drawing". |
| One exit and a curve angle of about 180° or more | Probably a **loop module**. The outline is drawn from the parameters; the return-loop track is drawn by hand. Marked "check". |
| Angle ≥ 360°, or a turntable module | Drawn as far as possible and marked "check". |

### 8.7 Result
- The output is a module with its drawing, valid against the module schema (EX-7).
- The drawing records `autoDraw: { generatorVersion, parameters }`. While the drawing is unedited, it can be **re-generated** from changed parameters. The Registry does this when module data changes.
- Editing a generated part by hand clears the auto-draw marker of the drawing. The Registry then stops overwriting it.

### 8.8 Upload to the Registry: data flowing back
A drawing contains exact data that is often missing in the Registry (§12). When a module drawing is uploaded, the Registry can offer to update the module with values derived from the drawing. The user confirms each change:

| Registry field | From the drawing |
|---|---|
| *ModuleExit* rows (label, direction, end profile) | The exits of the drawing. The "exits" with the *Internal* profile become internal joints in the drawing; the Registry keeps or adds them according to its own convention. |
| *Straight*, *Radius*, *Angle*, *Length* | The main path (§6.1). |
| *Width* | The derived width (§5.4). |
| *NumberOfSections* | The number of sections. |
| *NumberOfThroughTracks* | The number of through tracks. |
| *IsEndOfLine*, *IsDuckunder* | The number of exits, and the duck-under area. |

The app supports this by including the **derived values** in the module file, next to the drawing. A receiver can then use them without re-computing the geometry. For a module drawing to be complete, every exit must reference a **drawn** end profile. End profiles are therefore drawn (or imported from the Registry) before modules are drawn.

## 9. Visual style (default)

| Element | Style |
|---|---|
| Baseboard | Light grey fill, dark grey outline, 1 px (screen). |
| End plate | Dark outline, 3 px. |
| Section line | Dashed dark grey. |
| Track | The standard's colour (default blue `#1f5fbf`). A band at low zoom, rails at high zoom. |
| Operator space | Hatched, neutral (e.g. grey-green), transparent. |
| Public side | A thick dotted edge in a distinct colour, with an "eye" symbol. |
| Duck-under area | Diagonal cross-hatch, with a "duck-under" symbol. |
| End profile shading | The form of the end profile, folded into the module from each end plate: dark where the profile is above rail top and light where it is below (§9.1). |
| End profile name | The end profile's **short name** (its designation if it has none), in bold, turned 90° to run along the end plate, just inside the module beyond the shading. |
| Exit label | Not drawn: real modules aren't marked with exit labels. The label is still data, as text for the reader; nothing in a hall plan depends on it. Nothing is drawn outside the module's area. |
| Auto-drawn parts | Shown normally. A small "auto" badge appears in the property panel, not in the drawing. |

The colours are design tokens with dark-mode variants (spec AP-5). In hall plans the same styles
apply. Joint fit markers (spec HP-11) are drawn on top.

### 9.1 End profile shading
The kind of end profile is shown in plan view the way DWG and SKP module drawings do it. The
profile's upper edge is **folded into the module** about the end plate: a point of the edge at height
*y* above or below rail top is drawn |*y*| inwards from the plate, at its lateral position. The area
between the plate and the folded edge is filled
- **dark** where the edge is **above rail top**, e.g. the mountain side of FREMO-H0-B96;
- **light** where it is **below rail top**, e.g. the ballast shoulders and a low side.

So the shading has the form of the profile, at true size. The upper edge is the profile's ground line,
or the top of the plate outline when there is no ground line. The profile is taken as built at the
exit, so a mirrored exit shows mirrored shading. Where the edge crosses rail top, dark and light meet
at the plate.

In the app, the drawing uses the end profiles as they are in the catalogue, so a changed short name
shows at once; profiles that aren't in the catalogue are taken from the module file.

## 10. Validation inside a module drawing

| Check | Severity |
|---|---|
| A module without exits | Warning |
| The End-of-line flag doesn't match the number of exits | Warning |
| An exit's end profile is of another scale than the module | Error |
| A track ends at an exit off a profile track position, or a profile track position has no track | Warning (MD-7) |
| A custom side crosses itself or another side (the outline isn't simple) | Error |
| The derived length, width or angle differs from the entered Registry values | Warning (MD-10) |
| A through track couldn't be generated (§6.2) | Warning |
| A junction with exits whose position isn't set | Warning |
| An end plate that isn't straight, or an exit's tracks outside its plate | Error |
| An end plate without exits | Warning |

## 11. Examples

| # | Module | Parameters | Result |
|---|---|---|---|
| 1 | Straight line module | Straight 1000, FREMO H0 single-track profile at both exits (W, E) | A 1000 × plate-width rectangle with one track |
| 2 | Curve | R 2000, angle 30°, left, single track | An arc module 1047 mm long, with concentric arc sides |
| 3 | Straight + curve | Straight 500, R 2500, angle 15°, straight before | 500 + 654 = 1154 mm long |
| 4 | Double-track transition | Straight 1000; exits with double-track c/c 45 → 55 mm | Two S-curve tracks |
| 5 | Terminus | 1 exit, Straight 1200, two tracks, end of line | Two tracks with buffer stops |
| 6 | Station (3 sections) | Straight 3000, Width 600 (> plate), operation place, 3 sections | A widened body, an operator space on the right-hand (south) side, 2 section lines; the track work is drawn by hand |
| 7 | Junction | 3 exits (W, E, E) | Main path W–E. The second E exit is placed provisionally and marked for manual completion. |

The examples reflect the most common Registry cases (§12): straight 1 200 mm, curve R 2 000 mm at 30°, single track, FREMO-H0-B96.

## 12. Findings from Module Registry data (2026-11-13)
These are aggregate statistics from the production database: 918 modules. They are the basis for
the defaults above and for the inference rules in §8.6.

| Topic | Finding | Consequence |
|---|---|---|
| Standards | FREMO/H0-RE 460, MMM-2R 317, MMM-2R/SSM 40, MMM-3R 29, FREMO/SSM 19, FREMO 0-RE 15, FREMO/N-RE 14, AmericaN 9, USMS 5. There are also a few MMM/H0n3, SIH0 and FREMO/E modules. | The built-in catalogue covers FREMO/H0-RE, MMM-2R and MMM-3R first, then FREMO 0-RE and FREMO/N-RE. *SSM = MMM-Summer*: the two SSM standards map to MMM-2R + the MMM-Summer add-on (§13). |
| **Exits** | **648 modules (71 %) have no exits registered.** 57 have 1, 187 have 2, 17 have 3 and 9 have 4. | Auto-draw must **infer exits** when there are none (§8.6). Otherwise most modules couldn't be drawn. |
| End-of-line flag | 19 modules are flagged; 11 of them have no exits and 8 have 1. 49 one-exit modules are *not* flagged. | A single exit, or the flag, means end of line. A flag that disagrees with the exits only gives a warning. |
| Exit directions (2 exits) | EW 152, undefined 34, WW 1. | Undefined directions: use the registered exit order. |
| Geometry (H0) | Straight only 453, curve only 293, straight + curve 56, no geometry 77. **24 modules have an angle but no radius.** | A curve without a radius uses the standard's default radius, with a warning (§8.6). Modules with no geometry can't be auto-drawn. |
| Ranges (H0) | Straight 0–16 500 mm (median 1 200); radius 600–10 000 (median 2 000); angle 1–360° (median 30°). | Long stations exist. Angles over 180° (loops) are allowed; 360° and turntable modules are flagged "check". |
| Common angles | 30° (90), 45° (42), 15° (37), 90° (36), 22.5° (20), 10° (16). | These are offered as presets in the template dialog. |
| Width | FREMO/H0-RE median 500 (P90 550, max 2 365); MMM-2R median 480 (max 1 600). A few implausible values (30, 100). | Most modules are as wide as their end plate. Widened bodies exist, mostly among stations. A width **smaller than the plate width is treated as a data error**: it is ignored, with a warning. |
| End profiles | The most used are FREMO-H0-B96 (152 exits), FREMO-H0-F96 (88), MMM280 (47), MMM480 (37), MMM337 (35), FREMO-H0-IH05, E96, B09, MMM680 and so on. The MMM profiles are named after their **plate width** (280/337/480/680 mm). **No end profile has a PDF in the Registry.** | The plate widths of the MMM profiles follow from their names. The source drawings have to come from FREMO and MMM directly. |
| **"Internal" end profile** | The Registry has an end profile designated **Internal** in every scale (20 exits use it in H0). | Exits with the *Internal* profile are **internal joints between sections**, not exits (§8.6). |
| Transition modules | 56 modules have exits with different end profiles. Some of these differ only because of *Internal* exits. | Transition modules (§8.3) are common enough to support from the start. |
| Through tracks | 1: 719, 2: 174, 0: 10, 3–4: 15. | Single and double track cover 97 %. |
| Sections | 1 section: 671; 2–5: 169; 6–31: 77. | Multi-section modules are common. Equal-length sections are a reasonable default. |
| Flags | Duck-under 25, end of line 19, end-profile switcher 12, turntable 23, variant label 41. | As specified. |
| Stations | 281 primary modules. 172 of them have no exits. Average width about 500 mm, but about 780 mm for stations with 3 exits. **267 of 281 stations consist of one module.** | This confirms "one module = one drawing, several sections". Wide stations are mostly junction stations. |

## 13. Open questions
1. **Curve direction** and **straight position** are not in the Registry. Should the Registry get these two fields, so that auto-draw becomes unambiguous? Until then the defaults in §8.1 apply, and the Registry can re-generate with corrected values.
2. **Widening side** of station modules: is it usually towards the back (the non-operator side), as assumed?
3. **Default operator space depth** per standard (e.g. 800 mm)?
4. **Track colours** for the built-in standards: blue for all, or one colour per standard family (e.g. FREMO blue, MMM another)?
5. **Default end profiles** per standard, used when exits are inferred. *Decided for the spikes (2026-11-13):* FREMO/H0-RE → FREMO-H0-B96 (single track) and FREMO-H0-2E96 (double track); MMM-2R → MMM480. Still open: MMM-3R, and whether these defaults hold for the release.

### Resolved
- **SSM** is **MMM-Summer** (2026-11-13). Registry modules with the standard *MMM-2R/SSM* or *FREMO/SSM* map to standard **MMM-2R** with the add-on **MMM-Summer**, and get **MMM480** end profiles when exits are inferred.
- **Registry data quality** (2026-11-13): this app improves it.
  - When a module is drawn, its exits must have **drawn end profiles**, so end profiles are drawn first (or imported).
  - End profile drawings are kept in the Registry and imported into the app.
  - When a module drawing is **uploaded to the Registry**, the Registry can set missing properties and update measures from the drawing (§8.8).
