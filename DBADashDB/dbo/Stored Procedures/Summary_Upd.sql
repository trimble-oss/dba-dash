CREATE PROC dbo.Summary_Upd
AS
/* 
	Updates dbo.Summary table used by Summary_Get
*/
SET NOCOUNT ON
SET XACT_ABORT ON

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
GROUP BY InstanceID

SELECT InstanceID,
		MIN(NULLIF(FullBackupStatus,3)) AS FullBackupStatus,
		MIN(NULLIF(LogBackupStatus,3)) AS LogBackupStatus,
		MIN(NULLIF(DiffBackupStatus,3)) AS DiffBackupStatus
INTO #BackupStatus
FROM dbo.BackupStatus BS
GROUP BY InstanceID

SELECT	InstanceID, 
		MIN(Status) AS DriveStatus
INTO #DriveStatus 
FROM dbo.DriveStatus D
WHERE Status<>3
GROUP BY InstanceID

SELECT InstanceID,
	MIN(CASE WHEN data_space_id <> 0 THEN NULLIF(FreeSpaceStatus,3) ELSE NULL END) AS FileFreeSpaceStatus,
	MIN(CASE WHEN data_space_id = 0 THEN NULLIF(FreeSpaceStatus,3) ELSE NULL END) AS LogFreeSpaceStatus,
	MIN(NULLIF(PctMaxSizeStatus,3)) AS PctMaxSizeStatus
INTO #FileGroupStatus
FROM dbo.FilegroupStatus F
GROUP BY InstanceID

SELECT InstanceID,
	MIN(JobStatus) AS JobStatus
INTO #JobStatus
FROM dbo.AgentJobStatus J
WHERE JobStatus<>3
AND enabled=1
GROUP BY InstanceID

SELECT D.InstanceID, 
	MIN(hadr.synchronization_health) AS synchronization_health
INTO #AGStatus
FROM dbo.DatabasesHADR hadr
JOIN dbo.Databases D ON D.DatabaseID = hadr.DatabaseID
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
GROUP BY I.InstanceID;

