# Build and release

## Build configurations

RSSAM supports `Debug` and `Release` configurations for `x86` and `x64`. Shared settings in `Directory.Build.props` map each solution platform to its matching Windows runtime identifier.

Use x64 for routine development. Build x86 whenever native Steam interop, callback layouts, installers or release packaging changes.

## Visual Studio build

1. Open `RSSAM.sln`.
2. Select `Debug` or `Release`.
3. Select `x64` or `x86`.
4. Build the solution.
5. Start `RSSAM.App` for interactive testing.

The unit-test project is intentionally x64-only. Visual Studio maps it to its x64 configuration even when production projects are built for x86.

## Command-line build

```powershell
.\scripts\build.ps1 -Configuration Release -Architecture All
```

This performs restore and build for both production architectures. It does not create distributable packages.

## Publish output

```powershell
.\scripts\publish.ps1 -Configuration Release -Architecture All
```

The publish script creates self-contained, unpackaged output below:

```text
artifacts/
└─ publish/
   ├─ win-x64/
   └─ win-x86/
```

Each directory contains the matching RSSAM executable, managed assemblies including `RSSAM.CardIdler.dll`, SteamKit2, QRCoder and their dependencies, the native Steam bridge, WinUI and .NET runtime dependencies, assets, documentation and `resources.pri`.

Publishing uses isolated temporary build roots and validates required files before making the result available under `artifacts`. This prevents stale `bin` or `obj` files from entering a release.

## Installer packages

Install Inno Setup 6, then run:

```powershell
.\scripts\build-installer.ps1 -Configuration Release -Architecture All
```

The script publishes first unless `-SkipPublish` is supplied. Separate per-user installers are generated for x86 and x64 under `artifacts/installer`.

Current installers are unsigned. Signing must happen after package generation and before checksum publication or release upload.

## Portable packages

```powershell
.\scripts\build-portable.ps1 -Configuration Release -Architecture All -SkipPublish
```

Use `-SkipPublish` only after a successful matching publish. Portable archives contain the complete self-contained runtime and must be extracted before use.

## Source package

```powershell
.\scripts\build-source-zip.ps1
```

The source archive contains the repository source and documentation while excluding generated build folders and repository metadata.

## Release checklist

1. Confirm the intended three-part version.
2. Run `set-version.ps1` if the version changes.
3. Update the top entry in `CHANGELOG.md`.
4. Update versioned package examples in the application README when required.
5. Clean generated project output.
6. Run all tests with coverage.
7. Publish x86 and x64.
8. Create and validate both installers.
9. Create and validate both portable archives.
10. Create and validate the source archive.
11. Smoke-test the installed and portable x64 versions on Windows 11.
12. Smoke-test x86 when a suitable Windows environment is available.
13. Verify Steam connection, game loading, achievement loading, save confirmation and settings persistence.
14. Verify Card Idler password/Steam Guard and QR sign-in, badge scanning, title-bar search, all three views, toolbar sliders, start/stop, playing-elsewhere pause and sign-out token removal.
15. Verify light and dark title-bar buttons, 150 percent DPI scaling and every supported language.
16. Generate checksums after signing and after all file content is final.

## GitHub Actions

`.github/workflows/release.yml` supports manual execution and tags matching `v*`. The Windows runner:

1. restores the repository;
2. runs the unit tests with coverage;
3. builds x86 and x64 installers;
4. creates portable archives;
5. creates the source archive;
6. uploads workflow artifacts;
7. attaches packages to the tagged GitHub release.

Local release validation remains important because the workflow cannot replace interactive Steam and WinUI smoke tests.

## Version locations

The compiled version is synchronized across:

- `Directory.Build.props`;
- `src/RSSAM.App/AppVersion.cs`;
- `src/RSSAM.App/app.manifest`.

The Windows manifest uses four parts; the final component remains zero. User-facing RSSAM versions use three parts, for example `2.0.2`.
