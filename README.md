# RSSAM

<p align="center">
  <img src="src/RSSAM.App/Assets/RSSAM-AppIcon-256.png" width="128" height="128" alt="RSSAM application icon">
</p>

<p align="center">
  <a href="https://github.com/riggi89/RSSAM/actions"><img alt="Unit tests: 21 passed" src="https://img.shields.io/badge/unit_tests-21_passed-brightgreen"></a>
  <a href="https://github.com/riggi89/RSSAM/releases"><img alt="Windows x86 supported" src="https://img.shields.io/badge/Windows-x86-0078D4?logo=windows11&amp;logoColor=white"></a>
  <a href="https://github.com/riggi89/RSSAM/releases"><img alt="Windows x64 supported" src="https://img.shields.io/badge/Windows-x64-0078D4?logo=windows11&amp;logoColor=white"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img alt=".NET 10" src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&amp;logoColor=white"></a>
  <a href="https://learn.microsoft.com/windows/apps/winui/winui3/"><img alt="WinUI 3" src="https://img.shields.io/badge/UI-WinUI_3-0078D4"></a>
  <a href="https://github.com/riggi89/RSSAM/releases"><img alt="Version 1.0.31" src="https://img.shields.io/badge/version-1.0.31-blue"></a>
</p>

<p align="center">
  <strong>Riggi's Steam Achievement Manager</strong><br>
  A modern WinUI 3 application for viewing and managing Steam achievements and statistics on Windows.
</p>

> [!CAUTION]
> RSSAM changes achievement and statistic data through the Steam client. Close running games before making changes and use RSSAM only with accounts and games you own.

## Features

- View and search the Steam game library.
- Grid, list, and read-only TableView layouts.
- Save games as favorites and filter the library.
- View, unlock, lock, and save achievements.
- Read and edit supported integer and floating-point statistics.
- English and German user interface.
- Light, dark, and system themes with Mica or Acrylic backdrops.
- Local JSON settings and favorites.
- Separate self-contained x86 and x64 releases.

## Screenshots

| Game library | Achievement management |
| --- | --- |
| ![RSSAM game library](docs/images/game-library-grid.png) | ![RSSAM achievement management](docs/images/achievement-management.png) |

Additional layouts: [List view](docs/images/game-library-list.png) · [TableView](docs/images/game-library-table.png) · [Favorites](docs/images/game-library-favorites.png) · [Settings](docs/images/settings.png)

## Download and installation

Download the latest version from [GitHub Releases](https://github.com/riggi89/RSSAM/releases).

| Package | Intended use |
| --- | --- |
| `RSSAM_1.0.31-win-x64-Setup.exe` | Installer for most Windows computers |
| `RSSAM_1.0.31-win-x86-Setup.exe` | Installer for 32-bit Windows |
| `RSSAM_1.0.31-win-x64-Portable.zip` | Portable x64 version |
| `RSSAM_1.0.31-win-x86-Portable.zip` | Portable x86 version |
| `RSSAM_1.0.31-Source.zip` | Source code |

The installers are self-contained, install per user to `%LOCALAPPDATA%\Programs\RSSAM`, and do not require administrator rights. Portable packages must be extracted completely before starting `RSSAM.exe`.

> [!NOTE]
> The installers are currently unsigned. Windows SmartScreen may show an **Unknown publisher** warning. Verify that the file came from the official RSSAM release before selecting **More info → Run anyway**.

### Requirements

- Windows 10 version 1809 or newer; Windows 11 recommended.
- Steam installed, running, and signed in.
- Internet access for game information and images.

### Updating

Install the new version over the existing installation using the same architecture. To switch between x86 and x64, uninstall RSSAM first. Settings and favorites are preserved.

## Usage

1. Start Steam and sign in.
2. Start RSSAM and wait for the game library to load.
3. Select a game to open its achievements and statistics.
4. Review confirmation dialogs before saving or resetting changes.

RSSAM stores its user data under `%LOCALAPPDATA%\RSSAM`:

| Path | Purpose |
| --- | --- |
| `settings.json` | Application and appearance settings |
| `favorites.json` | Favorite Steam App IDs |
| `Logs\startup.log` | Startup and error diagnostics |

## Troubleshooting

If RSSAM does not start or cannot connect to Steam:

- Confirm that Steam is running, signed in, and uses the same privilege level as RSSAM.
- Install or extract the complete package; do not copy only `RSSAM.exe`.
- Try **Reload games** after Steam has finished starting.
- Reinstall the latest package if files are missing or quarantined.
- Check `%LOCALAPPDATA%\RSSAM\Logs\startup.log` for details.

To reset settings or favorites, close RSSAM and remove the corresponding JSON file. RSSAM recreates it with default values.

## Development

### Requirements

- .NET 10 SDK
- Visual Studio with WinUI / Windows App SDK tooling
- PowerShell 5.1 or newer
- Inno Setup 6 for installer builds

### Projects

| Project | Responsibility |
| --- | --- |
| `RSSAM.App` | WinUI 3 user interface and startup |
| `RSSAM.Core` | Models, services, storage, search, and localization |
| `RSSAM.API` | Native Steam interfaces and interoperability |
| `RSSAM.UnitTests` | xUnit tests for testable core functions |

See [ARCHITECTURE.md](ARCHITECTURE.md) and [PROJECTS.md](PROJECTS.md) for implementation details.

### Build and test

```powershell
.\scripts\build.ps1 -Configuration Debug -Architecture x64
.\scripts\test.ps1 -Configuration Release
```

Build all release packages:

```powershell
.\scripts\set-version.ps1 -Version 1.0.31
.\scripts\test.ps1 -Configuration Release
.\scripts\build.ps1 -Configuration Release -Architecture All
.\scripts\build-installer.ps1 -Configuration Release
.\scripts\build-portable.ps1 -Configuration Release -SkipPublish
.\scripts\build-source-zip.ps1
```

The GitHub Actions workflow builds and tests x86 and x64 releases when a `v*` tag is pushed.

## Support and contribution

- [Discussions](https://github.com/riggi89/RSSAM/discussions) — questions, feedback, and ideas in English or German
- [Issues](https://github.com/riggi89/RSSAM/issues) — reproducible bugs
- [Wiki](https://github.com/riggi89/RSSAM/wiki) — guides and documentation
- [Changelog](CHANGELOG.md) — release history

Bug reports should include the RSSAM and Windows versions, architecture, reproduction steps, expected and actual behavior, and relevant log entries. Remove personal information before publishing screenshots or logs. Never publish Steam passwords, authentication codes, session tokens, cookies, or API keys.

## License and attribution

Fork modifications: Copyright (c) 2026 Daniel Riggi (riggi89)  
Original software: Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)

RSSAM is based on the original [Steam Achievement Manager](https://github.com/gibbed/SteamAchievementManager) and uses [WinUI.TableView](https://github.com/w-ahmad/WinUI.TableView).

See [LICENSE.md](LICENSE.md) and [NOTICE.md](NOTICE.md) for the complete license and attribution information.

RSSAM is not affiliated with or endorsed by Valve Corporation.

---

<p align="center">
  <a href="https://github.com/riggi89/RSSAM/releases">Releases</a> ·
  <a href="https://github.com/riggi89/RSSAM/discussions">Discussions</a> ·
  <a href="https://github.com/riggi89/RSSAM/issues">Issues</a> ·
  <a href="https://github.com/riggi89/RSSAM/wiki">Wiki</a>
</p>
