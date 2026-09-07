# RSSAM Changelog

## 1.0.31 - 2026-09-07

- Renamed the project from RSAM to RSSAM.
- Fixed `dotnet publish` failing with MSB3094 when the repository path contains an apostrophe (for example `Riggi's Software`) by publishing to a safe temporary staging directory before copying the validated payload to `artifacts\publish`.
- Isolated every publish run in fresh temporary `bin`/`obj` directories so Visual Studio/Roslyn file locks and stale files from the former RSAM project name cannot interfere with RSSAM release builds.
- Added `scripts/build-portable.ps1` to create self-contained x86 and x64 portable ZIP archives without an installer.
- Added the portable archives to the GitHub Actions release artifacts and tagged GitHub releases.

## 1.0.30 - 2026-09-04

- Added README badges for the GitHub Actions build/test status, the verified 21-test suite, Windows x86/x64 support, .NET 10, WinUI 3, the current release version and the zlib license.
- Linked the live build badge directly to the RSSAM GitHub Actions release workflow.
- Increased the RSSAM build version to **1.0.31**.

## 1.0.29 - 2026-09-04

- Fixed `RSSAM.Core.dll` and `RSSAM.API.dll` still being omitted from the Visual Studio UnitTests output directory.
- Added a dedicated `BuildingForUnitTests` MSBuild path that prevents runtime identifiers from being applied to Core/API project-reference builds used by the tests.
- Added an explicit direct `RSSAM.API` reference to `RSSAM.UnitTests` so both required RSSAM assemblies participate in copy-local resolution.
- Added post-build validation that stops immediately with a clear build error if either required assembly is missing from the UnitTests output.
- Kept runtime identifiers unchanged for the production x86/x64 application, publish and installer builds.
- Increased the RSSAM build version to **1.0.29**.

## 1.0.28 - 2026-09-04

- Fixed every unit test failing with `FileNotFoundException` because `RSSAM.Core.dll` was not available to the Visual Studio test host.
- Removed the unnecessary runtime identifier from the managed UnitTests output while retaining its x64 platform target.
- Made `RSSAM.UnitTests` framework-dependent and enabled runtime/dependency file generation for standard Visual Studio test discovery.
- Explicitly enabled copy-local behavior for NuGet and project-reference assemblies so `RSSAM.Core.dll` and `RSSAM.API.dll` are deployed beside the test assembly.
- Removed the runtime-specific argument from `scripts/test.ps1` to match Visual Studio's framework-dependent test-host behavior.
- Increased the RSSAM build version to **1.0.28**.

## 1.0.27 - 2026-09-04

- Fixed `RSSAM.UnitTests.dll` failing to load when Visual Studio used its x64 test host while the solution was configured for x86.
- Made `RSSAM.UnitTests` an explicit x64-only test project because the suite tests managed Core/API behavior and does not load the native Steam client.
- Added `RSSAM.UnitTests.runsettings` to select the x64 Visual Studio test host consistently.
- Mapped the solution's x86 configurations to the x64 UnitTests configuration while keeping all production projects native x86.
- Simplified `scripts/test.ps1` so it always runs the suite once with the matching x64 runtime and test host.
- Increased the RSSAM build version to **1.0.27**.

## 1.0.26 - 2026-09-04

- Added the dedicated `RSSAM.UnitTests` xUnit project to the solution for x86 and x64 configurations.
- Added tests for settings normalization, persistence and reset behavior.
- Added tests for favorites persistence, damaged JSON recovery, duplicate IDs and invalid App IDs.
- Added tests for achievement/statistic models, culture-aware numeric parsing, localization, search delegation and the binary Steam KeyValue parser.
- Added `scripts/test.ps1` with selectable x86/x64 execution and optional XPlat code coverage collection.
- Added the x64 unit-test suite to the GitHub release workflow before installer creation.
- Made settings and favorites storage directories injectable so tests never access real user data.
- Fixed possible temporary-file collisions between multiple settings or favorites service instances by using unique file names.
- Added validation for invalid Steam App IDs and missing required game/search data.
- Increased the RSSAM build version to **1.0.26**.

