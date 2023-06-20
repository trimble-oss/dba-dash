CREATE PROC dbo.Summary_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@ShowHidden BIT=1,
	@GroupAzure BIT=1,
	@ForceRefresh BIT = 0,
	@SummaryCacheDurationSec INT=NULL
)
AS
SET NOCOUNT ON
SET XACT_ABORT ON
CREATE TABLE #Instances(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO #Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO #Instances
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;

IF @SummaryCacheDurationSec IS NULL
BEGIN
	SELECT @SummaryCacheDurationSec = CAST(SettingValue AS INT)
	FROM dbo.Settings
	WHERE SettingName = 'SummaryCacheDurationSec'
END

IF @SummaryCacheDurationSec>0
BEGIN
	

	IF ISNULL((SELECT TOP(1) RefreshDate 
		FROM dbo.Summary
		),'19000101')  < DATEADD(s,-@SummaryCacheDurationSec,GETUTCDATE()) OR @ForceRefresh=1
	BEGIN
		BEGIN TRAN
		IF ISNULL((SELECT TOP(1) RefreshDate 
			FROM dbo.Summary WITH(UPDLOCK,TABLOCK)
			),'19000101')  < DATEADD(s,-@SummaryCacheDurationSec,GETUTCDATE()) OR @ForceRefresh=1
		BEGIN
			PRINT 'Refreshing summary'	
			DELETE dbo.Summary
			INSERT INTO dbo.Summary
			(
				InstanceID,
				Instance,
				InstanceGroupName,
				LogShippingStatus,
				FullBackupStatus,
				LogBackupStatus,
				DiffBackupStatus,
				DriveStatus,
				FileFreeSpaceStatus,
				LogFreeSpaceStatus,
				JobStatus,
				AGStatus,
				DetectedCorruptionDateUtc,
				CorruptionStatus,
				CollectionErrorStatus,
				CollectionErrorCount,
				SnapshotAgeMin,
				SnapshotAgeMax,
				SnapshotAgeStatus,
				sqlserver_start_time_utc,
				UTCOffset,
				sqlserver_uptime,
				UptimeStatus,
				UptimeWarningThreshold,
				UptimeCriticalThreshold,
				UptimeConfiguredLevel,
				AdditionalUptime,
				host_uptime,
				host_start_time_utc,
				MemoryDumpCount,
				LastMemoryDump,
				LastMemoryDumpUTC,
				MemoryDumpStatus,
				LastGoodCheckDBStatus,
				LastGoodCheckDBCriticalCount,
				LastGoodCheckDBWarningCount,
				LastGoodCheckDBHealthyCount,
				LastGoodCheckDBNACount,
				OldestLastGoodCheckDBTime,
				DaysSinceLastGoodCheckDB,
				LastAlert,
				LastCritical,
				TotalAlerts,
				AlertStatus,
				AlertSnapshotDate,
				IsAgentRunning,
				IsAgentRunningStatus,
				CustomCheckStatus,
				MirroringStatus,
				ElasticPoolStorageStatus,
				PctMaxSizeStatus,
				QueryStoreStatus,
				DBMailStatus,
				DBMailStatusDescription,
				IdentityStatus,
				MaxIdentityPctUsed,
				ShowInSummary,
				IsHidden,
				DatabaseStateStatus,
				RefreshDate,
				IsAzure
			)
			EXEC dbo.Summary_Get @InstanceIDs = NULL,
								 @ShowHidden = 1,
								 @GroupAzure=0,
								 @SummaryCacheDurationSec=0		
		END
		COMMIT
	END
	SELECT 	InstanceID,
			Instance,
			InstanceGroupName,
			LogShippingStatus,
			FullBackupStatus,
			LogBackupStatus,
			DiffBackupStatus,
			DriveStatus,
			FileFreeSpaceStatus,
			LogFreeSpaceStatus,
			JobStatus,
			AGStatus,
			DetectedCorruptionDateUtc,
			CorruptionStatus,
			CollectionErrorStatus,
			CollectionErrorCount,
			SnapshotAgeMin,
			SnapshotAgeMax,
			SnapshotAgeStatus,
			sqlserver_start_time_utc,
			UTCOffset,
			sqlserver_uptime,
			UptimeStatus,
			UptimeWarningThreshold,
			UptimeCriticalThreshold,
			UptimeConfiguredLevel,
			AdditionalUptime,
			host_uptime,
			host_start_time_utc,
			MemoryDumpCount,
			LastMemoryDump,
			LastMemoryDumpUTC,
			MemoryDumpStatus,
			LastGoodCheckDBStatus,
			LastGoodCheckDBCriticalCount,
			LastGoodCheckDBWarningCount,
			LastGoodCheckDBHealthyCount,
			LastGoodCheckDBNACount,
			OldestLastGoodCheckDBTime,
			DaysSinceLastGoodCheckDB,
			LastAlert,
			LastCritical,
			TotalAlerts,
			AlertStatus,
			AlertSnapshotDate,
			IsAgentRunning,
			IsAgentRunningStatus,
			CustomCheckStatus,
			MirroringStatus,
			ElasticPoolStorageStatus,
			PctMaxSizeStatus,
			QueryStoreStatus,
			DBMailStatus,
			DBMailStatusDescription,
			IdentityStatus,
			MaxIdentityPctUsed,
			ShowInSummary,
			IsHidden,
			DatabaseStateStatus,
			RefreshDate,
			CAST(0 AS BIT) AS IsAzure
		FROM dbo.Summary S
		WHERE EXISTS	(	
						SELECT 1 
						FROM #Instances T 
						WHERE T.InstanceID = S.InstanceID
						)
		AND IsAzure=0
		AND (S.ShowInSummary=1 OR @ShowHidden=1)
		UNION ALL
		SELECT 	NULL AS InstanceID,
				Instance,
				InstanceGroupName,
				3 AS LogShippingStatus,
				3 AS FullBackupStatus,
				3 AS LogBackupStatus,
				3 AS DiffBackupStatus,
				3 AS DriveStatus,
				ISNULL(MIN(NULLIF(S.FileFreeSpaceStatus,3)),3) AS FileFreeSpaceStatus,
				ISNULL(MIN(NULLIF(S.LogFreeSpaceStatus,3)),3) AS LogFreeSpaceStatus,
				3 AS JobStatus,
				3 AS AGStatus,
				NULL AS DetectedCorruptionDateUtc,
				3 AS CorruptionStatus,
				ISNULL(MIN(NULLIF(S.CollectionErrorStatus,3)),3) AS CollectionErrorStatus,
				ISNULL(SUM(S.CollectionErrorCount),0) AS CollectionErrorCount,
				MIN(S.SnapshotAgeMin) AS SnapshotAgeMin,
				MAX(S.SnapshotAgeMax) AS SnapshotAgeMax,
				ISNULL(MIN(NULLIF(S.SnapshotAgeStatus,3)),3) AS SnapshotAgeStatus,
				NULL AS sqlserver_start_time_utc,
				0 AS UTCOffset,
				NULL AS sqlserver_uptime,
				3 AS UptimeStatus,
				NULL AS UptimeWarningThreshold,
				NULL AS UptimeCriticalThreshold,
				NULL AS UptimeConfiguredLevel,
				NULL AS AdditionalUptime,
				NULL AS host_uptime,
				NULL AS	host_start_time_utc,
				NULL AS MemoryDumpCount,
				NULL AS LastMemoryDump,
				NULL AS LastMemoryDumpUTC,
				3 AS MemoryDumpStatus,
				3 AS  LastGoodCheckDBStatus,
				NULL AS LastGoodCheckDBCriticalCount,
				NULL AS LastGoodCheckDBWarningCount,
				NULL AS LastGoodCheckDBHealthyCount,
				NULL AS LastGoodCheckDBNACount,
				NULL AS OldestLastGoodCheckDBTime,
				NULL AS DaysSinceLastGoodCheckDB,
				NULL AS LastAlert,
				NULL AS LastCritical,
				NULL AS TotalAlerts,
				3 AlertStatus,
				NULL AS AlertSnapshotDate,
				NULL AS IsAgentRunning,
				3 AS IsAgentRunningStatus,
				ISNULL(MIN(NULLIF(S.CustomCheckStatus,3)),3) AS CustomCheckStatus,
				3 AS MirroringStatus,
				ISNULL(MIN(NULLIF(S.ElasticPoolStorageStatus,3)),3)  AS ElasticPoolStorageStatus,
				ISNULL(MIN(NULLIF(S.PctMaxSizeStatus,3)),3) AS PctMaxSizeStatus,
				ISNULL(MIN(NULLIF(S.QueryStoreStatus,3)),3) AS QueryStoreStatus,
				3 AS DBMailStatus,
				NULL AS DBMailStatusDescription,
				ISNULL(MIN(NULLIF(S.IdentityStatus,3)),3) IdentityStatus,
				MAX(S.MaxIdentityPctUsed) AS MaxIdentityPctUsed,
				CAST(MAX(CAST(S.ShowInSummary AS TINYINT)) AS BIT) AS ShowInSummary,
				~CAST(MAX(CAST(S.ShowInSummary AS TINYINT)) AS BIT) AS IsHidden,
				ISNULL(MIN(S.DatabaseStateStatus),4) AS DatabaseStateStatus,
				MIN(S.RefreshDate) AS RefreshDate,
				CAST(1 AS BIT) AS IsAzure
		FROM dbo.Summary S
		WHERE EXISTS	(	
						SELECT 1 
						FROM #Instances T 
						WHERE T.InstanceID = S.InstanceID
						)
		AND IsAzure=1
		AND (S.ShowInSummary=1 OR @ShowHidden=1)
		GROUP BY Instance,InstanceGroupName


		RETURN
