param([string]$TargetDir)

$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue'

# 1. Query the GitHub API for the latest release metadata
$Repo = "trimble-oss/serialized-dataset-viewer"
$ReleasesApi = "https://api.github.com/repos/$Repo/releases/latest"
$ReleaseData = Invoke-RestMethod -Uri $ReleasesApi

# 2. Narrow the asset match to the specific zip name and ensure only one result
# Using -like "SerializedDataSetViewer_*.zip" to ignore other potential zip assets
$URL = $ReleaseData.assets | 
       Where-Object { $_.name -like "SerializedDataSetViewer_*.zip" } | 
       Select-Object -ExpandProperty browser_download_url -First 1

if (-not $URL) { 
    throw "Could not find a valid SerializedDataSetViewer zip asset in the latest release." 
}

# 3. Setup paths
$FileName = [System.IO.Path]::GetFileName($URL)
$DownloadPath = [System.IO.Path]::Combine($TargetDir, $FileName)
$Folder = [System.IO.Path]::Combine($TargetDir, "SerializedDataSetViewer")
$ConfigFile = [System.IO.Path]::Combine($Folder, "appsettings.json")

# 4. Download and Extract
Write-Host "Downloading latest version: $FileName"
Invoke-WebRequest -Uri $URL -OutFile $DownloadPath
Expand-Archive -Path $DownloadPath -DestinationPath $Folder -Force

# 5. Cleanup and Config
Remove-Item -Path $DownloadPath
Set-Content -Path $ConfigFile -Value '{ "DefaultPath": "..\\Failed" }'

Write-Host "Deployment of latest $Repo version complete."