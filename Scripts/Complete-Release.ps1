<#
.SYNOPSIS
    Finishes a draft release of DBA Dash: signs it, builds the installers and adds them to it.
.DESCRIPTION
    The release process is:

      1. Run the "Tag and Create Release" workflow.  It builds DBA Dash and creates a draft release with the unsigned zips.
      2. Run this script, on the corporate network.
      3. Review the draft release and publish it.
      4. Once it's published, run New-WingetManifest.ps1 for the Visualizer's winget package.

    This script does, for the draft release:

      - Signs the executables in each -unsigned.zip and replaces it with the signed zip (SignRelease.ps1).
      - Builds the Visualizer's installer - setup program, .msi, package and update feed - from its signed zip and uploads it
        (Pack-Installer.ps1 -App Visualizer).
      - The same for the GUI, from the GUI only zip (Pack-Installer.ps1 -App GUI), at the same time as the Visualizer.
      - Checks every file the release should have is there, and that nothing unsigned is left.

    Most of the time is the signing tool: it sends each file to the signing service, and the GUI's setup program and .msi are
    around 100 MB each.  So files are signed several at a time (-SignThrottle), and the two installers are built side by side.
    The signed zips are kept from the signing step, not downloaded again.  The installers' output is shown prefixed with the
    app, and written to DBADashBuild\<App>Installer.log.

    GitHub's limits are not close: a dozen API calls, about 20 uploads, at most six at once.  Each upload and download is
    retried with a growing wait, which also gets past a secondary rate limit.

    Each step can be run again on its own if one fails.  Run again, this script skips signing when the signed zips are already
    in the release, and rebuilds and replaces the installers.

    Requires PowerShell 7.4, the GitHub CLI (gh), authenticated, and the Trimble signing tool (SignFiles.exe), which works only on
    the corporate network.
.PARAMETER Version
    The draft release, e.g. 4.21.0.  Defaults to the latest draft release.
.PARAMETER Repo
    The GitHub owner and repository.
.PARAMETER SignTool
    The signing tool.
.PARAMETER NoMsi
    Don't build the .msi installers.
.PARAMETER SignThrottle
    How many files to send to the signing tool at once.  1 does everything one at a time, the installers one after the other,
    as a fallback if the signing service objects.
.EXAMPLE
    ./Scripts/Complete-Release.ps1
#>
#Requires -Version 7.4
[CmdletBinding()]
param (
    [string]$Version,
    [string]$Repo = "trimble-oss/dba-dash",
    [string]$SignTool = "C:\Sign\SignFiles.exe",
    [switch]$NoMsi,
    [ValidateRange(1, 10)]
    [int]$SignThrottle = 4
)

$ErrorActionPreference = "Stop"

$started = Get-Date
function Write-Step([string]$text) {
    Write-Host ""
    Write-Host ("==== {0} ==== ({1:mm\:ss} so far)" -f $text, ((Get-Date) - $started)) -ForegroundColor Green
}

function Get-Assets {
    # By id: a draft release can't be looked up by its tag.  --jq rather than ConvertFrom-Json, as --paginate runs the pages together.
    $names = @(gh api "repos/$Repo/releases/$($draft.id)/assets?per_page=100" --paginate --jq '.[].name')
    if ($LASTEXITCODE -ne 0) { throw "Unable to list the assets of release $tag." }
    $names
}

# ---- Checks first: better to fail now than half way through -------------------------------------------------------------
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw "The GitHub CLI (gh) is needed: https://cli.github.com/" }
gh auth status *> $null
if ($LASTEXITCODE -ne 0) { throw "The GitHub CLI isn't signed in.  Run 'gh auth login'." }
if (-not (Test-Path $SignTool)) { throw "Sign tool not found at $SignTool.  It works only on the corporate network." }

$releases = gh api "repos/$Repo/releases"
# e.g. a token not yet authorized for the organization's SAML single sign-on
if ($LASTEXITCODE -ne 0) { throw "Unable to read the releases of $Repo with gh: $releases" }
$drafts = @($releases | ConvertFrom-Json | Where-Object { $_.draft -eq $true })
if ($Version) {
    $draft = $drafts | Where-Object { $_.tag_name -eq $Version } | Select-Object -First 1
    if (-not $draft) { throw "No draft release $Version in $Repo." }
}
else {
    if ($drafts.Count -eq 0) { throw "No draft release found in $Repo.  Run the 'Tag and Create Release' workflow first." }
    if ($drafts.Count -gt 1) { throw "There are $($drafts.Count) draft releases ($($drafts.tag_name -join ', ')).  Say which with -Version." }
    $draft = $drafts[0]
}
$tag = $draft.tag_name
Write-Host "Draft release $tag" -ForegroundColor Green

$build = Join-Path (Split-Path -Parent $PSScriptRoot) "DBADashBuild"
# For the installers' logs, when signing - which would otherwise create it - is skipped
New-Item $build -ItemType Directory -Force | Out-Null
# The signed zips, kept from signing for the installers
$signedZips = Join-Path $build "SignedZips"
if (Test-Path $signedZips) { Remove-Item $signedZips -Recurse -Force }

