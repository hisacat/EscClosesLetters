#Requires -Version 5.1
<#
.SYNOPSIS
  Reads About/About.xml <supportedVersions> and runs dotnet clean for each RimWorld version,
  for both Debug and Release (so OutputPath and obj records match each build).

.NOTES
  Mirrors BuildAllAndPublish.ps1 path rules (folder name = solution name under Source/).
#>
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = $PSScriptRoot
$modDirName = Split-Path $Root -Leaf
$AboutPath = Join-Path $Root 'About\About.xml'
$Sln = Join-Path $Root "Source\$modDirName.sln"

if (-not (Test-Path -LiteralPath $AboutPath)) {
    throw "About.xml not found: $AboutPath"
}
if (-not (Test-Path -LiteralPath $Sln)) {
    throw "Solution not found: $Sln"
}

[xml] $aboutDoc = Get-Content -LiteralPath $AboutPath -Raw -Encoding UTF8
$supported = $aboutDoc.ModMetaData.supportedVersions
if ($null -eq $supported) {
    throw 'About.xml: <supportedVersions> not found.'
}

$versions = [System.Collections.Generic.List[string]]::new()
foreach ($li in @($supported.li)) {
    $text = if ($li -is [string]) { $li } else { $li.InnerText }
    $v = $text.Trim()
    if ($v) {
        $versions.Add($v)
    }
}

if ($versions.Count -eq 0) {
    throw 'About.xml: <supportedVersions> has no <li> entries.'
}

$configs = @('Debug', 'Release')

Write-Host "Clean: supported versions = $($versions -join ', ')" -ForegroundColor Cyan
Write-Host "Configurations = $($configs -join ', ')`n" -ForegroundColor Cyan

foreach ($ver in $versions) {
    foreach ($cfg in $configs) {
        Write-Host "=== dotnet clean -c $cfg (RimWorld $ver) ===" -ForegroundColor Yellow
        $cleanArgs = @(
            'clean',
            $Sln,
            '-c', $cfg,
            "/p:RimWorldVersionOverride=$ver"
        )
        & dotnet @cleanArgs
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet clean failed for RimWorld $ver / $cfg (exit code $LASTEXITCODE)."
        }
    }
}

Write-Host "`nDone." -ForegroundColor Green
