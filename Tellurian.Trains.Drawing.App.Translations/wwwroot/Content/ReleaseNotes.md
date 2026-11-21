# Release Notes

## Release 0.9.1
Released 2026-11-18
- After *Flip curve*, the view is fitted to the whole module again.

## Release 0.9.0
Released 2026-11-18
- Module editor: a new *Ends* pane. Give end A or end B another end profile of the module's scale, and the module is drawn again with it. *Flip* turns an asymmetric end profile's plate the other side out; *Endewender* follows by itself.
- *Flip curve* in the *Edit* pane makes a curved module turn the other way. Section joints keep their place along the track; with tracks off the centre of the end plate, you are asked first, since their bends move.
- When you open a module, end profiles that have changed in your catalogue are updated in it. The *Archive* pane tells which; save the module to keep them in its file.
- The title of an end profile drawing leaves out words that are repeated, e.g. *FREMO/H0-RE B96L* instead of *FREMO/H0-RE H0 FREMO H0 B96L*.
- Help: a *?* on the start page, the catalogue, the settings and every pane of the editors opens help about it, and many fields have a hint below them.
- The texts say *end profile* throughout, not *plate*; the help explains how the end profile relates to the board at the module's end.
- The information page tells what this pre-release can do now, and what isn't ready yet.

## Release 0.8.0
Released 2026-11-17
- Module editor: divide a module into sections. Choose *Section joint* and click where the module is divided; a straight joint is added across the track there. Sections don't have to be equally long, and their lengths along the track are shown.
- Select a joint to type its exact distance from the start along the track, or the exact position of each of its points. Add points to make the joint bend, and drag joints and points in the drawing; the ends stay on the module's edge.
- Joints between sections always fit; end profiles are compared only where modules meet in a hall plan.
- The module's identification is changed beside the drawing and can be undone.

## Release 0.7.2
Released 2026-11-17
- *Settings* tells how to make the browser you use keep your settings and drafts, e.g. as an exception in Firefox's privacy settings, with this site's address to copy.
- Export your settings and language to a file, and import them again if the browser has forgotten them, or in another browser.
- The information page tells where your settings and drafts are kept.

## Release 0.7.1
Released 2026-11-17
- The app asks the browser to keep your settings and drafts as soon as it starts. *Settings* shows whether the browser keeps them, and can ask it again.

## Release 0.7.0
Released 2026-11-17
- Hall editor: *New hall…* on the start page starts from a rectangle. Draw other walls, drag corners and walls, or type the corners, wall lengths and arcs in the *Walls* pane. Hall measures are in metres; you can also type e.g. 450 mm.
- Add round and square pillars, furniture, doors, emergency exits and no-go zones. Select an item to resize it with its handles, and drag inside it to move it. A snap grid, 0.1 m unless you choose another, makes rectangles easy to draw.
- Doors stay in their wall. A selected door shows its distances from the corners to the opening, which you can also type.
- Mark the doors through which modules can be carried in and out; they get a blue double arrow. Emergency exits are green, with an emergency exit sign pointing towards the exit. Labels are always readable on top.
- The hall shows its floor area and its effective floor area: without pillars, furniture, the areas kept free in front of doors and no-go zones.
- *Print* prints the hall on a landscape page, with its size and floor areas in the middle.
- Numbers are shown and typed the way your language writes them, e.g. 12,5 in Swedish and 12.5 in English.
- A button in the upper right corner of every drawing fits the whole drawing in view again, also for module drawings.

