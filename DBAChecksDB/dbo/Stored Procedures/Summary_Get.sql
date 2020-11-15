CREATE PROC [dbo].[Summary_Get](@InstanceIDs VARCHAR(MAX)=NULL)
AS
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;

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
	SELECT InstanceID,MIN(FreeSpaceStatus) AS FileFreeSpaceStatus
	FROM dbo.DBFileStatus
	WHERE FreeSpaceStatus<>3
	GROUP BY InstanceID
),
J AS (
	SELECT InstanceID,MIN(JobStatus) AS JobStatus
	FROM dbo.AgentJobStatus
	WHERE JobStatus<>3
	AND enabled=1
	GROUP BY InstanceID
)
,ag AS (
	SELECT D.InstanceID, MIN(hadr.synchronization_health) AS synchronization_health
	FROM dbo.DatabasesHADR hadr
	JOIN dbo.Databases D ON D.DatabaseID = hadr.DatabaseID
	GROUP BY D.InstanceID
),
dc AS (
	SELECT I.InstanceID,MAX(c.UpdateDate) AS DetectedCorruptionDate
	FROM dbo.Instances I
	JOIN dbo.Databases D ON D.InstanceID = I.InstanceID
	JOIN dbo.Corruption c ON D.DatabaseID = c.DatabaseID
	WHERE I.IsActive=1
	AND D.IsActive=1
	GROUP BY I.InstanceID
),
err AS (
	SELECT InstanceID,ErrorSource,COUNT(*) cnt,MAX(ErrorDate) AS LastError
	FROM dbo.CollectionErrorLog
	WHERE ErrorDate>=DATEADD(d,-2,GETUTCDATE())
	GROUP BY InstanceID,ErrorSource
),
errSummary AS(
	SELECT err.InstanceID, SUM(err.cnt) cnt,MIN(x.SucceedAfterErrorCount) AS SucceedAfterErrorCount
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
		MIN(CASE WHEN Status=3 THEN NULL ELSE Status END) AS CollectionDatesStatus,
		MAX(SnapshotAge) AS SnapshotAgeMax,
		MIN(SnapshotAge) AS SnapshotAgeMin,
		MIN(SnapshotDate) AS OldestSnapshot
	FROM dbo.CollectionDatesStatus
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
		MAX(CASE WHEN IsCriticalAlert=1 THEN last_occurrence_utc ELSE NULL END) AS LastCritical
	FROM dbo.sysalerts
	GROUP BY InstanceID
)
SELECT I.InstanceID,
	I.Instance,
	ISNULL(LS.LogShippingStatus,3) AS LogShippingStatus,
	ISNULL(B.FullBackupStatus,3) AS FullBackupStatus,
	ISNULL(B.LogBackupStatus,3) AS LogBackupStatus,
	ISNULL(B.DiffBackupStatus,3) AS DiffBackupStatus,
	ISNULL(D.DriveStatus,3) AS DriveStatus,
	ISNULL(F.FileFreeSpaceStatus,3) AS FileFreeSpaceStatus,
	ISNULL(J.JobStatus,3) AS JobStatus,
	CASE ag.synchronization_health WHEN 0 THEN 1 WHEN 1 THEN 2 WHEN 2 THEN 4 ELSE 3 END AS AGStatus,
	dc.DetectedCorruptionDate,
	CASE WHEN dc.DetectedCorruptionDate IS NULL THEN 4 
		WHEN DATEDIFF(d,dc.DetectedCorruptionDate,GETUTCDATE())<14 THEN 1
		WHEN DATEDIFF(d,dc.DetectedCorruptionDate,GETUTCDATE())<30 THEN 2
		ELSE 3 END AS CorruptionStatus,
	CASE WHEN  errSummary.SucceedAfterErrorCount=0 THEN 1 WHEN errSummary.cnt>0 THEN 2 ELSE 4 END AS CollectionErrorStatus,
	SSD.SnapshotAgeMin,
	SSD.SnapshotAgeMax,
	SSD.CollectionDatesStatus as SnapshotAgeStatus,
	DATEADD(mi,I.UtcOffSet,I.sqlserver_start_time) AS sqlserver_start_time_utc,
	I.UTCOffset,
	DATEDIFF(mi,DATEADD(mi,I.UtcOffSet,I.sqlserver_start_time),OSInfoCD.SnapshotDate) AS sqlserver_uptime,
	CASE WHEN DATEDIFF(mi,DATEADD(mi,I.UtcOffSet,I.sqlserver_start_time),OSInfoCD.SnapshotDate)<UTT.UptimeCriticalThreshold THEN 1
		WHEN DATEDIFF(mi,DATEADD(mi,I.UtcOffSet,I.sqlserver_start_time),OSInfoCD.SnapshotDate)<UTT.UptimeWarningThreshold THEN 2
		WHEN UTT.UptimeWarningThreshold IS NULL AND UTT.UptimeCriticalThreshold IS NULL THEN 3
		ELSE 4 END AS UptimeStatus,
	UTT.UptimeWarningThreshold,
	UTT.UptimeCriticalThreshold,
	UTT.UptimeConfiguredLevel,
	DATEDIFF(mi,OSInfoCD.SnapshotDate,GETUTCDATE()) AS AdditionalUptime,
	I.ms_ticks/60000 AS host_uptime,
	DATEADD(s,-I.ms_ticks/1000,OSInfoCD.SnapshotDate) AS host_start_time_utc,
	I.MemoryDumpCount,
	I.LastMemoryDump,
	CASE WHEN I.MemoryDumpCount=0 THEN 4 
		WHEN I.MemoryDumpCount IS NULL THEN 3 
		WHEN DATEDIFF(d,I.LastMemoryDump,GETUTCDATE()) < 2 THEN 1 
		WHEN DATEDIFF(d,I.LastMemoryDump,GETUTCDATE()) < 14 THEN 2
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
	CASE WHEN a.LastAlert IS NULL THEN 3
		WHEN DATEDIFF(hh,a.LastCritical,GETUTCDATE())<168 THEN 1
		WHEN DATEDIFF(hh,a.LastAlert,GETUTCDATE())<72 THEN 2
		ELSE 4 END AS AlertStatus,
	AlertCD.SnapshotDate AS AlertSnapshotDate,
	I.IsAgentRunning
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
WHERE EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
AND I.IsActive=1
AND I.EditionID<>1674378470 -- exclude azure 
OPTION(MAX_GRANT_PERCENT=0.1)