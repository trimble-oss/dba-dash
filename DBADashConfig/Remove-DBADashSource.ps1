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
if($BackupConfig){
    ./DBADashConfig -a "Remove" -c $ConnectionString
}
else{
    ./DBADashConfig -a "Remove" -c $ConnectionString --NoBackupConfig
}