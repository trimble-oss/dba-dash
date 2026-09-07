<#
.SYNOPSIS
    Produces a real deadlock so the Deadlocks collection has something to capture.

.DESCRIPTION
    Written for the CI workflows.  The Deadlocks collection reads an extended events session, so a workflow
    that wants to prove the collection works end to end has to make the instance deadlock first.

    Two connections take the same two rows in the opposite order.  The second statement of each is inside a
    stored procedure, so the captured graph carries a module name - which is what the report's procedure
    grouping and its execution-stats link are built on.  A graph made only of ad-hoc statements would leave
    both untested.

    SQL Server picks the victim, so either connection can be the one rolled back.  Both are watched for
    error 1205 and the attempt is retried if neither saw one.

    Needs a SqlClient it can drive two concurrent connections with, which Invoke-Sqlcmd does not give.
    Windows PowerShell 5.1 has one in the box; PowerShell 7 needs the SqlServer module.

.PARAMETER SessionName
    Wait for this extended events session to be running before deadlocking.  The session DBA Dash manages is
    created by the first collection rather than by the installer, so a deadlock produced before that has
    nowhere to be recorded.  Pass an empty string to skip the wait - system_health is always running.

.PARAMETER SetupOnly
    Create the database, tables and procedures and stop.  Run this before the DBA Dash service starts so the
    Databases collection sees the test database, and the deadlock's database_id resolves to a DatabaseID on
    import.  Deadlocking itself has to wait until the session exists.

.EXAMPLE
    ./New-TestDeadlock -SetupOnly

.EXAMPLE
    ./New-TestDeadlock -SessionName "DBADash_Deadlocks"

.EXAMPLE
    ./New-TestDeadlock -SessionName "system_health" -ConnectionString "Data Source=localhost;UID=sa;pwd=x"
#>
Param(
    [string]$ServerInstance = "LOCALHOST",
    [string]$Database = "DBADashDeadlockTest",
    # Named so a test can assert the graph carried it through to dbo.DeadlockProcesses.ClientApp.
    [string]$ApplicationName = "DBADash CI Deadlock",
    # Overrides $ServerInstance.  For a workflow leg whose SQL login is not the one running the script.
    [string]$ConnectionString,
    [string]$SessionName = "DBADash_Deadlocks",
    [int]$SessionTimeoutSeconds = 240,
    [int]$Attempts = 5,
    [switch]$SetupOnly
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

# Connection string keywords ignore case and internal spaces, so "InitialCatalog" and "Initial Catalog"
# are the same keyword.  Compared that way so a caller's spelling is recognised and replaced rather than
# duplicated.
function Get-KeywordName {
    Param([string]$Pair)

    return (($Pair.Split("=", 2)[0]) -replace "\s", "").ToLowerInvariant()
}

# Built as text rather than with DbConnectionStringBuilder.  Under PowerShell that type is reached through
# its dictionary adapter, which swallowed every key set on it and handed back the string that went in - the
# tables were created in master and the application name never reached the graph.
function Get-TestConnectionString {
    Param([string]$InitialCatalog)

    $base = if ($ConnectionString) { $ConnectionString } else { "Data Source=$ServerInstance;Integrated Security=True" }

    $set = [ordered]@{
        "Initial Catalog"        = $InitialCatalog
        "Application Name"       = $ApplicationName
        "Encrypt"                = "True"
        "TrustServerCertificate" = "True"
    }
    $replaced = @($set.Keys | ForEach-Object { Get-KeywordName -Pair $_ })

    # A duplicate keyword is not a merge, so whatever the caller said about these four is dropped first.
    $keep = @($base.Split(";") |
        Where-Object { $_.Trim() } |
        Where-Object { $replaced -notcontains (Get-KeywordName -Pair $_) })

    $added = @($set.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" })
    return (($keep + $added) -join ";")
}

function New-TestConnection {
    Param([string]$InitialCatalog)

    $connection = $sqlConnectionType::new((Get-TestConnectionString -InitialCatalog $InitialCatalog))
    $connection.Open()
    return $connection
}

function Invoke-NonQuery {
    Param($Connection, [string]$Sql, [int]$Timeout = 60)

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = $Sql
        $command.CommandTimeout = $Timeout
        [void]$command.ExecuteNonQuery()
    }
    finally {
        $command.Dispose()
    }
}

function Invoke-Scalar {
    Param($Connection, [string]$Sql, [int]$Timeout = 60)

    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = $Sql
        $command.CommandTimeout = $Timeout
        return $command.ExecuteScalar()
    }
    finally {
        $command.Dispose()
    }
}

# 1205 can arrive wrapped - the async path surfaces it inside an AggregateException - so the whole chain is
# walked rather than only the outermost exception.
function Test-DeadlockVictim {
    Param($ErrorRecord)

    $exception = $ErrorRecord.Exception
    while ($exception) {
        $number = $exception.PSObject.Properties['Number']
        if ($number -and $number.Value -eq 1205) { return $true }
        $exception = $exception.InnerException
    }
    return $false
}

