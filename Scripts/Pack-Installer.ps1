<#
.SYNOPSIS
    Packs DBA Dash Visualizer or the DBA Dash GUI as a Velopack installer and update feed, signs it, and adds it to the draft
    release.
.DESCRIPTION
    Turns the app's zip of a draft release into what its installer channel needs.  For the Visualizer:

      DBADash_Visualizer_Setup_<version>.exe    the setup program - one click, per user, no elevation
      DBADash_Visualizer_Setup_<version>.msi    the same, as an .msi that asks whether to install for you or all users
      DBADash_Visualizer-<version>-full.nupkg   the package the updater installs
      DBADash_Visualizer-<version>-delta.nupkg  what changed since the last release, if there was one
      releases.win.json, RELEASES, assets.win.json   the feed the installed app reads to find updates

    and the same for the GUI, from the GUI only zip: DBADash_GUI_Setup_<version>.exe, DBADash_GUI-<version>-full.nupkg and so
    on, with its feed in releases.gui.json, RELEASES-gui and assets.gui.json.  The two apps share each release, so the GUI is
    packed on its own channel ("gui") to keep its feed apart from the Visualizer's.  The Visualizer keeps the default channel
    ("win"): copies already installed read that feed.

    The installed Visualizer updates to the latest release.  The installed GUI reads the feed of the release for its
    repository database's version - so each release has to have it - and installs that version, never the latest.

    Velopack has to sign what goes in the package itself, part way through building it, so this is not a step after the zips
    are signed.  It calls the Trimble signing tool (SignFiles.exe) once for each file, so it signs only what identifies the app:
    its executable, Velopack's launcher stub and updater.  Not the dozens of libraries.  The setup program and .msi are signed
    after vpk has built them, both at once: the tool sends the whole file to be signed, and they're the size of the package -
    for the GUI, minutes each.  Nothing vpk writes refers to them by hash, and the .msi has the package in it, not the setup
    program.  Like SignRelease.ps1 it works only on the corporate network.

    Run it after SignRelease.ps1 and before publishing the release.  It uses the signed zip if there is one - and then doesn't
    sign the executable again - and otherwise the unsigned zip.  Complete-Release.ps1 runs it for both apps at once.

    Nothing is uploaded unless -Upload is given.
.PARAMETER App
    Visualizer or GUI.
.PARAMETER Version
    The release to pack, e.g. 4.19.0.  Defaults to the tag of the latest draft release.
.PARAMETER Repo
    The GitHub owner and repository.
.PARAMETER ZipPath
    A local zip to pack - DBADash_Visualizer_<version>[-unsigned].zip or DBADash_GUI_Only_<version>[-unsigned].zip - instead of
    downloading one from the draft release.  -Version is then taken from the file name.  With -Upload it has to be the signed
    zip that is in the release, as SignRelease.ps1 -OutputFolder leaves it.
.PARAMETER OutputFolder
    Where the installer and feed are written.  Defaults to DBADashBuild\<App>Installer under the repository.  Emptied first.
.PARAMETER SignTool
    The signing tool.  Defaults to C:\Sign\SignFiles.exe.
.PARAMETER SkipSigning
    Don't sign.  For trying the script out; the result must not be released.
.PARAMETER NoDelta
    Don't look for the previous release to build a delta from.  The first release has none, and it isn't needed: an update
    then downloads the whole package.
.PARAMETER NoMsi
    Don't build the .msi.  It is built by default: it offers to install for the current user or for all users of the machine
    (see Docs/Visualizer.md and Docs/GUIInstaller.md), and takes about a minute more.
.PARAMETER Upload
    Add the files to the draft release with the GitHub CLI (gh), three at a time.  Supports -WhatIf.
.PARAMETER SignThrottle
    How many files to send to the sign tool at once.  1 signs them one at a time.
.EXAMPLE
    ./Scripts/Pack-Installer.ps1 -App Visualizer -Upload
.EXAMPLE
    ./Scripts/Pack-Installer.ps1 -App GUI -Upload
