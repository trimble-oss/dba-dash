<#
.SYNOPSIS
    Set the destination connection string for the DBA Dash repository DB. 
.DESCRIPTION
    Set the destination connection string for the DBA Dash repository DB.  If the config file doesn't exist, a new config file will be created
    Note: Changes to the config file require a service restart
.PARAMETER ConnectionString
    The connection string for the DBA Dash repository database including the database name for the repository DB (Initial Catalog)
.PARAMETER BackupConfig
    Keep a copy of the old config file before saving
.EXAMPLE
    ./Set-DBADashDestination -ConnectionString "Data Source=MYSERVER;Integrated Security=SSPI;Initial Catalog=DBADashDB"
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
    #Create new config
    $config = New-Object DBADash.CollectionConfig
}
else{
    # Read the config file
    $configJson = Get-Content -Path $configPath

    # Convert json to CollectionConfig object
    $config = [DBADash.CollectionConfig]::Deserialize($configJson)
}

$config.Destination = $ConnectionString
if($config.DestinationConnection.Type -eq "SQL"){
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $ConnectionString
    if($builder.InitialCatalog -eq "" -or $builder.InitialCatalog -eq "master"){
        throw "Please specify a database name for the DBA Dash repository database (Initial Catalog)"   
    }    
}

if($BackupConfig -and (Test-Path -Path $configPath)){
    $backupPath = $configPath + ".backup_" + [DateTime]::Now.ToString("yyyyMMddHHmmssFFF")
    "Saving old config to: " + $backupPath
    [System.IO.File]::Move($configPath, $backupPath);
}

"Writing new config"
$config.Serialize() | Out-File -FilePath $configPath  