## Release 0.6.0
Released 2026-11-16
- New module: end B can have another end profile than end A, also of another standard of the same scale, e.g. FREMO at one end and MMM at the other. Mark the module as an Endewender when an asymmetric end profile changes side. An asymmetric end profile, such as FREMO-H0-B96, is now mirrored at end B, so that its high side stays on the same side of the track and two such modules fit without mismatch.
- Module drawings show the kind of end profile the way DWG and SKP drawings do: the form of the end profile is folded into the module at each end, dark where the profile is above rail top and light where it is below. The end profile's name is written along each end, inside the module. Exit labels and arrows are no longer drawn.
- End profiles have a short name, e.g. *B96*, shown in module drawings. Without one, the designation is shown. The built-in FREMO end profiles have the short names *B96*, *B96f* and *2E96*.
- Module drawings fill the window, with the file actions beside them, and can be printed: *Print* prints the drawing alone with the module's name, fitted to a landscape page.
- A module is identified by the owner's FREMO prefix and a FREMO number, e.g. *SFjä 012*, by a name, or by both. A FREMO number needs the prefix; without one, the module needs a name. The owner's full name can be given too; it may be someone other than you. Enter them in *New module*, and change them in the *Properties* pane beside the module drawing. The app shows the FREMO designation with the name.
- The module's FREMO designation, e.g. *SFjä 7900*, or else its name, is written on the module, with the owner's name in smaller text under it. It runs along the track, centred between the ends, above the track as you read it.

## Release 0.5.0
Released 2026-11-15
- Drawings are saved as plain JSON files, named after the kind, e.g. *Station Nord.module.json* or *FREMO-H0-B96.endprofile.json*. They open in any JSON editor. Files saved with the earlier extensions (.tdmodule, .tdendprofile and so on) still open.
- Open and import accept any JSON file; the app reads the kind from the content. Other JSON files in a folder or ZIP archive are skipped.
- Scale setting: choose the scale you work in, and only standards and end profiles of that scale are offered, together with your country.

## Release 0.4.0
Released 2026-11-15
- Settings page, under Settings in the top menu: your user name, the country you work in, and your Module Registry API key. Settings are kept in your browser, also after updates, and are never saved in files.
- Revision log: when you save a drawing, your user name and the date are recorded in it, one entry per person with the last date. The log is shown in the properties panel. Without a user name nothing is recorded, and the problems indicator shows a warning.
- Standards that apply only in some countries, such as MMM in Sweden, are offered only when you choose one of those countries in the settings; their add-ons and end profiles likewise. Drawings that use them open as usual. Your own standards can name their countries.
- The start page no longer shows the version number; it is in the top bar.

## Release 0.3.0
Released 2026-11-15
- The information page is now the first page, with the sections General and Browser support. The start page with drafts and recent files is under Start in the top menu.
- Release notes in the app: click the bullhorn next to the version number. They are shown in your language.
- Problems indicator in the top bar: a green check, or a red sign with the number of problems in your own catalogue entries (standards, add-ons and compatibility overrides). Click a problem to open the entry and correct it.
- Choose the language with flags in the top bar.
- The app has a name in each language: Module Draw, Modulritaren, Modulzeichner and Modultegneren.
- German texts address you with "du".
- Icons also work offline.

## Release 0.2.0
Released 2026-11-15
- First public pre-release, for trying out. The app is now called **Module Draw**.
- An information page when the app starts, also opened from "Module Draw" in the top menu.
- End profile editor: draw the end profile's upper edge from a dimensioned drawing with lines, arcs and fillets (with the side of the fillet's centre), place tracks and screw holes, with automatic dimensions measured from rail top or the end profile's lower left corner.
- Print an end profile on a landscape A4 page, with paper-sized dimensions and the title "Standard Scale Designation".
- Create modules from templates, with a wait message while the module is drawn, and view end profiles, modules and halls.
- Catalogue of standards and end profiles (FREMO, MMM), with your own standards, add-ons and overrides; import end profiles from files.
- Resume work: Save, Edit and Discard buttons for each draft. Drafts are saved automatically in your browser; save your drawings to files to keep and share them.
- Works offline and can be installed as an app; texts in English, Swedish, German, Danish and Norwegian.
- Known limitations: the module, hall and hall plan editors are not ready yet. The file format may still change before version 1.0. Saving and opening files works best in Chrome or Edge on a desktop computer.

## Release 0.1.0
Released 2026-11-13
- First deployment of the app skeleton: installable and works offline.
