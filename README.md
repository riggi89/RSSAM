# RSSAM

<p align="center">
  <img src="src/RSSAM.App/Assets/RSSAM-AppIcon-256.png" width="128" height="128" alt="RSSAM application icon">
</p>

<p align="center">
  <a href="https://github.com/riggi89/RSSAM/actions">
    <img alt="Unit tests: 21 passed" src="https://img.shields.io/badge/unit_tests-21_passed-brightgreen">
  </a>
  <a href="https://github.com/riggi89/RSSAM/releases">
    <img alt="Windows x86 supported" src="https://img.shields.io/badge/Windows-x86-0078D4?logo=windows11&amp;logoColor=white">
  </a>
  <a href="https://github.com/riggi89/RSSAM/releases">
    <img alt="Windows x64 supported" src="https://img.shields.io/badge/Windows-x64-0078D4?logo=windows11&amp;logoColor=white">
  </a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0">
    <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&amp;logoColor=white">
  </a>
  <a href="https://learn.microsoft.com/windows/apps/winui/winui3/">
    <img alt="WinUI 3" src="https://img.shields.io/badge/UI-WinUI_3-0078D4">
  </a>
  <a href="https://github.com/riggi89/RSSAM/releases">
    <img alt="Version 1.0.31" src="https://img.shields.io/badge/version-1.0.31-blue">
  </a>
</p>

<p align="center">
  <strong>Riggi's Steam Achievement Manager</strong>
</p>

<p align="center">
  A modern WinUI 3 desktop application for viewing and managing Steam achievements and statistics on Windows.
</p>

---

