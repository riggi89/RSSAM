# RSSAM

<p align="center">
  <img src="src/RSSAM.App/Assets/RSSAM-AppIcon-256.png" width="128" height="128" alt="RSSAM application icon">
</p>

<p align="center">
  <a href="https://github.com/riggi89/RSSAM/releases"><img alt="Windows x86 supported" src="https://img.shields.io/badge/Windows-x86-0078D4?logo=windows11&amp;logoColor=white"></a>
  <a href="https://github.com/riggi89/RSSAM/releases"><img alt="Windows x64 supported" src="https://img.shields.io/badge/Windows-x64-0078D4?logo=windows11&amp;logoColor=white"></a>
  <a href="https://github.com/riggi89/RSSAM/releases"><img alt="Version 2.0.1" src="https://img.shields.io/badge/version-2.0.1-blue"></a>
  <a href="LICENSE.md"><img alt="zlib license" src="https://img.shields.io/badge/license-zlib-green"></a>
</p>

<p align="center">
  <strong>Riggi's Steam Achievement Manager</strong><br>
  A modern Windows application for viewing and managing Steam achievements and statistics.
</p>

> [!CAUTION]
> RSSAM changes achievement and statistic data through the Steam client. Close running games before making changes and use RSSAM only with accounts and games you own.

## About RSSAM

RSSAM provides one clear interface for a Steam game library, achievements and supported statistics. Games can be displayed as tiles, a list or a table. Favorites, search and filters make large libraries easier to navigate.

Version 2.0 adds Card Idler as a dedicated navigation area. It scans the signed-in account for remaining Steam trading-card drops and can idle several eligible games at the same time without opening them individually.

RSSAM is an independently maintained fork of Steam Achievement Manager. It is not affiliated with or endorsed by Valve Corporation.

## Features

- Fast Steam game-library loading and global search.
- Tile, list and read-only table layouts.
- Favorites with a dedicated favorites filter.
- Achievement progress and global rarity information.
- Unlock, lock, invert and save achievement changes.
- View and edit supported integer and floating-point statistics.
- Integrated Card Idler with password, Steam Guard or Steam Mobile QR sign-in, title-bar search, three layouts, drop scanning, queue management and up to 32 simultaneous idle games.
- CSV export for achievement data.
- One-click game opening from the complete table row.
- Loading progress directly below the toolbar.
- Light, dark and system themes with Mica or Acrylic backgrounds.
- German, English, Spanish, French and Turkish interface languages.
- High-DPI support for displays such as 4K at 150 percent scaling.
- Separate self-contained x86 and x64 editions.

## Screenshots

| Game library | Achievement management |
| --- | --- |
| ![RSSAM game library](docs/images/game-library-grid.png) | ![RSSAM achievement management](docs/images/achievement-management.png) |

Additional views: [List](docs/images/game-library-list.png) · [Table](docs/images/game-library-table.png) · [Favorites](docs/images/game-library-favorites.png) · [Settings](docs/images/settings.png)

## Download and installation

