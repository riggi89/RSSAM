# RSSAM original build script.
# Copyright (c) 2026 Daniel Riggi (riggi89).
# Distributed under the project license; see LICENSE.md and NOTICE.md.

#requires -Version 5.1

param(
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'

# -----------------------------------------------------------------------------
# Projektstamm bestimmen
# -----------------------------------------------------------------------------

$root = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $root 'artifacts\source'
}

# -----------------------------------------------------------------------------
# Version lesen
# -----------------------------------------------------------------------------

$propsPath = Join-Path $root 'Directory.Build.props'

if (-not (Test-Path -LiteralPath $propsPath)) {
    throw "Directory.Build.props wurde nicht gefunden: $propsPath"
}

[xml]$props = Get-Content -LiteralPath $propsPath -Raw

$version = [string](
    $props.Project.PropertyGroup |
    Select-Object -First 1
).Version

if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'Version not found in Directory.Build.props.'
}

# -----------------------------------------------------------------------------
# Ausgabe vorbereiten
# -----------------------------------------------------------------------------

New-Item `
    -ItemType Directory `
    -Force `
    -Path $OutputDirectory |
    Out-Null

$zip = Join-Path $OutputDirectory "RSSAM_$version-Source.zip"

if (Test-Path -LiteralPath $zip) {
    Remove-Item -LiteralPath $zip -Force
}

$temp = Join-Path `
    ([System.IO.Path]::GetTempPath()) `
    ("RSSAM-src-" + [Guid]::NewGuid().ToString('N'))

New-Item `
    -ItemType Directory `
    -Force `
    -Path $temp |
    Out-Null

# -----------------------------------------------------------------------------
# Ausgeschlossene Verzeichnisse
#
# Diese Namen werden in JEDER Verzeichnistiefe ausgeschlossen.
# -----------------------------------------------------------------------------

$excludeDirs = @(
    '.git',
    '.vs',
    'bin',
    'obj',
    'artifacts'
    '*.png'
)

function Test-IsExcludedPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $relativePath = $Path.Substring($root.Length).TrimStart(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar
    )

    if ([string]::IsNullOrWhiteSpace($relativePath)) {
        return $false
    }

    $parts = $relativePath -split '[\\/]'

    foreach ($part in $parts) {
        if ($excludeDirs -contains $part) {
            return $true
        }
    }

    return $false
}

try {

    Write-Host ""
    Write-Host "RSSAM Source ZIP"
    Write-Host "---------------"
    Write-Host "Version : $version"
    Write-Host "Quelle  : $root"
    Write-Host "Ziel    : $zip"
    Write-Host ""

    # -------------------------------------------------------------------------
    # Verzeichnisse anlegen
    # -------------------------------------------------------------------------

    Get-ChildItem `
        -LiteralPath $root `
        -Directory `
        -Recurse `
        -Force |
    Where-Object {
        -not (Test-IsExcludedPath -Path $_.FullName)
    } |
    ForEach-Object {

        $relativePath = $_.FullName.Substring($root.Length).TrimStart(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar
        )

        $destination = Join-Path $temp $relativePath

        New-Item `
            -ItemType Directory `
            -Force `
            -Path $destination |
            Out-Null
    }

    # -------------------------------------------------------------------------
    # Dateien kopieren
    # -------------------------------------------------------------------------

    $files = @(
        Get-ChildItem `
            -LiteralPath $root `
            -File `
            -Recurse `
            -Force |
        Where-Object {
            -not (Test-IsExcludedPath -Path $_.FullName)
        }
    )

    foreach ($file in $files) {

        $relativePath = $file.FullName.Substring($root.Length).TrimStart(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar
        )

        $destination = Join-Path $temp $relativePath
        $destinationDirectory = Split-Path -Parent $destination

        if (-not (Test-Path -LiteralPath $destinationDirectory)) {
            New-Item `
                -ItemType Directory `
                -Force `
                -Path $destinationDirectory |
                Out-Null
        }

        Copy-Item `
            -LiteralPath $file.FullName `
            -Destination $destination `
            -Force
    }

    # -------------------------------------------------------------------------
    # ZIP erzeugen
    # -------------------------------------------------------------------------

    Compress-Archive `
        -Path (Join-Path $temp '*') `
        -DestinationPath $zip `
        -CompressionLevel Optimal

    Write-Host ""
    Write-Host "Source ZIP erfolgreich erstellt:"
    Write-Host $zip
    Write-Host ""
    Write-Host "Dateien im Archiv: $($files.Count)"
    Write-Host ""
    Write-Host "Ausgeschlossen:"
    
    foreach ($excludeDir in $excludeDirs) {
        Write-Host "  - $excludeDir"
    }

    Write-Host ""
}
finally {

    if (Test-Path -LiteralPath $temp) {
        Remove-Item `
            -LiteralPath $temp `
            -Recurse `
            -Force `
            -ErrorAction SilentlyContinue
    }
}
