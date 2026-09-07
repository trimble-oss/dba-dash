param(
    [string]$Database = "DBADashDB_GitHubAction",
	[string]$Server = "LOCALHOST",
	# Only the leg that enables the default perfmon counters asserts they were collected.
	[bool]$Perfmon = $false,
	# Set by the leg whose connection is configured with --NoWMI, which has no WMI data to assert.
	[bool]$NoWMI = $false,
	# Only a leg that enabled deadlock capture and then made the instance deadlock asserts the
	# collection.  Scripts\New-TestDeadlock.ps1 is what creates the database below and deadlocks in it.
	[bool]$Deadlocks = $false,
	[string]$DeadlockDatabase = "DBADashDeadlockTest",
	[string]$DeadlockClientApp = "DBADash CI Deadlock"
)

# Get SQL Server version at the script level
$params = @{
    ServerInstance = $Server
    Database = $Database
}

$versionQuery = "SELECT SERVERPROPERTY('ProductVersion') AS Version, SERVERPROPERTY('ProductMajorVersion') AS MajorVersion"
$versionResult = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query $versionQuery
$majorVersion = [int]$versionResult.MajorVersion
$isSql2016OrLower = $majorVersion -le 13

Write-Host "Detected SQL Server Major Version: $majorVersion" -ForegroundColor Yellow
Write-Host "Is SQL Server 2016 or lower: $isSql2016OrLower" -ForegroundColor Yellow

# Build the test cases array dynamically based on SQL version
$TableCountGreaterThanZeroTestCases = @(
      @{TableName="dbo.AgentJobThresholds"}
      @{TableName="dbo.AzureDBElasticPoolStorageThresholds"}
      @{TableName="dbo.BackupThresholds"}
      @{TableName="dbo.CollectionDates"}
      @{TableName="dbo.CounterMapping"}
      @{TableName="dbo.Counters"}
      @{TableName="dbo.CPU"}
      @{TableName="dbo.CPU_60MIN"}
      @{TableName="dbo.DatabasePermissions"}
      @{TableName="dbo.DatabasePrincipals"}
      @{TableName="dbo.DatabaseQueryStoreOptions"}
      @{TableName="dbo.DatabaseRoleMembers"}
      @{TableName="dbo.Databases"}
      @{TableName="dbo.DataRetention"}
      @{TableName="dbo.DBADashAgent"}
      @{TableName="dbo.DBConfig"}
      @{TableName="dbo.DBConfigOptions"}
      @{TableName="dbo.DBFiles"}
      @{TableName="dbo.DBFileSnapshot"}
      @{TableName="dbo.DBFileThresholds"}
      @{TableName="dbo.DBIOStats"}
      @{TableName="dbo.DBIOStats_60MIN"}
      @{TableName="dbo.DBObjects"}
      @{TableName="dbo.DBVersionHistory"}
      @{TableName="dbo.DDL"}
	  @{TableName="dbo.DDLHistory"}
	  @{TableName="dbo.DDLSnapshotOptions"}
	  @{TableName="dbo.DDLSnapshots"}
	  @{TableName="dbo.DDLSnapshotsLog"}
      @{TableName="dbo.Drives"}
      @{TableName="dbo.DriveThresholds"}
      @{TableName="dbo.InstanceCounters"}
      @{TableName="dbo.InstanceIDsTags"}
      @{TableName="dbo.Instances"}
      @{TableName="dbo.InstanceUptimeThresholds"}
      @{TableName="dbo.JobDDLHistory"}
      @{TableName="dbo.Jobs"}
      @{TableName="dbo.JobSteps"}
      @{TableName="dbo.LastGoodCheckDBThresholds"}
      @{TableName="dbo.LogRestoreThresholds"}
      @{TableName="dbo.MemoryClerkType"}
      @{TableName="dbo.MemoryUsage"}
      @{TableName="dbo.ObjectExecutionStats"}
      @{TableName="dbo.ObjectExecutionStats_60MIN"}
      @{TableName="dbo.ObjectType"}
      @{TableName="dbo.OSLoadedModules"}
      @{TableName="dbo.OSLoadedModulesStatus"}
      @{TableName="dbo.PerformanceCounters"}
      @{TableName="dbo.PerformanceCounters_60MIN"}
      @{TableName="dbo.ResourceGovernorConfigurationHistory"}
      @{TableName="dbo.RunningQueriesSummary"}
      @{TableName="dbo.ServerPermissions"}
      @{TableName="dbo.ServerPrincipals"}
      @{TableName="dbo.ServerRoleMembers"}
      @{TableName="dbo.SessionWaits"}
      @{TableName="dbo.Settings"}
      @{TableName="dbo.SysConfig"}
      @{TableName="dbo.SysConfigOptions"}
      @{TableName="dbo.Tags"}
      @{TableName="dbo.Waits"}
      @{TableName="dbo.Waits_60MIN"}
      @{TableName="dbo.WaitType"}
	  @{TableName="dbo.BuildReference"}
	  @{TableName="dbo.IdentityColumns"}
	  @{TableName="dbo.ServerServices"}
)

