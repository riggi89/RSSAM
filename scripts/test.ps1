# RSSAM unit-test script.
# Copyright (c) 2026 Daniel Riggi (riggi89).
# Distributed under the project license; see LICENSE.md and NOTICE.md.

#requires -Version 5.1

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$CollectCoverage
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'tests\RSSAM.UnitTests\RSSAM.UnitTests.csproj'
$arguments = @(
    'test', $project,
    '--configuration', $Configuration,
    '--property', 'Platform=x64',
    '--settings', (Join-Path $root 'tests\RSSAM.UnitTests\RSSAM.UnitTests.runsettings'),
    '--logger', 'console;verbosity=normal'
)

if ($CollectCoverage) {
    $arguments += @(
        '--collect', 'XPlat Code Coverage',
        '--results-directory', (Join-Path $root 'artifacts\test-results\x64')
    )
}

Write-Host "Running RSSAM.UnitTests $Configuration in the x64 test host ..."
& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Unit tests failed with exit code $LASTEXITCODE."
}
