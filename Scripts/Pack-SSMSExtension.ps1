<#
.SYNOPSIS
    Builds and packages the DBA Dash SSMS extension for release.
.DESCRIPTION
    Builds DBADash.SSMSExtension and copies the resulting .vsix to DBADash_SSMSExtension-<version>.vsix,
    named in the style of the other release assets.

    Published separately from the rest of DBA Dash, to the Open SSMS VSIX Gallery on its own cadence
    (.github/workflows/publish-ssms-extension.yml) - not as a GitHub release, which the upgrade checks
    would take for the latest DBA Dash release.  The extension is a VSIX for SQL Server Management
    Studio, built as its own net472 project (see DBADash.SSMSExtension.csproj for why), and is expected
    to change far less often than the app itself. Version comes from
    source.extension.vsixmanifest - the number SSMS and VSIXInstaller actually look at - rather than the
    built assembly's own file version, so there's one place that sets it.
.PARAMETER Configuration
    Build configuration.  Defaults to Release.
.PARAMETER OutputFolder
    Where to copy the versioned .vsix to.  Defaults to the repository root.
.EXAMPLE
    ./Scripts/Pack-SSMSExtension.ps1
#>
[CmdletBinding()]
param (
    [string]$Configuration = "Release",
    [string]$OutputFolder
)

$ErrorActionPreference = "Stop"

$repo = Split-Path -Parent $PSScriptRoot
if (-not $OutputFolder) { $OutputFolder = $repo }

$projectFolder = Join-Path $repo "DBADash.SSMSExtension"
$project = Join-Path $projectFolder "DBADash.SSMSExtension.csproj"

[xml]$manifest = Get-Content (Join-Path $projectFolder "source.extension.vsixmanifest")
$version = $manifest.PackageManifest.Metadata.Identity.Version
if (-not $version) { throw "Could not read the version from source.extension.vsixmanifest." }

Write-Host "Building $project ($Configuration) - version $version" -ForegroundColor Cyan
dotnet build $project -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }

$builtVsix = Join-Path $projectFolder "bin\$Configuration\net472\DBADash.SSMSExtension.vsix"
if (-not (Test-Path $builtVsix)) { throw "Expected to find $builtVsix after building." }

$destPath = Join-Path $OutputFolder "DBADash_SSMSExtension-$version.vsix"
Copy-Item $builtVsix $destPath -Force

Write-Host ("Packaged {0} ({1:N1} KB)" -f $destPath, ((Get-Item $destPath).Length / 1KB)) -ForegroundColor Green

[pscustomobject]@{ Version = $version; Vsix = $destPath }
