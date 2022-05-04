<#
.SYNOPSIS
    Output a list of tables with count of rows in each table.
.DESCRIPTION
    Output a list of tables with count of rows in each table.  Script created for use in GitHub workflow.
.PARAMETER ServerInstance
    Name of the SQL Server Instance
.PARAMETER Database
    Name of the database
.EXAMPLE
    ./Get-TableCounts -ServerInstance "LOCALHOST" -Database "DBADashDB 
#>
Param(
    [Parameter(Mandatory=$true)]
    [string]$ServerInstance,
    [Parameter(Mandatory=$true)]
    [string]$Database
)
Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -Query `
"SELECT   S.name + '.' + O.name AS TableName
      , SUM(P.Rows) AS CountOfRows
FROM sys.objects AS O 
JOIN sys.partitions AS P ON O.object_id = P.object_id
JOIN sys.schemas S ON O.schema_id = S.schema_id
WHERE O.type = 'U' AND O.is_ms_shipped = 0
AND index_id < 2 /* 0=Heap, 1=Clustered */
GROUP BY  S.name , O.name
ORDER BY [TableName]"