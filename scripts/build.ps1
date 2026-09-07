# RSSAM original build script.
# Copyright (c) 2026 Daniel Riggi (riggi89).
# Distributed under the project license; see LICENSE.md and NOTICE.md.

param(
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [ValidateSet('x86','x64','All')][string]$Architecture = 'All'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$targetArchitectures = if ($Architecture -eq 'All') { @('x86', 'x64') } else { @($Architecture) }

foreach ($targetArchitecture in $targetArchitectures) {
    Write-Host "Building RSSAM $Configuration for $targetArchitecture ..."
    dotnet restore "$root\RSSAM.sln" -p:Platform=$targetArchitecture
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed for $targetArchitecture with exit code $LASTEXITCODE."
    }

    dotnet build "$root\RSSAM.sln" -c $Configuration -p:Platform=$targetArchitecture --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed for $targetArchitecture with exit code $LASTEXITCODE."
    }
}
