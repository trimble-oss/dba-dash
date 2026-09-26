<#
.SYNOPSIS
    Creates the winget manifest for a DBA Dash Visualizer release.
.DESCRIPTION
    Writes the three manifest files winget wants - version, installer and default locale - for a published GitHub release,
    and validates them with 'winget validate' when winget is installed.

    The package is the setup program, DBADash_Visualizer_Setup_<version>.exe.  Winget runs it silently: it installs for the
    user only, needs no elevation, adds a Start menu entry and an Installed apps entry, and the app then keeps itself up to
    date, as well as winget being able to.

    Run it after the release has been signed and published: the manifest carries the SHA-256 of the setup program that people
    download, so it has to be the signed one, and the URL has to work.  It works out the hash by downloading the file from
    the release, or use -PackagePath to hash a copy you already have.

    It doesn't submit anything.  To publish the package, open a pull request to https://github.com/microsoft/winget-pkgs
    with the files it writes - or run 'wingetcreate submit' on the folder - see Docs/Visualizer.md.
.PARAMETER Version
    The release, e.g. 4.19.0.  The release's tag is expected to be the same.
.PARAMETER PackageIdentifier
    winget's identifier for the package: Publisher.Package.
.PARAMETER Repo
    The GitHub owner and repository the release is in.
.PARAMETER PackagePath
    A local copy of the release's setup program to take the hash from, rather than downloading it.
.PARAMETER OutputFolder
    Where to write the manifest.  Defaults to DBADashBuild\winget under the repository.  The files go in the layout
    winget-pkgs uses: manifests\<first letter>\<publisher>\<package>\<version>.
.PARAMETER ReleaseDate
    The date the release was published, yyyy-MM-dd.  Left out of the manifest if not given.
.EXAMPLE
    ./Scripts/New-WingetManifest.ps1 -Version 4.19.0
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,
    [string]$PackageIdentifier = "Trimble.DBADashVisualizer",
    [string]$Repo = "trimble-oss/dba-dash",
    [string]$PackagePath,
    [string]$OutputFolder,
    [ValidatePattern('^\d{4}-\d{2}-\d{2}$')]
    [string]$ReleaseDate
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputFolder) { $OutputFolder = Join-Path $repoRoot "DBADashBuild\winget" }

$fileName = "DBADash_Visualizer_Setup_$Version.exe"
$installerUrl = "https://github.com/$Repo/releases/download/$Version/$fileName"
$releaseUrl = "https://github.com/$Repo/releases/tag/$Version"

$temp = $null
if (-not $PackagePath) {
    $temp = Join-Path ([IO.Path]::GetTempPath()) ("winget-" + [guid]::NewGuid().ToString("N"))
    New-Item $temp -ItemType Directory | Out-Null
    $PackagePath = Join-Path $temp $fileName
    Write-Host "Downloading $installerUrl" -ForegroundColor Cyan
    Invoke-WebRequest -Uri $installerUrl -OutFile $PackagePath
}

try {
    $sha256 = (Get-FileHash -Path $PackagePath -Algorithm SHA256).Hash.ToUpperInvariant()

    # The setup is signed, and winget's checks include the signature: a setup that isn't is a mistake worth catching here.
    $signature = Get-AuthenticodeSignature -FilePath $PackagePath
    if ($signature.Status -ne "Valid") {
        Write-Warning "The setup program isn't validly signed ($($signature.Status)).  Is this the signed one?"
    }
}
finally {
    if ($temp) { Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue }
}

