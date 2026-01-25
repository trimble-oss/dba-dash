CREATE PROC dbo.Instance_Del(
	@InstanceID INT=NULL,
	@ConnectionID NVARCHAR(128)=NULL,
	@IsActive BIT=0,
	@HardDelete BIT=0
)
AS
SET NOCOUNT ON
SET XACT_ABORT ON
IF @InstanceID IS NULL AND @ConnectionID IS NULL
BEGIN
	RAISERROR('InstanceID or ConnectionID must be provided',11,1)
	RETURN
END
ELSE IF @InstanceID IS NULL
BEGIN
	SELECT @InstanceID = InstanceID
	FROM dbo.Instances
	WHERE ConnectionID = @ConnectionID

	IF @InstanceID IS NULL
	BEGIN
		RAISERROR('Instance not found',11,1)
		RETURN
	END
END

IF @HardDelete = 0
BEGIN
	UPDATE dbo.Instances
	SET IsActive=@IsActive
	WHERE InstanceID = @InstanceID
END
ELSE
BEGIN
	IF EXISTS(SELECT 1 FROM dbo.Instances WHERE InstanceID = @InstanceID AND IsActive=1)
	BEGIN
		RAISERROR('Mark Instance IsActive=0 before delete',11,1)
		RETURN
	END
	IF EXISTS(
		SELECT * 
		FROM dbo.CollectionDates 
		WHERE InstanceID=@InstanceID
		AND SnapshotDate>=DATEADD(d,-1,GETUTCDATE())
		)
	BEGIN 
		RAISERROR('The instance can''t be deleted at this time.  Please wait 24hrs after removing the collection.',11,1)
		RETURN
	END

	DELETE dbo.AgentJobThresholds
	WHERE InstanceID = @InstanceID

	DELETE dbo.Alerts
	WHERE InstanceID=@InstanceID

	DELETE dbo.AvailabilityGroups
	WHERE InstanceID = @InstanceID

	DELETE dbo.AvailabilityReplicas
	WHERE InstanceID = @InstanceID

	DELETE dbo.AzureDBDTULimitChange
	WHERE InstanceID = @InstanceID

	DELETE EPH 
	FROM dbo.AzureDBElasticPoolHistory EPH
	WHERE EXISTS(SELECT 1 
				FROM dbo.AzureDBElasticPool EP
				WHERE EP.InstanceID = @InstanceID
				AND EP.PoolID = EPH.PoolID
				)

	DELETE EPT
	FROM dbo.AzureDBElasticPoolStorageThresholds EPT
	WHERE EXISTS(SELECT 1 
				FROM dbo.AzureDBElasticPool EP
				WHERE EP.InstanceID = @InstanceID
				AND EP.PoolID = EPT.PoolID
				)
	EXEC dbo.AzureDBElasticPoolResourceStats_Del @InstanceID =@InstanceID,@DaysToKeep=0
	EXEC dbo.AzureDBElasticPoolResourceStats_60MIN_Del @InstanceID =@InstanceID,@DaysToKeep=0

	DELETE dbo.AzureDBResourceGovernanceHistory
	WHERE InstanceID = @InstanceID

	DELETE dbo.AzureDBElasticPool
	WHERE InstanceID= @InstanceID

	EXEC dbo.AzureDBResourceStats_Del @InstanceID =@InstanceID,@DaysToKeep=0
	EXEC dbo.AzureDBResourceStats_60MIN_Del @InstanceID =@InstanceID,@DaysToKeep=0

	DELETE dbo.AzureDBServiceObjectives 
	WHERE InstanceID = @InstanceID

	DELETE dbo.AzureDBServiceObjectivesHistory 
	WHERE InstanceID = @InstanceID

	DELETE B 
	FROM dbo.Backups B
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = B.DatabaseID
				)

	DELETE dbo.BackupThresholds
	WHERE InstanceID = @InstanceID

	EXEC dbo.BlockingSnapshotSummary_Del @InstanceID =@InstanceID,@DaysToKeep=0

	DELETE dbo.CollectionDates
	WHERE InstanceID = @InstanceID

	DELETE dbo.CollectionDatesThresholds
	WHERE InstanceID = @InstanceID

	DELETE dbo.CollectionErrorLog
	WHERE InstanceID= @InstanceID

	DELETE C 
	FROM dbo.Corruption C
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = C.DatabaseID
				)


	EXEC dbo.CPU_Del @InstanceID=@InstanceID,@DaysToKeep=0

	EXEC dbo.CPU_60MIN_Del @InstanceID=@InstanceID,@DaysToKeep=0

	DELETE dbo.CustomChecks
	WHERE InstanceID = @InstanceID

	DELETE dbo.CustomChecksHistory
	WHERE InstanceID= @InstanceID

	DELETE dbo.DatabaseMirroring 
	WHERE InstanceID = @InstanceID

	DELETE DP 
	FROM dbo.DatabasePermissions DP
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = DP.DatabaseID
				)

	DELETE DP 
	FROM dbo.DatabasePrincipals DP
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = DP.DatabaseID
				)

	DELETE DQS
	FROM dbo.DatabaseQueryStoreOptions DQS
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = DQS.DatabaseID
				)

	DELETE DRM 
	FROM dbo.DatabaseRoleMembers DRM
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = DRM.DatabaseID
				)


	DELETE HADR 
	FROM dbo.DatabasesHADR HADR
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = HADR.DatabaseID
				)

	DELETE DC
	FROM dbo.DBConfig DC
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = DC.DatabaseID
				)

	DELETE DCH
	FROM dbo.DBConfigHistory DCH
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = DCH.DatabaseID
				)

	EXEC dbo.DBFileSnapshot_Del @InstanceID=@InstanceID,@DaysToKeep=0

	DELETE dbo.DBFileThresholds
	WHERE InstanceID = @InstanceID

	DELETE F
	FROM dbo.DBFiles F
	WHERE F.InstanceID = @InstanceID

	DELETE DOH
	FROM dbo.DBOptionsHistory DOH
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = DOH.DatabaseID
				)

	DELETE dbo.DBTuningOptions 
	WHERE InstanceID =@InstanceID

	DELETE dbo.DBTuningOptionsHistory
	WHERE InstanceID = @InstanceID


	DELETE DDLH
	FROM dbo.DDLHistory DDLH
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = DDLH.DatabaseID
				)

	DELETE DSS 
	FROM dbo.DDLSnapshots DSS
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = DSS.DatabaseID
				)

	DELETE DSSL 
	FROM dbo.DDLSnapshotsLog DSSL
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = DSSL.DatabaseID
				)

	DELETE dbo.LogRestoreThresholds
	WHERE InstanceID = @InstanceID

	DELETE LR 
	FROM dbo.LogRestores LR
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = LR.DatabaseID
				)

	DELETE dbo.JobSteps
	WHERE InstanceID = @InstanceID

	DELETE dbo.JobHistory 
	WHERE InstanceID = @InstanceID

	DELETE 
	FROM dbo.Jobs
	WHERE InstanceID = @InstanceID

	DELETE dbo.JobDDLHistory
	WHERE InstanceID = @InstanceID

	SELECT DDLID 
	INTO #DDLToDelete
	FROM dbo.DDL 
	WHERE NOT EXISTS(
			SELECT 1 
			FROM dbo.DDLHistory H
			WHERE H.DDLID = DDL.DDLID
			UNION ALL 
			SELECT 1 
			FROM dbo.Jobs J
			WHERE J.DDLID = DDL.DDLID
			UNION ALL 
			SELECT 1
			FROM dbo.JobDDLHistory H
			WHERE H.DDLID = DDL.DDLID
			)

	DELETE D 
	FROM dbo.DDL D
	WHERE EXISTS(
				SELECT 1 
				FROM #DDLToDelete T
				WHERE T.DDLID = D.DDLID
				)


	EXEC dbo.DBIOStats_Del @InstanceID=@InstanceID,@DaysToKeep=0
	EXEC dbo.DBIOStats_60MIN_Del @InstanceID=@InstanceID,@DaysToKeep=0

	DELETE dbo.Drivers WHERE InstanceID = @InstanceID

	DELETE dbo.DriversHistory WHERE InstanceID = @InstanceID 

	EXEC dbo.DriveSnapshot_Del @InstanceID=@InstanceID,@DaysToKeep=0

	DELETE dbo.DriveThresholds 
	WHERE InstanceID = @InstanceID

	DELETE dbo.Drives 
	WHERE InstanceID = @InstanceID

	DELETE dbo.HostUpgradeHistory 
	WHERE InstanceID = @InstanceID

	DELETE dbo.InstanceCounters
	WHERE InstanceID = @InstanceID

	DECLARE @DeletedTags TABLE(
		TagID INT
	)
	
	BEGIN TRAN
	
	DELETE dbo.InstanceIDsTags
	OUTPUT DELETED.TagID INTO @DeletedTags
	WHERE InstanceID = @InstanceID 

	DELETE T
	FROM dbo.Tags T
	WHERE EXISTS(SELECT 1 
				FROM @DeletedTags D
				WHERE D.TagID = T.TagID
				)
	AND NOT EXISTS(SELECT 1 
				FROM dbo.InstanceIDsTags I
				WHERE I.TagID = T.TagID
				)
	COMMIT

	DELETE dbo.InstanceUptimeThresholds
	WHERE InstanceID = @InstanceID

	EXEC dbo.JobStats_60MIN_Del @InstanceID=@InstanceID,@DaysToKeep=0

	DELETE dbo.LastGoodCheckDBThresholds
	WHERE InstanceID = @InstanceID 

	EXEC dbo.MemoryUsage_Del @InstanceID=@InstanceID,@DaysToKeep=0

	EXEC dbo.ObjectExecutionStats_Del @InstanceID=@InstanceID,@DaysToKeep=0

	EXEC dbo.ObjectExecutionStats_60MIN_Del @InstanceID=@InstanceID,@DaysToKeep=0

	DELETE DO
	FROM dbo.DBObjects DO
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Databases D
				WHERE D.InstanceID = @InstanceID
				AND D.DatabaseID = DO.DatabaseID
				)


	DELETE dbo.OSLoadedModules 
	WHERE InstanceID = @InstanceID

	EXEC dbo.PerformanceCounters_Del @InstanceID=@InstanceID,@DaysToKeep=0

	EXEC dbo.PerformanceCounters_60MIN_Del @InstanceID=@InstanceID,@DaysToKeep=0

	DELETE dbo.ResourceGovernorConfigurationHistory
	WHERE InstanceID = @InstanceID

	EXEC dbo.RunningQueries_Del @InstanceID=@InstanceID,@DaysToKeep=0

	EXEC dbo.RunningQueriesSummary_Del @InstanceID=@InstanceID,@DaysToKeep=0

	DELETE dbo.ServerPermissions
	WHERE InstanceID = @InstanceID

	DELETE dbo.ServerPrincipals
	WHERE InstanceID = @InstanceID 

	DELETE dbo.ServerRoleMembers
	WHERE InstanceID = @InstanceID

	EXEC dbo.SessionWaits_Del @InstanceID=@InstanceID,@DaysToKeep=0

	EXEC dbo.SlowQueries_Del @InstanceID=@InstanceID,@DaysToKeep=0

	EXEC dbo.SlowQueriesStats_Del @InstanceID=@InstanceID,@DaysToKeep=0

	DELETE dbo.SQLPatchingHistory WHERE InstanceID = @InstanceID

	DELETE dbo.SysConfig
	WHERE InstanceID = @InstanceID 

	DELETE dbo.SysConfigHistory
	WHERE InstanceID = @InstanceID 

	DELETE dbo.TraceFlagHistory
	WHERE InstanceID = @InstanceID 

	DELETE dbo.TraceFlags
	WHERE InstanceID = @InstanceID

	EXEC dbo.Waits_Del @InstanceID=@InstanceID,@DaysToKeep=0

	EXEC dbo.Waits_60MIN_Del @InstanceID=@InstanceID,@DaysToKeep=0

	DELETE dbo.IdentityColumnsHistory 
	WHERE InstanceID = @InstanceID

	DELETE dbo.IdentityColumns 
	WHERE InstanceID = @InstanceID

	DELETE dbo.Databases 
	WHERE InstanceID = @InstanceID

	DELETE Staging.Waits
	WHERE InstanceID = @InstanceID

	DELETE Staging.ObjectExecutionStats
	WHERE InstanceID = @InstanceID
	
	DELETE Staging.IOStats
	WHERE InstanceID = @InstanceID

	DELETE Staging.PerformanceCounters
	WHERE InstanceID = @InstanceID	

	DELETE dbo.RunningJobs
	WHERE InstanceID = @InstanceID

	DELETE dbo.ServerServices
	WHERE InstanceID = @InstanceID

	DELETE CTK 
	FROM Alert.CustomThreadKey CTK
	WHERE EXISTS(
				SELECT 1 
				FROM Alert.ActiveAlerts AA
				WHERE AA.InstanceID = @InstanceID
				AND AA.AlertID = CTK.AlertID
				)

	DELETE NL 
	FROM Alert.NotificationLog NL
	WHERE EXISTS(
				SELECT 1 
				FROM Alert.ActiveAlerts AA
				WHERE AA.InstanceID = @InstanceID
				AND AA.AlertID = NL.AlertID
				)

	DELETE Alert.ActiveAlerts
	WHERE InstanceID = @InstanceID 

	DELETE Alert.ClosedAlerts 
	WHERE InstanceID = @InstanceID

	DELETE Alert.BlackoutPeriod
	WHERE ApplyToInstanceID = @InstanceID

	DELETE Alert.Rules
	WHERE ApplyToInstanceID = @InstanceID

	DELETE dbo.OfflineInstances
	WHERE InstanceID = @InstanceID

	DELETE dbo.RepositoryMetricsConfig
	WHERE InstanceID = @InstanceID

	DELETE dbo.InstanceMetadata
	WHERE InstanceID = @InstanceID

	IF EXISTS(SELECT 1 
			FROM dbo.InstanceMetadataHistory
			WHERE InstanceID = @InstanceID)
	BEGIN
		/* System versioning needs to be turned off on InstanceMetadata to allow us to remove data from InstanceMetadataHistory */
		BEGIN TRAN
		ALTER TABLE dbo.InstanceMetadata
		SET (SYSTEM_VERSIONING = OFF )

		DECLARE @SQL NVARCHAR(MAX) = N'
		DELETE dbo.InstanceMetadataHistory 
		WHERE InstanceID = @InstanceID'
		EXEC sp_executesql @SQL, N'@InstanceID INT', @InstanceID = @InstanceID

		ALTER TABLE dbo.InstanceMetadata 
		SET ( SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.InstanceMetadataHistory))

		COMMIT
	END
	
	DELETE dbo.Instances
	WHERE InstanceID = @InstanceID

END
