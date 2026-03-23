#Requires -Version 5.1
<#
.SYNOPSIS
  Reads About/About.xml <supportedVersions>, builds the solution once per version, then runs filterexport (same as Publish.ps1).

.PARAMETER Configuration
  MSBuild configuration (Debug or Release). Release is typical before publish.

.PARAMETER SkipPublish
  Only build all supported versions; do not run filterexport.
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [switch] $SkipPublish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = $PSScriptRoot
$modDirName = Split-Path $Root -Leaf
$AboutPath = Join-Path $Root 'About\About.xml'
$Sln = Join-Path $Root "Source\$modDirName.sln"
$PublishDir = "../${modDirName}_Publish" 

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

Write-Host "RimWorld targets from About.xml: $($versions -join ', ')" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration`n" -ForegroundColor Cyan

foreach ($ver in $versions) {
    Write-Host "=== dotnet build (TargetRimWorldVersion=$ver) ===" -ForegroundColor Yellow
    $buildArgs = @(
        'build',
        $Sln,
        '-c', $Configuration,
        "/p:RimWorldVersionOverride=$ver",
        '/p:GenerateFullPaths=true',
        '/consoleloggerparameters:NoSummary;ForceNoAlign'
    )
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed for RimWorld $ver (exit code $LASTEXITCODE)."
    }
}

if ($SkipPublish) {
    Write-Host "`nSkipPublish: done after builds." -ForegroundColor Green
    return
}

Write-Host "`n=== filterexport (publish) ===" -ForegroundColor Yellow
Push-Location -LiteralPath $Root
try {
    # Same relative layout as Publish.ps1 / tasks.json "publish"
    & filterexport -c './PublishFileFilter.xml' -o $PublishDir -y
    if ($LASTEXITCODE -ne 0) {
        throw "filterexport failed (exit code $LASTEXITCODE). Is filterexport on PATH?"
    }
}
finally {
    Pop-Location
}

Write-Host "`nDone. Output: $PublishDir" -ForegroundColor Green
