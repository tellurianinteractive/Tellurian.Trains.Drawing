# ADR 0001 – SVG rendered by Blazor for all drawing canvases

Status: **Proposed** (2026-11-13). It becomes *Accepted* when the iPad test is done.
Spike: [pull request #1](https://github.com/tellurianinteractive/Tellurian.Trains.Drawing/pull/1), page `/spike/canvas`.

## Context
End profiles, modules and hall plans are drawn on a canvas that must pan, zoom and drag smoothly.
The target (spec NF-2) is a hall plan of **300 modules** at ≥ 30 fps, with drag responses within 50 ms.
The components must also run in the Module Registry under Blazor Server (NF-10). This favours
markup (SVG) over imperative drawing (an HTML canvas element), which would need JS for everything.

## Spike
The spike page renders 100–1000 modules in a 40 × 25 m hall. The mix of shapes follows the Module
Registry statistics. Each module is an SVG group with its outline and track paths, positioned with a
`transform`. Pan and zoom change the `viewBox` in JS. A module can be dragged in two ways:
- **JS mode**: JS moves the element directly, and .NET is told on drop;
- **.NET mode**: .NET re-renders the canvas on every animation frame.

## Results
| Environment | Modules | Result |
|---|---|---|
| Desktop Chrome, AOT Release build (preview) | 1000 | No problems displaying, panning, zooming or moving a module. |
| Desktop Firefox, AOT Release build (preview) | 1000 | No problems displaying, panning, zooming or moving a module. |
| Safari on iPad | – | *Pending.* |

So the desktop handles more than three times the 300-module target without visible problems.

## Decision (proposed)
1. All canvases (end profile, module, hall plan) are **SVG rendered by Razor components** in the `Components` library, in millimetres.
2. **Pan and zoom are handled in JS** by changing the `viewBox`. .NET is told the final view, debounced.
3. **While dragging, JS moves the element**, and the model is updated in .NET on drop. (The spike's JS mode.) In hall plans the dragged element is a whole **line** of modules, since single modules are not placed freely there (UseCases.md UC-8). Reordering and flipping are list operations, followed by one re-render.
   Snapping previews during a drag are computed in .NET at most once per animation frame, and only for the dragged item.
4. Static content uses `@key` and per-item components with `ShouldRender`, so that a change re-renders only what changed.
5. Read-only display (EX-8) needs no JS at all. Pan and zoom are an optional enhancement.

## Consequences
- No canvas fallback is needed, unless the iPad test shows otherwise.
- The JS module of the canvas is small and lives with the canvas component (`*.razor.js`). It must work under Blazor Server too, so it uses no synchronous interop.
- The spike code is thrown away. The real canvas is built in Phase 4.
