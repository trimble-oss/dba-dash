<#
  Script to sign executables in the latest draft release of a GitHub repository.
  Downloads release assets labeled with "-unsigned.zip", extracts them & signs files that match the specified pattern.
  Recompresses the signed files into a new zip and uploads it back to the draft release.  Removes the original unsigned assets if specified.

  The zips are downloaded, signed and uploaded in parallel.  An executable that is in more than one zip - the same file, by its
  hash - is signed once and copied to the others.

  Requires PowerShell 7, GitHub CLI (gh) and Trimble signing tool (SignFiles.exe).
  GitHub CLI needs to be authenticated with the repository.
  Trimble signing tool requires authentication & works only on the corporate network.

  Usage:
    .\SignRelease.ps1 -Folder "C:\temp" -Repo "trimble-oss/dba-dash" -SignTool "C:\Sign\SignFiles.exe" -SignPattern "DBADash*.exe" -RemoveUnsigned $true
#>
#Requires -Version 7.2
param (
    # Temp folder to use for signing.  A SignTemp sub folder will be created in this folder & a separate folder used for each run.  Folders are deleted after successful signing.
    # Old folders in the SignTemp folder are deleted after 1 day.
    [string]$Folder="C:\temp",
    # Repository to look for draft releases in.
    [string]$Repo="trimble-oss/dba-dash",
    # Path of the Trimble Sign Tool to use for signing the executables.
    [string]$SignTool = "C:\Sign\SignFiles.exe",
    # Pattern to match the executables to sign.  Defaults to "DBADash*.exe" which matches all executables starting with "DBADash".
    [string]$SignPattern = "DBADash*.exe",
    # Remove the original unsigned assets after signing and uploading the new signed assets.
    [bool]$RemoveUnsigned = $true,
    # The draft release to sign.  Defaults to the latest draft release.
    [string]$Tag,
    # Where to leave the signed zips, e.g. for Pack-Installer.ps1 -ZipPath, rather than download them again.  Not kept by default.
    [string]$OutputFolder,
    # How many files to send to the sign tool at once.  1 signs them one at a time.
    [int]$SignThrottle = 4
)

$ErrorActionPreference = "Stop"
$common = Join-Path $PSScriptRoot "ReleaseCommon.ps1"
. $common
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Check sign tool exists
if(-not (Test-Path $signTool)) {
    Write-Host "Sign tool not found at $signTool. Exiting." -ForegroundColor Red
    exit 1
}
# Check GitHub CLI (gh) is installed and accessible
try {
    $null = gh version
    Write-Host "GitHub CLI (gh) is installed and accessible." -ForegroundColor Green
}
catch {
    Write-Host "Error: GitHub CLI (gh) is not installed or not in your PATH." -ForegroundColor Red
    Write-Host "Please install it from https://cli.github.com/ and ensure it's in your system's PATH." -ForegroundColor Red
    exit 1
}
# Find the latest draft release in the repository
$DraftRelease = gh api repos/$repo/releases | ConvertFrom-Json | Where-Object { $_.draft -eq $true -and (-not $Tag -or $_.tag_name -eq $Tag) } | Select-Object -First 1

# Check if a draft release was found
if($null -eq $DraftRelease) {
    Write-Host "No draft release found for repository '$repo'. Exiting." -ForegroundColor Yellow
    exit 1
}
Write-Host "Found draft release: '$($DraftRelease.name)' (Tag: '$($DraftRelease.tag_name)')" -ForegroundColor Green
$releaseTag = $DraftRelease.tag_name

# Create temp folder
$signTempFolder = Join-Path $folder "SignTemp"
$tempFolder = Join-Path $signTempFolder ("SignTemp-" + [guid]::NewGuid().ToString())
New-Item -Path $tempFolder -ItemType Directory -Force | Out-Null

## Clean up old folders in the sign temp folder
Write-Host "Cleaning up old folders in $signTempFolder..." -ForegroundColor Cyan
$CutoffDate = (Get-Date).AddDays(-1)
Get-ChildItem -Path $signTempFolder -Directory |
    Where-Object { $_.LastWriteTime -lt $CutoffDate -and $_.Name -like "SignTemp*" } |
    Remove-Item -Recurse -Force

function Get-AssetList {
    # By id: a draft release can't be looked up by its tag.  --jq rather than ConvertFrom-Json, as --paginate runs the pages together.
    $lines = @(gh api "repos/$repo/releases/$($DraftRelease.id)/assets?per_page=100" --paginate --jq '.[] | "\(.id) \(.name)"')
    if ($LASTEXITCODE -ne 0) { throw "Unable to list the assets of release $releaseTag." }
    $lines | ForEach-Object { $id, $name = $_ -split " ", 2; [pscustomobject]@{ id = $id; name = $name } }
}

$Assets = Get-AssetList
$UnsignedAssets = @($Assets | Where-Object {$_.name -like "*-unsigned.zip"})

if($UnsignedAssets.Count -eq 0) {
    Write-Host "No unsigned zip files found in the draft release." -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "Found $($UnsignedAssets.Count) unsigned zip files in the draft release." -ForegroundColor Green
}
# A previous run that failed part way may have uploaded some signed zips already - sign only the rest, so a rerun recovers
$ToSign = @($UnsignedAssets | Where-Object { $_.name.Replace("-unsigned.zip", ".zip") -notin $Assets.name })
if ($ToSign.Count -lt $UnsignedAssets.Count) {
    Write-Host "$($UnsignedAssets.Count - $ToSign.Count) already signed in the draft release - skipping them." -ForegroundColor Yellow
}

