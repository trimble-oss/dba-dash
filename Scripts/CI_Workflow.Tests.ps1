Describe 'CI Workflow checks' {
    BeforeEach {
            $params = @{
                ServerInstance = "LOCALHOST"
                Database = "DBADashDB_GitHubAction"
            }
        }
    It 'Test Instance Count' {
         $result= Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -Query "SELECT COUNT(*) AS CountOfInstances FROM dbo.Instances"
         $result.CountOfInstances | Should -Be 1
    }
    It 'Check for errors' {
         $results= Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -Query "SELECT * FROM dbo.CollectionErrorLog"
         $results.Count  | Should -Be 0
         
    }
    It 'Check CPU table count' {
         $results= Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database -Query "SELECT COUNT(*) cnt FROM dbo.CPU"
         $results.cnt  | Should -BeGreaterThan 0
    }
   
    $TableCountGreaterThanZeroTestCases = @(
	      @{TableName="dbo.AgentJobThresholds"}
	      @{TableName="dbo.AzureDBElasticPoolStorageThresholds"}
	      @{TableName="dbo.BackupThresholds"}
	      @{TableName="dbo.CollectionDates"}
	      @{TableName="dbo.CollectionDatesThresholds"}
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
	      @{TableName="dbo.DBTuningOptions"}
	      @{TableName="dbo.DBTuningOptionsHistory"}
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
    )
    It 'Check table counts for <TableName>' -TestCases $TableCountGreaterThanZeroTestCases {
        param($tableName)
             $results= Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database  -Query "SELECT COUNT(*) cnt FROM $tableName"
             $results.cnt  | Should -BeGreaterThan 0
         
    }

	$TableCountGreaterThanZeroTestCasesWMI = @(
		@{TableName="dbo.Drivers"}
	)
	It 'Check WMI table counts for <TableName>' -TestCases $TableCountGreaterThanZeroTestCasesWMI -Skip:$NoWMI {
		param($tableName)
			
			$results= Invoke-Sqlcmd -ServerInstance $params.ServerInstance -Database $params.Database  -Query "SELECT COUNT(*) cnt FROM $tableName"
										
			$results.cnt | Should -BeGreaterThan 0
					
	}

}