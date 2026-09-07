# RSSAM architecture

## Dependency direction

```text
RSSAM.App  ->  RSSAM.Core  ->  RSSAM.API
                   ^
                   |
            RSSAM.UnitTests
```

### RSSAM.App

WinUI 3 presentation and composition layer. `MainWindow` owns window chrome and the custom TitleBar. `ShellPage` owns the page toolbar, NavigationView, ContentFrame and statusbar. Content pages provide toolbar/search/back/status state through shell contracts. No native Steam interop belongs here.

### RSSAM.Core

Reusable RSSAM application/domain layer. It owns models, JSON settings storage, the modular global-search contracts/providers, Steam catalog/game-stat orchestration and Valve schema parsing.

### RSSAM.API

Native x86/x64 Steam client interoperability derived from Steam Achievement Manager. It remains isolated so native vtable/marshalling changes do not leak into WinUI code.

### RSSAM.UnitTests

Independent, framework-dependent x64 xUnit test project for Core and API behavior. It exercises isolated storage directories, settings normalization, favorites, models, localization, search, native-wrapper guards and Steam KeyValue parsing without starting WinUI or opening a Steam session. The production projects retain native x86 and x64 configurations; only the managed test host is fixed to x64 for reliable Visual Studio discovery and execution. The `BuildingForUnitTests` MSBuild property removes production runtime identifiers from referenced Core/API builds, allowing their assemblies to be copied locally beside the test assembly. A post-build check prevents tests from starting with an incomplete output directory.

The current worker architecture loads `steamclient.dll` for x86 or `steamclient64.dll` for x64 and retains the selected module until process exit. Native client sessions are serialized because the low-level Steam pipe and user lifecycle operations are not thread-safe, and because RSSAM hosts the former picker/game workflows inside one process.

## Shell geometry

```text
40 px  TitleBar
*      NavigationView: full-height pane + content column
       Content column: 64 px page toolbar + active page
32 px  statusbar (optional)
```

All shell chrome uses the same translucent theme surface. Mica/Acrylic therefore continues visually from the TitleBar through the toolbar and navigation to the statusbar without explicit separator lines.

## Future modules/pages

New UI modules should be added below `RSSAM.App/Presentation/Views` and register stable navigation IDs under `Composition/Navigation`. Searchable modules should implement/register an `IGlobalSearchProvider` from `RSSAM.Core/Interfaces` rather than adding another TitleBar search box.

## Localization

UI and Core text are resolved through `RSSAM.Core/Localization/LocalizationService`.
Language resources are embedded JSON files under `RSSAM.Core/Localization/Resources`.
New modules should request strings by key from `ILocalizationService` instead of hardcoding German or English text in service errors and runtime messages.

The Changelog is the deliberate exception: it is always English and is loaded from the embedded root `CHANGELOG.md`, so release notes are maintained in one place and are not part of the localization dictionaries.
The selected language is persisted in `AppSettings.Language`.

Initial languages:

- `de-DE` – Deutsch
- `en-US` – English

## Documentation placement

All Markdown documentation is stored in the repository root. Source folders under `src/` contain no `.md` files. Project-specific notes for `RSSAM.App`, `RSSAM.Core` and `RSSAM.API` are consolidated in `PROJECTS.md`.

## Global InfoBar

Runtime notifications are routed through `RSSAM.App/Services/InfoBarService`. The service creates an `InfoBar` inside a WinUI `Popup` bound to the shell `XamlRoot`, so messages float above toolbar, navigation and page content. `ShellPage.xaml` keeps one collapsed `GlobalInfoBar` only as a reserve/compatibility element.

Severity-specific styles use the shared `AppInfoBar*` brushes from `Resources/Styles/Colors.xaml`.

## Game library and loading feedback (1.0.17)

The `NavigationView` owns the complete shell area above the status bar. Its pane therefore starts directly below the TitleBar, while its content column contains the optional 64 px page toolbar followed by the active page.

The game library provides Tile, List and WinUI.TableView-based Table modes. The selected mode remains in `settings.json`; favorite Steam App IDs are stored separately and atomically in `%LOCALAPPDATA%\RSSAM\favorites.json` by `GameFavoritesService`.

`ManagerPage.StatusText` exposes catalog/game-data progress, completion and failure messages to the shell status bar. An InfoBar accompanies the operation only when a user explicitly starts it with a Reload button. Automatic startup loading and the initial load after selecting a game therefore do not create page-navigation notifications.

The TitleBar leading column has a responsive but state-independent width, so toggling Back-button visibility cannot move the search field. The regular search width is 540 px, with smaller fixed responsive widths below 1000 px.

## Central dialogs (1.0.15)

All WinUI `ContentDialog` instances are displayed through `RSSAM.App/Services/DialogService.cs`. A semaphore serializes dialog requests for the active XamlRoot, and the service applies the current window theme plus the shared RSSAM dialog styles. Pages use the service for confirmation and multi-choice flows instead of maintaining local dialog implementations.

## Steam process status (1.0.15)

`SteamProcessStatusService` performs an exception-safe process lookup in the Core layer. `ShellPage` runs that lookup away from the UI thread every three seconds and displays the localized result with a green or red indicator in the lower-left status bar.

## Isolated Steam workers (1.0.14)

The selected native Steam client module and its Steam App ID context are process-wide. The visible WinUI application therefore does not load the native Steam client directly. Public Core services serialize a request, start the same architecture of the executable in hidden worker mode and read the serialized result after that worker exits.

## Architecture and installer outputs (1.0.19)

The solution exposes x86 and x64 configurations. Shared MSBuild properties map them to `win-x86` and `win-x64`, and the publish script keeps the self-contained outputs in separate directories. The x64 compilation symbol selects the 64-bit Steam callback packing where pointer alignment changes native field offsets.

The Inno Setup definition is compiled once per selected architecture. It creates separate unsigned `win-x86` and `win-x64` per-user installers, and each setup contains only its matching publish directory. Application settings and favorites remain in `%LOCALAPPDATA%\RSSAM` when the program is uninstalled.

Catalog operations run with no App ID. Game load, store and reset operations run in a fresh worker whose `SteamAppId` environment variable is set before native initialization. The main RSSAM process and window remain open, and the temporary request/response directory is removed after every operation. This avoids cross-game native state reuse without a visible application restart.

## Persistent application state (1.0.14)

`%LOCALAPPDATA%\RSSAM\settings.json` is the central persisted UI state store. `RSSAM.Core/Models/AppSettings.cs` defines the complete schema and `AppSettingsService` normalizes, loads, resets and atomically saves it. Game favorites deliberately use the independent `%LOCALAPPDATA%\RSSAM\favorites.json` store so a settings reset does not delete them.

Persisted state includes language/theme/backdrop, navigation mode and pane state, statusbar visibility, the Tile/List/Table game-library view, startup behavior, window bounds/maximized state, confirmation preferences, the default statistics-editing preference and global-search text per search context.

All visible application settings are loaded into their controls when `SettingsPage` opens and every change is saved immediately. Window and shell state are also persisted by the shell/window lifecycle. Page and selected-game detail state are intentionally not restored.

## Toolbar alignment

Shell toolbar commands are intentionally placed in the left toolbar host. New page toolbar items should use `ShellToolbarItemPlacement.Left`; the shell also renders legacy placement values in the left host to keep the command layout consistent.
