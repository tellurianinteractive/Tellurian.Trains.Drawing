# ADR 0002 – Files first, with a fallback per browser; IndexedDB as workbench

Status: **Proposed** (2026-11-13). It becomes *Accepted* when the remaining tests are done.
Spike: [pull request #2](https://github.com/tellurianinteractive/Tellurian.Trains.Drawing/pull/2), page `/spike/files`.

## Context
The use cases (UseCases.md) make **JSON files the primary result**. IndexedDB holds work in progress:
drafts, recent files and module collections. A hall plan gets its module files from a **ZIP archive**
exported from the Module Registry, or from a **shared folder**. Browsers differ a lot in their
file-system support. Editing is done on desktop (spec AP-3), so the desktop browsers decide.

## Results so far

### Capabilities
| Capability | Firefox | Chrome / Edge | Safari (macOS) |
|---|---|---|---|
| File System Access API: open/save pickers, folder picker | ✕ | *pending* | *pending* |
| Launch queue (open files from the OS) | ✕ | *pending* | *pending* |
| Install as an app | ✕ (desktop Firefox can't install web apps) | *pending* | *pending* |
| IndexedDB | ✓ | *pending* | *pending* |
| Persistent storage API | ✓ | *pending* | *pending* |

### Measurements
| Test | Firefox | Chrome / Edge | Safari (macOS) |
|---|---|---|---|
| ZIP import of 300 module files: unzip and parse in .NET (AOT) | **18 ms** | *pending* | *pending* |
| Folder fallback (folder upload) | *pending* | – | *pending* |
| Draft resumed after reload | *pending* | *pending* | *pending* |
| Download / upload round trip | *pending* | – | *pending* |
| Save in place, re-open recent file, re-read folder | – | *pending* | – |
| Persistent storage granted | *pending* | *pending* | *pending* |

## Decision (proposed)
1. **A file service with two implementations, chosen by capability detection:**
   - **File System Access API** (Chromium): open, save in place, save as, recent files re-opened from stored handles, a shared folder re-read from its stored handle, and manifest file handlers;
   - **Fallback** (Firefox, Safari): "Save" means **download**, "Open" means upload, and a folder is a folder upload that has to be picked again. The UI says this plainly, e.g. "Save (download)".
2. **ZIP archives are read and written in .NET** (`System.IO.Compression`), the same way in every browser. It's fast enough that no progress UI is needed for typical meeting sizes.
3. **IndexedDB is the workbench**: autosaved drafts with an unsaved-changes flag, recent files (with handles where supported), module collections, the user catalogue and settings. The start page offers to resume drafts. The app requests **persistent storage** on the first save, and it reminds the user to save a file when a draft has unsaved changes. A draft is never the only copy of finished work.
4. **Installation is optional.** The app works offline in a normal tab, thanks to the service worker. The help explains which browsers can install the app and what Firefox users miss: installing, file handlers and save in place.

## Consequences
- Firefox and Safari users save by downloading. That works, but it creates new files rather than overwriting the old one. The app should suggest the previous file name, so that the browser's "replace" prompt can be used where there is one.
- In Firefox and Safari, re-reading a shared folder means picking it again. In Chromium it's one click.
- The file service is an interface in the App, with the two implementations behind it. Components never touch files directly (spec NF-10).
