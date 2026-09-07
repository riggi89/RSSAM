# RSSAM notices

## RSSAM modifications

**RSSAM – Riggi's Steam Achievement Manager** is a WinUI 3 / .NET 10 adaptation maintained by **Daniel Riggi (riggi89)**.

RSSAM-specific UI, integration and modernization changes:

- Copyright © 2026 **Daniel Riggi (riggi89)**.
- The project has been renamed to **RSSAM – Riggi's Steam Achievement Manager**.
- The original WinForms picker/game-manager split has been replaced with a single WinUI 3 application.
- The original Steam native API bridge and portions of the Steam achievement/statistics schema logic were adapted for the new application structure.
- RSSAM 1.0.6 retains the responsive shell, adaptive TitleBar branding/search behavior, compact CommandBar/navigation behavior and Settings-based program/license presentation, and adds a separated src/RSSAM.App + src/RSSAM.Core + src/RSSAM.API architecture with a unified seamless shell surface for TitleBar, toolbar, navigation and status bar.

- RSSAM 1.0.11 keeps the central `ShellPage` and floating Popup-based global InfoBar service, hardens native Steam/session handling and schema parsing, serializes mutable operations, and uses the English root `CHANGELOG.md` as the sole in-app Changelog source.
- RSSAM 1.0.13 adds a process-level Steam App ID handoff for selected games, makes Games navigation return to the catalog, fixes current Language/Theme selection display, and removes persisted page/game-detail restoration.
- RSSAM 1.0.14 replaces the visible App ID restart with isolated hidden Steam workers and adds persistent Tile/List game-library layouts with uncropped proportional artwork.
- RSSAM 1.0.15 centralizes WinUI dialogs, adds a live Steam process indicator to the status bar, and explicitly labels RSSAM-authored and SAM-derived source/build files.
- RSSAM 1.0.16 confines the page toolbar to the content column, mirrors loading progress into the InfoBar and status bar, stabilizes the TitleBar search layout, and adopts three-part version numbers.
- RSSAM 1.0.17 centers TitleBar search, adds a WinUI.TableView game-library view, persists game favorites in one JSON file, and reserves loading InfoBars for explicit Reload commands while retaining status-bar feedback.
- RSSAM 1.0.18 fixes the WinUI favorite-control build, removes unused duplicate files from `RSSAM.API`, and makes the root `LICENSE.md` the single license file shipped with RSSAM.
- RSSAM 1.0.19 adds x64 alongside x86, supplies a combined unsigned Windows installer and tagged-release workflow, and adopts the supplied fork/modification/zlib notice as the central license text.
- RSSAM 1.0.20 makes the game-library TableView display-only, opens achievements from a row click, and detects per-user Inno Setup installations.
- RSSAM 1.0.21 validates the complete self-contained publish payload, recovers the WinUI resource index when necessary, and records failures that occur before the application shell is available.
- RSSAM 1.0.22 consolidates the complete user/build/release documentation and application screenshots in the README, and synchronizes all compiled version locations through the version script.
- RSSAM 1.0.23 makes unpackaged publishing explicitly generate and deploy the WinUI resource index required at startup, while removing remaining nullable and unused-event compiler warnings.
- RSSAM 1.0.24 adds original RSSAM icon artwork for the executable, installer, Windows shortcuts, installed-app entry, TitleBar and project documentation.
- RSSAM 1.0.25 corrects the native `ISteamClient018.GetISteamApps` calling convention and object pointer so Steam game metadata and ownership interfaces initialize correctly on x86 and x64.

## Third-party UI dependency

RSSAM references **WinUI.TableView 1.4.1** for the game-library Table view. WinUI.TableView is distributed under the MIT License; its package and source are available from https://github.com/w-ahmad/WinUI.TableView.

## Original project

RSSAM is derived from **Steam Achievement Manager (SAM)** by **Rick (Gibbed)**:

- Original author/coder: Rick (Gibbed)
- Original project: https://github.com/gibbed/SteamAchievementManager
- Original license: zlib License
- Original copyright notice: Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)

The central `LICENSE.md` contains the supplied fork copyright, original-project copyright, modification notice and zlib third-party notice. Source files copied or substantially derived from the original project also retain the original copyright header where applicable.

RSSAM is an altered source version and is not presented as the original Steam Achievement Manager.
