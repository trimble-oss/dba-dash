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
####################
# Check .NET Version
# https://github.com/trimble-oss/dba-dash/issues/42
####################

function CheckDotNetVersion([System.Version]$AppVersion){

    $DotNet8AppVersion = [System.Version]::Parse("3.0.0")
    $DotNet10AppVersion = [System.Version]::Parse("4.0.0")

    if($AppVersion.CompareTo($DotNet10AppVersion) -ge 0){
        $MinVersion = [System.Version]::Parse("10.0.0")
    }
    elseif($AppVersion.CompareTo($DotNet8AppVersion) -ge 0){
        $MinVersion = [System.Version]::Parse("8.0.0")
    }
    else{
        $MinVersion = [System.Version]::Parse("6.0.2")
    }

    $RuntimeVersions = dotnet --list-runtimes  | Where-Object { $_ -like "Microsoft.WindowsDesktop.App*" }

    $VersionCheck = @($RuntimeVersions | Where-Object { $_.Split(" ")[1] -as [System.Version] -ge $MinVersion }).Count -ge 1

    if (!$VersionCheck){
        if($MinVersion.Major -eq 10){
            Write-Warning ("The version of the .NET runtime appears to be out of date (Min Version: $MinVersion).`nDBA Dash $DotNet10AppVersion and later require the .NET 10 runtime. Please download the latest .NET 10 Desktop runtime.`n`nhttps://dotnet.microsoft.com/en-us/download/dotnet/10.0`n`nVersions detected:`n" + $RuntimeVersions)
        }
        elseif($MinVersion.Major -eq 8){
            Write-Warning ("The version of the .NET runtime appears to be out of date (Min Version: $MinVersion).`nDBA Dash $DotNet8AppVersion and later require the .NET 8 runtime. Please download the latest .NET 8 Desktop runtime.`n`nhttps://dotnet.microsoft.com/en-us/download/dotnet/8.0`n`nVersions detected:`n" + $RuntimeVersions)
        }
        else{
            Write-Warning ("The version of the .NET runtime appears to be out of date (Min Version: $MinVersion).  Please download the latest .NET 6 Desktop runtime.`n`nhttps://dotnet.microsoft.com/en-us/download/dotnet/6.0`n`nVersions detected:`n" + $RuntimeVersions)
        }
        return $false
    }
    return $true

}

<#
    Expand a zip file with multiple retry
    Retry might be beneficial if there is a locked file.
#>
function ExpandWithRetry([string]$ZipFile,[int]$RetryCount,[int]$WaitBetweenRetry){
    $Error.Clear()
    $cnt=1
    do {
        if($Error.Count -gt 0){
            "Waiting $WaitBetweenRetry seconds between retries"
            Start-Sleep -Seconds $WaitBetweenRetry
        }
        "Expanding $zip" + (&{If($cnt -gt 1) {" (Attempt $cnt of $RetryCount)"} Else {""}})
        $Error.Clear()
        Expand-Archive -Path $zip -DestinationPath (Get-Location) -Force -ErrorAction Continue
        $cnt+=1
    }
    while ($cnt -le $RetryCount -and $Error.Count -gt 0)

    if($Error.Count -gt 0){
        throw "Failed to extract files"
    }
}

$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue' # Improves performance of Invoke-WebRequest. #1233
$upgradeFile = "DBADash.Upgrade"

# Set security protocol to avoid 'Could not create SSL/TLS secure channel' error.
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Check if current folder looks like the DBA Dash installation directoy.
if (!(Test-Path -Path "DBADash.dll")){
    throw "Invalid Folder"
    return
 }

# If $Tag parameter isn't passed in, get the latest release tag.  Otherwise check that the tag exists
if($Tag.Length -eq 0){
    Write-Host "Get the latest release"
    $Tag = (Invoke-WebRequest "https://api.github.com/repos/$Repo/releases/latest" -UseBasicParsing | ConvertFrom-Json).tag_name
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

$servicePath = [System.IO.Path]::Combine((Get-Location),"DBADashService.exe")

$serviceName = (Get-WmiObject -Class Win32_Service | Where-Object { $_.PathName -ilike "*$servicePath*" } | Select-Object Name).Name

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

# Check to see if this is a deployment of GUI only to see which binary we need
if (Test-Path $servicePath){
    Write-Host "Installation Type: Agent"
    Write-Host "ServiceName: $serviceName"
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
    ## Check .NET version
    try {
        if (!(CheckDotNetVersion -AppVersion $newVersion)){
            return
        }
    }
    catch{
        Write-Warning ".NET version check failed."
        $proceed = Read-Host "Are you sure you want to proceed (Y/N)"
        if($proceed -ne "Y"){
            return $false
        }

    }

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
    if($serviceName -ne $null){
        Write-Host "Stopping Service: $serviceName"
        Stop-Service -Name $serviceName -ErrorAction Stop
    }
    # Stop the GUI if running - we don't want locks on any files
    Write-Host "Stop processes"
    Get-Process -Name DBADash -ErrorAction Ignore | Where-Object { $_.Path -like (Get-Location).Path + "*" } | Stop-Process -Force
    Get-Process -Name DBADashServiceConfigTool -ErrorAction Ignore | Where-Object { $_.Path -like (Get-Location).Path + "*" } | Stop-Process -Force
    Get-Process -Name DBADashConfig -ErrorAction Ignore | Where-Object { $_.Path -like (Get-Location).Path + "*" } | Stop-Process -Force
    # Service should already be stopped but this will ensure the process isn't running.
    Get-Process -Name DBADashService -ErrorAction Ignore | Where-Object { $_.Path -like (Get-Location).Path + "*" } | Stop-Process -Force

    # Wait for file locks to be released
    Start-Sleep -Seconds 2

    # If clean option is used, remove existing files from the installation folder.
    if($Clean){
        Write-Host "Clean folder"
        Get-ChildItem (Get-Location) -Recurse -Exclude "ServiceConfig.json","Log","*.ps1","*.json","*.xml",".json.*",$zip | Remove-Item -Recurse
    }
    Write-Host "Extract Files from: $zip"
    "Upgrade Started " + (Get-Date).ToString() | Out-File $upgradeFile
    ExpandWithRetry -ZipFile $zip -RetryCount 3 -WaitBetweenRetry 5
    Remove-Item $upgradeFile # Item is removed on successful file extract

    # If service exists, start it back up
    if($serviceName -ne $null){
        Write-Host "Starting Service. This can take some time while the repository database is upgraded..."
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

    Write-Host "Remove installation files"
    Get-ChildItem -Path "." -Filter "DBADash*.zip" | Remove-Item -Force
    Write-Host "Upgrade Completed" -ForegroundColor DarkGreen
}
else{
    Write-Host "DBA Dash version is up to date." -ForegroundColor DarkGreen
}
