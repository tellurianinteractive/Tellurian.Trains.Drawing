# ADR 0003 – Localization with Tellurian.Localization; culture chosen before .NET starts

Status: **Accepted** (2026-11-13).
Spike: [pull request #3](https://github.com/tellurianinteractive/Tellurian.Trains.Drawing/pull/3), page `/spike/localization`.

## Context
The UI must be available in English, German, Danish, Swedish and Norwegian (spec AP-2), with short
texts in RESX and longer help texts in Markdown (AP-6). It has to work in the trimmed, AOT-compiled
WebAssembly app, offline, and the components must also work in the Module Registry (Blazor Server, NF-10).

## Spike
- **Culture:** the user's choice is stored in `localStorage` and passed to `Blazor.start({ applicationCulture })` in `index.html`, before .NET starts. Changing language stores the new culture and reloads.
- **RESX:** the Tellurian.Localization RESX provider, with a marker type instead of a generated designer class.
- **Markdown:** `AddHttpMarkdownResourceProvider("Content", neutralLanguage: "en")`. There is deliberately no Norwegian file, to test the fallback.
- **Offline:** the service worker caches `.md` files. Before the spike it didn't, so help would have been missing offline.

## Results (AOT build, desktop browser)
| Test | Result |
|---|---|
| Switching between en, sv, de, da and nb | Works. Texts, culture and satellite assemblies follow the choice. |
| RESX translations | Works. Unit tests cover all five languages and the fallback to English. |
| ICU formatting and sorting (e.g. Swedish Å Ä Ö) | Works, without `BlazorWebAssemblyLoadAllGlobalizationData`. |
| Markdown in the chosen language | Works. |
| Markdown fallback for a missing language (nb) | **Failed at first**: the app's HTML page was shown. Fixed, see below. |
| Trimming/AOT warnings | None. |

### The fallback failure
Azure Static Web Apps answers unknown paths with `index.html` and **status 200**, so that client-side
routes work (`navigationFallback`). The missing `help.nb.md` therefore returned the app's HTML page,
and the Markdown provider took it for the translation. The Debug dev server does the same
(`StaticWebAssetSpaFallbackEnabled`).

## Decision
1. **Tellurian.Localization** for all UI texts: RESX for short strings, Markdown over HTTP for help. In the Module Registry (Blazor Server) the file-based Markdown provider is used instead, behind the same interface.
2. **The culture is chosen before .NET starts** (`applicationCulture` in `index.html`). The choice is kept in `localStorage`, and changing language reloads the app. `BlazorWebAssemblyLoadAllGlobalizationData` isn't needed, which keeps the download small.
3. **Content files are excluded from the navigation fallback** in `staticwebapp.config.json`: `/Content/*`, `*.md`, and the drawing file types. A missing file returns 404, so the provider falls back to the neutral language.
4. **The service worker caches `.md` files**, so help works offline.
5. **Suggested library improvement:** `HttpMarkdownResourceProvider` in Tellurian.Localization should treat a response with `Content-Type: text/html` as *missing*, and so should a network error while offline (fall back to the neutral file). That would protect every app behind an SPA fallback, including the Debug dev server, without relying on host configuration.

## Consequences
- Changing language reloads the app. That's acceptable, since it rarely happens, **provided no data is lost** (spec AP-7): the reload goes through the safe-reload service, which writes all unsaved drawings as drafts, waits for confirmation, and reopens them in the same state afterwards. If the drafts can't be written, the language isn't changed until the user has saved.
- Help files must exist at least in English, the neutral language. The other languages are optional.
- Help for a language that isn't cached can't be fetched while offline. With improvement 5 this falls back to English; without it, it shows an error.
