# Getting started with RSSAM development

## Development environment

RSSAM is a Windows desktop solution written in C# with .NET 10 and WinUI 3. Development and release packaging are supported on Windows.

Install the following tools:

- A current Visual Studio release with support for .NET 10.
- The **.NET desktop development** workload.
- WinUI 3 and Windows App SDK tooling.
- The .NET 10 SDK.
- Windows 10 SDK version 10.0.19041.0 or newer.
- PowerShell 5.1 or newer.
- Inno Setup 6 only when creating installer packages.
- Git for source-control operations.

## Open the solution in Visual Studio

1. Clone or extract the complete source tree.
2. Open `RSSAM.sln` in Visual Studio.
3. Allow NuGet package restore to finish.
4. Select `Debug` or `Release`.
5. Select the required `x64` or `x86` solution platform.
6. Set `RSSAM.App` as the startup project.
7. Start the project with or without the debugger.

Use `x64` for normal development unless a change specifically concerns the 32-bit Steam interface. The application and native Steam client bridge must use matching architectures.

## First command-line build

Run PowerShell from the repository root:

```powershell
.\scripts\build.ps1 -Configuration Debug -Architecture x64
```

Run the unit tests separately:

```powershell
.\scripts\test.ps1 -Configuration Release
```

The scripts stop on restore, compiler, test or packaging errors. Do not continue a release after an earlier command fails.

## Important solution characteristics

- The target framework is `net10.0-windows10.0.19041.0`.
- The minimum declared Windows platform version is 10.0.17763.0.
- Production builds support `x86` and `x64`.
- The app is unpackaged and self-contained for release publishing.
- Steam must be installed and running for live catalog or achievement testing.
- Card Idler uses SteamKit2 and QRCoder and requires network access plus a test account with Steam Guard and Steam Mobile for complete password and QR sign-in testing.
- Unit tests do not require the WinUI shell or an active Steam session.

## Recommended workflow

1. Read [Architecture](ARCHITECTURE.md) and [Project structure](PROJECT-STRUCTURE.md).
2. Make the smallest focused source change.
3. Build the affected architecture.
4. Run the complete unit-test suite.
5. Test light and dark themes when changing UI code.
6. Test 100 and 150 percent display scaling when changing layout or typography.
7. Update the current entry in [`CHANGELOG.md`](../../CHANGELOG.md).

See [PowerShell scripts](POWERSHELL-SCRIPTS.md) for command details and [Build and release](BUILD-AND-RELEASE.md) for package creation.
