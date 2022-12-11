CREATE PROC dbo.Summary_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@IncludeHidden BIT=0
)
AS
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

WITH LS AS (
	SELECT InstanceID,MIN(Status) AS LogShippingStatus
	FROM dbo.LogShippingStatus
	WHERE Status<>3
	GROUP BY InstanceID
)
, B AS (
	SELECT InstanceID,
			MIN(NULLIF(FullBackupStatus,3)) AS FullBackupStatus,
			MIN(NULLIF(LogBackupStatus,3)) AS LogBackupStatus,
			MIN(NULLIF(DiffBackupStatus,3)) AS DiffBackupStatus
	FROM dbo.BackupStatus
	GROUP BY InstanceID
)
, D AS (
	SELECT InstanceID, MIN(Status) AS DriveStatus
	FROM dbo.DriveStatus
	WHERE Status<>3
	GROUP BY InstanceID
),
 F AS (
	SELECT InstanceID,
		MIN(CASE WHEN data_space_id <> 0 THEN NULLIF(FreeSpaceStatus,3) ELSE NULL END) AS FileFreeSpaceStatus,
		MIN(CASE WHEN data_space_id = 0 THEN NULLIF(FreeSpaceStatus,3) ELSE NULL END) AS LogFreeSpaceStatus,
		MIN(NULLIF(PctMaxSizeStatus,3)) AS PctMaxSizeStatus
	FROM dbo.FilegroupStatus
	GROUP BY InstanceID
),
J AS (
	SELECT InstanceID,
		MIN(JobStatus) AS JobStatus
	FROM dbo.AgentJobStatus
	WHERE JobStatus<>3
	AND enabled=1
	GROUP BY InstanceID
)
,ag AS (
	SELECT D.InstanceID, 
		MIN(hadr.synchronization_health) AS synchronization_health
	FROM dbo.DatabasesHADR hadr
	JOIN dbo.Databases D ON D.DatabaseID = hadr.DatabaseID
	GROUP BY D.InstanceID
),
dc AS (
	SELECT I.InstanceID,
		MAX(c.UpdateDate) AS DetectedCorruptionDate
	FROM dbo.Instances I
	JOIN dbo.Databases D ON D.InstanceID = I.InstanceID
	JOIN dbo.Corruption c ON D.DatabaseID = c.DatabaseID
	WHERE I.IsActive=1
	AND D.IsActive=1
	GROUP BY I.InstanceID
),
err AS (
	SELECT InstanceID,
		ErrorSource,
		COUNT(*) cnt,
		MAX(ErrorDate) AS LastError
	FROM dbo.CollectionErrorLog
	WHERE ErrorDate>=@ErrorsFrom
	AND ErrorContext NOT LIKE '%[[]Retrying]'
	GROUP BY InstanceID,ErrorSource
),
errSummary AS(
	SELECT err.InstanceID, 
		SUM(err.cnt) CollectionErrorCount,
		MIN(x.SucceedAfterErrorCount) AS SucceedAfterErrorCount
	FROM err
	CROSS APPLY(SELECT COUNT(*) SucceedAfterErrorCount
				FROM dbo.CollectionDates CD 
				WHERE CD.InstanceID = err.InstanceID 
				AND CD.Reference = err.ErrorSource
				AND CD.SnapshotDate>err.LastError
				) x
	GROUP BY err.InstanceID
),
SSD AS (
	SELECT InstanceID,
		MIN(Status) AS CollectionDatesStatus,
		MAX(SnapshotAge) AS SnapshotAgeMax,
		MIN(SnapshotAge) AS SnapshotAgeMin,
		MIN(SnapshotDate) AS OldestSnapshot
	FROM dbo.CollectionDatesStatus
	WHERE Status<>3
	GROUP BY InstanceID
),
dbc AS (
	SELECT InstanceID,
		MIN(CASE WHEN Status = 3 THEN NULL ELSE Status END) AS LastGoodCheckDBStatus,
		SUM(CASE WHEN Status=1 THEN 1 ELSE 0 END) AS LastGoodCheckDBCriticalCount,
		SUM(CASE WHEN Status=2 THEN 1 ELSE 0 END) AS LastGoodCheckDBWarningCount,
		SUM(CASE WHEN Status=4 THEN 1 ELSE 0 END) AS LastGoodCheckDBHealthyCount,
		SUM(CASE WHEN Status=3 OR Status IS NULL THEN 1 ELSE 0 END) AS LastGoodCheckDBNACount,
		MIN(CASE WHEN Status=3 THEN NULL ELSE LastGoodCheckDbTime END) AS OldestLastGoodCheckDBTime,
		DATEDIFF(d,NULLIF(MIN(CASE WHEN Status <> 3 THEN LastGoodCheckDbTime ELSE NULL END),'1900-01-01'),GETUTCDATE()) AS DaysSinceLastGoodCheckDB
	FROM dbo.LastGoodCheckDB
	GROUP BY InstanceID
),
a AS(
	SELECT InstanceID,
		MAX(last_occurrence_utc) AS LastAlert,
		SUM(occurrence_count) AS TotalAlerts,
		MAX(CASE WHEN IsCriticalAlert=1 THEN last_occurrence_utc ELSE NULL END) AS LastCritical,
		COUNT(*) ConfiguredAlertCount
	FROM dbo.SysAlerts
	GROUP BY InstanceID
)
,cus AS (
	SELECT cc.InstanceID, 
		MIN(cc.Status) AS Status
	FROM dbo.CustomChecks cc
	WHERE Status <> 3
	GROUP BY cc.InstanceID
)
, dbm AS (
	SELECT	DM.InstanceID,
		MIN(CASE WHEN DM.mirroring_state IN(4,6) AND DM.mirroring_witness_state IN(0,1) THEN 4 WHEN DM.mirroring_state IN(2,4,6) THEN 2 ELSE 1 END) AS MirroringStatus
	FROM dbo.DatabaseMirroring DM 
	GROUP BY DM.InstanceID
),
QS AS (
	SELECT D.InstanceID,
		MIN(CASE WHEN QS.actual_state=3 THEN 1 
				WHEN QS.readonly_reason NOT IN(0,1,8) THEN 2
				WHEN QS.actual_state=2 THEN 4
				ELSE NULL END) AS QueryStoreStatus
	FROM dbo.DatabaseQueryStoreOptions QS
	JOIN dbo.Databases D ON QS.DatabaseID = D.DatabaseID
	GROUP BY D.InstanceID
),
Ident AS (
	/* Get max % used for each instance */
	SELECT	InstanceID,
			MIN(NULLIF(IdentityStatus,3)) AS IdentityStatus,
			MAX(pct_used) AS MaxIdentityPctUsed
	FROM dbo.IdentityColumnsInfo
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
)
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
	dc.DetectedCorruptionDate,
	CASE WHEN dc.DetectedCorruptionDate IS NULL THEN 4 
		WHEN DATEDIFF(d,dc.DetectedCorruptionDate,GETUTCDATE())<14 THEN 1
		WHEN DATEDIFF(d,dc.DetectedCorruptionDate,GETUTCDATE())<30 THEN 2
		ELSE 3 END AS CorruptionStatus,
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
	CASE WHEN a.ConfiguredAlertCount=0 THEN 3
		WHEN DATEDIFF(hh,a.LastCritical,GETUTCDATE())<168 THEN 1
		WHEN DATEDIFF(hh,a.LastAlert,GETUTCDATE())<72 THEN 2
		ELSE 4 END AS AlertStatus,
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
	I.ShowInSummary
