#Requires -Version 5.1
<#
.SYNOPSIS
  Runs filterexport from the mod root (same behavior as the former Publish.bat).
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = $PSScriptRoot
$modDirName = Split-Path $Root -Leaf
$publishDir = "../${modDirName}_Publish"

Push-Location -LiteralPath $Root
try {
    & filterexport -c './PublishFileFilter.xml' -o $publishDir -y
    if ($LASTEXITCODE -ne 0) {
        throw "filterexport failed (exit code $LASTEXITCODE). Is filterexport on PATH?"
    }
}
finally {
    Pop-Location
}

Write-Host "Done. Output: $publishDir" -ForegroundColor Green
