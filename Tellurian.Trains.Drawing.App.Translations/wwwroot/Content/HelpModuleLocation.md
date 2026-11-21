# Operation location

An operation location is what a timetable planner sees of the module: a place where trains stop, meet or are routed. A module without one is plain line.

- **Make operation location** makes the module a station with the module's name. Give it a signature.
- **Remove operation location** makes the module plain line again. Its tracks stay in the *Tracks* pane.

## The properties

- **Name** and **Signature**: the signature is the short name used in timetables, e.g. *Tby*. It is unique within the country.
- **Country**: the country the location is operated as part of, as a code of two letters, e.g. *SE*.
- **Kind**:
  - **Station**: trains stop to meet, be overtaken, or exchange passengers or cargo.
  - **Signal controlled location**: a block post, junction or crossing place. Trains never stop there.
  - **Industrial area**: cargo only.
  - **Other place**: e.g. a halt. Passengers only.
- The marks say what can be done at the location. A mark that the kind can't have is switched off, and one it always has is switched on. When you change the kind, you are asked before anything is cleared.
- **Trains can cross** is chosen for a signal controlled location. A station can cross trains when it has more than one scheduled track.
- **Shadow station**: a station that stands for the world outside the layout. Passengers and cargo are always exchanged there.
- **Centre**: distances to other locations are measured from the centre, e.g. the station building, along the main track. It is in the middle of the module until you untick **Centre in the middle of the module** and enter the distance from the left end.

Problems are shown in red but don't stop you. Changes can be undone.
