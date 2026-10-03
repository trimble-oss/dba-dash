<#
.SYNOPSIS
    Runs a few queries slow enough for the SlowQueries collection to capture.

.DESCRIPTION
    Written for the CI workflows.  The SlowQueries collection reads an extended events session, so a workflow
    that wants to prove the collection works end to end has to run something over the threshold first.

    Each round runs one batch and one RPC, so both events the capture sessions record - sql_batch_completed and
    rpc_completed - are exercised.  Both wait for longer than the threshold, and both connect with
    $ApplicationName so a test can find exactly these rows in dbo.SlowQueries.

    Needs a SqlClient it can drive a parameterised command with, which Invoke-Sqlcmd does not give.  Windows
    PowerShell 5.1 has one in the box; PowerShell 7 needs the SqlServer module.

.PARAMETER SessionName
    Wait for one of these extended events sessions to be running before running anything.  DBA Dash creates the
    capture session on the first SlowQueries collection rather than at install, so a query run before that has
    nowhere to be recorded.  Comma separated: the ring buffer mode alternates between DBADash_1 and DBADash_2 in
    dual session mode, so either one running will do.

.EXAMPLE
    ./New-TestSlowQuery -SessionName "DBADash_SlowQueries"

.EXAMPLE
    ./New-TestSlowQuery -SessionName "DBADash_1,DBADash_2" -ConnectionString "Data Source=localhost;UID=sa;pwd=x"
#>
Param(
    [string]$ServerInstance = "LOCALHOST",
    # Named so a test can assert the rows carried it through to dbo.SlowQueries.client_app_name.
    [string]$ApplicationName = "DBADash CI Slow Query",
    # Overrides $ServerInstance.  For a workflow leg whose SQL login is not the one running the script.
    [string]$ConnectionString,
    [string]$SessionName = "DBADash_1,DBADash_2",
    [int]$SessionTimeoutSeconds = 240,
    # Comfortably over the 1000ms threshold the workflows configure.
    [int]$DelayMs = 1500,
    [int]$Rounds = 3
)

$ErrorActionPreference = "Stop"

# Windows PowerShell has System.Data in the box.  PowerShell 7 does not, but the SqlServer module brings
# Microsoft.Data.SqlClient with it.  Either will do - this only uses members both have.
$sqlConnectionType = 'System.Data.SqlClient.SqlConnection' -as [type]
if (-not $sqlConnectionType) {
    Import-Module SqlServer -ErrorAction Stop
    $sqlConnectionType = 'Microsoft.Data.SqlClient.SqlConnection' -as [type]
}
if (-not $sqlConnectionType) {
    throw "No SqlClient available.  Run under Windows PowerShell 5.1, or install the SqlServer module."
}

# Connection string keywords ignore case and internal spaces - compared that way so a caller's spelling is
# recognised and replaced rather than duplicated.  See New-TestDeadlock.ps1 for why this is built as text.
function Get-KeywordName {
    Param([string]$Pair)

    return (($Pair.Split("=", 2)[0]) -replace "\s", "").ToLowerInvariant()
}

function Get-TestConnectionString {
    $base = if ($ConnectionString) { $ConnectionString } else { "Data Source=$ServerInstance;Integrated Security=True" }

    $set = [ordered]@{
        "Initial Catalog"        = "master"
        "Application Name"       = $ApplicationName
        "Encrypt"                = "True"
        "TrustServerCertificate" = "True"
    }
    $replaced = @($set.Keys | ForEach-Object { Get-KeywordName -Pair $_ })
    $keep = @($base.Split(";") |
        Where-Object { $_.Trim() } |
        Where-Object { $replaced -notcontains (Get-KeywordName -Pair $_) })
    $added = @($set.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" })
    return (($keep + $added) -join ";")
}

function Wait-ForXESession {
    Param([string[]]$Names, [int]$TimeoutSeconds)

    $list = ($Names | ForEach-Object { "N'$($_.Replace("'", "''"))'" }) -join ","
    Write-Host "Waiting up to $TimeoutSeconds seconds for one of the extended events sessions $($Names -join ', ') to be running"
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ($true) {
        $command = $connection.CreateCommand()
        try {
            $command.CommandText = "SELECT TOP(1) name FROM sys.dm_xe_sessions WHERE name IN ($list)"
            $running = $command.ExecuteScalar()
        }
        finally {
            $command.Dispose()
        }
        if ($running) {
            Write-Host "Session '$running' is running"
            return
        }
        if ((Get-Date) -ge $deadline) {
            throw "None of the extended events sessions $($Names -join ', ') was running after $TimeoutSeconds seconds.  DBA Dash creates the session on the first SlowQueries collection - check that slow query capture is enabled for the connection and the collection has run."
        }
        Start-Sleep -Seconds 5
    }
}

$delay = [TimeSpan]::FromMilliseconds($DelayMs).ToString("hh\:mm\:ss\.fff")
$connection = $sqlConnectionType::new((Get-TestConnectionString))
$connection.Open()
try {
    $names = @($SessionName.Split(",") | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    if ($names.Count -gt 0) {
        Wait-ForXESession -Names $names -TimeoutSeconds $SessionTimeoutSeconds
    }

    for ($round = 1; $round -le $Rounds; $round++) {
        # A batch: no parameters, so SqlClient sends it as a language event - sql_batch_completed.
        $command = $connection.CreateCommand()
        try {
            $command.CommandText = "/* DBADash CI slow batch */ WAITFOR DELAY '$delay';"
            $command.CommandTimeout = 60
            [void]$command.ExecuteNonQuery()
        }
        finally {
            $command.Dispose()
        }

        # An RPC: a parameter makes SqlClient send it through sp_executesql - rpc_completed.
        $command = $connection.CreateCommand()
        try {
            $command.CommandText = "/* DBADash CI slow rpc */ WAITFOR DELAY @delay;"
            $command.CommandTimeout = 60
            $parameter = $command.CreateParameter()
            $parameter.ParameterName = "@delay"
            $parameter.Value = $delay
            [void]$command.Parameters.Add($parameter)
            [void]$command.ExecuteNonQuery()
        }
        finally {
            $command.Dispose()
        }
        Write-Host "Round $round of $Rounds ran a slow batch and a slow RPC ($DelayMs ms each)"
    }
}
finally {
    $connection.Dispose()
}
