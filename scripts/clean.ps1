# RSSAM clean script.
# Copyright (c) 2026 Daniel Riggi (riggi89).
# Distributed under the project license; see LICENSE.md and NOTICE.md.

#requires -Version 5.1

[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param()

$ErrorActionPreference = 'Stop'

$projectRoot = [System.IO.Path]::GetFullPath(
    (Split-Path -Parent $PSScriptRoot))
$solutionPath = Join-Path $projectRoot 'RSSAM.sln'

if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
    throw "RSSAM.sln was not found in the expected project root: $projectRoot"
}

$searchRoots = @(
    (Join-Path $projectRoot 'src'),
    (Join-Path $projectRoot 'tests')
)

$directories = @(
    @(
        foreach ($searchRoot in $searchRoots) {
            if (-not (Test-Path -LiteralPath $searchRoot -PathType Container)) {
                Write-Warning "Directory not found and skipped: $searchRoot"
                continue
            }

            Write-Host "Searching below: $searchRoot"

            Get-ChildItem -Path $searchRoot -Directory -Recurse -Force -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -eq 'bin' -or $_.Name -eq 'obj' }
        }
    ) | Sort-Object -Property FullName -Descending
)

if ($directories.Count -eq 0) {
    Write-Host 'No bin or obj directories were found.'
    return
}

$removedCount = 0

foreach ($directory in $directories) {
    if ($PSCmdlet.ShouldProcess($directory.FullName, 'Remove directory recursively')) {
        Remove-Item -LiteralPath $directory.FullName -Recurse -Force
        $removedCount++
        Write-Host "Removed: $($directory.FullName)"
    }
}

if ($WhatIfPreference) {
    Write-Host "WhatIf completed. $($directories.Count) directories would be removed."
}
else {
    Write-Host "Cleanup completed. $removedCount directories removed."
}