# Each parallel block returns a line for anything that failed, rather than throwing part way through the others
function Assert-NoFailures([string]$step, [object[]]$failures) {
    if ($failures.Count -gt 0) { throw "$step failed: $($failures -join '; ')" }
}

# ---- Download: three at a time is well inside GitHub's limits, and a download is a redirect to its storage, not an API call ----
Write-Host "Downloading $($ToSign.Count) zips from the draft release..." -ForegroundColor Cyan
$failures = @($ToSign.name | ForEach-Object -ThrottleLimit 3 -Parallel {
    . $using:common
    $fileName = $_
    $releaseTag = $using:releaseTag; $repo = $using:repo; $dir = $using:tempFolder
    try {
        Invoke-WithRetry -What "Downloading $fileName" -Attempts 3 -DelaySeconds 5 -Command {
            gh release download $releaseTag --pattern $fileName --repo $repo --dir $dir --clobber
        }
        Write-Host "Downloaded $fileName" -ForegroundColor Green
    }
    catch { "$fileName - $($_.Exception.Message)" }
})
Assert-NoFailures "Download" $failures

# ---- Extract, then sign each different executable once, several at a time ----------------------------------------------------
$extracted = foreach ($asset in $ToSign) {
    $extractPath = Join-Path $tempFolder ([IO.Path]::GetFileNameWithoutExtension($asset.name))
    Write-Host "Extracting $extractPath"
    [IO.Compression.ZipFile]::ExtractToDirectory((Join-Path $tempFolder $asset.name), $extractPath, $true)
    $extractPath
}
$exes = @($extracted | ForEach-Object { Get-ChildItem $_ -File | Where-Object { $_.Name -like $SignPattern } })
$distinct = @($exes | Group-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash })
Write-Host "Signing $($distinct.Count) executables ($($exes.Count) in the zips), $SignThrottle at a time..." -ForegroundColor Cyan

$failures = @($distinct | ForEach-Object -ThrottleLimit $SignThrottle -Parallel {
    . $using:common
    $copies = $_.Group
    $exe = $copies[0]
    try {
        Invoke-Sign -SignTool $using:SignTool -Path $exe.FullName -Name $exe.Name
        $copies | Select-Object -Skip 1 | ForEach-Object { Copy-Item -LiteralPath $exe.FullName -Destination $_.FullName -Force }
        Write-Host "Signed $($exe.Name)$(if ($copies.Count -gt 1) { " - and copied to $($copies.Count - 1) other zip(s)" })" -ForegroundColor Green
    }
    catch { "$($exe.FullName) - $($_.Exception.Message)" }
})
Assert-NoFailures "Signing" $failures

$notSigned = @($exes | Where-Object { (Get-AuthenticodeSignature -LiteralPath $_.FullName).Status -ne 'Valid' })
if ($notSigned.Count -gt 0) { throw "Code signing verification failed for $($notSigned.FullName -join ', ')" }

# ---- Zip each one again and upload it, in parallel ----------------------------------------------------------------------------
# ZipFile rather than Compress-Archive: it's many times faster on a few hundred MB, and writes / in the paths, as zips should.
Write-Host "Recompressing and uploading the signed zips..." -ForegroundColor Cyan
$failures = @($ToSign.name | ForEach-Object -ThrottleLimit 3 -Parallel {
    . $using:common
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $releaseTag = $using:releaseTag; $repo = $using:repo
    $newZip = Join-Path $using:tempFolder $_.Replace("-unsigned.zip", ".zip")
    $signedName = Split-Path $newZip -Leaf
    try {
        if (Test-Path $newZip) { Remove-Item -LiteralPath $newZip -Force }
        $source = Join-Path $using:tempFolder ([IO.Path]::GetFileNameWithoutExtension($_))
        [IO.Compression.ZipFile]::CreateFromDirectory($source, $newZip, [IO.Compression.CompressionLevel]::Optimal, $false)
        Invoke-WithRetry -What "Uploading $signedName" -Command {
            gh release upload $releaseTag $newZip --repo $repo --clobber
        }
        Write-Host "Uploaded $signedName" -ForegroundColor Green
    }
    catch { "$signedName - $($_.Exception.Message)" }
})
Assert-NoFailures "Upload" $failures

# Check every signed zip is now in the draft release
$Assets = Get-AssetList
$missing = @($UnsignedAssets.name | ForEach-Object { $_.Replace("-unsigned.zip", ".zip") } | Where-Object { $_ -notin $Assets.name })
if ($missing.Count -gt 0) {
  Write-Host "Not in the draft release after uploading: $($missing -join ', ')" -ForegroundColor Yellow
  exit 1
}
if($RemoveUnsigned){
  # Delete the original unsigned assets from the draft release
  Write-Host "Deleting original unsigned assets from the draft release..." -ForegroundColor Cyan
  $Assets | Where-Object {$_.name -like "*-unsigned.zip"} | ForEach-Object {
    $assetId = $_.id
    Invoke-WithRetry -What "Deleting $($_.name)" -Attempts 3 -DelaySeconds 5 -Command { gh api "repos/$repo/releases/assets/$assetId" --method DELETE }
  }
}

if ($OutputFolder) {
  New-Item -Path $OutputFolder -ItemType Directory -Force | Out-Null
  $ToSign.name | ForEach-Object { Move-Item -LiteralPath (Join-Path $tempFolder $_.Replace("-unsigned.zip", ".zip")) -Destination $OutputFolder -Force }
  Write-Host "Signed zips left in $OutputFolder" -ForegroundColor Green
}

# Clean up temporary folder
Remove-Item -Path $tempFolder -Recurse -Force

Write-Host "Signing completed successfully." -ForegroundColor Green
exit 0
