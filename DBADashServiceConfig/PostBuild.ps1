param([string]$TargetDir);
$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue'
$URL = "https://github.com/trimble-oss/serialized-dataset-viewer/releases/download/1.2.0/SerializedDataSetViewer_1.2.0.zip"
$FileName = [System.IO.Path]::GetFileName($URL)

$DownloadPath = [System.IO.Path]::Combine($TargetDir, $FileName)
$Folder = [System.IO.Path]::Combine($TargetDir,"SerializedDataSetViewer")
$ConfigFile = [System.IO.Path]::Combine($Folder,"appsettings.json")

Invoke-WebRequest $URL -Out $DownloadPath

Expand-Archive -Path $DownloadPath -DestinationPath $Folder -Force -ErrorAction Stop

Remove-Item -Path $DownloadPath

$ConfigFile
Set-Content -Path $ConfigFile -Value '{ "DefaultPath": "..\\Failed" }'