# RSSAM original build script.
# Copyright (c) 2026 Daniel Riggi (riggi89).
# Distributed under the project license; see LICENSE.md and NOTICE.md.

#requires -Version 5.1
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$props = Join-Path $root 'Directory.Build.props'
$appVersion = Join-Path $root 'src\RSSAM.App\AppVersion.cs'
$appManifest = Join-Path $root 'src\RSSAM.App\app.manifest'
$utf8WithoutBom = [System.Text.UTF8Encoding]::new($false)

# Validate every version location before writing any file. This prevents a
# changed source layout from silently leaving RSSAM with mixed version values.
$files = @(
    @{
        Path = $props
        Replacements = @(
            @{ Pattern = '(?<=<Version>)\d+\.\d+\.\d+(?=</Version>)'; Value = $Version },
            @{ Pattern = '(?<=<AssemblyVersion>)\d+\.\d+\.\d+(?=</AssemblyVersion>)'; Value = $Version },
            @{ Pattern = '(?<=<FileVersion>)\d+\.\d+\.\d+(?=</FileVersion>)'; Value = $Version }
        )
    },
    @{
        Path = $appVersion
        Replacements = @(
            @{ Pattern = '(?<=public const string Fallback = ")\d+\.\d+\.\d+(?=";)'; Value = $Version }
        )
    },
    @{
        Path = $appManifest
        Replacements = @(
            @{ Pattern = '(?<=<assemblyIdentity version=")\d+\.\d+\.\d+\.\d+(?=" name="RSSAM\.app"/>)'; Value = "${Version}.0" }
        )
    }
)

$pendingWrites = @()
foreach ($file in $files) {
    if (-not (Test-Path -LiteralPath $file.Path)) {
        throw "Version file was not found: $($file.Path)"
    }

    $content = [System.IO.File]::ReadAllText($file.Path)
    foreach ($replacement in $file.Replacements) {
        $expression = [System.Text.RegularExpressions.Regex]::new(
            [string]$replacement.Pattern)
        if ($expression.Matches($content).Count -ne 1) {
            throw "Expected exactly one version match in $($file.Path): $($replacement.Pattern)"
        }

        $content = $expression.Replace($content, [string]$replacement.Value, 1)
    }

    $pendingWrites += @{
        Path = $file.Path
        Content = $content
    }
}

foreach ($write in $pendingWrites) {
    [System.IO.File]::WriteAllText(
        [string]$write.Path,
        [string]$write.Content,
        $utf8WithoutBom)
}

Write-Host "RSSAM source and manifest version set to $Version"
Write-Host 'Update CHANGELOG.md and the current-version text in README.md manually.'