END

DECLARE @ErrorsFrom DATETIME
DECLARE @MemoryDumpWarningThresholdHrs INT 
DECLARE @MemoryDumpCriticalThresholdHrs INT
DECLARE @MemoryDumpAckDate DATETIME

SELECT @ErrorsFrom=CONVERT(DATETIME,SettingValue)
FROM dbo.Settings 
WHERE SettingName = 'ErrorAckDate';

EXEC dbo.MemoryDumpThresholds_Get @MemoryDumpWarningThresholdHrs=@MemoryDumpWarningThresholdHrs OUT,
									@MemoryDumpCriticalThresholdHrs =@MemoryDumpCriticalThresholdHrs OUT,
									@MemoryDumpAckDate=@MemoryDumpAckDate OUT

SET @ErrorsFrom = CASE WHEN @ErrorsFrom > DATEADD(d,-1,GETUTCDATE()) THEN @ErrorsFrom ELSE DATEADD(d,-1,GETUTCDATE())  END;


SELECT	InstanceID,
		MIN(Status) AS LogShippingStatus
INTO #LogShippingStatus
FROM dbo.LogShippingStatus L
WHERE Status<>3
AND EXISTS	(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = L.InstanceID
			)
GROUP BY InstanceID


SELECT InstanceID,
		MIN(NULLIF(FullBackupStatus,3)) AS FullBackupStatus,
		MIN(NULLIF(LogBackupStatus,3)) AS LogBackupStatus,
		MIN(NULLIF(DiffBackupStatus,3)) AS DiffBackupStatus
