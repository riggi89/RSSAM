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
$runSettings = Join-Path $root 'tests\RSSAM.UnitTests\RSSAM.UnitTests.runsettings'
$resultsDirectory = Join-Path $root 'artifacts\test-results\x64'
$temporaryRoot = Join-Path `
    ([System.IO.Path]::GetTempPath()) `
    ("RSSAM-tests-" + [Guid]::NewGuid().ToString('N'))
$buildRoot = Join-Path $temporaryRoot 'build'

if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
    throw "Unit-test project was not found: $project"
}

if (-not (Test-Path -LiteralPath $runSettings -PathType Leaf)) {
    throw "Unit-test runsettings file was not found: $runSettings"
}

if ($null -eq (Get-Command 'dotnet' -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK command (dotnet) was not found.'
}

New-Item -ItemType Directory -Path $buildRoot -Force | Out-Null
New-Item -ItemType Directory -Path $resultsDirectory -Force | Out-Null

$arguments = @(
    'test', $project,
    '--configuration', $Configuration,
    '--property', 'Platform=x64',
    '--property', "RSSAMBuildRoot=$buildRoot",
    '--property', 'UseSharedCompilation=false',
    '--settings', $runSettings,
    '--results-directory', $resultsDirectory,
    '--logger', 'console;verbosity=normal',
    '-m:1',
    '-nr:false'
)

if ($CollectCoverage) {
    $arguments += @('--collect', 'XPlat Code Coverage')
}

$testExitCode = 1

try {
    Write-Host "Running RSSAM.UnitTests $Configuration in the x64 test host ..."
    Write-Host "Isolated build root: $buildRoot"

    & dotnet @arguments
    $testExitCode = $LASTEXITCODE
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item `
            -LiteralPath $temporaryRoot `
            -Recurse `
            -Force `
            -ErrorAction SilentlyContinue
    }
}

if ($testExitCode -ne 0) {
    throw "Unit tests failed with exit code $testExitCode."
}

Write-Host "Unit tests completed successfully. Results: $resultsDirectory"
