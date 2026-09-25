<#
.SYNOPSIS
    Publishes DBA Dash Visualizer and zips it for a release.
.DESCRIPTION
    Publishes DBADashVisualizer (framework dependent, win-x64), removes the debug symbols, checks that nothing that
    belongs to the rest of DBA Dash has found its way in, and zips the result as DBADash_Visualizer_<version>.zip.

    The checks are the point of the small footprint: the Visualizer is meant for someone with a plan and no DBA Dash, so
    it must not pull in the collection service, SMO, the AWS SDK and so on.  A reference that brings one back fails here,
    rather than turning up as a much bigger download.

    Used by the release workflow and by CI, and can be run locally to see what would ship.
.PARAMETER Configuration
    Build configuration.  Defaults to Release.
.PARAMETER OutputFolder
    Where to publish to.  Defaults to DBADashBuild\Visualizer under the repository.  Emptied first.
.PARAMETER ZipFolder
    Where to put the zip.  Defaults to the repository root.
.PARAMETER Unsigned
    Name the zip DBADash_Visualizer_<version>-unsigned.zip.  The signing script (SignRelease.ps1) picks up assets named
    -unsigned.zip, signs the executables in them, and uploads the result without the suffix.
.PARAMETER NoZip
    Publish and check, but don't create the zip.
.EXAMPLE
    ./Scripts/Publish-Visualizer.ps1 -Unsigned
#>
[CmdletBinding()]
param (
    [string]$Configuration = "Release",
    [string]$OutputFolder,
    [string]$ZipFolder,
    [switch]$Unsigned,
    [switch]$NoZip
)

$ErrorActionPreference = "Stop"

$repo = Split-Path -Parent $PSScriptRoot
if (-not $OutputFolder) { $OutputFolder = Join-Path $repo "DBADashBuild\Visualizer" }
if (-not $ZipFolder) { $ZipFolder = $repo }

$project = Join-Path $repo "DBADashVisualizer\DBADashVisualizer.csproj"

# Files that mean the rest of DBA Dash - the collection service, the repository code, cloud SDKs - has come along.
# Patterns, matched against the file name.
$forbidden = @(
    "DBADashTools.dll",
    "DBADashService*",
    "DBADashConfig*",
    "Microsoft.SqlServer.Smo*",
    "Microsoft.SqlServer.Management.*",
    "Microsoft.SqlServer.Dac*",
    "Microsoft.SqlServer.TransactSql.ScriptDom*",
    "AWSSDK.*",
    "Quartz*",
    "MailKit*",
    "MimeKit*",
    "Octokit*",
    "System.Management.Automation*"
)

# Files that have to be there.
$required = @(
    "DBADashVisualizer.exe",
    "DBADashVisualizer.dll",
    "DBADashVisualizer.runtimeconfig.json",
    "DBADash.Viewers.GUI.dll",
    "LICENSE"
)

if (Test-Path $OutputFolder) { Remove-Item $OutputFolder -Recurse -Force }

Write-Host "Publishing $project to $OutputFolder" -ForegroundColor Cyan
dotnet publish $project -c $Configuration -o $OutputFolder --no-self-contained
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

# Symbols are for debugging, and the native ones are large - Skia's alone is around 80 MB.
Get-ChildItem $OutputFolder -Recurse -Filter *.pdb | Remove-Item -Force
# The WebView2 assemblies come with API documentation alongside.
Get-ChildItem $OutputFolder -Recurse -Filter *.xml | Where-Object { $_.Name -like "Microsoft.Web.WebView2*" } | Remove-Item -Force

$problems = @()
foreach ($name in $required) {
    if (-not (Test-Path (Join-Path $OutputFolder $name))) { $problems += "Missing: $name" }
}
foreach ($pattern in $forbidden) {
    Get-ChildItem $OutputFolder -Recurse -File -Filter $pattern | ForEach-Object {
        $problems += "Not expected in the Visualizer: $($_.FullName.Substring($OutputFolder.Length + 1))"
    }
}
if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    throw "The published Visualizer is not what was expected."
}

$exe = Join-Path $OutputFolder "DBADashVisualizer.exe"
$info = (Get-Item $exe).VersionInfo
$version = "{0}.{1}.{2}" -f $info.FileMajorPart, $info.FileMinorPart, $info.FileBuildPart

$size = (Get-ChildItem $OutputFolder -Recurse -File | Measure-Object Length -Sum).Sum / 1MB
Write-Host ("Published version {0}: {1} files, {2:N1} MB" -f $version, (Get-ChildItem $OutputFolder -Recurse -File).Count, $size) -ForegroundColor Green

if ($NoZip) {
    [pscustomobject]@{ Version = $version; Folder = $OutputFolder; Zip = $null }
    return
}

$suffix = if ($Unsigned) { "-unsigned" } else { "" }
$zipPath = Join-Path $ZipFolder "DBADash_Visualizer_$version$suffix.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

# The executable has to be at the root of the zip: the signing script signs the .exe files it finds there.
Compress-Archive -Path (Join-Path $OutputFolder "*") -DestinationPath $zipPath
Write-Host ("Created {0} ({1:N1} MB)" -f $zipPath, ((Get-Item $zipPath).Length / 1MB)) -ForegroundColor Green

[pscustomobject]@{ Version = $version; Folder = $OutputFolder; Zip = $zipPath }