FROM dbo.Instances I 
LEFT JOIN LS ON I.InstanceID = LS.InstanceID
LEFT JOIN B ON I.InstanceID = B.InstanceID
LEFT JOIN D ON I.InstanceID = D.InstanceID
LEFT JOIN F ON I.InstanceID = F.InstanceID
LEFT JOIN J ON I.InstanceID = J.InstanceID
LEFT JOIN ag ON I.InstanceID= ag.InstanceID
LEFT JOIN dc ON I.InstanceID = dc.InstanceID
LEFT JOIN errSummary ON I.InstanceID = errSummary.InstanceID
LEFT JOIN SSD ON I.InstanceID = SSD.InstanceID
LEFT JOIN dbc ON I.InstanceID = dbc.InstanceID
LEFT JOIN a ON I.InstanceID = a.InstanceID 
LEFT JOIN dbo.CollectionDates OSInfoCD ON OSInfoCD.InstanceID = I.InstanceID AND OSInfoCD.Reference='OSInfo'
LEFT JOIN dbo.CollectionDates AlertCD ON AlertCD.InstanceID = I.InstanceID AND AlertCD.Reference='Alerts'
OUTER APPLY(SELECT TOP(1) IUT.WarningThreshold AS UptimeWarningThreshold,
                          IUT.CriticalThreshold AS UptimeCriticalThreshold,
						  CASE WHEN IUT.InstanceID =-1 THEN 'Root' ELSE 'Instance' END AS UptimeConfiguredLevel
			FROM dbo.InstanceUptimeThresholds IUT
			WHERE (IUT.InstanceID = I.InstanceID OR IUT.InstanceID=-1) 
			ORDER BY IUT.InstanceID DESC) UTT		
LEFT JOIN cus ON cus.InstanceID = I.InstanceID
LEFT JOIN dbm ON dbm.InstanceID = I.InstanceID
LEFT JOIN QS ON QS.InstanceID = I.InstanceID
LEFT JOIN Ident ON Ident.InstanceID = I.InstanceID
WHERE EXISTS(SELECT 1 FROM #Instances t WHERE I.InstanceID = t.InstanceID)
AND I.IsActive=1
AND I.EngineEdition<> 5 -- not azure
AND (I.ShowInSummary=1 OR @IncludeHidden=1)
UNION ALL
SELECT NULL AS InstanceID,
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
	NULL AS DetectedCorruptionDate,
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
	CAST(MAX(CAST(I.ShowInSummary AS TINYINT)) AS BIT) AS ShowInSummary
FROM dbo.Instances I
LEFT JOIN errSummary  ON I.InstanceID = errSummary.InstanceID
LEFT JOIN F ON I.InstanceID = F.InstanceID
LEFT JOIN SSD ON I.InstanceID = SSD.InstanceID
LEFT JOIN cus ON cus.InstanceID = I.InstanceID
LEFT JOIN dbo.AzureDBElasticPoolStorageStatus EPS ON I.InstanceID = EPS.InstanceID
LEFT JOIN QS ON QS.InstanceID = I.InstanceID
LEFT JOIN Ident ON Ident.InstanceID = I.InstanceID
WHERE I.EngineEdition=5 -- azure
AND EXISTS(SELECT 1 FROM #Instances t WHERE I.InstanceID = t.InstanceID)
AND I.IsActive=1
GROUP BY I.Instance,I.InstanceGroupName
HAVING (MAX(CAST(I.ShowInSummary AS TINYINT)) = 1 OR @IncludeHidden=1)