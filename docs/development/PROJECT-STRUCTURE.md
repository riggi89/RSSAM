# RSSAM project structure

## Solution overview

| Project | Responsibility | Platform |
| --- | --- | --- |
| `RSSAM.App` | WinUI 3 application, title bar, shell, pages and startup | x86 and x64 |
| `RSSAM.CardIdler` | Embedded WinUI page and SteamKit2 trading-card idling engine | x86 and x64 |
| `RSSAM.Core` | Models, services, storage, localization, search and schemas | x86 and x64 through the app |
| `RSSAM.API` | Native Steam client interoperability | x86 and x64 |
| `RSSAM.UnitTests` | Tests for Core and API behavior | x64 test host |

## RSSAM.App

Important locations:

| Path | Purpose |
| --- | --- |
| `App.xaml` / `App.xaml.cs` | Application startup, services and hidden worker dispatch |
| `MainWindow.xaml` / `.cs` | Custom title bar, theme toggle, global search and native caption styling |
| `Presentation/Views` | Shell, manager, settings and changelog pages |
| `Presentation/Controls` | Reusable WinUI controls such as `SettingsRow` |
| `Presentation/ViewModels` | Reserved for presentation-specific view models |
| `Resources/Styles` | Colors, brushes, controls, toolbar and shell styles |
| `Services` | Dialog, InfoBar and other UI-specific services |
| `Assets` | Application icon files used by the executable and title bar |

`RSSAM.App` references `RSSAM.Core` and `RSSAM.CardIdler`. Native achievement-management Steam calls must remain behind Core services.

## RSSAM.CardIdler

Important locations:

| Path | Purpose |
| --- | --- |
| `Views/CardIdlerPage.xaml` / `.cs` | Embedded WinUI interface, tile/list/TableView game layouts, title-bar search and shell toolbar integration |
| `ViewModels` | Password, Steam Guard and QR login, scan, queue and idle-loop state |
| `Services/SteamService.cs` | SteamKit2 credentials/QR authentication, playing state and community cookies |
| `Services/BadgeScraper.cs` | Private badge-page parsing and remaining-drop detection |
| `Services/MetadataService.cs` | Regional Steam Store metadata and cache |
| `Models/GameInfo.cs` | Bindable game/drop model |
| `LICENSE.CardIdler.txt` | MIT license retained from CardIdler by Sam-218 |

The project builds as `RSSAM.CardIdler.dll`. It uses SteamKit2 independently of the native API bridge, QRCoder for local Steam Mobile QR rendering and WinUI.TableView for the read-only detail layout. Its data is stored below `%LOCALAPPDATA%\RSSAM\CardIdler`.

## RSSAM.Core

Important locations:

| Path | Purpose |
| --- | --- |
| `Interfaces` | Localization and search contracts shared with the app |
| `Presentation/Shell` | Presentation-neutral page-to-shell contracts and toolbar item models |
| `Localization` | Localization service and embedded JSON dictionaries |
| `Models` | Games, achievements, settings and worker data models |
| `Search` | Search provider implementations |
| `Services` | Catalog, statistics, workers and Steam error handling |
| `Stats` | Editable statistic definitions and validation |
| `Storage` | Settings and favorites persistence |
| `Infrastructure/SteamSchema` | Valve KeyValue and Steam schema parsing |

Core should remain independent of Microsoft.UI and other presentation namespaces.

## RSSAM.API

Important locations:

| Path | Purpose |
| --- | --- |
| `Callbacks` | Native Steam callback payload definitions |
| `Client` | Steam client initialization and failure handling |
| `Common` | Shared native helpers and string conversion |
| `Interfaces` | Marshalled Steam interface tables |
| `Native` | Vtable invocation infrastructure |
| `Types` | Steam results, identifiers and value structures |
| `Wrappers` | Managed wrappers around native interfaces |

Files copied from or substantially derived from the original Steam Achievement Manager retain their original attribution headers.

## RSSAM.UnitTests

The test project is framework-dependent and always uses an x64 test host. It validates storage, models, localization, CSV export, search, native-wrapper guards and Valve KeyValue parsing without starting WinUI or opening a live Steam session.

Core, API and Card Idler are RID-neutral class libraries. Only the executable App project owns a default runtime identifier, so Visual Studio and the x64 test host resolve the same Core/API reference-assembly paths. Both dependencies are copied beside the test assembly.

## Repository folders

| Folder | Purpose |
| --- | --- |
| `.github/workflows` | Tagged and manually started Windows release workflow |
| `docs` | Developer documentation, screenshots and asset notes |
| `installer` | Inno Setup definition |
| `scripts` | PowerShell build, test, publish and packaging scripts |
| `src` | Production C# projects |
| `tests` | Automated test projects and run settings |

## Root files

| File | Purpose |
| --- | --- |
| `README.md` | Application overview and end-user instructions |
| `CHANGELOG.md` | English release history displayed inside RSSAM |
| `LICENSE.md` | Project license and third-party license text |
| `NOTICE.md` | Fork and third-party attribution |
| `Directory.Build.props` | Shared version, platform, isolated-output and unit-test properties |
| `Directory.Build.targets` | Post-evaluation removal of stale repository-local generated C# files during isolated builds |
| `RSSAM.sln` | Visual Studio solution |

The root `ARCHITECTURE.md` and `PROJECTS.md` files are compatibility links to this documentation because the application project still includes those filenames in its published documentation set.