WITH err AS (
	SELECT InstanceID,
		ErrorSource,
		COUNT(*) cnt,
		MAX(ErrorDate) AS LastError
	FROM dbo.CollectionErrorLog E
	WHERE ErrorDate>=@ErrorsFrom
	AND ErrorContext NOT LIKE '%[[]Retrying]'
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
GROUP BY InstanceID

SELECT InstanceID,
	MAX(last_occurrence_utc) AS LastAlert,
	SUM(occurrence_count) AS TotalAlerts,
	MAX(CASE WHEN IsCriticalAlert=1 THEN last_occurrence_utc ELSE NULL END) AS LastCritical,
	COUNT(*) ConfiguredAlertCount,
	ISNULL(MIN(NULLIF(AlertStatus,3)),3) AS AlertStatus
INTO #AlertStatus
FROM dbo.SysAlerts A
GROUP BY InstanceID

SELECT cc.InstanceID, 
	MIN(cc.Status) AS Status
INTO #CustomChecksStatus
FROM dbo.CustomChecks cc
WHERE Status <> 3
GROUP BY cc.InstanceID

SELECT	DM.InstanceID,
	MIN(CASE WHEN DM.mirroring_state IN(4,6) AND DM.mirroring_witness_state IN(0,1) THEN 4 WHEN DM.mirroring_state IN(2,4,6) THEN 2 ELSE 1 END) AS MirroringStatus
INTO #MirroringStatus
FROM dbo.DatabaseMirroring DM 
GROUP BY DM.InstanceID

SELECT D.InstanceID,
	MIN(CASE WHEN QS.actual_state=3 THEN 1 
			WHEN QS.readonly_reason NOT IN(0,1,8) THEN 2
			WHEN QS.actual_state=2 THEN 4
			ELSE NULL END) AS QueryStoreStatus
INTO #QueryStoreStatus
FROM dbo.DatabaseQueryStoreOptions QS
JOIN dbo.Databases D ON QS.DatabaseID = D.DatabaseID
GROUP BY D.InstanceID

/* Get max % used for each instance */
SELECT	InstanceID,
		MIN(NULLIF(IdentityStatus,3)) AS IdentityStatus,
		MAX(pct_used) AS MaxIdentityPctUsed
INTO #IdentityStatus
FROM dbo.IdentityColumnsInfo I
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

SELECT	InstanceID, 
		1 AS DatabaseStateStatus 
INTO #DatabaseStateStatus
FROM dbo.Databases D
WHERE IsActive = 1
AND state IN(3,4,5) /* Recovery Pending, Suspect, Emergency */
GROUP BY InstanceID

SELECT	EPS.InstanceID,
		MIN(NULLIF(EPS.ElasticPoolStorageStatus,3)) ElasticPoolStorageStatus
INTO #ElasticPoolStatus
FROM dbo.AzureDBElasticPoolStorageStatus EPS
GROUP BY EPS.InstanceID

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
INTO #Summary
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
WHERE I.IsActive=1
AND I.EngineEdition<> 5 -- not azure
UNION ALL
SELECT I.InstanceID,
	I.Instance,
	I.InstanceGroupName,
	3 AS LogShippingStatus,
	3 AS FullBackupStatus,
	3 AS LogBackupStatus,
	3 AS DiffBackupStatus,
	3 AS DriveStatus,
	ISNULL(F.FileFreeSpaceStatus,3) AS FileFreeSpaceStatus,
	ISNULL(F.LogFreeSpaceStatus,3) AS LogFreeSpaceStatus,
	3 AS JobStatus,
	3 AS AGStatus,
	NULL AS DetectedCorruptionDateUtc,
	3 AS CorruptionStatus,
	CASE WHEN  errSummary.SucceedAfterErrorCount=0 THEN 1 WHEN errSummary.CollectionErrorCount>0 THEN 2 ELSE 4 END AS CollectionErrorStatus,
	ISNULL(errSummary.CollectionErrorCount,0) AS CollectionErrorCount,
	SSD.SnapshotAgeMin AS SnapshotAgeMin,
	SSD.SnapshotAgeMax AS SnapshotAgeMax,
	ISNULL(SSD.CollectionDatesStatus,3) AS SnapshotAgeStatus,
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
	ISNULL(cus.Status,3) AS CustomCheckStatus,
	3 AS MirroringStatus,
	ISNULL(EPS.ElasticPoolStorageStatus,3) AS ElasticPoolStorageStatus,
	ISNULL(F.PctMaxSizeStatus,3) AS PctMaxSizeStatus,
	ISNULL(QS.QueryStoreStatus,3) AS QueryStoreStatus,
	3 AS DBMailStatus,
	NULL AS DBMailStatusDescription,
	ISNULL(Ident.IdentityStatus,3) IdentityStatus,
	Ident.MaxIdentityPctUsed AS MaxIdentityPctUsed,
	I.ShowInSummary,
	~I.ShowInSummary AS IsHidden,
	ISNULL(DBState.DatabaseStateStatus,4) AS DatabaseStateStatus,
	GETUTCDATE() AS RefreshDate,
	CAST(1 AS BIT) AS IsAzure
FROM dbo.Instances I
LEFT JOIN #CollectionErrorStatus errSummary  ON I.InstanceID = errSummary.InstanceID
LEFT JOIN #FileGroupStatus F ON I.InstanceID = F.InstanceID
LEFT JOIN #CollectionStatus SSD ON I.InstanceID = SSD.InstanceID
LEFT JOIN #CustomChecksStatus cus ON cus.InstanceID = I.InstanceID
LEFT JOIN #ElasticPoolStatus EPS ON I.InstanceID = EPS.InstanceID
LEFT JOIN #QueryStoreStatus QS ON QS.InstanceID = I.InstanceID
LEFT JOIN #IdentityStatus Ident ON Ident.InstanceID = I.InstanceID
LEFT JOIN #DatabaseStateStatus DBState ON I.InstanceID = DBState.InstanceID
WHERE I.EngineEdition=5 -- azure
AND I.IsActive=1

BEGIN TRAN

DELETE dbo.Summary
INSERT INTO dbo.Summary(
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
SELECT InstanceID,
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
FROM #Summary

COMMIT