INTO #BackupStatus
FROM dbo.BackupStatus BS
WHERE EXISTS(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = BS.InstanceID
			)
GROUP BY InstanceID


SELECT	InstanceID, 
		MIN(Status) AS DriveStatus
INTO #DriveStatus 
FROM dbo.DriveStatus D
WHERE Status<>3
AND EXISTS	(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = D.InstanceID
			)
GROUP BY InstanceID

SELECT InstanceID,
	MIN(CASE WHEN data_space_id <> 0 THEN NULLIF(FreeSpaceStatus,3) ELSE NULL END) AS FileFreeSpaceStatus,
	MIN(CASE WHEN data_space_id = 0 THEN NULLIF(FreeSpaceStatus,3) ELSE NULL END) AS LogFreeSpaceStatus,
	MIN(NULLIF(PctMaxSizeStatus,3)) AS PctMaxSizeStatus
INTO #FileGroupStatus
FROM dbo.FilegroupStatus F
WHERE EXISTS(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = F.InstanceID
			)
GROUP BY InstanceID

SELECT InstanceID,
	MIN(JobStatus) AS JobStatus
INTO #JobStatus
FROM dbo.AgentJobStatus J
WHERE JobStatus<>3
AND enabled=1
AND EXISTS	(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = J.InstanceID
			)
