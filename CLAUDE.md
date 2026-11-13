# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project

Progressive web app for drawing model railway **end profiles**, **modules** and **hall plans** (all 2D). Blazor WebAssembly **standalone**, **AOT compiled** in Release, .NET 11, hosted on **Azure Static Web Apps**. No server code; data lives in the browser (IndexedDB) and is shared as JSON files.

Read these before changing behaviour:
- `docs/Specification.md` – requirements with stable IDs (CA, EP, MD, HA, HP, VA, RP, EX, AP, NF). Terminology: *hall plan* (not "layout plan").
- `docs/ImplementationPlan.md` – architecture, phases, traceability.

The domain follows the sibling repository **Module Registry** (`../Modules Registry App`, entities `Module`, `ModuleExit`, `ModuleEndProfile`, `ModuleStandard`, `ModuleOwnership`).

## Solution layout

`Tellurian.Trains.Drawing.slnx`. Shared settings in `Directory.Build.props` (target framework, version, NuGet metadata). Dependencies run one way: `App → Components → Editing → Model → Geometry`.
- `Tellurian.Trains.Drawing.Geometry` – pure 2D math (poses, transforms, line/arc segments, distances). No domain knowledge.
- `Tellurian.Trains.Drawing.Model` – domain model, catalogue, joint fit evaluator, validation, file format.
- `Tellurian.Trains.Drawing.Editing` – UI-independent editor state: commands, undo/redo, snapping, tools.
- `Tellurian.Trains.Drawing.Components` – Razor class library with **all** view and editor components and their collocated JS (`*.razor.js`). Reused by the Module Registry (Blazor Server), so it must work in any render mode: no WebAssembly-only APIs, JS interop only after first render, read-only display without JS.
- `Tellurian.Trains.Drawing.App` – Blazor WASM PWA: shell, pages, IndexedDB storage, file access, service worker. No drawing components here.
- `*.Tests` – MSTest.Sdk on Microsoft Testing Platform (version pinned in `global.json`); Components and App tests use bUnit.

Geometry, Model, Editing and Components are packable NuGet libraries (spec NF-10). Module auto-draw (spec MD-20) is a public API in Model so the Registry can generate module JSON server-side.

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
- The module and end profile file formats are public contracts (spec EX-7): schema changes must be backward compatible within a major schema version and are logged.
- UI strings in English resources first; languages en, de, da, sv, nb (Tellurian.Localization).
- UI changes must be checked in a browser; the AOT Release build before releases.

## Release

CI (`.github/workflows/build-and-deploy.yml`) builds and tests every push and pull request. A push to `master` deploys when `<Version>` in `Directory.Build.props` has no tag `v<Version>`; it then tags and creates a GitHub release. Every release needs a `RELEASENOTES.md` section (`## Release x.y.z`, `Released yyyy-MM-dd`, at least one bullet; `- Maintenance update.` for internal-only changes). Pull requests from this repository get an Azure preview environment.