## 1.0.25 - 2026-09-04

- Fixed `ISteamClient018.GetISteamApps` native interop passing an invalid argument list on x86 and x64.
- Added the missing native object pointer to the `GetISteamApps` delegate and invocation.
- Applied the correct `ThisCall` declaration consistently with the other `ISteamClient018` methods.
- Fixed Steam returning a null `ISteamApps001` object while loading the owned-games catalog.
- Restored initialization of both `ISteamApps001` metadata access and `ISteamApps008` ownership checks.
- Made legacy `ISteamApps001` metadata optional so the catalog can still load with App-ID names and standard capsule URLs if Steam removes that interface in the future.
- Kept this Changelog English-only and outside all localization resource files.
- Increased the RSSAM build version to **1.0.25**.

## 1.0.24 - 2026-09-04

- Added a dedicated RSSAM application icon combining a game controller with an achievement medal.
- Added a seven-size Windows ICO resource for sharp rendering from 16 through 256 pixels.
- Embedded the icon in `RSSAM.exe` for Windows, taskbar, shortcut and installed-app presentation.
- Applied the same icon to the Inno Setup executable and uninstall registration.
- Replaced the generic TitleBar glyph with the new RSSAM icon.
- Added PNG source variants and displayed the application icon in the README.
- Kept this Changelog English-only and outside all localization resource files.
- Increased the RSSAM build version to **1.0.24**.

## 1.0.23 - 2026-09-04

- Fixed installer publishing failing because the unpackaged WinUI resource index was not copied to the publish directory.
- Explicitly enabled PRI generation and standardized the project resource index name as `resources.pri`.
- Added an MSBuild post-publish fallback that copies the generated PRI beside `RSSAM.exe`.
- Extended `publish.ps1` to recover either `resources.pri` or `RSSAM.pri` from architecture-specific build output.
- Kept strict publish validation so an installer cannot be created from a payload that would fail during WinUI startup.
- Fixed nullable-reference warnings in the SAM-derived binary KeyValue reader.
- Removed the unused backing event warning from the static Changelog page.
- Kept this Changelog English-only and outside all localization resource files.
- Increased the RSSAM build version to **1.0.23**.

## 1.0.22 - 2026-09-04

- Reorganized `README.md` into a complete end-user, developer, build, publishing, installer, release and troubleshooting guide.
- Added five README screenshots covering the Tile, List, TableView, favorites-only and Settings views.
- Added a full PowerShell script reference with parameters, defaults, outputs, prerequisites and example commands.
- Updated `set-version.ps1` to synchronize `Directory.Build.props`, the displayed-version fallback and the four-part Windows application manifest version.
- Made `set-version.ps1` validate every expected version location before writing changes and preserve UTF-8 without a byte-order mark.
- Made `build.ps1` stop immediately when `dotnet restore` or `dotnet build` returns a non-zero exit code.
- Kept version-specific release history in this English-only Changelog instead of duplicating it in the README or localization resources.
- Increased the RSSAM build version to **1.0.22**.

## 1.0.21 - 2026-09-04

- Added early startup diagnostics at `%LOCALAPPDATA%\\RSSAM\\Logs\\startup.log`, including a native error dialog when WinUI cannot create the application window.
- Made the publish script fail immediately when `dotnet publish` returns an error instead of allowing a partially generated installer payload.
- Added strict validation for the executable, RSSAM assemblies, WinUI.TableView, Windows App SDK resources and self-contained .NET runtime files before the installer can be built.
- Added a compatibility fallback that copies a generated `resources.pri` into the publish directory when Windows App SDK/MSBuild leaves it in the architecture-specific build output.
- Explicitly disabled single-file publishing and trimming for the folder-based self-contained WinUI deployment.
- Set the installation directory as the working directory for Start menu, desktop and post-install launches.
- Kept the supplied MSIX bundle helper out of the project because the unsigned RSSAM release uses a traditional Inno Setup installer instead of an MSIX package.
- Kept this Changelog English-only and outside all localization resource files.
- Increased the RSSAM build version to **1.0.21**.

