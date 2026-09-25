# Testing RSSAM

## Test project

`tests/RSSAM.UnitTests` is an xUnit project targeting .NET 10 for Windows. It is framework-dependent and fixed to an x64 test host because the suite tests managed Core and API behavior without loading the production Steam client module.

The current suite contains 23 tests covering:

- settings defaults, normalization, persistence and reset;
- favorites persistence;
- domain-model behavior;
- localization and Steam-language mapping;
- search-provider forwarding and argument validation;
- achievement CSV escaping and output;
- Steam native-wrapper argument guards;
- Valve KeyValue parsing.

## Run from PowerShell

```powershell
.\scripts\test.ps1 -Configuration Release
```

Collect code coverage:

```powershell
.\scripts\test.ps1 -Configuration Release -CollectCoverage
```

Coverage results are written below `artifacts/test-results/x64`.

## Run from Visual Studio

1. Open `RSSAM.sln`.
2. Build the solution.
3. Open **Test Explorer**.
4. Run all tests.

`RSSAM.UnitTests.runsettings` selects the x64 test host. Do not switch the test assembly to x86 simply because the current production configuration is x86.

## Test isolation

Storage tests use dedicated temporary directories. New settings or persistence tests must not read or modify `%LOCALAPPDATA%\RSSAM`.

Steam-facing unit tests validate managed parsing and guards only. They must not require:

- a running Steam process;
- a signed-in user;
- ownership of a game;
- live network access;
- WinUI initialization.

Use manual smoke tests for behavior that requires Steam, native client initialization, window composition or input.

## Adding tests

Add a regression test when fixing deterministic Core or API behavior. Prefer one focused assertion group per scenario and descriptive names in the form:

```text
Method_WhenCondition_ExpectedResult
```

For localization changes, verify:

- culture normalization;
- the Steam schema language;
- at least one translated value;
- key parity across every dictionary;
- matching composite-format placeholders.

For persistence changes, verify defaults, round-trip behavior, invalid input normalization and atomic-save cleanup.

## Manual UI matrix

Before a release, test at least:

| Area | Required variants |
| --- | --- |
| Architecture | x64; x86 when available |
| Theme | System, Light, Dark |
| DPI | 100 percent and 150 percent |
| Library | Tile, List, Table, Favorites |
| Detail page | Achievements and Statistics |
| Card Idler | Password/Steam Guard and QR sign-in, scan, tile/list/TableView detail layouts, complete-row clicks, search, toolbar controls, start/stop, pause and sign-out |
| Input | Complete row click, toolbar buttons, search, dialogs |
| Languages | German, English, Spanish, French, Turkish |
| Distribution | Installer and portable ZIP |

Confirm that title-bar caption buttons remain visible in normal, hover and pressed states in both light and dark themes.
