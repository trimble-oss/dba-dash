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
.PARAMETER SkipValidation
    Option to skip the validation check on the source connection string
.EXAMPLE
    ./Set-DBADashDestination -ConnectionString "Data Source=MYSERVER;Integrated Security=SSPI;Initial Catalog=DBADashDB"
#>  
Param(
    [Parameter(Mandatory=$true)]
    [string]$ConnectionString,
    [bool]$BackupConfig=$true,
    [switch]$SkipValidation
)

$command ="./DBADashConfig -a `"SetDestination`" "
$command+="-c `"$ConnectionString`" "

if($SkipValidation.IsPresent){
    $command+="--SkipValidation "
}

if(!$BackupConfig){
    $command+="--NoBackupConfig "
} 

Invoke-Expression $command