<#
.SYNOPSIS
    Upgrade to the latest version of DBA Dash
.DESCRIPTION
    Check the current version against the latest release in github.  
    Download the new version if available.  (Detecting if GUI only or full package is required)
    Stop the service & GUI app if runing.  
    Remove the existing installation files (with Clean parameter).  
    Extract the new files.  
    Start the service.
.PARAMETER Clean
    Remove exisitng files from the folder
.PARAMETER Tag
    Specify a specific version to upgrade to
.PARAMETER ForceUpgrade
    Force the upgrade to run regardless of the outcome of the version check.  Warning: For testing only purposes only.
.PARAMETER StartGUI
    Start GUI once upgrade process is complete.  
.PARAMETER StartConfig
    Start Config tool once the upgrade process is complete. 
.PARAMETER Repo
    Option to pass in the github owner and repo name.  e.g. trimble-oss/dba-dash.
.EXAMPLE
    ./UpgradeDBADash -Clean
.EXAMPLE
    ./UpgradeDBADash 
#>
Param(
    [Parameter(Mandatory=$false)]
    [switch]$Clean,
    [Parameter(Mandatory=$false)]
    [string]$Tag,
    [Parameter(Mandatory=$false)]
    [switch]$ForceUpgrade,
    [Parameter(Mandatory=$false)]
    [switch]$StartGUI,
    [Parameter(Mandatory=$false)]
    [switch]$StartConfig,
    [Parameter(Mandatory=$false)]
    [string]$Repo="trimble-oss/dba-dash"
)
$ErrorActionPreference = "Stop"
$upgradeFile = "DBADash.Upgrade"

# Set security protocol to avoid 'Could not create SSL/TLS secure channel' error.
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Check if the configuration file exists - if not the current path isn't valid.  Exit.
if (!(Test-Path -Path "ServiceConfig.json")){
    throw "Invalid Folder"
    return
 }

# If $Tag parameter isn't passed in, get the latest release tag.  Otherwise check that the tag exists
if($Tag.Length -eq 0){
    Write-Host "Get the latest release"
    $Tag = (Invoke-WebRequest "https://api.github.com/repos/$Repo/releases/latest" | ConvertFrom-Json).tag_name
}
else{
    Write-Host "Checking specified tag exists"
    Stop-Process -Name DBADashServiceConfigTool -ErrorAction Ignore
}

# Convert tag string to Sytem.Version.  Pad out to 0.0.0.0 format
$newVersion = [System.Version]::Parse($Tag + ".0" * (4-($Tag.Split(".")).Count))

# Get existing version
$path = [System.IO.Path]::Combine((Get-Location),"DBADash.dll")
$existingVersion=[System.Version](Get-Item $path).VersionInfo.ProductVersion

# Create object from the config file.
$config = Get-Content -Raw -Path "ServiceConfig.json" | ConvertFrom-Json
# Get the name of the service so we can stop/start it.
$serviceName = $config.ServiceName

# Check if the service name specified exists (user might not have installed as service yet)
if ($serviceName -eq $null){
    $serviceExists = $false
}
else{
    $serviceExists = (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) -ne $null 
}
$versionCompare = $existingVersion.CompareTo($newVersion) 

$statusColour = "Cyan" # Default (ahead of release)
if($versionCompare -eq -1){
    $statusColour = "Magenta" # A new version is available
}
elseif($versionCompare -eq 0){
    $statusColour="DarkGreen" # We are up-to-date
}
 
# Display info
Write-Host "Existing Version: $($existingVersion.ToString(3))" -ForegroundColor $statusColour
Write-Host "New Version: $($newVersion.ToString(3))" -ForegroundColor $statusColour


if(Test-Path $upgradeFile){
    Write-Host "Warning - a previous upgrade operation didn't complete successfully.  Upgrade will be attempted" -ForegroundColor DarkYellow
    $ForceUpgrade=$true
}
$servicePath = [System.IO.Path]::Combine((Get-Location),"DBADashService.exe")

# Check to see if this is a deployment of GUI only to see which binary we need
if (Test-Path $servicePath){
    Write-Host "Installation Type: Agent"
    Write-Host "ServiceName: $serviceName"
    Write-Host "Service Exists: $serviceExists"
    if($Tag -eq "2.11"){
        $zip =  "DBADash_20220113.zip" # 2.11 was a manual release and doesn't conform to naming format
    }
    else{
        $zip = "DBADash_$Tag.zip"
    }
}
else{
    Write-Host "Installation Type: GUI"
    if($Tag -eq "2.11"){
        $zip = "DBADashGUI_20220113.zip"  # 2.11 was a manual release and doesn't conform to naming format
    }
    else{
        $zip = "DBADash_GUI_Only_$Tag.zip"
    }
}

# Check if we need to upgrade
if ($versionCompare -eq -1 -or $ForceUpgrade){   
    $download = "https://github.com/$Repo/releases/download/$tag/$zip"
    
    # Check if we already have the zip downloaded   
    if(!(Test-Path -Path $zip)){
        Write-Host "Download latest release:$download"
        Invoke-WebRequest $download -Out $zip
    }
    else{
        "Latest version already downloaded: $zip"
    }
    # Ensure we have the zip file
    if(!(Test-Path -Path $zip)){
        throw "File not downloaded: $zip"
        return
    }

    # Take service offline for install if it exists
    if($serviceExists){
        Write-Host "Stopping Service: $serviceName"
        Stop-Service -Name $serviceName -ErrorAction Stop
    }
    # Stop the GUI if running - we don't want locks on any files
    Write-Host "Stop processes"
    Get-Process -Name DBADash -ErrorAction Ignore | Where-Object { $_.Path -like (Get-Location).Path + "*" } | Stop-Process
    Get-Process -Name DBADashServiceConfigTool -ErrorAction Ignore | Where-Object { $_.Path -like (Get-Location).Path + "*" } | Stop-Process
    Get-Process -Name DBADashConfig -ErrorAction Ignore | Where-Object { $_.Path -like (Get-Location).Path + "*" } | Stop-Process
    
    # Wait for file locks to be released
    Start-Sleep -Seconds 2

    # If clean option is used, remove existing files from the installation folder.
    if($Clean){
        Write-Host "Clean folder"
        Get-ChildItem (Get-Location) -Recurse -Exclude "ServiceConfig.json","Log","*.ps1","*.json","*.xml",".json.*",$zip | Remove-Item -Recurse
    }
    Write-Host "Extract Files from: $zip"
    "Upgrade Started " + (Get-Date).ToString() | Out-File $upgradeFile
    Expand-Archive -Path $zip -DestinationPath (Get-Location) -Force -ErrorAction Stop
    Remove-Item $upgradeFile

    # If service exists, start it back up
    if($serviceExists){
        Write-Host "Start Service"
        Start-Service $serviceName
    }
    # Option to start GUI after upgrade
    if($StartGUI){
        Write-Host "Start GUI"
        Start-Process .\DBADash.exe
    }
    # Option to start config tool after upgrade
    if($StartConfig){
        Write-Host "Start Service Config Tool"
        Start-Process .\DBADashServiceConfigTool.exe
    }
    Write-Host "Upgrade Completed" -ForegroundColor DarkGreen
}
else{
    Write-Host "DBA Dash version is up to date." -ForegroundColor DarkGreen
}