GROUP BY InstanceID


SELECT D.InstanceID, 
	MIN(hadr.synchronization_health) AS synchronization_health
INTO #AGStatus
FROM dbo.DatabasesHADR hadr
JOIN dbo.Databases D ON D.DatabaseID = hadr.DatabaseID
WHERE EXISTS(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = D.InstanceID
			)
GROUP BY D.InstanceID


SELECT I.InstanceID,
	MAX(DATEADD(mi,I.UTCOffset,c.UpdateDate)) AS DetectedCorruptionDateUtc,
	MIN(CASE WHEN c.CountOfRows>=1000 AND c.SourceTable=1 THEN 1 
			WHEN c.AckDate >=DATEADD(mi,I.UTCOffset,c.UpdateDate) THEN 5 ELSE 1 END) AS CorruptionStatus
INTO #CorruptionStatus
FROM dbo.Instances I
JOIN dbo.Databases D ON D.InstanceID = I.InstanceID
JOIN dbo.Corruption c ON D.DatabaseID = c.DatabaseID
WHERE I.IsActive=1
AND D.IsActive=1
AND EXISTS	(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = I.InstanceID
			)
GROUP BY I.InstanceID;

WITH err AS (
	SELECT InstanceID,
		ErrorSource,
		COUNT(*) cnt,
		MAX(ErrorDate) AS LastError
	FROM dbo.CollectionErrorLog E
	WHERE ErrorDate>=@ErrorsFrom
	AND ErrorContext NOT LIKE '%[[]Retrying]'
	AND EXISTS	(	
				SELECT 1 
				FROM #Instances T 
				WHERE T.InstanceID = E.InstanceID
				)
	GROUP BY InstanceID,ErrorSource
)
SELECT err.InstanceID, 
	SUM(err.cnt) CollectionErrorCount,
	MIN(x.SucceedAfterErrorCount) AS SucceedAfterErrorCount
INTO #CollectionErrorStatus
FROM err
CROSS APPLY(SELECT COUNT(*) SucceedAfterErrorCount
			FROM dbo.CollectionDates CD 
			WHERE CD.InstanceID = err.InstanceID 
			AND CD.Reference = err.ErrorSource
			AND CD.SnapshotDate>err.LastError
			) x
GROUP BY err.InstanceID

SELECT InstanceID,
	MIN(Status) AS CollectionDatesStatus,
	MAX(SnapshotAge) AS SnapshotAgeMax,
	MIN(SnapshotAge) AS SnapshotAgeMin,
	MIN(SnapshotDate) AS OldestSnapshot
INTO #CollectionStatus
FROM dbo.CollectionDatesStatus C
WHERE Status<>3
AND EXISTS	(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = C.InstanceID
			)
GROUP BY InstanceID


SELECT InstanceID,
	MIN(CASE WHEN Status = 3 THEN NULL ELSE Status END) AS LastGoodCheckDBStatus,
	SUM(CASE WHEN Status=1 THEN 1 ELSE 0 END) AS LastGoodCheckDBCriticalCount,
	SUM(CASE WHEN Status=2 THEN 1 ELSE 0 END) AS LastGoodCheckDBWarningCount,
	SUM(CASE WHEN Status=4 THEN 1 ELSE 0 END) AS LastGoodCheckDBHealthyCount,
	SUM(CASE WHEN Status=3 OR Status IS NULL THEN 1 ELSE 0 END) AS LastGoodCheckDBNACount,
	MIN(CASE WHEN Status=3 THEN NULL ELSE LastGoodCheckDbTime END) AS OldestLastGoodCheckDBTime,
	DATEDIFF(d,NULLIF(MIN(CASE WHEN Status <> 3 THEN LastGoodCheckDbTime ELSE NULL END),'1900-01-01'),GETUTCDATE()) AS DaysSinceLastGoodCheckDB
INTO #DBCCStatus
FROM dbo.LastGoodCheckDB LG
WHERE EXISTS	(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = LG.InstanceID
			)
GROUP BY InstanceID