## 1.0.20 - 2026-09-04

- Made the game-library TableView fully read-only and disabled column resizing, reordering, sorting and filtering.
- Replaced the interactive TableView favorite checkbox with a display-only favorite indicator so it cannot consume row clicks or modify favorites.
- Made a single click anywhere on a TableView row open the selected game's Achievements tab.
- Added `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe` to the automatic Inno Setup compiler search paths.
- Kept this Changelog English-only and outside all localization resource files.
- Increased the RSSAM build version to **1.0.20**.

## 1.0.19 - 2026-09-04

- Added native x64 builds alongside the existing x86 builds and made x64 the default architecture for direct project builds.
- Updated the Steam bridge to load `steamclient64.dll` in x64 workers and `steamclient.dll` in x86 workers.
- Corrected pointer-sensitive Steam callback layouts for the Windows x64 ABI while retaining the x86 callback layout.
- Added architecture-selectable build/publish scripts that can create x86, x64 or both outputs.
- Added an unsigned, per-user Inno Setup installer that automatically installs the x64 build on 64-bit Windows and the x86 build on 32-bit Windows.
- Added a GitHub Actions workflow that builds the unsigned setup and source archive and attaches both to tagged releases.
- Added the supplied fork, modification and zlib third-party notices to the central root `LICENSE.md`.
- Kept this Changelog English-only and outside all localization resource files.
- Increased the RSSAM build version to **1.0.19**.

## 1.0.18 - 2026-09-04

- Fixed the game-favorite UI build by importing WinUI's `Microsoft.UI.Xaml.Controls.Primitives` namespace for `ToggleButton`.
- Removed the invalid attempt to set `Handled` on WinUI `RoutedEventArgs` and removed the unnecessary favorite-button Click handler.
- Removed the unused `Pink.ico` and duplicate `LICENSE.txt` files from the `RSSAM.API` project.
- Renamed the single root license file from `LICENSE.txt` to `LICENSE.md` and updated all source, project, documentation and localized UI references.
- Kept this Changelog English-only and outside all localization resource files.
- Increased the RSSAM build version to **1.0.18**.

## 1.0.17 - 2026-09-04

- Centered the universal search box against the complete TitleBar instead of the remaining space after the application branding.
- Added a third, persisted Table view for the Steam game library using WinUI.TableView 1.4.1, with image, game name, App ID, type and favorite columns.
- Added per-game favorites to the Tile, List and Table views together with an icon-only favorites filter.
- Stored all favorite Steam App IDs in one atomically written `%LOCALAPPDATA%\\RSSAM\\favorites.json` file, independently from UI settings.
- Migrated `settings.json` to schema version 7 so the selected Tile, List or Table view is retained.
- Limited game-loading InfoBars to refreshes explicitly started by the Reload buttons; automatic startup loads and opening a game now remain unobtrusive.
- Mirrored loading progress, completion messages and errors into the status bar regardless of whether an InfoBar is shown.
- Kept this Changelog English-only and outside all localization resource files.
- Increased the RSSAM build version to **1.0.17**.

## 1.0.16 - 2026-09-03

- Moved the page toolbar into the NavigationView content column so the navigation pane now spans the full shell height and the toolbar sits only above page content.
- Added a non-auto-closing loading mode to the global InfoBar and display it while the Steam game catalog or a game's achievements and statistics are loading.
- Mirrored current loading messages into the lower-left status bar and restored the normal game-count status after each operation finishes.
- Stabilized the TitleBar layout so showing the Back button no longer shifts or resizes the search field.
- Reduced the normal TitleBar search width by 25 percent from 720 to 540 pixels while retaining responsive widths on compact windows.
- Changed RSSAM from four-part `1.x.x.x` versions to three-part `1.x.x` versions and normalized the complete release history accordingly.
- Kept this Changelog English-only and outside all localization resource files.
- Increased the RSSAM build version to **1.0.16**.