Download the latest version from [GitHub Releases](https://github.com/riggi89/RSSAM/releases).

| Package | Recommended for |
| --- | --- |
| `RSSAM_2.0.1-win-x64-Setup.exe` | Most Windows 10 and Windows 11 computers |
| `RSSAM_2.0.1-win-x86-Setup.exe` | 32-bit Windows installations |
| `RSSAM_2.0.1-win-x64-Portable.zip` | Portable use on 64-bit Windows |
| `RSSAM_2.0.1-win-x86-Portable.zip` | Portable use on 32-bit Windows |

The installers install RSSAM for the current user under `%LOCALAPPDATA%\Programs\RSSAM` and do not require administrator rights. Extract a portable ZIP completely before starting `RSSAM.exe`.

> [!NOTE]
> The installers are currently unsigned. Windows SmartScreen may display an **Unknown publisher** warning. Verify that the file came from the official RSSAM release before selecting **More info → Run anyway**.

### Requirements

- Windows 10 version 1809 or newer; Windows 11 is recommended.
- Steam installed, running and signed in.
- Internet access for game information and images.

### Updating

Install the new release over the existing installation using the same architecture. Uninstall RSSAM first when switching between x86 and x64. Settings and favorites are retained.

## First steps

1. Start Steam and sign in.
2. Start RSSAM and wait for the game library to load.
3. Select a game to open its achievements and statistics.
4. Use the toolbar commands to change, export or reload data.
5. Review every confirmation dialog before saving or resetting changes.

## Card Idler

Open **Card Idler** from the navigation pane. Sign in with the Steam account name and password and complete Steam Guard when requested, or use the QR-code button and confirm the request in Steam Mobile. RSSAM scans the private badges pages for games with remaining card drops. The title-bar search filters these games, and the toolbar switches between tile, list and detail views. Select **Start idling** to report the configured batch as being played; the module stops briefly and rechecks at the selected interval so Steam can grant and report new drops.

The optional saved login uses a Windows DPAPI-protected refresh token tied to the current Windows user. RSSAM never stores the Steam password. Signing out removes the saved token. Starting a game elsewhere pauses Card Idler automatically until that playing session ends.

## Game library

Use the buttons above the library to switch between tile, list and table views. The star filter limits the result to favorite games. Long game names display their full text in a tooltip.

The global search field in the title bar searches the currently active area. In the game library it searches games; inside a selected game it searches achievements or statistics.

## Achievements and statistics

The achievement toolbar provides icon buttons with localized tooltips. **Save** is placed first, related actions are grouped with separators, and **Reload** remains at the far right.

Protected achievements and statistics cannot be modified. RSSAM displays an explanation when Steam or a game schema prevents a requested change.

The CSV export contains stable identifiers, names, descriptions, unlock state and time, protection and hidden flags. Files use UTF-8 with a byte-order mark for compatibility with Microsoft Excel.

## Appearance and languages

The title-bar theme button switches directly between light and dark mode. The Settings page also provides System, Light and Dark choices along with Mica, Acrylic and standard window backgrounds.

Available interface languages:

- Deutsch
- English
- Español
- Français
- Türkçe

## Local data

RSSAM stores its user data under `%LOCALAPPDATA%\RSSAM`.

| Path | Purpose |
| --- | --- |
| `settings.json` | Interface, window and behavior settings |
| `favorites.json` | Favorite Steam App IDs |
| `Logs\startup.log` | Startup and error diagnostics |
| `CardIdler\settings.json` | Card Idler account name, intervals and DPAPI-protected refresh token |
| `CardIdler\metadata-cache.json` | Cached Steam Store details for games with card drops |
| `CardIdler\log.txt` | Card Idler diagnostics without passwords or Steam Guard codes |

Deleting `settings.json` or `favorites.json` while RSSAM is closed resets the corresponding data. The application recreates missing files with default values.

## Troubleshooting

If RSSAM does not start or cannot connect to Steam:

- Confirm that Steam is running and signed in.
- Run Steam and RSSAM with the same privilege level; normally neither should run as administrator.
- Install or extract the complete package instead of copying only `RSSAM.exe`.
- Wait until Steam has finished starting, then select **Reload games**.
- Reinstall the current package if security software quarantined or removed files.
- Check `%LOCALAPPDATA%\RSSAM\Logs\startup.log` for details.

When reporting a problem, include the RSSAM version, Windows version, x86 or x64 architecture, reproduction steps and relevant log entries. Remove personal information and never publish Steam passwords, authentication codes, session tokens, cookies or API keys.

## Help and feedback

- [Discussions](https://github.com/riggi89/RSSAM/discussions) — questions, feedback and ideas
- [Issues](https://github.com/riggi89/RSSAM/issues) — reproducible problems
- [Wiki](https://github.com/riggi89/RSSAM/wiki) — additional guides
- [Changelog](CHANGELOG.md) — release history

## License and attribution

Fork modifications: Copyright (c) 2026 Daniel Riggi (riggi89)  
Original software: Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)

RSSAM is based on the original [Steam Achievement Manager](https://github.com/gibbed/SteamAchievementManager). The integrated Card Idler module is adapted from CardIdler by Sam-218 under the MIT License and uses SteamKit2 for Steam network authentication.

See [LICENSE.md](LICENSE.md) and [NOTICE.md](NOTICE.md) for the complete license and attribution information.

---

<p align="center">
  <a href="https://github.com/riggi89/RSSAM/releases">Releases</a> ·
  <a href="https://github.com/riggi89/RSSAM/discussions">Discussions</a> ·
  <a href="https://github.com/riggi89/RSSAM/issues">Issues</a> ·
  <a href="https://github.com/riggi89/RSSAM/wiki">Wiki</a>
</p>
