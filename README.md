# RSSAM

<p align="center">
  <img src="src/RSSAM.App/Assets/RSSAM-AppIcon-256.png" width="128" height="128" alt="RSSAM application icon">
</p>

<p align="center">
  <img alt="Unit tests: 21 passed" src="https://img.shields.io/badge/unit_tests-21_passed-brightgreen">
  <img alt="Windows x86 supported" src="https://img.shields.io/badge/Windows-x86-0078D4?logo=windows11&amp;logoColor=white">
  <img alt="Windows x64 supported" src="https://img.shields.io/badge/Windows-x64-0078D4?logo=windows11&amp;logoColor=white">
  <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&amp;logoColor=white">
  <img alt="WinUI 3" src="https://img.shields.io/badge/UI-WinUI_3-0078D4">
  <img alt="Version 1.0.30" src="https://img.shields.io/badge/version-1.0.30-blue">
</p>

RSSAM (Riggi's Steam Achievement Manager) is an unpackaged WinUI 3 desktop application for viewing and managing Steam achievements and statistics on Windows.

Current version: **1.0.30**  
Supported architectures: **x86 and x64**

> [!CAUTION]
> RSSAM changes achievement and statistic data through the Steam client. Use it carefully and only with games and accounts for which you understand the consequences. RSSAM is not affiliated with or endorsed by Valve Corporation.

## Contents

- [Features](#features)
- [Screenshots](#screenshots)
- [System requirements](#system-requirements)
- [Installation](#installation)
- [Using RSSAM](#using-RSSAM)
- [Local data and diagnostics](#local-data-and-diagnostics)
- [Troubleshooting](#troubleshooting)
- [Developer requirements](#developer-requirements)
- [Repository structure](#repository-structure)
- [Build and release scripts](#build-and-release-scripts)
- [GitHub Actions releases](#github-actions-releases)
- [Versioning](#versioning)
- [License and attribution](#license-and-attribution)

## Features

- Loads the Steam game catalog and displays header images from Steam.
- Provides tile, list, and read-only TableView layouts.
- Opens a game's achievements and statistics by selecting its card, list row, or table row.
- Stores favorite games locally and provides a favorites-only filter.
- Searches games globally from the title bar and searches achievements on the manager page.
- Reads, unlocks, locks, and stores achievements.
- Reads and edits supported integer and floating-point statistics.
- Protects destructive actions with configurable confirmation dialogs.
- Supports English and German UI resources, light/dark/system themes, and Mica, Acrylic, or standard backdrops.
- Shows Steam process state, progress, completion messages, and the application version in the status bar.
- Shows progress InfoBars only for explicit reload operations; ordinary page navigation does not create reload notifications.
- Persists settings and favorites as human-readable JSON under `%LOCALAPPDATA%\RSSAM`.
- Produces self-contained x86 and x64 releases and separate unsigned per-user installers for both architectures.

## Screenshots

The screenshots below show the application layouts included in version 1.0.30. The footer visible in these images was captured from the preceding 1.0.21 build; later versions retain the same main interface while improving documentation, publishing reliability, branding and Steam interoperability.

### Tile view

The default tile layout shows each game's complete header image, name, App ID, and favorite button.

![RSSAM Steam game library in tile view](docs/images/game-library-grid.png)

### List view

The list layout uses the available width for a compact, easily scannable game list.

![RSSAM Steam game library in list view](docs/images/game-library-list.png)

### TableView

The WinUI TableView is read-only and exposes favorite state, image, game name, App ID, and game type. Selecting a row opens that game's achievements; cells are not editable.

![RSSAM Steam game library in TableView](docs/images/game-library-table.png)

### Favorites filter

The star button limits the current view to games stored in `favorites.json`.

![RSSAM game library showing favorite games only](docs/images/game-library-favorites.png)

### Achievement management

Select a game to view, search, lock, unlock, invert, and save its achievements. The status bar shows the number of loaded achievements and statistics.

![RSSAM achievement management view](docs/images/achievement-management.png)

### Settings

Settings are applied immediately and restored from the local JSON settings file at the next start.

![RSSAM settings page](docs/images/settings.png)

## System requirements

### Installed application

- Windows 10 version 1809 (build 17763) or later; Windows 11 is recommended.
- An x86 or x64 Windows installation.
- Steam installed, running, and signed in to the intended account.
- Internet access for Steam catalog metadata and header images.

Download the installer that matches the Windows architecture. The published application is self-contained, so users do not need to install the .NET runtime or Windows App SDK runtime separately.

## Installation

1. Download the installer that matches the Windows architecture:
   - `RSSAM_1.0.30-win-x64-Setup.exe` for 64-bit Windows;
   - `RSSAM_1.0.30-win-x86-Setup.exe` for 32-bit Windows.
2. Run the installer and review the license page.
3. Optionally enable the desktop shortcut.
4. Start Steam and sign in.
5. Start RSSAM from the Start menu or desktop shortcut.

Most users should download the x64 installer. Use the x86 installer only on a 32-bit Windows installation.

RSSAM uses unsigned Inno Setup installers. Windows SmartScreen may therefore show **Unknown publisher**. Verify that the installer came from the expected GitHub release before choosing **More info** and **Run anyway**.

The installers:

- install per user to `%LOCALAPPDATA%\Programs\RSSAM`;
- do not require administrator rights;
- install only the application architecture named in the setup filename;
- use a stable application ID so a newer setup upgrades the existing installation;
- register RSSAM in Windows **Installed apps** for normal uninstallation.

When upgrading, use the same architecture as the existing installation. To switch between x86 and x64, uninstall the existing version first and then install the required architecture. Settings, favorites, and logs under `%LOCALAPPDATA%\RSSAM` are preserved.

Uninstalling the application removes program files but intentionally leaves user-created settings, favorites, and logs under `%LOCALAPPDATA%\RSSAM`. Remove that directory manually only if the data is no longer needed.

## Using RSSAM

1. Start Steam and wait until the client has signed in.
2. Start RSSAM. The lower-left status bar reports whether Steam is running.
3. Select **Reload games** when the catalog needs to be refreshed.
4. Use the icon buttons in the upper-right corner of the game page to select favorites-only, tile, list, or TableView mode.
5. Select a game card or row to open its achievements and statistics.
6. Change achievement check boxes or editable statistic values.
7. Store or reset changes only after checking the confirmation dialog.

### Game library controls

| Control | Result |
| --- | --- |
| Star filter | Shows all games or favorite games only. |
| Tile button | Displays responsive game cards with full header images. |
| List button | Displays one compact game row per item. |
| Table button | Displays the read-only WinUI TableView. Clicking a row opens the game. |
| Star on a game | Adds or removes the App ID in the local favorites file. |
| Reload games | Refreshes the catalog and reports progress in an InfoBar and the status bar. |

The selected library layout and search state are restored from `settings.json`. Favorite state is shared by all layouts through `favorites.json`.

## Local data and diagnostics

RSSAM stores user-specific data in `%LOCALAPPDATA%\RSSAM`:

| Path | Purpose |
| --- | --- |
| `settings.json` | Language, appearance, navigation, window, behavior, search, and selected library layout. |
| `favorites.json` | Favorite Steam App IDs. |
| `Logs\startup.log` | Current startup and fatal-error diagnostics. |
| `Logs\startup.previous.log` | Previous log after the active log exceeds 2 MiB and is rotated. |

Example favorites document:

```json
{
  "SchemaVersion": 1,
  "FavoriteAppIds": [
    251570,
    473690
  ]
}
```

Do not edit either JSON file while RSSAM is running. A malformed favorites file is ignored so it cannot prevent startup. Use **Open settings file** on the Settings page to inspect the active settings file.

## Troubleshooting

### The installed application does not start

1. Open `%LOCALAPPDATA%\RSSAM\Logs\startup.log`.
2. Reinstall the latest complete setup over the existing installation.
3. Check whether antivirus software quarantined files in `%LOCALAPPDATA%\Programs\RSSAM`.
4. If no startup log is created, inspect **Event Viewer > Windows Logs > Application**. A failure before the managed entry point may not reach RSSAM's own logger.
5. Include the startup log, Windows version, process architecture, and RSSAM version in a bug report. Remove personal information before publishing logs.

Do not copy only `RSSAM.exe` from a published folder. WinUI, Windows App SDK, .NET runtime files, `resources.pri`, and the project DLLs must remain together.

### Steam is running but RSSAM cannot connect

- Confirm that Steam is signed in and has completed its own startup.
- Run Steam and RSSAM at the same privilege level. Do not run only one of them as administrator.
- Close any already-running RSSAM process and restart Steam before trying again.
- If Steam reports that the selected game has registered a different App ID, close the running game and reload the selected game in RSSAM.
- Use **Reload games** after Steam becomes ready.
- Install the setup that matches the Windows architecture.

### Game images are missing

- Confirm internet access and retry **Reload games**.
- A game without compatible Steam header artwork may continue to show a placeholder.
- Temporary Steam CDN failures do not prevent achievement and statistic management.

### Settings or favorites need to be reset

Close RSSAM, back up `%LOCALAPPDATA%\RSSAM`, and then remove only `settings.json` or `favorites.json`. RSSAM recreates the missing file with defaults when required.

## Developer requirements

- Windows 10/11 development machine.
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- Visual Studio with the WinUI/Windows App SDK and .NET desktop development tools, or the equivalent command-line build environment.
- Windows PowerShell 5.1 or PowerShell 7.
- Internet access during restore for NuGet packages.
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) only when building the installer.
- Git when using the release workflow or creating tags.

The main UI package references are `Microsoft.WindowsAppSDK` 2.4.0 and `WinUI.TableView` 1.4.1. Restore uses the exact versions declared in the project files.

## Repository structure

```text
RSSAM.sln
Directory.Build.props
README.md
CHANGELOG.md
LICENSE.md
NOTICE.md
ARCHITECTURE.md
PROJECTS.md
.github/
  workflows/
    release.yml
docs/
  images/
installer/
  RSSAM.iss
scripts/
  build.ps1
  test.ps1
  publish.ps1
  build-installer.ps1
  build-source-zip.ps1
  set-version.ps1
src/
  RSSAM.App/
    Assets/
    Presentation/
    Services/
    Resources/
  RSSAM.Core/
    Models/
    Services/
    Storage/
    Localization/
  RSSAM.API/
tests/
  RSSAM.UnitTests/
```

| Project | Responsibility |
| --- | --- |
| `RSSAM.App` | WinUI 3 window, pages, dialogs, InfoBars, status bar, navigation, settings UI, and startup diagnostics. |
| `RSSAM.Core` | Game catalog, Steam process status, achievements/statistics coordination, models, storage, search, and localization. |
| `RSSAM.API` | Native Steam interfaces, callbacks, wrappers, and interop types derived from the original project. |
| `RSSAM.UnitTests` | Isolated xUnit tests for storage, settings, favorites, models, localization, search and Steam schema parsing. |

See [ARCHITECTURE.md](ARCHITECTURE.md) and [PROJECTS.md](PROJECTS.md) for further implementation notes.

## Build and release scripts

Run scripts from a PowerShell prompt opened in the repository root. All scripts stop on terminating errors; the build, test, publish, and installer scripts also reject failed external tools, failed tests, or incomplete publish payloads.

### Quick start

```powershell
dotnet restore .\RSSAM.sln -p:Platform=x64
dotnet build .\RSSAM.sln -c Debug -p:Platform=x64 --no-restore
```

Or use the repository wrapper:

```powershell
.\scripts\build.ps1 -Configuration Debug -Architecture x64
```

### Script summary

| Script | Important parameters | Default output or effect |
| --- | --- | --- |
| `set-version.ps1` | `-Version <x.y.z>` (required) | Synchronizes source, assembly, file, displayed fallback, and Windows manifest versions. |
| `build.ps1` | `-Configuration Debug\|Release`; `-Architecture x86\|x64\|All` | Restores and builds the selected architecture; defaults to Release and both architectures. |
| `test.ps1` | `-Configuration`; `-CollectCoverage` | Runs `RSSAM.UnitTests` once in the x64 test host and optionally creates XPlat coverage data. |
| `publish.ps1` | `-Configuration Debug\|Release`; `-Architecture x86\|x64\|All` | Creates and validates self-contained folders under `artifacts\publish`. |
| `build-installer.ps1` | `-Configuration`; `-Architecture x86\|x64\|All`; `-SkipPublish`; `-InnoCompiler` | Builds separate `win-x86` and/or `win-x64` setup files under `artifacts\installer`. |
| `build-source-zip.ps1` | `-OutputDirectory` | Builds `artifacts\source\RSSAM_<version>-Source.zip`. |

### `set-version.ps1`

```powershell
.\scripts\set-version.ps1 -Version 1.0.30
```

The version must contain exactly three numeric components. The script validates every expected location before it writes anything, then updates:

- `Version`, `AssemblyVersion`, and `FileVersion` in `Directory.Build.props`;
- the UI fallback in `src\RSSAM.App\AppVersion.cs`;
- the four-part Windows manifest version in `src\RSSAM.App\app.manifest`.

The script does not rewrite documentation. Update the current-version text in this README and add a new English entry to `CHANGELOG.md` manually. Changelog text must not be added to localization JSON files.

### `build.ps1`

```powershell
.\scripts\build.ps1
.\scripts\build.ps1 -Configuration Debug -Architecture x86
.\scripts\build.ps1 -Configuration Release -Architecture x64
```

Defaults are `Release` and `All`. For each selected architecture, the script runs `dotnet restore` followed by `dotnet build --no-restore` and stops immediately if either command returns a non-zero exit code.

### `test.ps1`

```powershell
.\scripts\test.ps1 -Configuration Release
.\scripts\test.ps1 -Configuration Release -CollectCoverage
```

The test script executes the separate framework-dependent `RSSAM.UnitTests` project in the x64 test host without starting WinUI or connecting to Steam. Tests use isolated temporary data directories and never modify `%LOCALAPPDATA%\RSSAM`. The test project remains x64 even when the solution's production configuration is x86, preventing Visual Studio from loading an x86 test assembly into its x64 test host. Its output is not runtime-specific, and dedicated test-build properties copy `RSSAM.Core.dll`, `RSSAM.API.dll` and all package dependencies beside `RSSAM.UnitTests.dll`. A post-build validation fails immediately if either RSSAM dependency is missing. Coverage results are written below `artifacts\test-results` when `-CollectCoverage` is specified.

### `publish.ps1`

```powershell
.\scripts\publish.ps1
.\scripts\publish.ps1 -Configuration Release -Architecture x64
```

Default publish directories:

```text
artifacts\publish\win-x86
artifacts\publish\win-x64
```

Publishing is self-contained, unpackaged, untrimmed, and not bundled into a single executable. This is intentional: WinUI and Windows App SDK resources must remain beside `RSSAM.exe`. The application project explicitly generates `resources.pri` and copies it to the publish directory. The script also recovers a misplaced `resources.pri` or `RSSAM.pri` from architecture-specific build output when necessary, then validates the application DLLs, WinUI/TableView runtime, Windows App Runtime, resource index, and .NET host/runtime files.

### `build-installer.ps1`

Normal release build:

```powershell
.\scripts\build-installer.ps1 -Configuration Release
```

The default architecture is `All`. The script publishes, validates, and packages x86 and x64 separately. To build only one installer, pass the required architecture:

```powershell
.\scripts\build-installer.ps1 -Configuration Release -Architecture x64
.\scripts\build-installer.ps1 -Configuration Release -Architecture x86
```

Use `-SkipPublish` only when valid publish output for every selected architecture already exists:

```powershell
.\scripts\build-installer.ps1 -Configuration Release -SkipPublish
```

Inno Setup compiler discovery order:

1. `ISCC.exe` on `PATH`;
2. `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`;
3. `%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe`;
4. `%ProgramFiles%\Inno Setup 6\ISCC.exe`.

For the standard per-user installation requested for this project, pass the path explicitly if automatic discovery is unavailable:

```powershell
.\scripts\build-installer.ps1 `
  -Configuration Release `
  -InnoCompiler "${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe"
```

The default build creates two intentionally unsigned installers:

```text
artifacts\installer\RSSAM_1.0.30-win-x86-Setup.exe
artifacts\installer\RSSAM_1.0.30-win-x64-Setup.exe
```

Each installer contains only its matching self-contained publish output. Both installers use the same multi-resolution icon embedded in `RSSAM.exe`. During the build, `LICENSE.md` is copied to the ignored temporary file `artifacts\installer\LICENSE.txt` because Inno Setup's license page accepts TXT/RTF. The root `LICENSE.md` remains the sole source license file.

### `build-source-zip.ps1`

```powershell
.\scripts\build-source-zip.ps1
.\scripts\build-source-zip.ps1 -OutputDirectory C:\Release\RSSAM
```

The default result is `artifacts\source\RSSAM_1.0.30-Source.zip`. The archive recursively excludes `.git`, `.vs`, `bin`, `obj`, and `artifacts` directories at every depth.

### Recommended release sequence

```powershell
.\scripts\set-version.ps1 -Version 1.0.30
.\scripts\test.ps1 -Configuration Release
.\scripts\build.ps1 -Configuration Release -Architecture All
.\scripts\build-installer.ps1 -Configuration Release
.\scripts\build-source-zip.ps1
```

Between versioning and building, update the English `CHANGELOG.md` entry and this README's current version. Then test the installer on clean x86 and x64 Windows environments before publishing it.

## GitHub Actions releases

`.github\workflows\release.yml` runs on `windows-latest` and:

1. installs the .NET 10 SDK and Inno Setup 6;
2. runs the x64 unit tests and collects code coverage;
3. publishes x86 and x64 payloads;
4. builds the separate unsigned x86 and x64 installers and the source ZIP;
5. uploads all three files as workflow artifacts;
6. creates or updates a GitHub release when the workflow was triggered by a `v*` tag.

`workflow_dispatch` performs the build and artifact upload without creating a tagged release. A release tag for this version can be created with:

```powershell
git tag v1.0.30
git push origin v1.0.30
```

## Versioning

- Source, displayed, setup, and release versions use `1.x.x`.
- The Windows application manifest requires four components, so `1.0.30` is stored there as `1.0.30.0`.
- Release notes belong only in the English [CHANGELOG.md](CHANGELOG.md).
- Localization files contain interface strings only and must never contain changelog entries.

## License and attribution

Fork modifications are Copyright (c) 2026 Daniel Riggi (riggi89).

The original Steam Achievement Manager code is Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us). Original project: [gibbed/SteamAchievementManager](https://github.com/gibbed/SteamAchievementManager/).

The complete modified zlib license and third-party notice are in [LICENSE.md](LICENSE.md). Additional attribution and modification information are in [NOTICE.md](NOTICE.md). WinUI.TableView is used under its own MIT license; see [w-ahmad/WinUI.TableView](https://github.com/w-ahmad/WinUI.TableView).

Source files modified or created for this fork are plainly marked with the project copyright and refer to `LICENSE.md` and `NOTICE.md`. Keep those notices intact when redistributing source code.
