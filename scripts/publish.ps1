# RSSAM original build script.
# Copyright (c) 2026 Daniel Riggi (riggi89).
# Distributed under the project license; see LICENSE.md and NOTICE.md.

#requires -Version 5.1

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [ValidateSet('x86', 'x64', 'All')]
    [string]$Architecture = 'All',

    [switch]$SkipClean
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $root 'src\RSSAM.App\RSSAM.App.csproj'
$artifactsRoot = Join-Path $root 'artifacts\publish'

$targetArchitectures = if ($Architecture -eq 'All') {
    @('x86', 'x64')
}
else {
    @($Architecture)
}

function New-SafeTemporaryDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $tempRoot = [System.IO.Path]::GetTempPath()

    if ([string]::IsNullOrWhiteSpace($tempRoot)) {
        throw 'Unable to determine the Windows temporary directory.'
    }

    $tempRoot = [System.IO.Path]::GetFullPath($tempRoot)

    if ($tempRoot.Contains("'")) {
        throw (
            "The temporary directory contains an apostrophe and cannot be used " +
            "as the safe RSSAM build path: $tempRoot"
        )
    }

    $directory = Join-Path `
        $tempRoot `
        ("RSSAM-$Name-" + [Guid]::NewGuid().ToString('N'))

    New-Item `
        -ItemType Directory `
        -Path $directory `
        -Force |
        Out-Null

    return [System.IO.Path]::GetFullPath($directory)
}

function Ensure-WinUiResourceIndex {
    param(
        [Parameter(Mandatory = $true)]
        [string]$OutputDirectory,

        [Parameter(Mandatory = $true)]
        [string]$BuildRoot,

        [Parameter(Mandatory = $true)]
        [string]$RuntimeIdentifier
    )

    $publishedResourceIndex = Join-Path `
        $OutputDirectory `
        'resources.pri'

    if (Test-Path -LiteralPath $publishedResourceIndex) {
        return
    }

    Write-Host "resources.pri was not found directly in publish output."
    Write-Host "Searching isolated build root..."

    $resourceIndex = Get-ChildItem `
        -LiteralPath $BuildRoot `
        -Filter '*.pri' `
        -File `
        -Recurse `
        -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -in @(
                'resources.pri',
                'RSSAM.pri'
            )
        } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $resourceIndex) {
        Write-Warning (
            "No WinUI PRI resource index could be recovered for " +
            "$RuntimeIdentifier."
        )

        return
    }

    Copy-Item `
        -LiteralPath $resourceIndex.FullName `
        -Destination $publishedResourceIndex `
        -Force

    Write-Host (
        "Recovered resources.pri for $RuntimeIdentifier from: " +
        $resourceIndex.FullName
    )
}

function Assert-PublishPayload {
    param(
        [Parameter(Mandatory = $true)]
        [string]$OutputDirectory,

        [Parameter(Mandatory = $true)]
        [string]$RuntimeIdentifier
    )

    if (-not (Test-Path -LiteralPath $OutputDirectory)) {
        throw "Publish output directory does not exist: $OutputDirectory"
    }

    $requiredFiles = @(
        'RSSAM.exe',
        'RSSAM.dll',
        'RSSAM.Core.dll',
        'RSSAM.API.dll',
        'RSSAM.CardIdler.dll',
        'SteamKit2.dll',
        'QRCoder.dll',
        'WinUI.TableView.dll',
        'Microsoft.UI.Xaml.dll',
        'Microsoft.WindowsAppRuntime.dll',
        'resources.pri',
        'coreclr.dll',
        'hostfxr.dll',
        'hostpolicy.dll'
    )

    $missingFiles = @(
        foreach ($requiredFile in $requiredFiles) {
            $path = Join-Path `
                $OutputDirectory `
                $requiredFile

            if (-not (Test-Path -LiteralPath $path)) {
                $requiredFile
            }
        }
    )

    if ($missingFiles.Count -gt 0) {
        throw (
            "Incomplete $RuntimeIdentifier publish output. " +
            "Missing required files: " +
            ($missingFiles -join ', ')
        )
    }

    $rssamExe = Join-Path `
        $OutputDirectory `
        'RSSAM.exe'

    $rssamDll = Join-Path `
        $OutputDirectory `
        'RSSAM.dll'

    if ((Get-Item -LiteralPath $rssamExe).Length -le 0) {
        throw "RSSAM.exe is empty: $rssamExe"
    }

    if ((Get-Item -LiteralPath $rssamDll).Length -le 0) {
        throw "RSSAM.dll is empty: $rssamDll"
    }
}

function Copy-PublishPayload {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceDirectory,

        [Parameter(Mandatory = $true)]
        [string]$DestinationDirectory
    )

    if (-not (Test-Path -LiteralPath $SourceDirectory)) {
        throw "Publish staging directory does not exist: $SourceDirectory"
    }

    if (Test-Path -LiteralPath $DestinationDirectory) {
        Remove-Item `
            -LiteralPath $DestinationDirectory `
            -Recurse `
            -Force
    }

    New-Item `
        -ItemType Directory `
        -Path $DestinationDirectory `
        -Force |
        Out-Null

    Copy-Item `
        -Path (Join-Path $SourceDirectory '*') `
        -Destination $DestinationDirectory `
        -Recurse `
        -Force
}

function Invoke-IsolatedPublish {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TargetArchitecture,

        [Parameter(Mandatory = $true)]
        [string]$RuntimeIdentifier,

        [Parameter(Mandatory = $true)]
        [string]$DestinationDirectory
    )

    $attemptCount = 2

    for ($attempt = 1; $attempt -le $attemptCount; $attempt++) {

        $temporaryRoot = New-SafeTemporaryDirectory `
            -Name "publish-$RuntimeIdentifier"

        $buildRoot = [System.IO.Path]::GetFullPath(
            (Join-Path $temporaryRoot 'build')
        )

        $stagingDirectory = [System.IO.Path]::GetFullPath(
            (Join-Path $temporaryRoot 'publish')
        )

        New-Item `
            -ItemType Directory `
            -Path $buildRoot `
            -Force |
            Out-Null

        New-Item `
            -ItemType Directory `
            -Path $stagingDirectory `
            -Force |
            Out-Null

        try {
            Write-Host ''
            Write-Host '------------------------------------------------------------'
            Write-Host "Publishing RSSAM $RuntimeIdentifier"
            Write-Host "Attempt       : $attempt/$attemptCount"
            Write-Host "Configuration : $Configuration"
            Write-Host "Architecture  : $TargetArchitecture"
            Write-Host "Build root    : $buildRoot"
            Write-Host "Staging       : $stagingDirectory"
            Write-Host "Output        : $DestinationDirectory"
            Write-Host '------------------------------------------------------------'
            Write-Host ''

            & dotnet publish `
                $appProject `
                -c $Configuration `
                -p:Platform=$TargetArchitecture `
                -r $RuntimeIdentifier `
                --self-contained true `
                -p:SelfContained=true `
                -p:WindowsPackageType=None `
                -p:AppxGeneratePriEnabled=true `
                -p:ProjectPriFileName=resources.pri `
                -p:PublishSingleFile=false `
                -p:PublishTrimmed=false `
                -p:RSSAMBuildRoot="$buildRoot" `
                -p:UseSharedCompilation=false `
                -m:1 `
                -nr:false `
                -o $stagingDirectory

            $publishExitCode = $LASTEXITCODE

            if ($publishExitCode -ne 0) {

                if ($attempt -lt $attemptCount) {
                    Write-Warning (
                        "dotnet publish failed for $RuntimeIdentifier " +
                        "with exit code $publishExitCode. " +
                        "Retrying once with a fresh isolated build root."
                    )

                    continue
                }

                throw (
                    "dotnet publish failed for $RuntimeIdentifier " +
                    "with exit code $publishExitCode."
                )
            }

            Ensure-WinUiResourceIndex `
                -OutputDirectory $stagingDirectory `
                -BuildRoot $buildRoot `
                -RuntimeIdentifier $RuntimeIdentifier

            Assert-PublishPayload `
                -OutputDirectory $stagingDirectory `
                -RuntimeIdentifier $RuntimeIdentifier

            Copy-PublishPayload `
                -SourceDirectory $stagingDirectory `
                -DestinationDirectory $DestinationDirectory

            Assert-PublishPayload `
                -OutputDirectory $DestinationDirectory `
                -RuntimeIdentifier $RuntimeIdentifier

            $fileCount = @(
                Get-ChildItem `
                    -LiteralPath $DestinationDirectory `
                    -File `
                    -Recurse
            ).Count

            $totalSize = (
                Get-ChildItem `
                    -LiteralPath $DestinationDirectory `
                    -File `
                    -Recurse |
                Measure-Object `
                    -Property Length `
                    -Sum
            ).Sum

            if ($null -eq $totalSize) {
                $totalSize = 0
            }

            $sizeMb = [Math]::Round(
                $totalSize / 1MB,
                2
            )

            Write-Host ''
            Write-Host "Published and validated $RuntimeIdentifier."
            Write-Host "Files : $fileCount"
            Write-Host "Size  : $sizeMb MB"
            Write-Host "Path  : $DestinationDirectory"
            Write-Host ''

            return
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
    }
}

# -----------------------------------------------------------------------------
# Validation
# -----------------------------------------------------------------------------

if (-not (Test-Path -LiteralPath $appProject)) {
    throw "RSSAM.App project not found: $appProject"
}

New-Item `
    -ItemType Directory `
    -Path $artifactsRoot `
    -Force |
    Out-Null

# -----------------------------------------------------------------------------
# Repository cleanup
# -----------------------------------------------------------------------------

# Older versions deleted src\*\bin and src\*\obj before every publish.
#
# This is no longer necessary because all publish-specific intermediate and
# output directories are redirected through RSSAMBuildRoot.
#
# -SkipClean remains available for compatibility with existing build scripts.
if (-not $SkipClean) {
    Write-Host (
        'Using isolated publish state; repository bin/obj cleanup is not required.'
    )
}

# -----------------------------------------------------------------------------
# Publish architectures
# -----------------------------------------------------------------------------

foreach ($targetArchitecture in $targetArchitectures) {

    $runtimeIdentifier = "win-$targetArchitecture"

    $outputDirectory = Join-Path `
        $artifactsRoot `
        $runtimeIdentifier

    Invoke-IsolatedPublish `
        -TargetArchitecture $targetArchitecture `
        -RuntimeIdentifier $runtimeIdentifier `
        -DestinationDirectory $outputDirectory
}

Write-Host ''
Write-Host '============================================================'
Write-Host 'RSSAM publish completed successfully.'
Write-Host '============================================================'

foreach ($targetArchitecture in $targetArchitectures) {

    $runtimeIdentifier = "win-$targetArchitecture"

    $outputDirectory = Join-Path `
        $artifactsRoot `
        $runtimeIdentifier

    Write-Host "  $runtimeIdentifier -> $outputDirectory"
}

Write-Host ''