#>
#Requires -Version 7.2
[CmdletBinding(SupportsShouldProcess)]
param (
    [Parameter(Mandatory)]
    [ValidateSet("Visualizer", "GUI")]
    [string]$App,
    [string]$Version,
    [string]$Repo = "trimble-oss/dba-dash",
    [string]$ZipPath,
    [string]$OutputFolder,
    [string]$SignTool = "C:\Sign\SignFiles.exe",
    [switch]$SkipSigning,
    [switch]$NoDelta,
    [switch]$NoMsi,
    [switch]$Upload,
    [int]$SignThrottle = 4
)

$ErrorActionPreference = "Stop"
$common = Join-Path $PSScriptRoot "ReleaseCommon.ps1"
. $common

$repoRoot = Split-Path -Parent $PSScriptRoot

# What differs between the apps.  The pack id names the packages, the install folder and the Installed apps entry: changing it
# makes a different app to Windows, and an installed copy would not be updated.  Likewise the channel names the feed.
$apps = @{
    Visualizer = @{
        Zip = "DBADash_Visualizer_"; PackId = "DBADash_Visualizer"; Exe = "DBADashVisualizer.exe"; Title = "DBA Dash Visualizer"
        Channel = "win"; Icon = "DBADash.Viewers.GUI\Resources\PlanViewer.ico"; Art = "Assets\Installer\Visualizer"
    }
    GUI        = @{
        Zip = "DBADash_GUI_Only_"; PackId = "DBADash_GUI"; Exe = "DBADash.exe"; Title = "DBA Dash"
        Channel = "gui"; Icon = "DBADashGUI\DBADash.ico"; Art = "Assets\Installer\GUI"
    }
}
$cfg = $apps[$App]
if (-not $OutputFolder) { $OutputFolder = Join-Path $repoRoot "DBADashBuild\$($App)Installer" }
$icon = Join-Path $repoRoot $cfg.Icon
# The installers' artwork, <art>_Splash.png and so on, drawn from the app's logo by Scripts/InstallerArt.cs
$art = Join-Path $repoRoot $cfg.Art
$exeName = $cfg.Exe
$zipPrefix = $cfg.Zip
$packId = $cfg.PackId
$channel = $cfg.Channel

# The feed files for the channel.  The default channel also writes the legacy RELEASES without a suffix.
$feedFiles = @("releases.$channel.json", "assets.$channel.json", $(if ($channel -eq "win") { "RELEASES" } else { "RELEASES-$channel" }))

function Remove-Folder([string]$path) {
    if (Test-Path $path) { [IO.Directory]::Delete($path, $true) }
}

function Invoke-Native([string]$exe, [string[]]$arguments) {
    & $exe @arguments
    if ($LASTEXITCODE -ne 0) { throw "$exe failed with exit code $LASTEXITCODE" }
}

$work = Join-Path ([IO.Path]::GetTempPath()) ("$($App)Installer-" + [guid]::NewGuid().ToString("N"))
New-Item $work -ItemType Directory | Out-Null