SELECT InstanceID,
	MAX(last_occurrence_utc) AS LastAlert,
	SUM(occurrence_count) AS TotalAlerts,
	MAX(CASE WHEN IsCriticalAlert=1 THEN last_occurrence_utc ELSE NULL END) AS LastCritical,
	COUNT(*) ConfiguredAlertCount,
	ISNULL(MIN(NULLIF(AlertStatus,3)),3) AS AlertStatus
INTO #AlertStatus
FROM dbo.SysAlerts A
WHERE EXISTS(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = A.InstanceID
			)
GROUP BY InstanceID


SELECT cc.InstanceID, 
	MIN(cc.Status) AS Status
INTO #CustomChecksStatus
FROM dbo.CustomChecks cc
WHERE Status <> 3
AND EXISTS	(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = cc.InstanceID
			)
GROUP BY cc.InstanceID


SELECT	DM.InstanceID,
	MIN(CASE WHEN DM.mirroring_state IN(4,6) AND DM.mirroring_witness_state IN(0,1) THEN 4 WHEN DM.mirroring_state IN(2,4,6) THEN 2 ELSE 1 END) AS MirroringStatus
INTO #MirroringStatus
FROM dbo.DatabaseMirroring DM 
WHERE EXISTS(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = DM.InstanceID
			)
GROUP BY DM.InstanceID


SELECT D.InstanceID,
	MIN(CASE WHEN QS.actual_state=3 THEN 1 
			WHEN QS.readonly_reason NOT IN(0,1,8) THEN 2
			WHEN QS.actual_state=2 THEN 4
			ELSE NULL END) AS QueryStoreStatus
INTO #QueryStoreStatus
FROM dbo.DatabaseQueryStoreOptions QS
JOIN dbo.Databases D ON QS.DatabaseID = D.DatabaseID
WHERE EXISTS(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = D.InstanceID
			)
GROUP BY D.InstanceID


/* Get max % used for each instance */
SELECT	InstanceID,
		MIN(NULLIF(IdentityStatus,3)) AS IdentityStatus,
		MAX(pct_used) AS MaxIdentityPctUsed
INTO #IdentityStatus
FROM dbo.IdentityColumnsInfo I
WHERE EXISTS(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = I.InstanceID
			)
GROUP BY InstanceID
UNION ALL
/* Show status 4 (OK) if we have collected data for a instance but we don't have rows in IdentityColumns table.  (No identity columns have hit the collection threshold) */
SELECT InstanceID,
		4 AS IdentityStatus,
		NULL AS MaxIdentityPctUsed
FROM dbo.CollectionDatesStatus CD
WHERE Reference='IdentityColumns'
AND Status=4
AND NOT EXISTS(SELECT 1 
				FROM dbo.IdentityColumns IC
				WHERE IC.InstanceID=CD.InstanceID
				)
AND EXISTS	(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = CD.InstanceID
			)


SELECT	InstanceID, 
		1 AS DatabaseStateStatus 
INTO #DatabaseStateStatus
FROM dbo.Databases D
WHERE IsActive = 1
AND state IN(3,4,5) /* Recovery Pending, Suspect, Emergency */
AND EXISTS	(	
			SELECT 1 
			FROM #Instances T 
			WHERE T.InstanceID = D.InstanceID
			)
GROUP BY InstanceID