$parts = $PackageIdentifier.Split('.')
$folder = Join-Path $OutputFolder ("manifests\{0}\{1}\{2}" -f $PackageIdentifier.Substring(0, 1).ToLowerInvariant(), ($parts -join '\'), $Version)
New-Item $folder -ItemType Directory -Force | Out-Null

$manifestVersion = "1.6.0"
$releaseDateLine = if ($ReleaseDate) { "ReleaseDate: $ReleaseDate" } else { $null }

$versionManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.$manifestVersion.schema.json
PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
DefaultLocale: en-US
ManifestType: version
ManifestVersion: $manifestVersion
"@

$installerLines = @(
    "# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.$manifestVersion.schema.json",
    "PackageIdentifier: $PackageIdentifier",
    "PackageVersion: $Version"
)

# A Velopack setup.  It isn't one of the installer frameworks winget knows the switches of, so they are given:
# --silent hides everything, and doesn't start the app afterwards.  It installs for the user, under
# %LOCALAPPDATA%, and registers itself in Installed apps under the id it was packed with - which is how winget finds it
# again to upgrade or uninstall.  The .NET Desktop Runtime is a dependency so winget installs it, rather than the setup
# having to.
$installerLines += @(
    "InstallerType: exe",
    "Scope: user",
    "InstallModes:",
    "- silent",
    "- silentWithProgress",
    "InstallerSwitches:",
    "  Silent: --silent",
    "  SilentWithProgress: --silent",
    "UpgradeBehavior: install",
    "MinimumOSVersion: 10.0.19041.0",
    "Installers:",
    "- Architecture: x64",
    "  InstallerUrl: $installerUrl",
    "  InstallerSha256: $sha256",
    "  ProductCode: DBADash_Visualizer",
    "  AppsAndFeaturesEntries:",
    "  - DisplayName: DBA Dash Visualizer",
    "    Publisher: Trimble, Inc.",
    "    ProductCode: DBADash_Visualizer",
    "Dependencies:",
    "  PackageDependencies:",
    "  - PackageIdentifier: Microsoft.DotNet.DesktopRuntime.10"
)

if ($releaseDateLine) { $installerLines += $releaseDateLine }
$installerLines += @("ManifestType: installer", "ManifestVersion: $manifestVersion")
$installerManifest = $installerLines -join "`n"

$localeManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.$manifestVersion.schema.json
PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
PackageLocale: en-US
Publisher: Trimble, Inc.
PublisherUrl: https://github.com/trimble-oss
PublisherSupportUrl: https://github.com/$Repo/issues
PackageName: DBA Dash Visualizer
PackageUrl: https://dbadash.com
License: MIT
LicenseUrl: https://github.com/$Repo/blob/main/LICENSE
Copyright: Copyright 2022 Trimble, Inc.
ShortDescription: View SQL Server execution plans and deadlock graphs.
Description: |-
  DBA Dash Visualizer opens SQL Server execution plans (.sqlplan) and deadlock graphs (.xdl) in a graphical viewer, with
  operator details, warnings, missing indexes, waits and insights for plans, and findings for deadlocks.  It needs no
  DBA Dash installation, repository or SQL Server connection.
Moniker: dbadashvisualizer
Tags:
- deadlock
- execution-plan
- query-plan
- sql-server
- sqlplan
- xdl
ReleaseNotesUrl: $releaseUrl
ManifestType: defaultLocale
ManifestVersion: $manifestVersion
"@

$files = @{
    "$PackageIdentifier.yaml"               = $versionManifest
    "$PackageIdentifier.installer.yaml"     = $installerManifest
    "$PackageIdentifier.locale.en-US.yaml"  = $localeManifest
}
foreach ($name in $files.Keys) {
    # Unix line endings and no byte order mark, as winget-pkgs has them.
    [IO.File]::WriteAllText((Join-Path $folder $name), (($files[$name] -replace "`r`n", "`n").TrimEnd() + "`n"), (New-Object Text.UTF8Encoding($false)))
}

Write-Host "Wrote the manifest for $PackageIdentifier $Version to $folder" -ForegroundColor Green
Write-Host "  $fileName  SHA-256 $sha256"

if (Get-Command winget -ErrorAction SilentlyContinue) {
    Write-Host "Validating with winget..." -ForegroundColor Cyan
    winget validate --manifest $folder
    if ($LASTEXITCODE -ne 0) { throw "winget validate failed with exit code $LASTEXITCODE" }
}
else {
    Write-Host "winget is not installed here, so the manifest has not been validated." -ForegroundColor Yellow
}

$folder