# ---- 1. Sign the zips ---------------------------------------------------------------------------------------------------
Write-Step "Signing"
$zips = "DBADash_$tag", "DBADash_GUI_Only_$tag", "DBADash_Visualizer_$tag"
$assets = Get-Assets
if ($assets -like "*-unsigned.zip") {
    & (Join-Path $PSScriptRoot "SignRelease.ps1") -Repo $Repo -SignTool $SignTool -Tag $tag -OutputFolder $signedZips -SignThrottle $SignThrottle
    if ($LASTEXITCODE -ne 0) { throw "SignRelease.ps1 failed with exit code $LASTEXITCODE." }
}
elseif (@($zips | Where-Object { "$_.zip" -notin $assets }).Count -eq 0) {
    Write-Host "Already signed - the signed zips are in the release." -ForegroundColor Yellow
}
else {
    throw "The release has neither the unsigned zips nor all the signed ones: $($assets -join ', ')"
}

# ---- 2 and 3. The installers, side by side -----------------------------------------------------------------------------------
Write-Step "Installers"
$installers = foreach ($app in "Visualizer", "GUI") {
    $packArgs = @{ App = $app; Version = $tag; Repo = $Repo; SignTool = $SignTool; NoMsi = $NoMsi; Upload = $true; SignThrottle = $SignThrottle }
    # The zip just signed, if this run signed it.  Otherwise Pack-Installer.ps1 downloads it.
    $zip = Join-Path $signedZips "$(if ($app -eq 'GUI') { 'DBADash_GUI_Only_' } else { 'DBADash_Visualizer_' })$tag.zip"
    if (Test-Path $zip) { $packArgs.ZipPath = $zip }
    [pscustomobject]@{ App = $app; Args = $packArgs; Log = Join-Path $build "$($app)Installer.log" }
}

$packScript = Join-Path $PSScriptRoot "Pack-Installer.ps1"
$pack = {
    param($packScript, $packArgs, $log, $app)
    Set-Content $log "" -NoNewline
    try {
        # Every stream as text, so it can be prefixed and logged.  Write-Host loses its colour.
        & $packScript @packArgs *>&1 | ForEach-Object {
            $line = "$_"
            Add-Content $log $line
            "[$app] $line"
        }
    }
    catch {
        Add-Content $log "FAILED: $_"
        "[$app] FAILED: $_"
        throw
    }
}

$failed = @()
if ($SignThrottle -eq 1) {
    foreach ($i in $installers) {
        try { & $pack $packScript $i.Args $i.Log $i.App | Write-Host }
        catch { $failed += "$($i.App): $_" }
    }
}
else {
    $jobs = @($installers | ForEach-Object { Start-ThreadJob -Name $_.App -ScriptBlock $pack -ArgumentList $packScript, $_.Args, $_.Log, $_.App })
    try {
        while ($jobs | Where-Object { $_.State -in "NotStarted", "Running" }) {
            $jobs | Receive-Job -ErrorAction SilentlyContinue | Write-Host
            Start-Sleep -Seconds 1
        }
        $jobs | Receive-Job -ErrorAction SilentlyContinue | Write-Host
        $failed = @($jobs | Where-Object State -ne "Completed" | ForEach-Object { "$($_.Name): $($_.JobStateInfo.Reason.Message)" })
    }
    finally {
        # Ctrl+C included: don't leave them running
        $jobs | Stop-Job
        $jobs | Remove-Job -Force
    }
}
if ($failed.Count -gt 0) {
    throw "Installer failed - $($failed -join '; ').  See DBADashBuild\<App>Installer.log.  Run this script again, or just that one: ./Scripts/Pack-Installer.ps1 -App <App> -Version $tag -Upload"
}

# ---- Check the release has everything -------------------------------------------------------------------------------------
Write-Step "Checking the release"
$expected = @(
    "DBADash_$tag.zip", "DBADash_GUI_Only_$tag.zip", "DBADash_Visualizer_$tag.zip",
    "DBADash_Visualizer_Setup_$tag.exe", "DBADash_Visualizer-$tag-full.nupkg", "releases.win.json", "RELEASES", "assets.win.json",
    "DBADash_GUI_Setup_$tag.exe", "DBADash_GUI-$tag-gui-full.nupkg", "releases.gui.json", "RELEASES-gui", "assets.gui.json"
)
if (-not $NoMsi) { $expected += "DBADash_Visualizer_Setup_$tag.msi", "DBADash_GUI_Setup_$tag.msi" }

$assets = Get-Assets
$missing = @($expected | Where-Object { $_ -notin $assets })
$unsigned = @($assets | Where-Object { $_ -like "*-unsigned*" })
$assets | Sort-Object | ForEach-Object { Write-Host "  $_" }
if ($missing.Count -gt 0) { throw "Missing from the release: $($missing -join ', ')" }
if ($unsigned.Count -gt 0) { throw "Still in the release, unsigned: $($unsigned -join ', ')" }

if (Test-Path $signedZips) { Remove-Item $signedZips -Recurse -Force }

Write-Host ""
Write-Host ("Done in {0:mm\:ss}.  Review and publish the draft: {1}" -f ((Get-Date) - $started), $draft.html_url) -ForegroundColor Green
Write-Host "Then, once it's published: ./Scripts/New-WingetManifest.ps1 -Version $tag" -ForegroundColor Green
