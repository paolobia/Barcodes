# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A Blazor WebAssembly (.NET 8) single-page app for managing barcodes: scan/generate barcodes, tag them with a name and GPS coordinates, and sort the list by proximity to the user's current location. All data lives in browser `localStorage` — there is no backend API and no database. It's distributed as a PWA (installable from the browser over HTTPS) — there is no native Android/APK packaging.

The UI text and code comments are in Italian.

## Commands

Build/run (from the repo root, where `Barcodes.csproj` lives):
```
dotnet build Barcodes.csproj
dotnet run                       # serves on the URL in Properties/launchSettings.json
dotnet run --urls=https://192.168.1.101:5134   # LAN URL used during on-device testing (see leggimi.txt)
dotnet watch run --project Barcodes.csproj      # hot-reload dev loop
dotnet publish -c Release --nologo              # outputs to bin/Release/net8.0/publish/wwwroot
```
There is no test project in this repo — do not invent test commands.

VS Code tasks (`.vscode/tasks.json`) wrap the same `build`/`publish`/`watch` commands.

Deploy is automated via GitHub Actions (`.github/workflows/deploy.yml`): every push to `main` publishes the app and deploys it to GitHub Pages at `https://paolobia.github.io/Barcodes/`. There is no other production deployment — the previous `barcodes.bialive.it` host has been decommissioned; do not reference it or the old `bupdate.sh`/`nupdate.sh` deploy scripts.

## Architecture

**Standard Blazor WASM composition root**: `Program.cs` registers `BarcodeService` and `GenerateBarcode` as scoped DI services and mounts `App.razor` → `Layout/MainLayout.razor` → routed pages in `Pages/`. There's currently one page, `Pages/Home.razor`, which contains essentially all app logic (list/search/add/edit/delete/import/export) directly in its `@code` block — there is no separate view-model or state-management layer.

**Data model**: `Model/BarcodeItem.cs` (namespace `Barcodes.Models`) — `Name`, `Code`, `Label` (barcode symbology, e.g. EAN13/CODE128/CODE39/EAN8/UPC), `Latitude`, `Longitude`. `Name` is the unique key (normalized to uppercase by `BarcodeService`) — there is no separate ID field.

**Persistence — `Services/BarcodeService.cs`**: holds the barcode list in an in-memory field, lazily loaded from and written to `localStorage` (key `barcodes_data`) via `IJSRuntime` JS interop calls to `localStorage.getItem`/`setItem`. All CRUD (`SaveBarcodeAsync`, `DeleteBarcodeAsync`, `SearchByNameAsync`) operates on that in-memory copy and re-persists after every mutation. `ExportToJsonAsync`/`ImportFromJsonAsync` serialize/deserialize the whole list as JSON for the manual backup/restore feature in the UI (there's no sync to any server).

**Barcode image generation — `Services/GenerateBarcode.cs`**: renders a data-URL PNG by calling the JS function `generateBarcodeSync` (defined in `wwwroot/index.html`, backed by `JsBarcode.all.min.js`) via `IJSInProcessRuntime` (synchronous JS interop, required because `Generate` is called inline from Razor markup, e.g. `@GenerateBarcode.Generate(...)`). If no symbology (`tipo`/Label) is given, it's inferred from the code's digit length (8→EAN8, 12→UPC, 13→EAN13, 14→ITF14, else CODE128).

**Barcode scanning**: not part of the C# service layer — it's plain JS in `wwwroot/index.html` using `Quagga` (`quagga.min.js`). `startScanner()`/`stopScanner()` drive the live camera decode loop and write the detected code directly into the `#barcode-input` DOM element that Home.razor's `editingBarcode.Code` is bound to.

**Other JS interop surface (all defined inline in `wwwroot/index.html`, called from `Home.razor` via `IJSRuntime`)**:
- `getCurrentLocation()` — wraps `navigator.geolocation` in a Promise; used both to sort the list by distance on load (`LoadBarcodes`, Haversine distance in `Home.razor`) and to fill in Lat/Lng when adding/editing an entry.
- `downloadFile(filename, content, contentType)` — triggers a browser download for JSON export.
- `selectAndReadFile(accept)` — opens a file picker and resolves with file text, used for JSON import.
- A version-check on `DOMContentLoaded` that fetches `https://paolobia.github.io/Barcodes/manifest.webmanifest` and alerts the user if the deployed version differs from the one cached in `localStorage['app-version']` — this is hardcoded to the GitHub Pages URL and will always hit that URL regardless of where the app is served from.

**PWA/service worker**: standard Blazor WASM PWA setup — `wwwroot/service-worker.js` (dev) / `service-worker.published.js` (prod, referenced from the `.csproj`), asset manifest generated at publish time as `service-worker-assets.js`. `wwwroot/manifest.webmanifest` carries the app `version` field that the in-page version check compares against.

**`wwwroot/barcodes.html`**: standalone static help page, linked from the app's UI as "HELP" (`https://paolobia.github.io/Barcodes/barcodes.html`) — not part of the Blazor render tree.
