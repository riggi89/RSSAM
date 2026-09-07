# RSSAM portable archive build script.
# Copyright (c) 2026 Daniel Riggi (riggi89).
# Distributed under the project license; see LICENSE.md and NOTICE.md.

#requires -Version 5.1

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [ValidateSet('x86', 'x64', 'All')]
    [string]$Architecture = 'All',

    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$propsPath = Join-Path $root 'Directory.Build.props'
$portableOutput = Join-Path $root 'artifacts\portable'

$targetArchitectures = if ($Architecture -eq 'All') {
    @('x86', 'x64')
}
else {
    @($Architecture)
}

[xml]$props = Get-Content -LiteralPath $propsPath -Raw
$version = [string](($props.Project.PropertyGroup | Select-Object -First 1).Version)

if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'Version not found in Directory.Build.props.'
}

if (-not $SkipPublish) {
    & (Join-Path $PSScriptRoot 'publish.ps1') `
        -Configuration $Configuration `
        -Architecture $Architecture

    if ($LASTEXITCODE -ne 0) {
        throw "Publishing failed with exit code $LASTEXITCODE."
    }
}

New-Item -ItemType Directory -Path $portableOutput -Force | Out-Null

$requiredFiles = @(
    'RSSAM.exe',
    'RSSAM.dll',
    'RSSAM.Core.dll',
    'RSSAM.API.dll',
    'WinUI.TableView.dll',
    'Microsoft.UI.Xaml.dll',
    'Microsoft.WindowsAppRuntime.dll',
    'resources.pri',
    'coreclr.dll',
    'hostfxr.dll',
    'hostpolicy.dll'
)

$createdArchives = @()

foreach ($targetArchitecture in $targetArchitectures) {
    $runtimeIdentifier = "win-$targetArchitecture"
    $publishDirectory = Join-Path $root "artifacts\publish\$runtimeIdentifier"

    if (-not (Test-Path -LiteralPath $publishDirectory)) {
        throw "Publish directory does not exist: $publishDirectory"
    }

    $missingFiles = @(
        $requiredFiles |
        Where-Object {
            -not (Test-Path -LiteralPath (Join-Path $publishDirectory $_))
        }
    )

    if ($missingFiles.Count -gt 0) {
        throw (
            "Incomplete $runtimeIdentifier publish output. Missing required files: " +
            ($missingFiles -join ', ')
        )
    }

    $archivePath = Join-Path `
        $portableOutput `
        "RSSAM_$version-$runtimeIdentifier-Portable.zip"

    if (Test-Path -LiteralPath $archivePath) {
        Remove-Item -LiteralPath $archivePath -Force
    }

    $tempDirectory = Join-Path `
        ([System.IO.Path]::GetTempPath()) `
        ("RSSAM-portable-" + [Guid]::NewGuid().ToString('N'))
    $portableFolder = Join-Path $tempDirectory 'RSSAM'

    try {
        New-Item -ItemType Directory -Path $portableFolder -Force | Out-Null

        Copy-Item `
            -Path (Join-Path $publishDirectory '*') `
            -Destination $portableFolder `
            -Recurse `
            -Force

        Compress-Archive `
            -Path $portableFolder `
            -DestinationPath $archivePath `
            -CompressionLevel Optimal

        if (-not (Test-Path -LiteralPath $archivePath)) {
            throw "Portable archive was not created: $archivePath"
        }

        $createdArchives += $archivePath
        Write-Host "Created portable archive: $archivePath"
    }
    finally {
        if (Test-Path -LiteralPath $tempDirectory) {
            Remove-Item `
                -LiteralPath $tempDirectory `
                -Recurse `
                -Force `
                -ErrorAction SilentlyContinue
        }
    }
}

Write-Host ''
Write-Host 'RSSAM portable archives created:'
foreach ($archive in $createdArchives) {
    Write-Host "  $archive"
}
