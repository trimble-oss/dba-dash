<#
.SYNOPSIS
    Packs DBA Dash Visualizer as a Velopack installer and update feed, signs it, and adds it to the draft release.
.DESCRIPTION
    Turns the Visualizer zip of a draft release into what the installer channel needs:

      DBA_Dash_Visualizer_Setup_<version>.exe   the setup program - one click, per user, no elevation
      DBADashVisualizer-<version>-full.nupkg    the package the updater installs
      DBADashVisualizer-<version>-delta.nupkg   what changed since the last release, if there was one
      releases.win.json, RELEASES, assets.win.json   the feed the installed app reads to find updates

    Velopack has to do the signing itself, part way through building the package, so this is not a step after the zips are
    signed.  It calls the Trimble signing tool (SignFiles.exe) once for each file, about half a minute each, so it signs only
    what identifies the app: the Visualizer executable, Velopack's launcher stub and updater, and the setup program.  Not the
    dozens of libraries.  Like SignRelease.ps1 it works only on the corporate network.

    Run it after SignRelease.ps1 and before publishing the release.  It uses the signed Visualizer zip if there is one - and
    then doesn't sign the executable again - and otherwise the unsigned zip.

    Nothing is uploaded unless -Upload is given.
.PARAMETER Version
    The release to pack, e.g. 4.19.0.  Defaults to the tag of the latest draft release.
.PARAMETER Repo
    The GitHub owner and repository.
.PARAMETER ZipPath
    A local DBADash_Visualizer_<version>[-unsigned].zip to pack, instead of downloading one from the draft release.
    -Version is then taken from the file name.
.PARAMETER OutputFolder
    Where the installer and feed are written.  Defaults to DBADashBuild\VisualizerInstaller under the repository.  Emptied first.
.PARAMETER SignTool
    The signing tool.  Defaults to C:\Sign\SignFiles.exe.
.PARAMETER SkipSigning
    Don't sign.  For trying the script out; the result must not be released.
.PARAMETER NoDelta
    Don't look for the previous release to build a delta from.  The first release has none, and it isn't needed: an update
    then downloads the whole package.
.PARAMETER Upload
    Add the files to the draft release with the GitHub CLI (gh).  Supports -WhatIf.
.EXAMPLE
    ./Scripts/Pack-VisualizerInstaller.ps1 -Upload
#>
[CmdletBinding(SupportsShouldProcess)]
param (
    [string]$Version,
    [string]$Repo = "trimble-oss/dba-dash",
    [string]$ZipPath,
    [string]$OutputFolder,
    [string]$SignTool = "C:\Sign\SignFiles.exe",
    [switch]$SkipSigning,
    [switch]$NoDelta,
    [switch]$Upload
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputFolder) { $OutputFolder = Join-Path $repoRoot "DBADashBuild\VisualizerInstaller" }
$icon = Join-Path $repoRoot "DBADash.Viewers.GUI\Resources\PlanViewer.ico"

function Remove-Folder([string]$path) {
    if (Test-Path $path) { [IO.Directory]::Delete($path, $true) }
}

function Invoke-Native([string]$exe, [string[]]$arguments) {
    & $exe @arguments
    if ($LASTEXITCODE -ne 0) { throw "$exe failed with exit code $LASTEXITCODE" }
}

$work = Join-Path ([IO.Path]::GetTempPath()) ("VisualizerInstaller-" + [guid]::NewGuid().ToString("N"))
New-Item $work -ItemType Directory | Out-Null