SELECT I.InstanceID,
	I.Instance,
	I.InstanceGroupName,
	ISNULL(LS.LogShippingStatus,3) AS LogShippingStatus,
	ISNULL(B.FullBackupStatus,3) AS FullBackupStatus,
	ISNULL(B.LogBackupStatus,3) AS LogBackupStatus,
	ISNULL(B.DiffBackupStatus,3) AS DiffBackupStatus,
	ISNULL(D.DriveStatus,3) AS DriveStatus,
	ISNULL(F.FileFreeSpaceStatus,3) AS FileFreeSpaceStatus,
	ISNULL(F.LogFreeSpaceStatus,3) AS LogFreeSpaceStatus,
	ISNULL(J.JobStatus,3) AS JobStatus,
	CASE ag.synchronization_health WHEN 0 THEN 1 WHEN 1 THEN 2 WHEN 2 THEN 4 ELSE 3 END AS AGStatus,
	dc.DetectedCorruptionDateUtc,
	ISNULL(CorruptionStatus,4) AS CorruptionStatus,
	CASE WHEN  errSummary.SucceedAfterErrorCount=0 THEN 1 WHEN errSummary.CollectionErrorCount>0 THEN 2 ELSE 4 END AS CollectionErrorStatus,
	ISNULL(errSummary.CollectionErrorCount,0) AS CollectionErrorCount, 
	SSD.SnapshotAgeMin,
	SSD.SnapshotAgeMax,
	ISNULL(SSD.CollectionDatesStatus,3) AS SnapshotAgeStatus,
	DATEADD(mi,I.UTCOffset,I.sqlserver_start_time) AS sqlserver_start_time_utc,
	I.UTCOffset,
	DATEDIFF(mi,DATEADD(mi,I.UTCOffset,I.sqlserver_start_time),OSInfoCD.SnapshotDate) AS sqlserver_uptime,
	CASE WHEN UTT.UptimeWarningThreshold IS NULL AND UTT.UptimeCriticalThreshold IS NULL THEN 3
		WHEN I.UptimeAckDate > DATEADD(mi,I.UTCOffset,I.sqlserver_start_time) THEN 4
		WHEN DATEDIFF(mi,DATEADD(mi,I.UTCOffset,I.sqlserver_start_time),OSInfoCD.SnapshotDate)<UTT.UptimeCriticalThreshold THEN 1
		WHEN DATEDIFF(mi,DATEADD(mi,I.UTCOffset,I.sqlserver_start_time),OSInfoCD.SnapshotDate)<UTT.UptimeWarningThreshold THEN 2		
		ELSE 4 END AS UptimeStatus,
	UTT.UptimeWarningThreshold,
	UTT.UptimeCriticalThreshold,
	UTT.UptimeConfiguredLevel,
	DATEDIFF(mi,OSInfoCD.SnapshotDate,GETUTCDATE()) AS AdditionalUptime,
	I.ms_ticks/60000 AS host_uptime,
	DATEADD(s,-I.ms_ticks/1000,OSInfoCD.SnapshotDate) AS host_start_time_utc,
	I.MemoryDumpCount,
	I.LastMemoryDump,
	I.LastMemoryDumpUTC,
	CASE WHEN I.MemoryDumpCount=0 THEN 4 
		WHEN I.MemoryDumpCount IS NULL THEN 3 
		WHEN I.LastMemoryDump < @MemoryDumpAckDate THEN 4
		WHEN DATEDIFF(hh,I.LastMemoryDump,GETUTCDATE()) < @MemoryDumpCriticalThresholdHrs THEN 1 
		WHEN DATEDIFF(hh,I.LastMemoryDump,GETUTCDATE()) < @MemoryDumpWarningThresholdHrs THEN 2
		ELSE 4 END AS MemoryDumpStatus,
    ISNULL(dbc.LastGoodCheckDBStatus,3) LastGoodCheckDBStatus,
    dbc.LastGoodCheckDBCriticalCount,
    dbc.LastGoodCheckDBWarningCount,
    dbc.LastGoodCheckDBHealthyCount,
    dbc.LastGoodCheckDBNACount,
	dbc.OldestLastGoodCheckDBTime,
	dbc.DaysSinceLastGoodCheckDB,
	a.LastAlert,
	a.LastCritical,
	a.TotalAlerts,
	a.AlertStatus,
	AlertCD.SnapshotDate AS AlertSnapshotDate,
	CASE WHEN I.EngineEdition = 4 THEN NULL ELSE I.IsAgentRunning END AS IsAgentRunning,
	CASE WHEN I.EngineEdition = 4 THEN 3 WHEN I.IsAgentRunning = 1 THEN 4 ELSE 1 END AS IsAgentRunningStatus,
	ISNULL(cus.Status,3) AS CustomCheckStatus,
	ISNULL(dbm.MirroringStatus,3) AS MirroringStatus,
	3 AS ElasticPoolStorageStatus,
	ISNULL(F.PctMaxSizeStatus,3) AS PctMaxSizeStatus,
	ISNULL(QS.QueryStoreStatus,3) AS QueryStoreStatus,
	CASE I.DBMailStatus WHEN 'STARTED' THEN 4 WHEN 'STOPPED' THEN 1 ELSE 3 END AS DBMailStatus,
	I.DBMailStatus as DBMailStatusDescription,
	ISNULL(Ident.IdentityStatus,3) AS IdentityStatus,
	Ident.MaxIdentityPctUsed,
	I.ShowInSummary,
	~I.ShowInSummary IsHidden,
	ISNULL(DBState.DatabaseStateStatus,4) AS DatabaseStateStatus,
	GETUTCDATE() AS RefreshDate,
	CAST(0 AS BIT) AS IsAzure