RSSAM (**Riggi's Steam Achievement Manager**) is an unpackaged WinUI 3 desktop application for viewing and managing Steam achievements and statistics on Windows.

Current version: **1.0.31**  
Supported architectures: **x86 and x64**

> [!NOTE]
> 🌐 **English and German are both welcome**
>
> The project documentation is primarily maintained in **English**.
>
> GitHub Discussions, questions, feedback, ideas, and support requests are welcome in either **English or German**.

> [!CAUTION]
> RSSAM changes achievement and statistic data through the Steam client.
>
> Use it carefully and only with games and accounts for which you understand the consequences.
>
> RSSAM is not affiliated with or endorsed by Valve Corporation.

## Quick links

| Resource | Link |
| --- | --- |
| 🏠 Repository | [RSSAM](https://github.com/riggi89/RSSAM) |
| 📦 Releases | [Download RSSAM](https://github.com/riggi89/RSSAM/releases) |
| 💬 Discussions | [RSSAM Discussions](https://github.com/riggi89/RSSAM/discussions) |
| 💬 General / Feedback | [General / Feedback](https://github.com/riggi89/RSSAM/discussions/categories/general) |
| 💡 Ideas / Feature Requests | [Ideas / Feature Requests](https://github.com/riggi89/RSSAM/discussions/categories/ideas) |
| 🙋 Q&A / Support | [Q&A / Support](https://github.com/riggi89/RSSAM/discussions/categories/q-a) |
| 🐛 Issues | [GitHub Issues](https://github.com/riggi89/RSSAM/issues) |
| 📖 Wiki | [RSSAM Wiki](https://github.com/riggi89/RSSAM/wiki) |
| 📋 Changelog | [CHANGELOG.md](CHANGELOG.md) |
| 🏗️ Architecture | [ARCHITECTURE.md](ARCHITECTURE.md) |
| 📂 Projects | [PROJECTS.md](PROJECTS.md) |
| ⚖️ License | [LICENSE.md](LICENSE.md) |
| ℹ️ Notices | [NOTICE.md](NOTICE.md) |

## Contents

- [Features](#features)
- [Screenshots](#screenshots)
- [System requirements](#system-requirements)
- [Downloads](#downloads)
- [Installation](#installation)
- [Using RSSAM](#using-rssam)
- [Local data and diagnostics](#local-data-and-diagnostics)
- [Troubleshooting](#troubleshooting)
- [Community and Discussions](#community-and-discussions)
- [Reporting bugs](#reporting-bugs)
- [Developer requirements](#developer-requirements)
- [Repository structure](#repository-structure)
- [Build and release scripts](#build-and-release-scripts)
- [GitHub Actions releases](#github-actions-releases)
- [Versioning](#versioning)
- [AI-assisted development](#ai-assisted-development)
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
- Supports English and German UI resources.
- Supports light, dark, and system themes.
- Supports Mica, Acrylic, and standard window backdrops.
- Shows Steam process state in the status bar.
- Shows loading progress and completion messages.
- Shows the current RSSAM version in the status bar.
- Shows progress InfoBars only for explicit reload operations.
- Ordinary page navigation does not create unnecessary reload notifications.
- Persists settings as human-readable JSON.
- Stores favorite games locally in `favorites.json`.
- Supports self-contained x86 releases.
- Supports self-contained x64 releases.
- Provides separate x86 and x64 installers.
- Provides separate x86 and x64 Portable ZIP packages.
- Does not require the .NET runtime to be installed separately when using the published self-contained builds.

## Screenshots

The screenshots below show the application layouts included in RSSAM.

Some screenshots may have been captured using an earlier build while retaining the same main interface. Later releases improve publishing reliability, branding, documentation, performance, and Steam interoperability.

### Tile view

The default tile layout shows each game's complete header image, name, App ID, and favorite button.

![RSSAM Steam game library in tile view](docs/images/game-library-grid.png)

### List view

The list layout uses the available width for a compact and easily scannable game list.

![RSSAM Steam game library in list view](docs/images/game-library-list.png)

### TableView

The WinUI TableView is read-only and exposes:

- Favorite state
- Game image
- Game name
- Steam App ID
- Game type

Selecting a row opens the selected game's achievements and statistics.

Cells are intentionally not editable.

![RSSAM Steam game library in TableView](docs/images/game-library-table.png)

### Favorites filter

The star button limits the current view to games stored in `favorites.json`.

![RSSAM game library showing favorite games only](docs/images/game-library-favorites.png)

### Achievement management

Select a game to view and manage its achievements and statistics.

Available actions include:

- Search achievements
- Unlock achievements
- Lock achievements
- Invert achievement selections
- Save changes
- Edit supported statistics

The status bar shows the number of loaded achievements and statistics.

![RSSAM achievement management view](docs/images/achievement-management.png)

### Settings

Settings are applied immediately and restored from the local JSON settings file at the next start.

Available settings include language, appearance, behavior, confirmation options, and library layout preferences.

![RSSAM settings page](docs/images/settings.png)

## System requirements

### Installed application

- Windows 10 version 1809 (build 17763) or later.
- Windows 11 is recommended.
- An x86 or x64 Windows installation.
- Steam installed.
- Steam running.
- Steam signed in to the intended account.
- Internet access for Steam catalog metadata and header images.

Download either the Installer or Portable ZIP that matches the Windows architecture.

Both distributions are self-contained, so users do not need to install the .NET runtime or Windows App SDK runtime separately.

## Downloads

The latest RSSAM release is available from:

👉 **[RSSAM Releases](https://github.com/riggi89/RSSAM/releases)**

RSSAM 1.0.31 is available in the following variants:

### Installer

```text
RSSAM_1.0.31-win-x64-Setup.exe
RSSAM_1.0.31-win-x86-Setup.exe
```

For most modern Windows computers, use:

```text
RSSAM_1.0.31-win-x64-Setup.exe
```

Use the x86 version only on a 32-bit Windows installation.

### Portable

```text
RSSAM_1.0.31-win-x64-Portable.zip
RSSAM_1.0.31-win-x86-Portable.zip
```

Portable builds do not require installation.

Extract the complete `RSSAM` directory from the ZIP archive and start:

```text
RSSAM.exe
```

> [!IMPORTANT]
> Do not copy only `RSSAM.exe`.
>
> WinUI, Windows App SDK, .NET runtime files, `resources.pri`, RSSAM project DLLs, and other dependencies must remain together.

### Source code

The release source archive is:

```text
RSSAM_1.0.31-Source.zip
```

The repository source is also available directly from:

👉 **[github.com/riggi89/RSSAM](https://github.com/riggi89/RSSAM)**

## Installation

1. Download the installer that matches the Windows architecture:

   - `RSSAM_1.0.31-win-x64-Setup.exe` for 64-bit Windows.
   - `RSSAM_1.0.31-win-x86-Setup.exe` for 32-bit Windows.

2. Run the installer.

3. Review the license page.

4. Optionally enable the desktop shortcut.

5. Start Steam.

6. Sign in to the intended Steam account.

7. Start RSSAM from the Start menu or desktop shortcut.

Most users should download the **x64 installer**.

Use the x86 installer only on a 32-bit Windows installation.

A Portable ZIP is also available when RSSAM should run without installation.

### Windows SmartScreen

RSSAM currently uses unsigned Inno Setup installers.

Windows SmartScreen may therefore display:

```text
Unknown publisher
```

Verify that the installer came from the expected RSSAM GitHub release before choosing:

```text
More info
Run anyway
```

> [!WARNING]
> Only download RSSAM from a source you trust.
>
> The official project releases are published through the RSSAM GitHub repository.

### Installer behavior

The installers:

- Install per user to `%LOCALAPPDATA%\Programs\RSSAM`.
- Do not require administrator rights.
- Install only the application architecture named in the setup filename.
- Use a stable application ID so newer setup versions can upgrade the existing installation.
- Register RSSAM in Windows **Installed apps** for normal uninstallation.

### Updating RSSAM

When upgrading, use the same architecture as the existing installation.

For example:

```text
x64 → x64
x86 → x86
```

To switch between x86 and x64:

1. Uninstall the existing RSSAM version.
2. Download the required architecture.
3. Install the new version.

Settings, favorites, and logs under:

```text
%LOCALAPPDATA%\RSSAM
```

are preserved.

### Uninstalling RSSAM

Uninstalling RSSAM removes the program files but intentionally keeps user-created:

- Settings
- Favorites
- Logs

These remain under:

```text
%LOCALAPPDATA%\RSSAM
```

Remove this directory manually only if the stored data is no longer needed.

## Using RSSAM

1. Start Steam and wait until the client has signed in.
2. Start RSSAM.
3. Check the lower-left status bar to confirm that Steam is running.
4. Select **Reload games** when the catalog needs to be refreshed.
5. Use the controls in the upper-right corner of the game page to select the desired view.
6. Select a game card or row.
7. Manage achievements and supported statistics.
8. Review confirmation dialogs before storing or resetting changes.

### Game library controls

| Control | Result |
| --- | --- |
| Star filter | Shows all games or favorite games only. |
| Tile button | Displays responsive game cards with complete header images. |
| List button | Displays one compact game row per item. |
| Table button | Displays the read-only WinUI TableView. Selecting a row opens the game. |
| Star on a game | Adds or removes the Steam App ID from the local favorites file. |
| Reload games | Refreshes the catalog and reports progress in an InfoBar and status bar. |

The selected library layout and search state are restored from `settings.json`.

Favorite state is shared by all layouts through:

```text
favorites.json
```

## Local data and diagnostics

RSSAM stores user-specific data under:

```text
%LOCALAPPDATA%\RSSAM
```

| Path | Purpose |
| --- | --- |
| `settings.json` | Language, appearance, navigation, window, behavior, search, and selected library layout. |
| `favorites.json` | Favorite Steam App IDs. |
| `Logs\startup.log` | Current startup and fatal-error diagnostics. |
| `Logs\startup.previous.log` | Previous startup log after log rotation. |

### Example favorites file

```json
{
  "SchemaVersion": 1,
  "FavoriteAppIds": [
    251570,
    473690
  ]
}
```

Do not edit `settings.json` or `favorites.json` while RSSAM is running.

A malformed favorites file is ignored so that it cannot prevent RSSAM from starting.

Use **Open settings file** on the Settings page to inspect the active settings file.

## Troubleshooting

### The installed application does not start

1. Open:

   ```text
   %LOCALAPPDATA%\RSSAM\Logs\startup.log
   ```

2. Reinstall the latest complete RSSAM setup over the existing installation.

3. Check whether antivirus software quarantined files in:

   ```text
   %LOCALAPPDATA%\Programs\RSSAM
   ```

4. If no startup log is created, inspect:

   ```text
   Event Viewer
   → Windows Logs
   → Application
   ```

   A failure before the managed entry point may not reach RSSAM's own logger.

5. Include the following information in a bug report:

   - Startup log
   - Windows version
   - RSSAM version
   - x86 or x64 architecture
   - Installer or Portable version

> [!IMPORTANT]
> Remove personal or sensitive information before publishing logs or screenshots.

Do not copy only `RSSAM.exe` from a published directory.

The following files must remain together with the application:

- WinUI files
- Windows App SDK files
- .NET runtime files
- `resources.pri`
- `RSSAM.Core.dll`
- `RSSAM.API.dll`
- Other runtime dependencies generated during publish

### Steam is running but RSSAM cannot connect

- Confirm that Steam is signed in.
- Wait until Steam has completed its own startup.
- Run Steam and RSSAM at the same privilege level.
- Do not run only one application as administrator.
- Close any already-running RSSAM process.
- Restart Steam before trying again.
- Use **Reload games** after Steam becomes ready.
- Install the RSSAM build matching the Windows architecture.

If Steam reports that a selected game has registered a different App ID:

1. Close the running game.
2. Return to RSSAM.
3. Reload or reopen the selected game.

### Game images are missing

- Confirm internet access.
- Retry **Reload games**.
- Some games may not provide compatible Steam header artwork.
- Games without compatible artwork may continue to display a placeholder.
- Temporary Steam CDN failures do not prevent achievement or statistic management.

### Settings or favorites need to be reset

Close RSSAM first.

Back up:

```text
%LOCALAPPDATA%\RSSAM
```

Then remove only:

```text
settings.json
```

or:

```text
favorites.json
```

RSSAM recreates missing files with default values when required.

## Community and Discussions

RSSAM uses GitHub Discussions for community conversations, feedback, ideas, questions, and support.

> [!NOTE]
> Discussions may be created in **English or German**.
>
> Use whichever language you are more comfortable with.

### 💬 General / Feedback

Share your experience with RSSAM.

👉 **[General / Feedback](https://github.com/riggi89/RSSAM/discussions/categories/general)**

Examples:

- How do you like RSSAM?
- Does RSSAM work reliably for you?
- Which features do you use most?
- How is the performance?
- Is the interface easy to use?
- Do your Steam games load correctly?
- Are achievements and statistics displayed correctly?
- What could be improved?

### 💡 Ideas / Feature Requests

Suggest new features and improvements.

👉 **[Ideas / Feature Requests](https://github.com/riggi89/RSSAM/discussions/categories/ideas)**

Suitable topics include:

- New functionality
- UI improvements
- Workflow improvements
- Performance improvements
- Additional settings
- New Steam-related functionality
- Accessibility improvements
- Future RSSAM ideas

When suggesting a feature, describe:

- What you want RSSAM to do.
- What problem it would solve.
- Why it would be useful.
- How you imagine the feature working.
- Screenshots or mockups, if available.

Please check existing Discussions before creating a duplicate request.

### 🙋 Q&A / Support

Ask questions or request help.

👉 **[Q&A / Support](https://github.com/riggi89/RSSAM/discussions/categories/q-a)**

Use this category for:

- Installation help
- Configuration questions
- x86/x64 questions
- Installer problems
- Portable problems
- Steam connection problems
- Game compatibility questions
- Achievement questions
- Statistic questions
- General troubleshooting

When requesting help, include:

- RSSAM version
- Windows version
- Architecture
- Installer or Portable
- Description of the problem
- Expected behavior
- Actual behavior
- Reproduction steps
- Screenshots
- Relevant log entries

## Reporting bugs

If you find a reproducible software problem, create a GitHub Issue.

👉 **[GitHub Issues](https://github.com/riggi89/RSSAM/issues)**

Please include:

- RSSAM version
- Windows version
- x86 or x64
- Installer or Portable
- Steam client state
- Game name
- Steam App ID when relevant
- Steps to reproduce
- Expected behavior
- Actual behavior
- Error messages
- Screenshots
- Relevant log entries

Logs are normally available under:

```text
%LOCALAPPDATA%\RSSAM\Logs\
```

Main startup log:

```text
%LOCALAPPDATA%\RSSAM\Logs\startup.log
```

Previous rotated startup log:

```text
%LOCALAPPDATA%\RSSAM\Logs\startup.previous.log
```

### Protect your account

Never publish:

- Steam passwords
- Steam Guard authentication codes
- Session tokens
- Cookies
- Private API keys
- Personal account information

> [!WARNING]
> **RSSAM never needs your Steam password.**

## Developer requirements

- Windows 10/11 development machine.
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- Visual Studio with WinUI / Windows App SDK tooling.
- .NET desktop development tools.
- Windows PowerShell 5.1 or PowerShell 7.
- Internet access during NuGet restore.
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) when building installers.
- Git when using release workflows or creating version tags.

The main UI package references are:

```text
Microsoft.WindowsAppSDK 2.4.0
WinUI.TableView 1.4.1
```

Restore uses the exact versions declared by the project files.

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
    game-library-grid.png
    game-library-list.png
    game-library-table.png
    game-library-favorites.png
    achievement-management.png
    settings.png

installer/
  RSSAM.iss

scripts/
  build.ps1
  test.ps1
  publish.ps1
  build-portable.ps1
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

### Projects

| Project | Responsibility |
| --- | --- |
| `RSSAM.App` | WinUI 3 window, pages, dialogs, InfoBars, status bar, navigation, settings UI, and startup diagnostics. |
| `RSSAM.Core` | Game catalog, Steam process state, achievements/statistics coordination, models, storage, search, and localization. |
| `RSSAM.API` | Native Steam interfaces, callbacks, wrappers, and interoperability types derived from the original project. |
| `RSSAM.UnitTests` | Isolated xUnit tests for storage, settings, favorites, models, localization, search, and Steam schema parsing. |

For additional implementation information, see:

- [ARCHITECTURE.md](ARCHITECTURE.md)
- [PROJECTS.md](PROJECTS.md)

## Build and release scripts

Run scripts from a PowerShell prompt opened in the repository root.

All build, test, publish, and installer scripts stop when an external tool, build, test, or publish operation fails.

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
| `set-version.ps1` | `-Version <x.y.z>` | Synchronizes source, assembly, file, displayed fallback, and manifest versions. |
| `build.ps1` | `-Configuration Debug\|Release`; `-Architecture x86\|x64\|All` | Restores and builds the selected architecture. |
| `test.ps1` | `-Configuration`; `-CollectCoverage` | Runs `RSSAM.UnitTests` using the x64 test host and optionally collects coverage. |
| `publish.ps1` | `-Configuration Debug\|Release`; `-Architecture x86\|x64\|All`; `-SkipClean` | Creates and validates self-contained publish directories. |
| `build-portable.ps1` | `-Configuration`; `-Architecture x86\|x64\|All`; `-SkipPublish` | Builds Portable ZIP packages. |
| `build-installer.ps1` | `-Configuration`; `-Architecture x86\|x64\|All`; `-SkipPublish`; `-InnoCompiler` | Builds separate x86/x64 setup files. |
| `build-source-zip.ps1` | `-OutputDirectory` | Builds the source archive. |

### `set-version.ps1`

```powershell
.\scripts\set-version.ps1 -Version 1.0.31
```

The version must contain exactly three numeric components.

The script validates every expected location before writing changes.

It updates:

- `Version` in `Directory.Build.props`.
- `AssemblyVersion` in `Directory.Build.props`.
- `FileVersion` in `Directory.Build.props`.
- UI fallback version in `src\RSSAM.App\AppVersion.cs`.
- Four-part Windows manifest version in `src\RSSAM.App\app.manifest`.

For version `1.0.31`, the Windows manifest uses:

```text
1.0.31.0
```

The script intentionally does not rewrite documentation.

Update the README version and `CHANGELOG.md` manually when preparing a release.

### `build.ps1`

```powershell
.\scripts\build.ps1
```

Build Debug x86:

```powershell
.\scripts\build.ps1 -Configuration Debug -Architecture x86
```

Build Release x64:

```powershell
.\scripts\build.ps1 -Configuration Release -Architecture x64
```

Defaults:

```text
Configuration = Release
Architecture  = All
```

For each selected architecture, the script performs:

1. `dotnet restore`
2. `dotnet build --no-restore`

The script immediately stops when either command returns a non-zero exit code.

### `test.ps1`

Run tests:

```powershell
.\scripts\test.ps1 -Configuration Release
```

Run tests with code coverage:

```powershell
.\scripts\test.ps1 -Configuration Release -CollectCoverage
```

The test script executes the separate framework-dependent `RSSAM.UnitTests` project using an x64 test host.

The tests:

- Do not start WinUI.
- Do not connect to Steam.
- Use isolated temporary data directories.
- Never modify `%LOCALAPPDATA%\RSSAM`.

The test project remains x64 even when a production x86 configuration is built.

This prevents Visual Studio from attempting to load an x86 test assembly into its x64 test host.

Dedicated test-build properties copy:

```text
RSSAM.Core.dll
RSSAM.API.dll
```

and all required package dependencies next to:

```text
RSSAM.UnitTests.dll
```

A post-build validation fails immediately if either RSSAM dependency is missing.

Coverage results are written below:

```text
artifacts\test-results
```

when `-CollectCoverage` is specified.

### `publish.ps1`

Publish both architectures:

```powershell
.\scripts\publish.ps1
```

Publish x64 only:

```powershell
.\scripts\publish.ps1 -Configuration Release -Architecture x64
```

Publish x86 only:

```powershell
.\scripts\publish.ps1 -Configuration Release -Architecture x86
```

Default publish directories:

```text
artifacts\publish\win-x86
artifacts\publish\win-x64
```

Publishing is:

- Self-contained
- Unpackaged
- Untrimmed
- Multi-file
- Architecture-specific

RSSAM is intentionally not published as a single executable.

WinUI and Windows App SDK resources must remain beside `RSSAM.exe`.

The application explicitly generates:

```text
resources.pri
```

and copies it to the publish directory.

The publishing process validates:

- `RSSAM.exe`
- `RSSAM.dll`
- `RSSAM.Core.dll`
- `RSSAM.API.dll`
- WinUI runtime files
- WinUI.TableView
- Windows App Runtime
- `resources.pri`
- .NET host files
- .NET runtime files

Publishing uses isolated temporary `bin` and `obj` roots for each architecture and publish attempt.

The normal:

```text
src\*\bin
src\*\obj
```

directories are not used by `publish.ps1`.

This prevents conflicts with:

- Visual Studio
- Roslyn
- MSBuild build servers
- Previous x86 builds
- Previous x64 builds
- Stale files from the previous RSAM project name

The validated publish payload is copied to `artifacts\publish` only after a successful publish operation.

### `build-portable.ps1`

Build both Portable packages:

```powershell
.\scripts\build-portable.ps1 -Configuration Release
```

Build x64:

```powershell
.\scripts\build-portable.ps1 -Configuration Release -Architecture x64
```

Build x86:

```powershell
.\scripts\build-portable.ps1 -Configuration Release -Architecture x86
```

By default, the script publishes first and then creates:

```text
artifacts\portable\RSSAM_1.0.31-win-x86-Portable.zip
artifacts\portable\RSSAM_1.0.31-win-x64-Portable.zip
```

Each ZIP contains a complete self-contained RSSAM directory.

Extract the directory and start:

```text
RSSAM.exe
```

No installer is required.

Use:

```powershell
-SkipPublish
```

only when valid publish output already exists for every selected architecture.

RSSAM Portable intentionally uses a multi-file deployment because WinUI 3, Windows App SDK, .NET runtime files, and `resources.pri` must remain beside the application.

### `build-installer.ps1`

Normal release build:

```powershell
.\scripts\build-installer.ps1 -Configuration Release
```

The default architecture is:

```text
All
```

Build only x64:

```powershell
.\scripts\build-installer.ps1 -Configuration Release -Architecture x64
```

Build only x86:

```powershell
.\scripts\build-installer.ps1 -Configuration Release -Architecture x86
```

Use existing publish output:

```powershell
.\scripts\build-installer.ps1 -Configuration Release -SkipPublish
```

Use `-SkipPublish` only when valid publish output for every selected architecture already exists.

#### Inno Setup compiler discovery

The script searches for `ISCC.exe` in this order:

1. `ISCC.exe` available on `PATH`.
2. `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`.
3. `%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe`.
4. `%ProgramFiles%\Inno Setup 6\ISCC.exe`.

The compiler path can also be provided manually:

```powershell
.\scripts\build-installer.ps1 `
  -Configuration Release `
  -InnoCompiler "${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe"
```

The default build creates:

```text
artifacts\installer\RSSAM_1.0.31-win-x86-Setup.exe
artifacts\installer\RSSAM_1.0.31-win-x64-Setup.exe
```

The installers are currently unsigned.

Each installer contains only the matching architecture-specific publish output.

Both installers use the multi-resolution application icon embedded in `RSSAM.exe`.

During the installer build, `LICENSE.md` is copied to the temporary ignored file:

```text
artifacts\installer\LICENSE.txt
```

because the Inno Setup license page accepts TXT/RTF files.

The root:

```text
LICENSE.md
```

remains the authoritative project license file.

### `build-source-zip.ps1`

Build the source archive:

```powershell
.\scripts\build-source-zip.ps1
```

Specify another output directory:

```powershell
.\scripts\build-source-zip.ps1 -OutputDirectory C:\Release\RSSAM
```

The default result is:

```text
artifacts\source\RSSAM_1.0.31-Source.zip
```

The archive recursively excludes:

```text
.git
.vs
bin
obj
artifacts
```

directories at every depth.

### Recommended release sequence

```powershell
.\scripts\set-version.ps1 -Version 1.0.31

.\scripts\test.ps1 -Configuration Release

.\scripts\build.ps1 `
  -Configuration Release `
  -Architecture All

.\scripts\build-installer.ps1 `
  -Configuration Release

.\scripts\build-portable.ps1 `
  -Configuration Release `
  -SkipPublish

.\scripts\build-source-zip.ps1
```

Before building the final release:

1. Set the version.
2. Update `CHANGELOG.md`.
3. Update the current version in `README.md`.
4. Run unit tests.
5. Build x86 and x64.
6. Build installers.
7. Build Portable packages.
8. Build the source archive.
9. Test the x86 package on an appropriate Windows environment.
10. Test the x64 package on an appropriate Windows environment.
11. Publish the GitHub release.

## GitHub Actions releases

The workflow:

```text
.github\workflows\release.yml
```

runs on:

```text
windows-latest
```

The release workflow:

1. Installs the .NET 10 SDK.
2. Installs Inno Setup 6.
3. Runs the x64 unit tests.
4. Collects code coverage.
5. Publishes the x86 application.
6. Publishes the x64 application.
7. Builds the x86 installer.
8. Builds the x64 installer.
9. Builds the x86 Portable ZIP.
10. Builds the x64 Portable ZIP.
11. Builds the Source ZIP.
12. Uploads all five release files as workflow artifacts.
13. Creates or updates a GitHub Release when triggered by a `v*` tag.

`workflow_dispatch` performs the build and uploads the generated artifacts without creating a tagged release.

For RSSAM 1.0.31:

```powershell
git tag v1.0.31
git push origin v1.0.31
```

The expected release files are:

```text
RSSAM_1.0.31-win-x86-Setup.exe
RSSAM_1.0.31-win-x64-Setup.exe
RSSAM_1.0.31-win-x86-Portable.zip
RSSAM_1.0.31-win-x64-Portable.zip
RSSAM_1.0.31-Source.zip
```

## Versioning

RSSAM uses three-part application versions:

```text
1.x.x
```

For example:

```text
1.0.31
```

Source, displayed, setup, and release versions use the same three-part version.

The Windows application manifest requires four components.

Therefore:

```text
1.0.31
```

is represented in the Windows manifest as:

```text
1.0.31.0
```

Release notes belong in:

```text
CHANGELOG.md
```

Localization files contain interface strings only and must not contain changelog entries.

## AI-assisted development

RSSAM is developed and maintained with the assistance of modern **AI-based development tools**.

AI assistance is used during development for tasks such as:

- Code analysis
- Debugging
- Refactoring suggestions
- Architecture discussions
- WinUI 3 implementation support
- .NET development support
- Steam interoperability analysis
- Build and publishing troubleshooting
- PowerShell script development
- Test development
- Documentation
- GitHub workflow preparation
- Release preparation
- Translation and localization assistance
- Identifying potential bugs and improvement opportunities

> [!NOTE]
> AI-assisted development does not mean that changes are accepted automatically.
>
> AI-generated or AI-assisted suggestions are reviewed, adapted, tested, and integrated as part of the normal development process.

The modernization and continued development of the **RSSAM fork**, including parts of its user interface, build infrastructure, release tooling, documentation, and ongoing improvements, have been created with **AI assistance**.

This does not change the attribution or copyright of the original Steam Achievement Manager source code.

The original project remains credited separately below.

## License and attribution

Fork modifications:

```text
Copyright (c) 2026 Daniel Riggi (riggi89)
```

Original software:

```text
Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
```

Original Steam Achievement Manager project:

[github.com/gibbed/SteamAchievementManager](https://github.com/gibbed/SteamAchievementManager/)

The complete modified zlib license and third-party notices are available in:

- [LICENSE.md](LICENSE.md)
- [NOTICE.md](NOTICE.md)

RSSAM uses **WinUI.TableView** under its own MIT license:

[w-ahmad/WinUI.TableView](https://github.com/w-ahmad/WinUI.TableView)

Source files modified or created for this fork are plainly marked with the project copyright and refer to `LICENSE.md` and `NOTICE.md`.

Keep those notices intact when redistributing the source code.

---

<p align="center">
  <strong>RSSAM – Riggi's Steam Achievement Manager</strong>
</p>

<p align="center">
  Made with ❤️ and AI-assisted development.
</p>

<p align="center">
  <a href="https://github.com/riggi89/RSSAM/releases">Releases</a>
  ·
  <a href="https://github.com/riggi89/RSSAM/discussions">Discussions</a>
  ·
  <a href="https://github.com/riggi89/RSSAM/issues">Issues</a>
  ·
  <a href="https://github.com/riggi89/RSSAM/wiki">Wiki</a>
</p>