## 1.0.15 - 2026-09-03

- Added a centralized, semaphore-protected WinUI `DialogService` with automatic XamlRoot, theme and shared-style handling.
- Migrated game store/reset confirmations, reset-scope selection and settings reset confirmation away from duplicated page-local dialog code.
- Added a live Steam process indicator to the lower-left status bar, refreshed every three seconds without blocking the UI.
- Added localized Steam running, not-running and checking labels while keeping all Changelog content outside localization files.
- Marked C#, XAML, project and build-script sources as RSSAM-authored or SAM-derived, while preserving Rick's original copyright and zlib attribution in modified source files.
- Kept this Changelog English-only and outside all localization resource files.
- Increased the RSSAM build version to **1.0.15**.

## 1.0.14 - 2026-09-03

- Removed the visible RSSAM restart when a game is selected.
- Isolated native Steam catalog and game-stat operations in hidden, short-lived worker processes so every operation receives the correct Steam App ID without replacing the main UI process.
- Kept the main RSSAM window, navigation state and current page active while a selected game is loaded.
- Added icon-only Tile view and List view buttons to the upper-right corner of the Steam game page and persisted the selected view.
- Added a full-width ListView layout with proportional game artwork, game names and App IDs.
- Fixed cropped game artwork in the tile layout by using Steam's capsule aspect ratio and proportional `Uniform` scaling.
- Migrated `settings.json` to schema version 6 for the persisted game-library view.
- Kept this Changelog English-only and outside all localization resource files.
- Increased the RSSAM build version to **1.0.14**.

## 1.0.13 - 2026-09-03

- Fixed selected-game initialization by restarting RSSAM with the chosen Steam App ID before the native Steam client is loaded, waiting for the previous process to exit and then reopening that game automatically.
- Fixed the Language and Theme controls showing an empty value by populating localized items before restoring their selections and refreshing the displayed selection after a language change.
- Made the Games navigation item always return to the Steam game list, including when the item is already selected while game details are open.
- Removed the Restore last page and Start page settings together with persisted page, game-detail, tab, filter and statistics-editing session state.
- Migrated `settings.json` to schema version 5; obsolete properties from older files are ignored and removed on the next save.
- Kept this Changelog English-only and outside all localization resource files.
- Increased the RSSAM build version to **1.0.13**.

## 1.0.12 - 2026-09-03

- Fixed repeated Steam client initialization in the combined RSSAM process by retaining `steamclient.dll` until process exit.
- Serialized native Steam client sessions and their non-thread-safe pipe/user lifecycle operations.
- Added a bounded retry when Steam temporarily refuses to create its IPC communication pipe.
- Replaced the raw `failed to create pipe` notification with localized, actionable Steam connection guidance.
- Moved Steam game-session initialization, statistics storage and statistics reset work off the UI thread.
- Kept this Changelog English-only and outside all localization resource files.
- Increased the RSSAM build version to **1.0.12**.

## 1.0.11 - 2026-09-03

- Made the English `CHANGELOG.md` the single source for the in-app Changelog.
- Removed all Changelog content and navigation keys from the German and English localization files.
- Added bounded, exception-safe parsing for Steam binary KeyValue schema files, including truncated-file and excessive-nesting protection.
- Fixed an out-of-bounds read in bounded native UTF-8 string conversion and guaranteed unmanaged buffer cleanup when Steam calls fail.
- Added reference-counted loading of `steamclient.dll` and moved native library ownership into each Steam client session.
- Made callback dispatch exception-safe so every native callback is released and callback pump failures reach the active load operation.
- Fixed statistics cancellation so user cancellation is no longer reported as a timeout.
- Serialized game load/save/reset operations and prevented stale or overlapping catalog requests from replacing current UI state.
- Added preflight validation for protected, non-finite, out-of-range, increment-only and maximum-change statistic values.
- Debounced search-state persistence and made settings-file writes synchronized and cleanup-safe.
- Centralized the displayed application version and increased the RSSAM build version to **1.0.11**.

