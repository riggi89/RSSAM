# PowerShell scripts

All scripts are intended to be run from a Windows PowerShell session. Examples assume the repository root is the current directory.

## Script overview

| Script | Purpose | Default output or effect |
| --- | --- | --- |
| `build.ps1` | Restore and build the solution | Project `bin` and `obj` folders |
| `test.ps1` | Run the x64 xUnit suite | Console results; optional coverage under `artifacts` |
| `clean.ps1` | Remove project `bin` and `obj` folders | Deletes generated build folders only |
| `publish.ps1` | Create validated self-contained app output | `artifacts/publish/win-x86` and `win-x64` |
| `build-installer.ps1` | Compile Inno Setup installers | `artifacts/installer` |
| `build-portable.ps1` | Create portable ZIP files | `artifacts/portable` |
| `build-source-zip.ps1` | Create a clean source archive | `artifacts/source` |
| `set-version.ps1` | Synchronize compiled version locations | Updates the three version-bearing source files |

## Build

```powershell
.\scripts\build.ps1 [-Configuration Debug|Release] [-Architecture x86|x64|All]
```

Examples:

```powershell
.\scripts\build.ps1 -Configuration Debug -Architecture x64
.\scripts\build.ps1 -Configuration Release -Architecture All
```

The script restores `RSSAM.sln` separately for every selected architecture and then builds with `--no-restore`.

## Test

```powershell
.\scripts\test.ps1 [-Configuration Debug|Release] [-CollectCoverage]
```

Examples:

```powershell
.\scripts\test.ps1 -Configuration Release
.\scripts\test.ps1 -Configuration Release -CollectCoverage
```

The test project always runs with `Platform=x64` and its checked-in runsettings file. Coverage output is written to `artifacts/test-results/x64`.

## Clean

```powershell
.\scripts\clean.ps1
.\scripts\clean.ps1 -WhatIf
```

The script searches only below `src` and `tests`, validates the repository root through `RSSAM.sln`, and removes `bin` and `obj` directories. Use `-WhatIf` to preview the operation.

## Publish

```powershell
.\scripts\publish.ps1 [-Configuration Debug|Release] [-Architecture x86|x64|All] [-SkipClean]
```

Publishing uses isolated temporary build and intermediate directories to avoid stale files, Visual Studio locks and apostrophes in repository paths. The script validates the runtime payload and ensures that the WinUI resource index is present before copying the result to `artifacts/publish`.

## Installer

```powershell
.\scripts\build-installer.ps1 `
    [-Configuration Debug|Release] `
    [-Architecture x86|x64|All] `
    [-SkipPublish] `
    [-InnoCompiler <path-to-ISCC.exe>]
```

If `-InnoCompiler` is omitted, the script checks the command path and common machine-wide and per-user Inno Setup 6 locations. `-SkipPublish` requires an already validated matching publish directory.

Expected filenames:

- `RSSAM_<version>-win-x64-Setup.exe`
- `RSSAM_<version>-win-x86-Setup.exe`

## Portable archives

```powershell
.\scripts\build-portable.ps1 `
    [-Configuration Debug|Release] `
    [-Architecture x86|x64|All] `
    [-SkipPublish]
```

The script validates required application, Card Idler, SteamKit2, WinUI, .NET runtime and resource files before creating each archive.

Expected filenames:

- `RSSAM_<version>-win-x64-Portable.zip`
- `RSSAM_<version>-win-x86-Portable.zip`

## Source archive

```powershell
.\scripts\build-source-zip.ps1 [-OutputDirectory <path>]
```

Without an output argument, the archive is created as `artifacts/source/RSSAM_<version>-Source.zip`. Generated build directories and repository metadata are excluded.

## Version synchronization

```powershell
.\scripts\set-version.ps1 -Version 2.0.0
```

The value must contain exactly three numeric parts. The script validates every expected match before writing and synchronizes:

- `Directory.Build.props` (`Version`, `AssemblyVersion`, `FileVersion`);
- `src/RSSAM.App/AppVersion.cs` (display fallback);
- `src/RSSAM.App/app.manifest` (four-part Windows version with `.0`).

The script deliberately does not rewrite changelog text, README package examples or release notes. Review those files manually for a new release.

## Safe release order

```powershell
.\scripts\set-version.ps1 -Version 2.0.0
.\scripts\clean.ps1
.\scripts\test.ps1 -Configuration Release -CollectCoverage
.\scripts\publish.ps1 -Configuration Release -Architecture All
.\scripts\build-installer.ps1 -Configuration Release -Architecture All -SkipPublish
.\scripts\build-portable.ps1 -Configuration Release -Architecture All -SkipPublish
.\scripts\build-source-zip.ps1
```

Stop immediately if any command fails. Do not package a partial or unvalidated publish directory.
