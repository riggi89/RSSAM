# RSSAM developer documentation

This directory contains the technical documentation for working on RSSAM. The repository [`README.md`](../README.md) is intentionally limited to the application, installation, usage and user support.

## Start here

| Document | Contents |
| --- | --- |
| [Getting started](development/GETTING-STARTED.md) | Visual Studio, .NET, C# and WinUI prerequisites; first local build |
| [Architecture](development/ARCHITECTURE.md) | Dependency direction, Card Idler module, shell composition, workers, state and localization |
| [Project structure](development/PROJECT-STRUCTURE.md) | Responsibilities of every project and important source folders |
| [C# and WinUI guidelines](development/CSHARP-AND-WINUI.md) | Code conventions, UI patterns, threading, theming, DPI and localization |
| [PowerShell scripts](development/POWERSHELL-SCRIPTS.md) | Parameters, examples and generated output for every script |
| [Build and release](development/BUILD-AND-RELEASE.md) | Debug builds, publish output, installers, portable archives and releases |
| [Testing](development/TESTING.md) | xUnit configuration, test execution and coverage |
| [Application icon assets](assets/APP-ICON.md) | Icon source, generated sizes and licensing |

## Documentation rules

- End-user information belongs in the repository [`README.md`](../README.md).
- Visual Studio, C#, .NET, WinUI, PowerShell, tests, architecture and release instructions belong below `docs/development`.
- Product screenshots belong below `docs/images`.
- Asset-specific technical notes belong below `docs/assets`.
- Release history remains in [`CHANGELOG.md`](../CHANGELOG.md).
- License and attribution text remains in [`LICENSE.md`](../LICENSE.md) and [`NOTICE.md`](../NOTICE.md).

When source behavior changes, update the related developer document and the current changelog entry in the same change.
