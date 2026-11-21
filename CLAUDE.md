# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project

Progressive web app for drawing model railway **end profiles**, **modules** and **hall plans** (all 2D). Blazor WebAssembly **standalone**, **AOT compiled** in Release, .NET 11, hosted on **Azure Static Web Apps**. No server code; data lives in the browser (IndexedDB) and is shared as JSON files.

Read these before changing behaviour:
- `docs/Specification.md` – requirements with stable IDs (CA, EP, MD, HA, HP, VA, RP, EX, AP, NF). Terminology: *hall plan* (not "layout plan").
- `docs/ImplementationPlan.md` – architecture, phases, traceability.

The domain follows the sibling repository **Module Registry** (`../Modules Registry App`, entities `Module`, `ModuleExit`, `ModuleEndProfile`, `ModuleStandard`, `ModuleOwnership`).

## Solution layout

`Tellurian.Trains.Drawing.slnx`. Shared settings in `Directory.Build.props` (target framework, version, NuGet metadata). Dependencies run one way: `App → Components → Editing → Model`, and Model uses the packages `Tellurian.Trains.Schedules.Model.Layouts`, `Model.Geometry`, `Importers.Drawing` (reading files) and `Exporters.Drawing` (writing files). The end profile with its parts, the catalogue (`DrawingCatalogue`, the one shared `ModuleStandard`, add-ons, overrides, validation), joint fit and the drawing values (`PathDrawing`, `Circle`, …) are the shared model's (mutable classes for things with identity, edited with `With()`; immutable records for values); `DrawingFileSerializer` hands end profiles and catalogues to the shared importer and exporter, and the other kinds follow (survey step 5). Gauges are lists (`EndProfileTrack.Gauges`, `ModuleStandard.Gauges`).
- Geometry – pure 2D math (poses, transforms, line/arc segments, distances), no domain knowledge – is the package `Tellurian.Trains.Schedules.Model.Geometry` from the Schedules repository (`../Tellurian.Trains.Schedules/Model.Geometry`), moved there 2026-11-21 (survey step 4). Change it there, with its tests.
- `Tellurian.Trains.Drawing.Model` – domain model, catalogue, joint fit evaluator, validation, file format. It uses the shared layout model, the package `Tellurian.Trains.Schedules.Model.Layouts` (operation location, station track, their rules), owned by the Schedules repository (`../Tellurian.Trains.Schedules/Model.Layouts`, design in its `Documentation/OperationLocations.md`). Names, units (metres) and meaning of those types are a contract between four systems: change them there, never here.
- `Tellurian.Trains.Drawing.Editing` – UI-independent editor state: commands, undo/redo, snapping, tools.
- `Tellurian.Trains.Drawing.Components` – Razor class library with **all** view and editor components and their collocated JS (`*.razor.js`). Reused by the Module Registry (Blazor Server), so it must work in any render mode: no WebAssembly-only APIs, JS interop only after first render, read-only display without JS.
- `Tellurian.Trains.Drawing.App` – Blazor WASM PWA: shell, pages, IndexedDB storage, file access, service worker. No drawing components here.
- `Tellurian.Trains.Drawing.App.Translations` – **all** the app's texts in one place: short UI strings in `Resources/AppTexts.<language>.resx`, longer texts (information page, release notes) as Markdown in `wwwroot/Content/<Name>.<language>.md`, read through `ContentDocuments` over HTTP (`_content/Tellurian.Trains.Drawing.App.Translations/Content`). Same principle as the Timetable Planning app's `Planning.App.Translations`.
- `*.Tests` – MSTest.Sdk on Microsoft Testing Platform (version pinned in `global.json`); Components and App tests use bUnit.

Model, Editing and Components are packable NuGet libraries (spec NF-10). Module Draw's model is moving into the Schedules repository step by step (its `Documentation/LayoutsLayerSurvey.md` §7 and `CrossAppReview.md` §0): build new model, geometry and validation code there, not here. Module auto-draw (spec MD-20) is a public API in Model so the Registry can generate module JSON server-side.