try {
    if (-not $SkipSigning -and -not (Test-Path $SignTool)) {
        throw "Sign tool not found at $SignTool.  Use -SignTool, or -SkipSigning to try the script out."
    }

    # ---- The zip to pack --------------------------------------------------------------------------------------------------
    $tag = $Version
    if ($ZipPath) {
        if (-not (Test-Path $ZipPath)) { throw "$ZipPath not found." }
        if ((Split-Path $ZipPath -Leaf) -notmatch "^$([regex]::Escape($zipPrefix))(\d+\.\d+\.\d+)(-unsigned)?\.zip$") {
            throw "Expected a file named $($zipPrefix)<version>[-unsigned].zip."
        }
        if ($Version -and $Version -ne $Matches[1]) { throw "$ZipPath is version $($Matches[1]), not $Version." }
        $tag = $Matches[1]
        $zip = $ZipPath
        $alreadySigned = -not $Matches[2]
        if ($Upload -and -not $alreadySigned) { throw "-Upload with -ZipPath needs the signed zip, not the -unsigned one." }
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
        $name = "$zipPrefix$tag.zip"
        $alreadySigned = $true
        if (-not ($assets | Where-Object { $_.name -eq $name })) {
            $name = "$zipPrefix$tag-unsigned.zip"
            $alreadySigned = $false
            if (-not ($assets | Where-Object { $_.name -eq $name })) { throw "Neither $zipPrefix$tag.zip nor the -unsigned zip is in release $tag." }
        }
        Write-Host "Downloading $name" -ForegroundColor Cyan
        Invoke-Native gh @("release", "download", $tag, "--pattern", $name, "--repo", $Repo, "--dir", $work, "--clobber")
        $zip = Join-Path $work $name
    }

    $publish = Join-Path $work "publish"
    # ZipFile rather than Expand-Archive: many times faster on the GUI's few hundred MB
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::ExtractToDirectory($zip, $publish, $true)
    if (-not (Test-Path (Join-Path $publish $exeName))) { throw "$exeName isn't at the root of $zip." }
    # The executable isn't signed again, so a signed zip had better be
    if ($alreadySigned -and -not $SkipSigning -and (Get-AuthenticodeSignature (Join-Path $publish $exeName)).Status -ne "Valid") {
        throw "$exeName in $zip isn't signed."
    }

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
    if (-not $NoDelta -and (-not $ZipPath -or $Upload)) {
        Write-Host "Looking for the previous release, to build a delta from" -ForegroundColor Cyan
        & $vpk download github --repoUrl "https://github.com/$Repo" --channel $channel --outputDir $OutputFolder
        if ($LASTEXITCODE -ne 0) {
            Write-Host "No previous installer release found - the package will be full only." -ForegroundColor Yellow
        }
    }
    $existing = @(Get-ChildItem $OutputFolder -File | ForEach-Object Name)

    # ---- Pack and sign -----------------------------------------------------------------------------------------------------
    $packArgs = @(
        # The id names the packages (DBADash_Visualizer-<version>-full.nupkg), the install folder and the Installed apps entry, in
        # the style of the zips.
        "pack", "--packId", $packId, "--packVersion", $tag, "--packDir", $publish, "--mainExe", $exeName, "--channel", $channel,
        "--outputDir", $OutputFolder, "--packTitle", $cfg.Title, "--packAuthors", "Trimble, Inc.",
        "--icon", $icon, "--framework", "net10.0-x64-desktop", "--runtime", "win-x64",
        # The Start menu, and not the Desktop as well.  The zip has its own portable copy, so no portable package.
        "--shortcuts", "StartMenuRoot", "--noPortable", "--yes",
        # Shown by the setup program while it installs, with a progress bar along the bottom.
        "--splashImage", "$($art)_Splash.png", "--splashProgressColor", "#7fc3f0"
    )

    if (-not $NoMsi) {
        # The .msi asks whether to install for the current user (as the setup program does) or for all users of the machine,
        # under Program Files.  An install for all users needs elevation to install, and again to update.
        $packArgs += @("--msi", "--instLocation", "Either")
        # The .msi's artwork: the brand panel behind the welcome and finish pages (493x312), and the strip across the top of the
        # others (493x58).
        $packArgs += @("--msiDialogBackground", "$($art)_Dialog.bmp", "--msiTopBanner", "$($art)_Banner.bmp")
    }

    if (-not $SkipSigning) {
        # vpk runs this for one file at a time.  A wrapper keeps the tool's arguments - the file, "$" for in place, and the
        # name shown in the signature - out of the way of how vpk splits a command.  vpk signs the setup and .msi whatever
        # --signExclude says, and one after the other: the wrapper passes over them, and they're signed together below.
        $wrapper = Join-Path $work "sign.cmd"
        Set-Content $wrapper (@(
                '@echo off'
                'if /i "%~x1"==".msi" exit /b 0'
                'set "name=%~n1"'
                'if /i "%name:~-6%"=="-Setup" exit /b 0'
                "`"$SignTool`" %1 $ `"$($cfg.Title)`""
                'exit /b %ERRORLEVEL%'
            ) -join "`r`n")

        # Sign the app's own executable (unless the zip already had it signed), and Velopack's launcher stub and updater.
        # Everything else is excluded: each file is half a minute, and signing the third party libraries with our certificate is
        # not something we do for the other packages either.
        $exclude = '^(?!.*(Squirrel\.exe|_ExecutionStub\.exe' + $(if ($alreadySigned) { '' } else { '|' + [regex]::Escape($exeName) }) + ')$).*$'

        $packArgs += @("--signTemplate", "`"$wrapper`" {{file}}", "--signExclude", $exclude)
        Write-Host "Packing $tag and signing with $SignTool - this takes a few minutes" -ForegroundColor Cyan
    }
    else {
        Write-Host "Packing $tag WITHOUT signing" -ForegroundColor Yellow
    }

    Invoke-Native $vpk $packArgs

    # ---- What came out -----------------------------------------------------------------------------------------------------
    $setup = Join-Path $OutputFolder "$packId-$channel-Setup.exe"
    if (-not (Test-Path $setup)) { throw "vpk didn't produce a setup program." }
    $setupName = "$($packId)_Setup_$tag.exe"
    Move-Item $setup (Join-Path $OutputFolder $setupName) -Force
    $setup = Join-Path $OutputFolder $setupName

    $msiName = $null
    if (-not $NoMsi) {
        $msiFile = Join-Path $OutputFolder "$packId-$channel.msi"
        if (-not (Test-Path $msiFile)) { throw "vpk didn't produce an .msi." }
        $msiName = "$($packId)_Setup_$tag.msi"
        Move-Item $msiFile (Join-Path $OutputFolder $msiName) -Force
    }

    if (-not $SkipSigning) {
        # The setup and .msi, at the same time: each is the size of the package, and the tool sends the whole file to be signed
        $installers = @($setupName, $msiName | Where-Object { $_ })
        Write-Host "Signing $($installers -join ' and ') - this takes a few minutes" -ForegroundColor Cyan
        $failures = @($installers | ForEach-Object -ThrottleLimit $SignThrottle -Parallel {
            . $using:common
            try { Invoke-Sign -SignTool $using:SignTool -Path (Join-Path $using:OutputFolder $_) -Name $using:cfg.Title }
            catch { "$_" }
        })
        if ($failures.Count -gt 0) { throw "Signing failed: $($failures -join '; ')" }

        $failed = @()
        foreach ($name in $installers) {
            if ((Get-AuthenticodeSignature (Join-Path $OutputFolder $name)).Status -ne "Valid") { $failed += $name }
        }

        # And inside the package the updater installs.  A channel other than the default is in its name.
        $full = Join-Path $OutputFolder $(if ($channel -eq "win") { "$packId-$tag-full.nupkg" } else { "$packId-$tag-$channel-full.nupkg" })
        $check = Join-Path $work "check"
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::ExtractToDirectory($full, $check)
        foreach ($exe in "lib\app\$exeName", "lib\app\Squirrel.exe") {
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

        # What is new: not the packages of earlier releases that vpk downloaded to build the delta from.
        $files = @(Get-ChildItem $OutputFolder -File | Where-Object {
            $_.Name -notin $existing -or $_.Name -in $feedFiles
        } | Where-Object { $PSCmdlet.ShouldProcess("release $tag of $Repo", "upload $($_.Name)") })

        # Three at a time: a handful of uploads, well inside GitHub's limits.  Each is retried, with a wait, if it fails.
        $failures = @($files | ForEach-Object -ThrottleLimit 3 -Parallel {
            . $using:common
            $file = $_; $tag = $using:tag; $repo = $using:Repo
            try {
                Invoke-WithRetry -What "Uploading $($file.Name)" -Command { gh release upload $tag $file.FullName --repo $repo --clobber }
                Write-Host "Uploaded $($file.Name)" -ForegroundColor Green
            }
            catch { "$_" }
        })
        if ($failures.Count -gt 0) { throw "Upload failed: $($failures -join '; ')" }
    }
    else {
        Write-Host "Nothing uploaded.  Use -Upload to add these to the draft release." -ForegroundColor Yellow
    }

    [pscustomobject]@{ Version = $tag; Folder = $OutputFolder; Setup = $setup }
}
finally {
    Remove-Folder $work
}
