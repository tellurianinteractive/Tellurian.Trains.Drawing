# Module Drawing – Detailed Definition

Status: **Draft 0.1** (2026-11-13). Part of the [specification](Specification.md); refines MD-1…MD-20.
It defines what a module drawing consists of, how its parts are built from each other, how it is
auto-drawn from Module Registry data, and how it looks.

---

## 1. Overview

A module drawing is a 2D plan view (from above) of one physical module, in one or more **variants**.
Each variant is built in layers, where each layer depends on the ones before it:

1. **Exits** – where the module can join other modules. Each exit has an end profile and a pose.
2. **End plates** – a straight edge at each exit, as wide as the exit's end profile.
3. **Sides** – the outline between the end plates. They are generated automatically or drawn by hand.
4. **Sections** – how the outline is divided into physical parts, with internal joints between them.
5. **Tracks** – the track centre lines. Through tracks are generated between exits; other track work is drawn by hand.
6. **Zones attached to sides** – operator spaces and public sides. Generated or drawn by hand.
7. **Other content** – the duck-under area, labels and markers.

The idea is that **a simple module is fully described by a few parameters**, such as length, radius,
angle and exits, so that it can be **auto-drawn** (§8). Anything the parameters can't describe is
drawn by hand on top of the generated parts, and only where needed: one side, one operator space,
or the track work of a station.

## 2. Coordinate system

- Plan view, millimetres, Y up, angles counter-clockwise from the X axis (spec §3.1).
- The origin is at the centre point of the **first exit's** end plate, and the X axis points **into** the module from that exit.
- For a module with exit directions, the first exit is the first **West** exit. So a straight
  pass-through module runs from West at the origin to East at (L, 0).
- Exit **headings point out of** the module. The first exit's heading is therefore 180°.

## 3. Topology: classification by number of exits

| Exits | Kind | Typical modules | Auto-draw from parameters |
|---|---|---|---|
| 0 | *Invalid* (warning) | A diorama without connections | No |
| 1 | **End of line** | A terminus or staging track with a buffer stop. Registry flag *IsEndOfLine*. | Yes (§8.4) |
| 2 | **Pass-through** | Straight, curve, straight+curve and transition modules, simple stations | Yes (§8.2, §8.3) |
| ≥ 3 | **Junction** | Branch-line junctions and stations with several exits per direction | Partly: the exits and main path are generated, the rest is drawn by hand (§8.5) |

The kind is **derived** from the number of exits in the variant; it isn't entered.
Two warnings help the user:
- the Registry flag *IsEndOfLine* is set but the variant has more than one exit;
- the variant has one exit but the flag isn't set.

## 4. Exits and end plates

### 4.1 Exit
| Property | Description |
|---|---|
| Label | E.g. "A", "B", or "West 1" (Registry *ModuleExit.Label*). |
| Direction | *West*, *East* or *Undefined*. Convention from the Registry: stand at the operation place with the module to the north; West is on your left and East on your right. |
| End profile | Must belong to the module's standard (MD-5). |
| Profile orientation | *Normal* / *Mirrored* (asymmetric profiles only). |
| Pose | The centre point of the end plate on its joint face, plus a heading pointing out of the module. |
| Rail-top offset | 0 = the standard's rail-top height (ramp modules only). |

### 4.2 End plate
- An **end plate** is a straight edge through the exit's position, perpendicular to its heading. Its length is the **plate width of the end profile**.
- It has two **corners**: the *left corner* and the *right corner*, as seen standing outside the module and looking at the end plate. This matches the end view in the profile drawing.
- The **track positions** at the end plate come from the end profile's track offsets (spec §3.1, end view X axis = the exit's lateral axis).
- End plates are drawn with a thicker line than the sides, and the end profile designation is written along them.

## 5. Outline: end plates and sides

### 5.1 Construction
The outline of a variant is a closed path. Walk around the module **counter-clockwise** and you
meet the end plates in that order. Between two consecutive end plates there is a **side**:
it runs from the corner where one plate ends to the corner where the next plate begins.

```
           side 2 (left side, seen travelling West → East)
     ┌───────────────────────────────────────────┐
     │                                           │
 W ══╡  ────────────── track ──────────────────  ╞══ E
     │                                           │
     └───────────────────────────────────────────┘
           side 1 (right side = operator side)
```

A pass-through module has 2 sides. An end-of-line module has 1 side, which runs from one corner of
its only end plate, around the far end, and back to the other corner. A module with n ≥ 3 exits has n sides.

### 5.2 Side modes
Each side has a **mode**:

| Mode | Geometry | Use |
|---|---|---|
| **Auto** | Generated from the module's main path and width (§5.3). | Almost all line modules. |
| **Custom** | A path of line and arc segments drawn by the user. Its end points stay attached to the two end plate corners, so moving an exit moves the ends of the side. | Bays, cut-outs, irregular stations, junctions. |

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

## 7. Sections, zones and other content

### 7.1 Sections
- The outline is divided into sections by **section lines**, which are cuts across the outline. In auto-draw they are perpendicular to the main path.
- *NumberOfSections* from the Registry gives equal-length sections by default. The positions can then be moved.
- Each section line is an **internal joint**. Its internal end profile is optional and free; it doesn't have to follow any standard (MD-4a).