## Commands

```bash
dotnet build Tellurian.Trains.Drawing.slnx
dotnet test --solution Tellurian.Trains.Drawing.slnx
dotnet run --project Tellurian.Trains.Drawing.App            # Debug, interpreter (fast)
dotnet publish Tellurian.Trains.Drawing.App -c Release        # AOT, needs the wasm-tools workload
dotnet pack Tellurian.Trains.Drawing.Components               # pack libraries one by one (packing the .slnx would AOT-build the App)
```

## Rules

- Geometry and validation logic go in the libraries with unit tests, never in components.
- Units are millimetres and degrees. Module placements are rotated and translated, **never mirrored**; mirroring exists only for comparing end profiles.
- Heights are offsets from the standard's rail top (RÖK); 0 = standard height (normally 1300 mm).
- Use `System.Text.Json` source generation (trim/AOT safe); no reflection-based serialization.
- The module and end profile file formats are public contracts (spec EX-7): schema changes must be backward compatible within a major schema version and are logged. The JSON Schemas and examples in `schemas/` (not deployed) are generated from the code: after a deliberate change, run the Model tests with `TELLURIAN_UPDATE_SCHEMAS=1` and log it in `schemas/CHANGELOG.md`. The Module Registry builds its API from them.
- UI strings in English resources first; languages en, de, da, sv, nb (Tellurian.Localization). German addresses the user with "du", not "Sie": the users are hobbyists.
- Texts longer than a sentence or two, or with several paragraphs or headings, go in a Markdown document in the Translations project, not in RESX. Every document exists in all five languages (`Name.md` in English, `Name.sv.md` and so on); a test fails when one is missing.
- UI changes must be checked in a browser; the AOT Release build before releases.

- A module file is **one complete module with one drawing** (`Module.Drawing`). There are no variants here: a module that can be set up in several ways is several modules, and only the Module Registry knows that they belong together. Every module has a list of tracks; the operation location is optional.
- A hall plan refers to a module's exit by side and number (`ExitPlace`: Left 1, Right 2), never by its label.

## Local NuGet Development

This repo consumes `Tellurian.Trains.Schedules.Model.Layouts`, which may come from the local feed `C:\NuGets` during development. When iterating on it locally, bump the `<PackageReference Version="…" />` in `Tellurian.Trains.Drawing.Model.csproj` to the library's local patch version. CI will reject any local-only version with NU1101: the library must be published to nuget.org before this app can ship with its changes.

Full workflow: user-level `local-nuget-development` skill.

## Release

CI (`.github/workflows/build-and-deploy.yml`) builds and tests every push and pull request. A push to `master` deploys when `<Version>` in `Directory.Build.props` has no tag `v<Version>`; it then tags and creates a GitHub release. Every release needs a section in `Tellurian.Trains.Drawing.App.Translations/wwwroot/Content/ReleaseNotes.md` (`## Release x.y.z`, `Released yyyy-MM-dd`, at least one bullet; `- Maintenance update.` for internal-only changes). Release notes are translated into every UI language: `ReleaseNotes.sv.md`, `.de.md`, `.da.md` and `.nb.md` beside it get the same releases and dates (translated headings, e.g. `## Version x.y.z`), using the app's own UI terms; a test fails when a translation is missing or out of step. The app shows the notes in the UI language, falling back to English. Pull requests from this repository get an Azure preview environment.

## Azure

Static Web App `tellurian-trains-drawing` in resource group `Tellurian.Trains` (location West Europe; Static Web Apps aren't available in North Europe). Defined in `infra/azuredeploy.json` together with the deploy identity `id-trainsdrawing-deploy`. GitHub Actions logs in with OpenID Connect (GitHub environments `production` and `preview`) and reads the deployment token at run time; no credentials are stored in GitHub. See `infra/README.md`.