# Only add SQL Server 2017+ tables if not SQL Server 2016 or lower
if (-not $isSql2016OrLower) {
    Write-Host "Adding SQL Server 2017+ tables to test cases" -ForegroundColor Green
    $TableCountGreaterThanZeroTestCases += @{TableName="dbo.DBTuningOptions"}
    $TableCountGreaterThanZeroTestCases += @{TableName="dbo.DBTuningOptionsHistory"}
} else {
    Write-Host "Excluding SQL Server 2017+ tables (DBTuningOptions, DBTuningOptionsHistory) for SQL Server 2016 or earlier" -ForegroundColor Yellow
}

Describe 'CI Workflow checks' {
    BeforeEach {
            $params = @{
                ServerInstance = $Server
                Database = $Database
            }
        }
    It 'Test Instance Count' {
         $result= Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) AS CountOfInstances FROM dbo.Instances"
         $result.CountOfInstances | Should -Be 1
    }
    It 'Check for errors' {
         $result= Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) AS CountOfErrors FROM dbo.CollectionErrorLog"
         $result.CountOfErrors  | Should -Be 0
         
    }
    It 'Check CPU table count' {
         $results= Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.CPU"
         $results.cnt  | Should -BeGreaterThan 0
    }
   
    It 'Check table counts for <TableName>' -TestCases $TableCountGreaterThanZeroTestCases {
        param($tableName)
             $results= Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM $tableName"
             $results.cnt  | Should -BeGreaterThan 0
         
    }

	$TableCountGreaterThanZeroTestCasesWMI = @(
		@{TableName="dbo.Drivers"}
	)
	It 'Check WMI table counts for <TableName>' -TestCases $TableCountGreaterThanZeroTestCasesWMI -Skip:$NoWMI {
		param($tableName)

			$results= Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM $tableName"

			$results.cnt | Should -BeGreaterThan 0

	}

	# OS-level (perfmon) counters land in the shared dbo.PerformanceCounters table, namespaced with the
	# 'PerfMon:' object_name prefix.  Skipped unless this leg enabled the default counters (-Perfmon).
	It 'Perfmon counters collected' -Skip:(-not $Perfmon) {
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.PerformanceCounters PC JOIN dbo.Counters C ON C.CounterID = PC.CounterID WHERE C.object_name LIKE 'PerfMon:%'"
		$results.cnt | Should -BeGreaterThan 0
	}
	It 'Perfmon Processor % Processor Time collected' -Skip:(-not $Perfmon) {
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.PerformanceCounters PC JOIN dbo.Counters C ON C.CounterID = PC.CounterID WHERE C.object_name = 'PerfMon:Processor' AND C.counter_name = '% Processor Time'"
		$results.cnt | Should -BeGreaterThan 0
	}
	It 'Perfmon Processor % Processor Time is within 0-100' -Skip:(-not $Perfmon) {
		# Proves the raw-delta cooking in PerfmonCounters_Upd produced sane values, not just that rows exist.
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.PerformanceCounters PC JOIN dbo.Counters C ON C.CounterID = PC.CounterID WHERE C.object_name = 'PerfMon:Processor' AND C.counter_name = '% Processor Time' AND (PC.Value < 0 OR PC.Value > 100)"
		$results.cnt | Should -Be 0
	}
	It 'Perfmon counters carry their stable WMI identity' -Skip:(-not $Perfmon) {
		# WmiClass/WmiProperty must be populated so the app can key off the WMI identity, not the display name.
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.Counters WHERE object_name LIKE 'PerfMon:%' AND (WmiClass IS NULL OR WmiProperty IS NULL)"
		$results.cnt | Should -Be 0
	}
	It 'Processor utilization counter is findable by WMI identity (exactly one row)' -Skip:(-not $Perfmon) {
		# The scenario that motivated this: a chart can locate the counter by WMI identity with confidence,
		# and there is exactly one row for it (no fragmentation across display names).
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.Counters WHERE WmiClass = 'Win32_PerfRawData_PerfOS_Processor' AND WmiProperty = 'PercentProcessorTime' AND instance_name = '_Total'"
		$results.cnt | Should -Be 1
	}

	# Deadlock collection.  Skipped unless this leg enabled deadlock capture and produced a deadlock for
	# it to find - see Scripts\New-TestDeadlock.ps1.  These assert the path rather than a row count: the
	# graph is read from an extended events session, shredded in the collector, sent as three table
	# valued parameters and reassembled by dbo.Deadlocks_Upd, and any of those steps can drop a column
	# quietly.
	It 'Deadlock collection ran' -Skip:(-not $Deadlocks) {
		# The collection date advances even when nothing was found, so this separates "no deadlocks" from
		# "the collection never ran".
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.CollectionDates WHERE Reference = 'Deadlocks'"
		$results.cnt | Should -BeGreaterThan 0
	}
	It 'Deadlocks captured' -Skip:(-not $Deadlocks) {
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.Deadlocks"
		$results.cnt | Should -BeGreaterThan 0
	}
	It 'Every deadlock has its participants' -Skip:(-not $Deadlocks) {
		# dbo.Deadlocks_Upd inserts the children only for headers that survived dedup, so a header with no
		# process rows means that filter dropped rows it should have kept.
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.Deadlocks D WHERE NOT EXISTS(SELECT 1 FROM dbo.DeadlockProcesses P WHERE P.InstanceID = D.InstanceID AND P.EventTime = D.EventTime AND P.DeadlockHash = D.DeadlockHash)"
		$results.cnt | Should -Be 0
	}
	It 'Every deadlock names a victim' -Skip:(-not $Deadlocks) {
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.Deadlocks D WHERE NOT EXISTS(SELECT 1 FROM dbo.DeadlockProcesses P WHERE P.InstanceID = D.InstanceID AND P.EventTime = D.EventTime AND P.DeadlockHash = D.DeadlockHash AND P.IsVictim = 1)"
		$results.cnt | Should -Be 0
	}
	It 'Contended resources captured' -Skip:(-not $Deadlocks) {
		# The tables New-TestDeadlock deadlocks over, so this proves the resource-list shredding produced a
		# usable object name rather than only a row.
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.DeadlockResources WHERE ObjectName LIKE '%DeadlockA%' OR ObjectName LIKE '%DeadlockB%'"
		$results.cnt | Should -BeGreaterThan 0
	}
	It 'Deadlock graph stored and decompresses' -Skip:(-not $Deadlocks) {
		# The collector gzips UTF-16 so SQL Server's own DECOMPRESS reads it back.  Reading it that way here
		# is what proves the contract - nothing else in the repository depends on it.
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.DeadlockXml WHERE CAST(DECOMPRESS(DeadlockXmlCompressed) AS NVARCHAR(MAX)) LIKE '%<deadlock%'"
		$results.cnt | Should -BeGreaterThan 0
	}
	It 'Every deadlock kept its graph' -Skip:(-not $Deadlocks) {
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.Deadlocks D WHERE NOT EXISTS(SELECT 1 FROM dbo.DeadlockXml X WHERE X.InstanceID = D.InstanceID AND X.EventTime = D.EventTime AND X.DeadlockHash = D.DeadlockHash)"
		$results.cnt | Should -Be 0
	}
	It 'Deadlock signature populated' -Skip:(-not $Deadlocks) {
		# The signature is what groups recurrences, and it is converted from a hex string to BINARY(8) on
		# import - a conversion that yields NULL rather than failing if the format ever changes.
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.Deadlocks WHERE Signature IS NULL OR SignatureVersion IS NULL"
		$results.cnt | Should -Be 0
	}
	It 'Deadlock event time is a sane UTC value' -Skip:(-not $Deadlocks) {
		# The event envelope carries the only timestamp there is, and it is parsed as UTC.  A local-time
		# reading would land hours out.  The window is wide enough for CI clock skew.
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.Deadlocks WHERE EventTime < DATEADD(HOUR, -2, GETUTCDATE()) OR EventTime > DATEADD(MINUTE, 5, GETUTCDATE())"
		$results.cnt | Should -Be 0
	}
	It 'Client application carried through to the participants' -Skip:(-not $Deadlocks) {
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.DeadlockProcesses WHERE ClientApp = '$DeadlockClientApp'"
		$results.cnt | Should -BeGreaterThan 0
	}
	It 'Deadlocking module captured' -Skip:(-not $Deadlocks) {
		# New-TestDeadlock puts the deadlocking statement inside a procedure, so the execution stack has a
		# module frame to name.  Ad-hoc frames are stored as NULL, so this is the only check that the module
		# path works - and the report groups by it.
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.DeadlockProcesses WHERE ProcedureName LIKE '%usp_DeadlockUpdate%'"
		$results.cnt | Should -BeGreaterThan 0
	}
	It 'Participants resolve to the database that deadlocked' -Skip:(-not $Deadlocks) {
		# The graph carries the source instance's database_id and dbo.Deadlocks_Upd maps it to a DatabaseID.
		# New-TestDeadlock creates its database before the service starts so the mapping has something to
		# find.
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.DeadlockProcesses P JOIN dbo.Databases DB ON DB.DatabaseID = P.DatabaseID WHERE DB.name = '$DeadlockDatabase'"
		$results.cnt | Should -BeGreaterThan 0
	}
	It 'DeadlockGraph_Get returns a graph for a stored deadlock' -Skip:(-not $Deadlocks) {
		# The report grid carries the key rather than the graph and fetches the one that is clicked, so this
		# is the path the viewer takes.
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "DECLARE @InstanceID INT, @EventTime DATETIME2(3), @DeadlockHash BINARY(16); SELECT TOP(1) @InstanceID = InstanceID, @EventTime = EventTime, @DeadlockHash = DeadlockHash FROM dbo.Deadlocks ORDER BY EventTime DESC; EXEC dbo.DeadlockGraph_Get @InstanceID = @InstanceID, @EventTime = @EventTime, @DeadlockHash = @DeadlockHash"
		$results.DeadlockGraph | Should -BeLike '*<deadlock*'
	}
	It 'DeadlockScope returns the collected deadlocks' -Skip:(-not $Deadlocks) {
		# The function both reports read, so a filter that selected nothing would leave both grids and all
		# seven charts empty while every table above still had rows.
		$results = Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "DECLARE @IDs dbo.IDs; INSERT INTO @IDs(ID) SELECT InstanceID FROM dbo.Instances; SELECT COUNT(*) cnt FROM dbo.DeadlockScope(@IDs, NULL, '19000101', '99991231 23:59:59.999', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0)"
		$results.cnt | Should -BeGreaterThan 0
	}

}