## 1.0.10 - 2026-09-02

- Moved all shell toolbar commands and Settings actions to consistent left-aligned layouts.
- Expanded `settings.json` into the central persistent state for appearance, shell, navigation, window placement, startup behavior, search contexts and manager/session state.
- Added saving and restoring of window size, window position and maximized state.
- Added **Start maximized**, **Remember window size and position**, **Restore last page** and **Start page** settings.
- Persisted navigation pane state, last shell page, last opened game, selected game tab, achievement filter, statistics-editing state and search text per search context.
- Added a direct **Open settings file** button to Settings and the Settings toolbar.
- Completed German and English localization for all new settings, commands and the full in-app changelog.
- Rebuilt the in-app Changelog page to show the complete release history from `1.0.0` through `1.0.10`.
- Kept the global floating InfoBar and ShellPage architecture introduced in `1.0.9`.
- Increased the RSSAM build version to **1.0.10**.

## 1.0.9 - 2026-09-02

- Introduced `ShellPage` as the central WinUI shell below `MainWindow`/TitleBar.
- Moved toolbar, NavigationView, ContentFrame and statusbar into `ShellPage`.
- Added page-driven shell contracts for toolbar commands, universal TitleBar search, back navigation and status text.
- Added a global `InfoBarService` that creates an `InfoBar` inside a `Popup` on the shell `XamlRoot`, so notifications float above toolbar, navigation and page content.
- Replaced page-specific InfoBars with the global floating InfoBar service.
- Added severity-specific InfoBar styles using the supplied light/dark brushes.
- Reworked shell/card/dialog resources around the supplied `App*Brush` palette.
- Settings and changelog are now real pages hosted by the shell `ContentFrame`; game picker/detail remain together in a cached `ManagerPage`.
- Kept all Markdown documentation in the repository root.
- Source-ZIP generation excludes `.git`, `.vs`, `artifacts`, `bin` and `obj` in every directory depth.
- Increased the RSSAM build version to **1.0.9**.

## 1.0.8 - 2026-09-02

- Fixed localization loading so German and English strings resolve instead of displaying resource keys such as `Settings.Title`.
- Localization resources now have an explicit manifest `LogicalName` and are also copied to the output/publish directory as a runtime fallback.
- Made `LocalizationService` resilient to manifest-name changes and physical-resource fallback paths.
- Removed the empty band between toolbar and NavigationView/content by disabling the unused NavigationView header area with `AlwaysShowHeader=False`.
- Kept the reference shell geometry at **40 px TitleBar / 64 px toolbar / flexible content / 32 px statusbar**.
- Consolidated all Markdown documentation into the repository root. Project-folder README files were merged into `PROJECTS.md`.
- Increased the RSSAM build version to **1.0.8**.

## 1.0.7 - 2026-09-02

- Fixed the incompatible `src` solution folder by using Visual Studio's correct Solution Folder project type GUID.
- Added a central localization service to `RSSAM.Core`.
- Added German (`de-DE`) and English (`en-US`) UI resources.
- Added a language selector to Settings with immediate runtime switching.
- Persisted the selected language in `%LOCALAPPDATA%\RSSAM\settings.json`.
- Localized navigation, global search placeholders, CommandBar labels, settings, status messages, dialogs and core error/progress messages.
- Achievement/statistic schema localization now prefers Steam `german` or `english` according to the selected RSSAM language.
- Increased the build version to `1.0.7`.

## 1.0.6 - 2026-09-02

