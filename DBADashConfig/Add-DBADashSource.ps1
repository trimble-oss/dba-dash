<#
.SYNOPSIS
    Add a new connection to the DBADash config
.DESCRIPTION
    Add a new connection string to the ServiceConfig.json file.  
    Note: Changes to the config file require a service restart
.PARAMETER ConnectionString
    The connection string for the SQL Instance you want to monitor.  e.g. "Data Source=MYSERVER;Integrated Security=SSPI;"
.PARAMETER NoWMI
    Don't collect any data via Windows Management Instruction (WMI). All data is collected via SQL queries. (WMI allows us to collect data for ALL drives and some other info we might not be able to get via SQL.  Service account needs permissions for WMI though)
.PARAMETER SlowQueryThresholdMs
    Set to -1 to disable extended event capture of rpc/batch completed events.  Set to 1000 to capture queries that take longer than 1second to run (or other value in milliseconds as required)
.PARAMETER PlanCollectionEnabled
    Set this switch to enable plan collection.  
.PARAMETER PlanCollectionCountThreshold
    Collect plan if we have >=$PlanCollectionCountThreshold running queries with the same plan
.PARAMETER PlanCollectionCPUThreshold
    Collect plan if CPU usage is higher than the specified threshold (ms)
.PARAMETER PlanCollectionDurationThreshold
    Collect plan if duration is higher than the specified threshold (ms)
.PARAMETER PlanCollectionMemoryGrantThreshold
    Collect plan if memory grant is higher than the specified threshold (in pages)
.PARAMETER SchemaSnapshotDBs
    Comma-separated list of databases to include in a schema snapshot.  Use "*" to snapshot all databases.
.PARAMETER SkipValidation
    Option to skip the validation check on the source connection string
.PARAMETER BackupConfig
    Keep a copy of the old config file before saving
.PARAMETER Replace
    Option to replace the existing connection if it already exists
.EXAMPLE
    ./Add-DBADashSource -ConnectionString "Data Source=MYSERVER;Integrated Security=SSPI;"
.EXAMPLE
    ./Add-DBADashSource -ConnectionString "Data Source=MYSERVER;Integrated Security=SSPI;" -SlowQueryThresholdMs 1000 -PlanCollectionEnabled

#>
Param(
    [Parameter(Mandatory=$true)]
    [string]$ConnectionString,
    [switch]$NoWMI,
    [int]$SlowQueryThresholdMs=-1,
    [int]$SlowQuerySessionMaxMemoryKB=4096,
    [switch]$PlanCollectionEnabled,
    [int]$PlanCollectionCountThreshold=2,
    [int]$PlanCollectionCPUThreshold=1000,
    [int]$PlanCollectionDurationThreshold=10000,
    [int]$PlanCollectionMemoryGrantThreshold=6400,
    [string]$SchemaSnapshotDBs="",
    [switch]$SkipValidation,
    [bool]$BackupConfig=$true,
    [switch]$Replace,
    [switch]$RestartService,
    [bool]$CollectSessionWaits=$true
)
if(!$SchemaSnapshotDBs){
    $SchemaSnapshotDBs="<null>"
}

$command ="./DBADashConfig -a `"Add`" "
$command+="-c `"$ConnectionString`" "
$command+="--SlowQueryThresholdMs $SlowQueryThresholdMs "
$command+="--SlowQuerySessionMaxMemoryKB $SlowQuerySessionMaxMemoryKB "
$command+="--PlanCollectionCountThreshold $PlanCollectionCountThreshold "
$command+="--PlanCollectionCPUThreshold $PlanCollectionCPUThreshold "
$command+="--PlanCollectionDurationThreshold $PlanCollectionDurationThreshold "
$command+="--PlanCollectionMemoryGrantThreshold $PlanCollectionMemoryGrantThreshold "
$command+="--SchemaSnapshotDBs `"$SchemaSnapshotDBs`" "
if ($PlanCollectionEnabled.IsPresent){
    $command+="--PlanCollectionEnabled "
}   
if($SkipValidation.IsPresent){
    $command+="--SkipValidation "
}
if(!$CollectSessionWaits){
    $command+="--NoCollectSessionWaits "
}
if(!$BackupConfig){
    $command+="--NoBackupConfig "
} 
if($NoWMI.IsPresent){
    $command+="--NoWMI "
}
if($Replace.IsPresent){
    $command+="--Replace "

}               
         
Invoke-Expression $command

if($RestartService.IsPresent){
    $ServiceName = ./DBADashConfig -a "GetServiceName"
    "Restarting service $ServiceName..."
    Restart-Service -Name $ServiceName
}