### 7.2 Operator space
- **Auto**: an operator space is **attached to a side** (or part of it, from a start offset to an end offset along the side). It is the area between the side and a line offset by the space's depth (default from the standard, e.g. 800 mm).
- **Default side**: from the exit direction convention, the operator stands south of the module, which is the **right-hand side when travelling from a West exit to an East exit**.
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
- The module name label (FullName and variant) is placed automatically at the centre of the outline. It can be moved.
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
| Exits (label, direction, end profile) | *ModuleExit* rows | – |
| End of line | *Module.IsEndOfLine* | from the number of exits |
| Duck-under | *Module.IsDuckunder* | false |
| Operation place | the module is the *Station.PrimaryModule* | false |
| *new* **Curve direction** | – | **Left**, seen travelling from the first exit to the second |
| *new* **Straight position** | – | **Before** the curve, seen from the first exit (alternatives: *after*, *split equally*) |
| *new* Widening side, taper length | – | the non-operator side; 45° |

### 8.2 Pass-through: straight, curve, straight + curve
1. Place exit 1 (the first West exit, otherwise the first exit) at the origin with heading 180°.
2. Build the main path from the straight length, radius, angle, curve direction and straight position.
3. Place exit 2 at the end of the main path, with its heading pointing out along the path's tangent.
4. Create the end plates, the auto sides (§5.3), the through tracks (§6.2) and the section lines (§7.1).
5. Add the operator space and the duck-under area if their flags are set (§7.2, §7.4).
6. Check: the derived length, width and angle must match the parameters (MD-10).

### 8.3 Transition modules
When the two exits have different end profiles, for example a single-track and a double-track
profile, or different c/c, the same steps apply. The through tracks follow §6.2: one track
continues, and any extra track ends in a buffer stop or is flagged for drawing by hand.

### 8.4 End of line
The single exit goes at the origin. The main path has the straight length (and curve, if given).
Then come the sides with a back edge, and the track(s) ending in buffer stops (§6.2).

### 8.5 Junction (≥ 3 exits)
The main path is generated between the first West exit and the first East exit, as for a
pass-through module. The **remaining exits are placed provisionally** beside the main path,
evenly spaced, and marked "position not set". Their sides start in Auto mode as straight lines.
The user then places the exits and draws the track work. A junction auto-draw always carries the
note "needs manual completion".

### 8.6 Result
- The output is a module drawing with one variant, valid against the module schema (EX-7).
- It records `autoDraw: { generatorVersion, parameters }` per variant. While a variant is unedited, it can be **re-generated** from changed parameters. The Registry does this when module data changes.
- Editing a generated part by hand clears the auto-draw marker for that variant. The Registry then stops overwriting it.

## 9. Visual style (default)

| Element | Style |
|---|---|
| Baseboard | Light grey fill, dark grey outline, 1 px (screen). |
| End plate | Dark outline, 3 px. Profile designation along the plate. |
| Section line | Dashed dark grey. |
| Track | The standard's colour (default blue `#1f5fbf`). A band at low zoom, rails at high zoom. |
| Operator space | Hatched, neutral (e.g. grey-green), transparent. |
| Public side | A thick dotted edge in a distinct colour, with an "eye" symbol. |
| Duck-under area | Diagonal cross-hatch, with a "duck-under" symbol. |
| Exit marker | The label plus a W/E arrow outside the end plate. |
| Auto-drawn parts | Shown normally. A small "auto" badge appears in the property panel, not in the drawing. |

The colours are design tokens with dark-mode variants (spec AP-5). In hall plans the same styles
apply. Joint fit markers (spec HP-11) are drawn on top.

## 10. Validation inside a module drawing

| Check | Severity |
|---|---|
| A variant without exits | Warning |
| The End-of-line flag doesn't match the number of exits | Warning |
| An exit's end profile doesn't belong to the module's standard | Warning |
| A track ends at an exit off a profile track position, or a profile track position has no track | Warning (MD-7) |
| A custom side crosses itself or another side (the outline isn't simple) | Error |
| The derived length, width or angle differs from the entered Registry values | Warning (MD-10) |
| A through track couldn't be generated (§6.2) | Warning |
| A junction with exits whose position isn't set | Warning |

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

The numbers are illustrative. §12 lists the checks to run against the Registry data.

## 12. To be checked against Module Registry data
The local copy of the Registry database wasn't running when this was written. These queries should
confirm or adjust the defaults:

1. The distribution of the number of exits per module (how common are 1, 2 and ≥ 3?).
2. Geometry combinations: straight only, curve only, and both. The ranges of Radius, Angle and Straight per scale.
3. *Width* compared with the plate width of the exits' end profiles: how many modules are wider, and by how much (widened bodies)?
4. End profiles per standard, and how often a module's two exits have different end profiles (transition modules).
5. *NumberOfSections* and *NumberOfThroughTracks* distributions.
6. Station primary modules: their widths and numbers of exits (to judge the widening side and the operator space defaults).

## 13. Open questions
1. **Curve direction** and **straight position** are not in the Registry. Should the Registry get these two fields, so that auto-draw becomes unambiguous? Until then the defaults in §8.1 apply, and the Registry can re-generate with corrected values.
2. **Widening side** of station modules: is it usually towards the back (the non-operator side), as assumed?
3. **Default operator space depth** per standard (e.g. 800 mm)?
4. **Track colours** for the built-in standards: blue for all, or one colour per standard family (e.g. FREMO blue, MMM another)?
