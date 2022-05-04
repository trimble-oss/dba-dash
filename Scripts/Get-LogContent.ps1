<#
.SYNOPSIS
    Get the content of the files in the DBA Dash Log folder.
.DESCRIPTION
    Output the contents of the log files.  Option to throw an error if files contain error log entries.  Script created for use in GitHub workflow.
.PARAMETER LogPath
    DBA Dash Log folder path
.PARAMETER ThrowError
    Option to throw an error if files contain error log entries
.EXAMPLE
    ./Get-LogContent -LogPath  "C:\DBADashService\Logs"
.EXAMPLE
    ./Get-LogContent -LogPath  "C:\DBADashService\Logs" -ThrowError
#>
Param(
    [Parameter(Mandatory=$true)]
    [string]$LogPath,
    [switch]$ThrowError
)
$Log = Get-ChildItem -Path $LogPath | Get-Content
$Log

if ($ThrowError){
    $Errors = $Log | Where-Object {$_ -like "*``[ERR]*"}
    if($Errors.Count -gt 0){
        throw $Errors
    }
}