FROM dbo.Instances I 
LEFT JOIN #LogShippingStatus LS ON I.InstanceID = LS.InstanceID
LEFT JOIN #BackupStatus B ON I.InstanceID = B.InstanceID
LEFT JOIN #DriveStatus D ON I.InstanceID = D.InstanceID
LEFT JOIN #FileGroupStatus F ON I.InstanceID = F.InstanceID
LEFT JOIN #JobStatus J ON I.InstanceID = J.InstanceID
LEFT JOIN #AGStatus ag ON I.InstanceID= ag.InstanceID
LEFT JOIN #CorruptionStatus dc ON I.InstanceID = dc.InstanceID
LEFT JOIN #CollectionErrorStatus errSummary ON I.InstanceID = errSummary.InstanceID
LEFT JOIN #CollectionStatus SSD ON I.InstanceID = SSD.InstanceID
LEFT JOIN #DBCCStatus dbc ON I.InstanceID = dbc.InstanceID
LEFT JOIN #AlertStatus a ON I.InstanceID = a.InstanceID 
LEFT JOIN dbo.CollectionDates OSInfoCD ON OSInfoCD.InstanceID = I.InstanceID AND OSInfoCD.Reference='OSInfo'
LEFT JOIN dbo.CollectionDates AlertCD ON AlertCD.InstanceID = I.InstanceID AND AlertCD.Reference='Alerts'
OUTER APPLY(SELECT TOP(1) IUT.WarningThreshold AS UptimeWarningThreshold,
                          IUT.CriticalThreshold AS UptimeCriticalThreshold,
						  CASE WHEN IUT.InstanceID =-1 THEN 'Root' ELSE 'Instance' END AS UptimeConfiguredLevel
			FROM dbo.InstanceUptimeThresholds IUT
			WHERE (IUT.InstanceID = I.InstanceID OR IUT.InstanceID=-1) 
			ORDER BY IUT.InstanceID DESC) UTT		
