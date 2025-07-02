<#
  Script to sign executables in the latest draft release of a GitHub repository.
  Downloads release assets labeled with "-unsigned.zip", extracts them & signs files that match the specified pattern.
  Recompresses the signed files into a new zip and uploads it back to the draft release.  Removes the original unsigned assets if specified.

  Requires GitHub CLI (gh) and Trimble signing tool (SignFiles.exe).  
  GitHub CLI needs to be authenticated with the repository.
  Trimble signing tool requires authentication & works only on the corporate network.

  Usage:
    .\DBADashSignRelease.ps1 -Folder "C:\temp" -Repo "trimble-oss/dba-dash" -SignTool "C:\Sign\SignFiles.exe" -SignPattern "DBADash*.exe" -RemoveUnsigned $true
#>
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
    [bool]$RemoveUnsigned = $true
)

$ErrorActionPreference = "Stop"
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
$DraftRelease = gh api repos/$repo/releases | ConvertFrom-Json | Where-Object { $_.draft -eq $true } | Select-Object -First 1

# Check if a draft release was found
if($null -eq $DraftRelease) {
    Write-Host "No draft release found for repository '$repo'. Exiting." -ForegroundColor Yellow
    exit 1
}
Write-Host "Found draft release: '$($DraftRelease.name)' (Tag: '$($DraftRelease.tag_name)')" -ForegroundColor Green

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

$Assets = gh api $DraftRelease.assets_url | ConvertFrom-Json 
$UnsignedAssets = $Assets | Where-Object {$_.name -like "*-unsigned.zip"} 

if(@($UnsignedAssets).Count -eq 0) {
    Write-Host "No unsigned zip files found in the draft release." -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "Downloaded $CountOfAssets unsigned zip files to $tempFolder" -ForegroundColor Green
}

# Download all assets from the draft release
Write-Host "Downloading assets from draft release..." -ForegroundColor Cyan
$UnsignedAssets | ForEach-Object {
    $Path = [IO.Path]::Combine($tempFolder,$_.name)
    Write-Host "Downloading asset: $($_.name) to $Path" -ForegroundColor Cyan
    gh api $_.url --header "Accept: application/octet-stream" > $Path
}

# Extract and sign each .exe file in the downloaded assets, then re-upload to GitHub
Get-ChildItem $tempFolder -File | Where-Object {$_.Name -like "*-unsigned.zip"} | ForEach-Object {
  $extractPath = [IO.Path]::Combine($tempFolder, [IO.Path]::GetFileNameWithoutExtension($_.Name))
  Write-Host "Extracting $extractPath"
  Expand-Archive -LiteralPath $_.FullName -DestinationPath $extractPath -Force
  Write-Host "Signing executables in $extractPath"
  Get-ChildItem $extractPath | Where-Object { $_.Name -like $SignPattern } | ForEach-Object {
    $exePath = $_.FullName
    Write-Host "Signing $exePath"
    & $signTool $exePath $ $_.Name
    if ($LASTEXITCODE -ne 0) {
      throw "Code signing failed for $exePath with exit code $LASTEXITCODE"
    }
    if ((Get-AuthenticodeSignature -FilePath $exePath).Status -ne 'Valid') {
      throw "Code signing verification failed for $exePath"
    }
  }
  Write-Host "Recompressing signed files into a new zip..."
  $newZip = $_.FullName.Replace("-unsigned.zip", ".zip")
  $compressPath = [IO.Path]::Combine($extractPath, "*")
  Compress-Archive -Path $compressPath -DestinationPath $newZip -Force
  $uploadUrl = $DraftRelease.upload_url -replace '\{\?.*\}', "?name=$(Split-Path $newZip -Leaf)"
  Write-Host "Uploading signed zip to draft release: $newZip" -ForegroundColor Cyan
  gh api $uploadUrl --method POST --input $newZip --header "Content-Type: application/zip" | Out-Null
}
# Check if the number of assets in the draft release matches the expected count
if (@(gh api $DraftRelease.assets_url | ConvertFrom-Json).Count -ne (@($Assets).Count + @($UnsignedAssets).Count)) {
  Write-Host "File Count not expected" -ForegroundColor Yellow
  exit 1
}
if($RemoveUnsigned){
  # Delete the original unsigned assets from the draft release
  Write-Host "Deleting original unsigned assets from the draft release..." -ForegroundColor Cyan
  gh api $DraftRelease.assets_url | ConvertFrom-Json | Where-Object {$_.name -like "*-unsigned.zip"} | ForEach-Object {
    gh api $_.url --method DELETE
  }
}

# Clean up temporary folder
Remove-Item -Path $tempFolder -Recurse -Force

Write-Host "Signing completed successfully." -ForegroundColor Green