try {
    if (-not $SkipSigning -and -not (Test-Path $SignTool)) {
        throw "Sign tool not found at $SignTool.  Use -SignTool, or -SkipSigning to try the script out."
    }

    # ---- The zip to pack --------------------------------------------------------------------------------------------------
    $tag = $Version
    if ($ZipPath) {
        if (-not (Test-Path $ZipPath)) { throw "$ZipPath not found." }
        if ((Split-Path $ZipPath -Leaf) -notmatch '^DBADash_Visualizer_(\d+\.\d+\.\d+)(-unsigned)?\.zip$') {
            throw "Expected a file named DBADash_Visualizer_<version>[-unsigned].zip."
        }
        $tag = $Matches[1]
        $zip = $ZipPath
        $alreadySigned = -not $Matches[2]
    }
    else {
        if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw "The GitHub CLI (gh) is needed to get the zip from the draft release." }

        if (-not $tag) {
            $draft = gh api "repos/$Repo/releases" | ConvertFrom-Json | Where-Object { $_.draft -eq $true } | Select-Object -First 1
            if (-not $draft) { throw "No draft release found in $Repo." }
            $tag = $draft.tag_name
        }
        Write-Host "Release $tag" -ForegroundColor Green

        # The signed zip if SignRelease.ps1 has been run, otherwise the unsigned one.
        $assets = gh api "repos/$Repo/releases" | ConvertFrom-Json | Where-Object { $_.tag_name -eq $tag } | ForEach-Object { $_.assets }
        $name = "DBADash_Visualizer_$tag.zip"
        $alreadySigned = $true
        if (-not ($assets | Where-Object { $_.name -eq $name })) {
            $name = "DBADash_Visualizer_$tag-unsigned.zip"
            $alreadySigned = $false
            if (-not ($assets | Where-Object { $_.name -eq $name })) { throw "Neither DBADash_Visualizer_$tag.zip nor the -unsigned zip is in release $tag." }
        }
        Write-Host "Downloading $name" -ForegroundColor Cyan
        Invoke-Native gh @("release", "download", $tag, "--pattern", $name, "--repo", $Repo, "--dir", $work, "--clobber")
        $zip = Join-Path $work $name
    }

    $publish = Join-Path $work "publish"
    Expand-Archive -LiteralPath $zip -DestinationPath $publish -Force
    if (-not (Test-Path (Join-Path $publish "DBADashVisualizer.exe"))) { throw "DBADashVisualizer.exe isn't at the root of $zip." }

    # ---- vpk, the version the app is built against -------------------------------------------------------------------------
    $props = Get-Content (Join-Path $repoRoot "Directory.Packages.props") -Raw
    if ($props -notmatch 'Include="Velopack"\s+Version="([^"]+)"') { throw "Velopack isn't in Directory.Packages.props." }
    $vpkVersion = $Matches[1]
    $tools = Join-Path $work "tools"
    Write-Host "Installing vpk $vpkVersion" -ForegroundColor Cyan
    Invoke-Native dotnet @("tool", "install", "vpk", "--version", $vpkVersion, "--tool-path", $tools)
    $vpk = Join-Path $tools "vpk.exe"

    # ---- The feed, and the previous release for a delta -------------------------------------------------------------------
    Remove-Folder $OutputFolder
    New-Item $OutputFolder -ItemType Directory | Out-Null
    if (-not $NoDelta -and -not $ZipPath) {
        Write-Host "Looking for the previous release, to build a delta from" -ForegroundColor Cyan
        & $vpk download github --repoUrl "https://github.com/$Repo" --outputDir $OutputFolder
        if ($LASTEXITCODE -ne 0) {
            Write-Host "No previous installer release found - the package will be full only." -ForegroundColor Yellow
        }
    }
    $existing = @(Get-ChildItem $OutputFolder -File | ForEach-Object Name)

    # ---- Pack and sign -----------------------------------------------------------------------------------------------------
    $packArgs = @(
        "pack", "--packId", "DBADashVisualizer", "--packVersion", $tag, "--packDir", $publish, "--mainExe", "DBADashVisualizer.exe",
        "--outputDir", $OutputFolder, "--packTitle", "DBA Dash Visualizer", "--packAuthors", "Trimble, Inc.",
        "--icon", $icon, "--framework", "net10.0-x64-desktop", "--runtime", "win-x64",
        # The Start menu, and not the Desktop as well.  The zip has its own portable copy, so no portable package.
        "--shortcuts", "StartMenuRoot", "--noPortable", "--yes"
    )

    if (-not $SkipSigning) {
        # vpk runs this for one file at a time.  A wrapper keeps the tool's arguments - the file, "$" for in place, and the
        # name shown in the signature - out of the way of how vpk splits a command.
        $wrapper = Join-Path $work "sign.cmd"
        Set-Content $wrapper ("@echo off`r`n`"$SignTool`" %1 $ `"DBA Dash Visualizer`"`r`nexit /b %ERRORLEVEL%")

        # Sign the app's own executable (unless the zip already had it signed), Velopack's launcher stub and updater, and the
        # setup.  Everything else is excluded: each file is half a minute, and signing the third party libraries with our
        # certificate is not something we do for the other packages either.
        $exclude = '^(?!.*(Squirrel\.exe|_ExecutionStub\.exe|-Setup\.exe' + $(if ($alreadySigned) { '' } else { '|DBADashVisualizer\.exe' }) + ')$).*$'

        $packArgs += @("--signTemplate", "`"$wrapper`" {{file}}", "--signExclude", $exclude)
        Write-Host "Packing $tag and signing with $SignTool - this takes a few minutes" -ForegroundColor Cyan
    }
    else {
        Write-Host "Packing $tag WITHOUT signing" -ForegroundColor Yellow
    }

    Invoke-Native $vpk $packArgs

    # ---- What came out -----------------------------------------------------------------------------------------------------
    $setup = Join-Path $OutputFolder "DBADashVisualizer-win-Setup.exe"
    if (-not (Test-Path $setup)) { throw "vpk didn't produce a setup program." }
    $setupName = "DBA_Dash_Visualizer_Setup_$tag.exe"
    Move-Item $setup (Join-Path $OutputFolder $setupName) -Force
    $setup = Join-Path $OutputFolder $setupName

    if (-not $SkipSigning) {
        $failed = @()
        if ((Get-AuthenticodeSignature $setup).Status -ne "Valid") { $failed += $setupName }

        # And inside the package the updater installs.
        $full = Join-Path $OutputFolder "DBADashVisualizer-$tag-full.nupkg"
        $check = Join-Path $work "check"
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::ExtractToDirectory($full, $check)
        foreach ($exe in "lib\app\DBADashVisualizer.exe", "lib\app\Squirrel.exe") {
            $path = Join-Path $check $exe
            if (-not (Test-Path $path)) { $failed += "$exe (missing)"; continue }
            if ((Get-AuthenticodeSignature $path).Status -ne "Valid") { $failed += $exe }
        }
        if ($failed.Count -gt 0) { throw "Not signed: $($failed -join ', ')" }
        Write-Host "Signatures check out: $setupName, and the app and updater inside the package." -ForegroundColor Green
    }

    Write-Host ""
    Get-ChildItem $OutputFolder -File | Select-Object Name, @{ n = "MB"; e = { [math]::Round($_.Length / 1MB, 2) } } | Format-Table | Out-String | Write-Host

    # ---- Upload ------------------------------------------------------------------------------------------------------------
    if ($Upload) {
        if ($SkipSigning) { throw "Won't upload an unsigned installer." }
        if ($ZipPath) { throw "-Upload takes the zip from the draft release, so -ZipPath can't be used with it." }

        # What is new: not the packages of earlier releases that vpk downloaded to build the delta from.
        $files = Get-ChildItem $OutputFolder -File | Where-Object {
            $_.Name -notin $existing -or $_.Name -in @("releases.win.json", "RELEASES", "assets.win.json")
        }
        foreach ($file in $files) {
            if ($PSCmdlet.ShouldProcess("release $tag of $Repo", "upload $($file.Name)")) {
                Invoke-Native gh @("release", "upload", $tag, $file.FullName, "--repo", $Repo, "--clobber")
            }
        }
    }
    else {
        Write-Host "Nothing uploaded.  Use -Upload to add these to the draft release." -ForegroundColor Yellow
    }

    [pscustomobject]@{ Version = $tag; Folder = $OutputFolder; Setup = $setup }
}
finally {
    Remove-Folder $work
}