function Initialize-DeadlockObjects {
    Write-Host "Creating $Database on $ServerInstance"

    $master = New-TestConnection -InitialCatalog "master"
    try {
        Invoke-NonQuery -Connection $master -Sql "IF DB_ID(N'$Database') IS NULL CREATE DATABASE [$Database];"
    }
    finally {
        $master.Dispose()
    }

    $connection = New-TestConnection -InitialCatalog $Database
    try {
        # Two rows in two tables, and a procedure per table holding the statement that will deadlock.  The
        # graph names the module the deadlocked frame is in, so the procedures are what put a value in
        # dbo.DeadlockProcesses.ProcedureName.
        $setup = @(
            "IF OBJECT_ID(N'dbo.DeadlockA') IS NULL CREATE TABLE dbo.DeadlockA(ID INT NOT NULL PRIMARY KEY, Val INT NOT NULL);",
            "IF OBJECT_ID(N'dbo.DeadlockB') IS NULL CREATE TABLE dbo.DeadlockB(ID INT NOT NULL PRIMARY KEY, Val INT NOT NULL);",
            "IF NOT EXISTS(SELECT 1 FROM dbo.DeadlockA WHERE ID = 1) INSERT INTO dbo.DeadlockA(ID, Val) VALUES(1, 0);",
            "IF NOT EXISTS(SELECT 1 FROM dbo.DeadlockB WHERE ID = 1) INSERT INTO dbo.DeadlockB(ID, Val) VALUES(1, 0);",
            "IF OBJECT_ID(N'dbo.usp_DeadlockUpdateA') IS NULL EXEC(N'CREATE PROC dbo.usp_DeadlockUpdateA AS UPDATE dbo.DeadlockA SET Val = Val + 1 WHERE ID = 1;');",
            "IF OBJECT_ID(N'dbo.usp_DeadlockUpdateB') IS NULL EXEC(N'CREATE PROC dbo.usp_DeadlockUpdateB AS UPDATE dbo.DeadlockB SET Val = Val + 1 WHERE ID = 1;');"
        )
        foreach ($statement in $setup) {
            Invoke-NonQuery -Connection $connection -Sql $statement
        }
    }
    finally {
        $connection.Dispose()
    }

    Write-Host "$Database is ready"
}

function Wait-ForXESession {
    Param([string]$Name, [int]$TimeoutSeconds)

    Write-Host "Waiting up to $TimeoutSeconds seconds for extended events session '$Name' to be running"
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $connection = New-TestConnection -InitialCatalog "master"
    try {
        while ($true) {
            $running = Invoke-Scalar -Connection $connection -Sql "SELECT COUNT(*) FROM sys.dm_xe_sessions WHERE name = N'$Name'"
            if ($running -gt 0) {
                Write-Host "Session '$Name' is running"
                return
            }
            if ((Get-Date) -ge $deadline) {
                throw "Extended events session '$Name' was not running after $TimeoutSeconds seconds.  For the session DBA Dash manages, that means the Deadlocks collection has not run - check its schedule and that deadlock capture is enabled for the connection."
            }
            Start-Sleep -Seconds 5
        }
    }
    finally {
        $connection.Dispose()
    }
}

function Reset-TestTransaction {
    Param($Connection)

    if (-not $Connection) { return }
    try {
        Invoke-NonQuery -Connection $Connection -Sql "IF @@TRANCOUNT > 0 ROLLBACK"
    }
    catch {
        # The victim's transaction is already gone and its connection may be unusable.  Nothing to salvage.
    }
}

function New-Deadlock {
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        Write-Host "Deadlock attempt $attempt of $Attempts"
        $first = $null
        $second = $null
        $deadlocked = $false
        try {
            $first = New-TestConnection -InitialCatalog $Database
            $second = New-TestConnection -InitialCatalog $Database

            # Each takes one row and holds it.  Neither blocks yet.
            Invoke-NonQuery -Connection $first -Sql "BEGIN TRAN; UPDATE dbo.DeadlockA SET Val = Val + 1 WHERE ID = 1;"
            Invoke-NonQuery -Connection $second -Sql "BEGIN TRAN; UPDATE dbo.DeadlockB SET Val = Val + 1 WHERE ID = 1;"

            # The first now asks for the row the second holds.  Issued asynchronously because it blocks.
            $command = $first.CreateCommand()
            $command.CommandText = "EXEC dbo.usp_DeadlockUpdateB"
            $command.CommandTimeout = 120
            $task = $command.ExecuteNonQueryAsync()

            # Give it a moment to reach the lock wait, or the cycle never forms and this is only blocking.
            Start-Sleep -Milliseconds 750

            # The second asks for the row the first holds, closing the cycle.  One of them is now the victim.
            try {
                Invoke-NonQuery -Connection $second -Sql "EXEC dbo.usp_DeadlockUpdateA" -Timeout 120
            }
            catch {
                if (Test-DeadlockVictim -ErrorRecord $_) { $deadlocked = $true } else { throw }
            }

            try {
                if (-not $task.Wait(120000)) {
                    throw "The blocked statement did not finish within 120 seconds."
                }
            }
            catch {
                if (Test-DeadlockVictim -ErrorRecord $_) { $deadlocked = $true } else { throw }
            }
            finally {
                $command.Dispose()
            }
        }
        finally {
            Reset-TestTransaction -Connection $first
            Reset-TestTransaction -Connection $second
            if ($first) { $first.Dispose() }
            if ($second) { $second.Dispose() }
        }

        if ($deadlocked) {
            Write-Host "Deadlock produced on attempt $attempt"
            return
        }

        Write-Host "No deadlock on attempt $attempt - neither session was chosen as a victim.  Retrying."
        Start-Sleep -Seconds 2
    }

    throw "No deadlock was produced after $Attempts attempts."
}

Initialize-DeadlockObjects

if ($SetupOnly) {
    Write-Host "Setup only - not deadlocking."
    return
}

if ($SessionName) {
    Wait-ForXESession -Name $SessionName -TimeoutSeconds $SessionTimeoutSeconds
}

New-Deadlock
