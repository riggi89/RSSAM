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

function Ensure-WinUiResourceIndex {
    param(
        [Parameter(Mandatory = $true)][string]$OutputDirectory,
        [Parameter(Mandatory = $true)][string]$TargetArchitecture,
        [Parameter(Mandatory = $true)][string]$RuntimeIdentifier,
        [Parameter(Mandatory = $true)][string]$Configuration
    )

    $publishedResourceIndex = Join-Path $OutputDirectory 'resources.pri'
    if (Test-Path -LiteralPath $publishedResourceIndex) {
        return
    }

    # Some Windows App SDK/MSBuild combinations leave the project PRI in the
    # architecture-specific build folder or name it after the assembly instead
    # of copying it to publish as resources.pri.
    $appProjectDirectory = Join-Path $root 'src\RSSAM.App'
    $architecturePattern = "[\\/]($([regex]::Escape($TargetArchitecture))|$([regex]::Escape($RuntimeIdentifier)))[\\/]"
    $resourceIndex = Get-ChildItem `
        -LiteralPath $appProjectDirectory `
        -Filter '*.pri' `
        -File `
        -Recurse `
        -ErrorAction SilentlyContinue |
    Where-Object {
        $_.FullName -notlike "$OutputDirectory*" -and
        $_.Name -in @('resources.pri', 'RSSAM.pri') -and
        $_.FullName -match $architecturePattern -and
        $_.FullName -match "[\\/]$([regex]::Escape($Configuration))[\\/]"
    } |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

    if ($null -ne $resourceIndex) {
        Copy-Item `
            -LiteralPath $resourceIndex.FullName `
            -Destination $publishedResourceIndex `
            -Force

        Write-Host "Recovered resources.pri from $($resourceIndex.FullName)"
    }
}

function Assert-PublishPayload {
    param(
        [Parameter(Mandatory = $true)][string]$OutputDirectory,
        [Parameter(Mandatory = $true)][string]$RuntimeIdentifier
    )

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

    $missingFiles = @(
        $requiredFiles |
        Where-Object {
            -not (Test-Path -LiteralPath (Join-Path $OutputDirectory $_))
        }
    )

    if ($missingFiles.Count -gt 0) {
        throw (
            "Incomplete $RuntimeIdentifier publish output. Missing required files: " +
            ($missingFiles -join ', ')
        )
    }
}

foreach ($targetArchitecture in $targetArchitectures) {
    $runtimeIdentifier = "win-$targetArchitecture"
    $outputDirectory = Join-Path $root "artifacts\publish\$runtimeIdentifier"

    if (Test-Path -LiteralPath $outputDirectory) {
        Remove-Item -LiteralPath $outputDirectory -Recurse -Force
    }

    dotnet publish `
        "$root\src\RSSAM.App\RSSAM.App.csproj" `
        -c $Configuration `
        -p:Platform=$targetArchitecture `
        -r $runtimeIdentifier `
        --self-contained true `
        -p:SelfContained=true `
        -p:WindowsPackageType=None `
        -p:WindowsAppSDKSelfContained=true `
        -p:AppxGeneratePriEnabled=true `
        -p:ProjectPriFileName=resources.pri `
        -p:PublishSingleFile=false `
        -p:PublishTrimmed=false `
        -o $outputDirectory

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $runtimeIdentifier with exit code $LASTEXITCODE."
    }

    Ensure-WinUiResourceIndex `
        -OutputDirectory $outputDirectory `
        -TargetArchitecture $targetArchitecture `
        -RuntimeIdentifier $runtimeIdentifier `
        -Configuration $Configuration

    Assert-PublishPayload `
        -OutputDirectory $outputDirectory `
        -RuntimeIdentifier $runtimeIdentifier

    Write-Host "Published and validated $runtimeIdentifier to $outputDirectory"
}
