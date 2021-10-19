<#
.SYNOPSIS
    Remove a connection from the DBADash config
.DESCRIPTION
    Remove a connection string from the ServiceConfig.json file.  
    Note: Changes to the config file require a service restart
.PARAMETER ConnectionString
    The connection string for the SQL Instance you want to remove from the config file
.PARAMETER BackupConfig
    Keep a copy of the old config file before saving
.EXAMPLE
    ./Remove-DBADashSource -ConnectionString "Data Source=MYSERVER;Integrated Security=SSPI;"
#>  
Param(
    [Parameter(Mandatory=$true)]
    [string]$ConnectionString,
    [bool]$BackupConfig=$true
)
$configPath = [System.IO.Path]::Combine((Get-Location),"ServiceConfig.json")
# Load DLLs to help us work with the config file
$dllsToLoad = "DBADashTools.dll","Microsoft.SqlServer.Smo.dll","Newtonsoft.Json.dll" 
$dllsToLoad | ForEach-Object {
    $path = [System.IO.Path]::Combine((Get-Location),$_)
    [Reflection.Assembly]::LoadFile($path) | Out-Null
}
if((Test-Path -Path $configPath) -eq $false){
    throw "ServiceConfig.json not found:" + $configPath
}

# Read the config file
$configJson = Get-Content -Path $configPath

# Convert json to CollectionConfig object
$config = [DBADash.CollectionConfig]::Deserialize($configJson)
"Current Connection Count: " + $config.SourceConnections.Count

$connection = $config.GetSourceFromConnectionString($ConnectionString)
if($connection -eq $null){
    "Connection not found"
    return;
}
$config.SourceConnections.Remove($connection) | Out-Null
    
"New Connection Count: " + $config.SourceConnections.Count

if($BackupConfig){
    $backupPath = $configPath + ".backup_" + [DateTime]::Now.ToString("yyyyMMddHHmmssFFF")
    "Saving old config to: " + $backupPath
    [System.IO.File]::Move($configPath, $backupPath);
}

"Writing new config"
$config.Serialize() | Out-File -FilePath $configPath  