LEFT JOIN #CustomChecksStatus cus ON cus.InstanceID = I.InstanceID
LEFT JOIN #MirroringStatus dbm ON dbm.InstanceID = I.InstanceID
LEFT JOIN #QueryStoreStatus QS ON QS.InstanceID = I.InstanceID
LEFT JOIN #IdentityStatus Ident ON Ident.InstanceID = I.InstanceID
LEFT JOIN #DatabaseStateStatus DBState ON I.InstanceID = DBState.InstanceID
WHERE EXISTS(SELECT 1 FROM #Instances t WHERE I.InstanceID = t.InstanceID)
AND I.IsActive=1
AND I.EngineEdition<> 5 -- not azure
AND (I.ShowInSummary=1 OR @ShowHidden=1)
UNION ALL
SELECT CASE WHEN @GroupAzure=1 THEN NULL ELSE I.InstanceID END AS InstanceID,
	I.Instance,
	I.InstanceGroupName,
	3 AS LogShippingStatus,
	3 AS FullBackupStatus,
	3 AS LogBackupStatus,
	3 AS DiffBackupStatus,
	3 AS DriveStatus,
	ISNULL(MIN(NULLIF(F.FileFreeSpaceStatus,3)),3) AS FileFreeSpaceStatus,
	ISNULL(MIN(NULLIF(F.LogFreeSpaceStatus,3)),3) AS LogFreeSpaceStatus,
	3 AS JobStatus,
	3 AS AGStatus,
	NULL AS DetectedCorruptionDateUtc,
	3 AS CorruptionStatus,
	MIN(CASE WHEN  errSummary.SucceedAfterErrorCount=0 THEN 1 WHEN errSummary.CollectionErrorCount>0 THEN 2 ELSE 4 END) AS CollectionErrorStatus,
	ISNULL(SUM(errSummary.CollectionErrorCount),0) AS CollectionErrorCount,
	MIN(SSD.SnapshotAgeMin) AS SnapshotAgeMin,
	MAX(SSD.SnapshotAgeMax) AS SnapshotAgeMax,
	ISNULL(MIN(SSD.CollectionDatesStatus),3) AS SnapshotAgeStatus,
	NULL AS sqlserver_start_time_utc,
	0 AS UTCOffset,
	NULL AS sqlserver_uptime,
	3 AS UptimeStatus,
	NULL AS UptimeWarningThreshold,
	NULL AS UptimeCriticalThreshold,
	NULL AS UptimeConfiguredLevel,
	NULL AS AdditionalUptime,
	NULL AS host_uptime,
	NULL AS	host_start_time_utc,
	NULL AS MemoryDumpCount,
	NULL AS LastMemoryDump,
	NULL AS LastMemoryDumpUTC,
	3 AS MemoryDumpStatus,
    3 AS  LastGoodCheckDBStatus,
    NULL AS LastGoodCheckDBCriticalCount,
    NULL AS LastGoodCheckDBWarningCount,
    NULL AS LastGoodCheckDBHealthyCount,
    NULL AS LastGoodCheckDBNACount,
	NULL AS OldestLastGoodCheckDBTime,
	NULL AS DaysSinceLastGoodCheckDB,
	NULL AS LastAlert,
	NULL AS LastCritical,
	NULL AS TotalAlerts,
	3 AlertStatus,
	NULL AS AlertSnapshotDate,
	NULL AS IsAgentRunning,
	3 AS IsAgentRunningStatus,
	ISNULL(MIN(cus.Status),3) AS CustomCheckStatus,
	3 AS MirroringStatus,
	ISNULL(MIN(NULLIF(EPS.ElasticPoolStorageStatus,3)),3) AS ElasticPoolStorageStatus,
	ISNULL(MIN(NULLIF(F.PctMaxSizeStatus,3)),3) AS PctMaxSizeStatus,
	ISNULL(MIN(QS.QueryStoreStatus),3) AS QueryStoreStatus,
	3 AS DBMailStatus,
	NULL AS DBMailStatusDescription,
	ISNULL(MIN(NULLIF(Ident.IdentityStatus,3)),3) IdentityStatus,
	MAX(Ident.MaxIdentityPctUsed) AS MaxIdentityPctUsed,
	CAST(MAX(CAST(I.ShowInSummary AS TINYINT)) AS BIT) AS ShowInSummary,
	~CAST(MAX(CAST(I.ShowInSummary AS TINYINT)) AS BIT) AS IsHidden,
	ISNULL(MIN(DBState.DatabaseStateStatus),4) AS DatabaseStateStatus,
	GETUTCDATE() AS RefreshDate,
	CAST(1 AS BIT) AS IsAzure
FROM dbo.Instances I
LEFT JOIN #CollectionErrorStatus errSummary  ON I.InstanceID = errSummary.InstanceID
LEFT JOIN #FileGroupStatus F ON I.InstanceID = F.InstanceID
LEFT JOIN #CollectionStatus SSD ON I.InstanceID = SSD.InstanceID
LEFT JOIN #CustomChecksStatus cus ON cus.InstanceID = I.InstanceID
LEFT JOIN dbo.AzureDBElasticPoolStorageStatus EPS ON I.InstanceID = EPS.InstanceID
LEFT JOIN #QueryStoreStatus QS ON QS.InstanceID = I.InstanceID
LEFT JOIN #IdentityStatus Ident ON Ident.InstanceID = I.InstanceID
LEFT JOIN #DatabaseStateStatus DBState ON I.InstanceID = DBState.InstanceID
WHERE I.EngineEdition=5 -- azure
AND EXISTS(SELECT 1 FROM #Instances t WHERE I.InstanceID = t.InstanceID)
AND I.IsActive=1
GROUP BY CASE WHEN @GroupAzure=1 THEN NULL ELSE I.InstanceID END,I.Instance,I.InstanceGroupName
HAVING (MAX(CAST(I.ShowInSummary AS TINYINT)) = 1 OR @ShowHidden=1)