- Restructured the solution into `src/RSSAM.App`, `src/RSSAM.Core` and `src/RSSAM.API`.
- Moved domain models, settings persistence, global-search contracts and Steam/game services into `RSSAM.Core`.
- Reorganized the native Steam bridge into `Callbacks`, `Client`, `Common`, `Interfaces`, `Native`, `Types` and `Wrappers`.
- Added shared shell resource dictionaries under `RSSAM.App/Resources/Styles`.
- Unified TitleBar, global toolbar, NavigationView and status bar backgrounds so Mica/Acrylic flow through the shell without visible separator bands.
- Aligned the custom TitleBar buttons with the reference shell: compact 28 px controls, transparent background, no explicit border and 6 px corner radius.
- Standardized the shell geometry to a 40 px TitleBar, 64 px toolbar and 32 px status bar.
- Updated publish/build paths for the new source layout.
- Increased the RSSAM build version to **1.0.6**.

## 1.0.5 - 2026-09-02

- Made the RSSAM shell responsive across wide, compact and narrow window sizes.
- The full `Riggi's Steam Achievement Manager` product name in the TitleBar is hidden automatically on smaller widths while `RSSAM` remains visible.
- The universal TitleBar search keeps the same maximum width and shrinks fluidly when less space is available.
- CommandBar labels collapse automatically on compact windows while dynamic overflow remains enabled.
- Navigation is forced into compact mode on narrow windows so the content area remains usable.
- `SettingsRow` now switches to a two-row layout on narrow widths so descriptions and controls no longer fight for horizontal space.
- Removed the separate **About RSSAM** item from the main navigation.
- Moved the complete RSSAM information, original-author attribution and zlib license information into **Settings**.
- Improved responsive margins for the games page, game details, changelog and settings.
- Increased the RSSAM build version to **1.0.5**.

## 1.0.4 - 2026-09-02

- Removed the separate Steam App-ID search/input from the TitleBar.
- Replaced page-specific search boxes with one fixed-width **universal TitleBar search**.
- Added a modular `IGlobalSearchProvider`/`DelegateSearchProvider` system so future pages can register their own search behavior.
- Global search now filters **games**, **achievements** and **statistics** depending on the active context.
- Refined the TitleBar back/hamburger controls to a compact WinUI-style appearance and order: Back, Menu, RSSAM.
- Reworked Settings to follow the supplied SettingsPage layout: centered responsive content, section headers and reusable SettingsRow cards.
- Added **Mica / Acrylic / Standard** window backdrop settings.
- Added **Show status bar** as a persisted JSON setting.
- Updated settings schema to version 2.
- Increased the RSSAM build version to **1.0.4**.

## 1.0.3 - 2026-09-02

- Moved the **game search** into the custom TitleBar.
- Moved the **Steam App-ID input** into the custom TitleBar.
- Kept the **achievement search** in the TitleBar and show it only on the Achievements tab.
- Added a custom **hamburger menu button** on the left side of the TitleBar.
- Moved the **back button** to the left side of the TitleBar.
- Added a bottom **status bar** with the loaded game count on the left and the version number on the right.
- Introduced the original four-part versioning scheme used by early releases.

## 1.0.2 - 2026-09-02

- Added a **Settings** entry at the bottom of the WinUI 3 `NavigationView`.
- Added a **Changelog** entry directly above Settings.
- Added JSON settings persistence at `%LOCALAPPDATA%\\RSSAM\\settings.json`.
- Added **System**, **Light** and **Dark** appearance modes.
- Added **Compact** and **Expanded** navigation modes.
- Added startup, confirmation, success-message and default statistics-editing preferences.
- Added one global **CommandBar** whose commands change with the active page/tab.
- Retained the x86-only Steam interop and the `NativeWrapper<TNativeFunctions>` compatibility fix.

## 1.0.1 - 2026-09-02

- Fixed early UI initialization issues that could trigger `NullReferenceException` during XAML loading.
- Fixed Steam native-wrapper marshaling compatibility for mixed `class` and `struct` interface definitions.
- Reaffirmed **x86-only** Steam interop to match the original SAM implementation.

## 1.0.0 - 2026-09-02

- Initial **RSSAM - Riggi's Steam Achievement Manager** release.
- WinUI 3 / .NET 10 frontend.
- Combined game picker and per-game achievement/statistics manager into one application.
- Preserved the original Rick (Gibbed) zlib license and attribution.