Describe 'CI Workflow checks' {
    BeforeEach {
            $params = @{
                ServerInstance = $Server
                Database = $Database
            }
        }
		It 'Check Instance_Del hard delete' {
			$DeleteQuery = "/* !!! WARNING !!!
			This script will hard delete all instances from the repository
			*/
			DECLARE @InstanceID INT
	
			WHILE 1=1
			BEGIN
	
			SELECT TOP(1) @InstanceID = InstanceID 
			FROM dbo.Instances 
			IF @@ROWCOUNT=0
				BREAK
	
			PRINT @InstanceID
	
			/* Perform soft delete */
			EXEC dbo.Instance_Del @InstanceID = @InstanceID,  
								@IsActive = 0, 
								@HardDelete = 0
	
			/* Set snapshot date to allow us to perform hard delete without waiting 24hrs */
			UPDATE dbo.CollectionDates
			SET SnapshotDate = '19000101'
			WHERE InstanceID = @InstanceID
	
			/* Delete the instance */
			EXEC dbo.Instance_Del @InstanceID = @InstanceID,  
								@IsActive = 0, 
								@HardDelete = 1
	
			END"
	
			Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -Query $DeleteQuery -TrustServerCertificate
	
			$results= Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -TrustServerCertificate -Query "SELECT COUNT(*) cnt FROM dbo.Instances"
			$results.cnt | Should -Be 0
	
		}
}