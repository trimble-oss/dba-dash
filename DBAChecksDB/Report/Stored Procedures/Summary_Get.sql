CREATE PROC [Report].[Summary_Get](@InstanceIDs VARCHAR(MAX)=NULL)
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
	SELECT Item
	FROM dbo.SplitStrings(@InstanceIDs,',')
END;

WITH LS AS (
	SELECT InstanceID,MIN(Status) AS LogShippingStatus
	FROM LogShippingStatus
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
	SELECT InstanceID,COUNT(*) cnt,MAX(ErrorDate) AS LastError
	FROM dbo.CollectionErrorLog
	WHERE ErrorDate>=DATEADD(d,-3,GETUTCDATE())
	GROUP BY InstanceID
),
SSD AS (
	SELECT InstanceID,
		MIN(DATEDIFF(mi,SnapshotDate,GETUTCDATE())) AS SnapshotAgeMin,
		MAX(DATEDIFF(mi,SnapshotDate,GETUTCDATE())) AS SnapshotAgeMax,
		MIN(SnapshotDate) AS OldestSnapshot
	FROM dbo.CollectionDates
	GROUP BY InstanceID
)
SELECT I.InstanceID,
	I.Instance,
	STUFF((SELECT ',' + T.Tag FROM dbo.InstanceTag IT JOIN dbo.Tags T ON T.TagID = IT.TagID WHERE IT.InstanceID = I.InstanceID AND IT.TagID <> -1 ORDER BY T.Tag FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'') AS Tags,
	ISNULL(LS.LogShippingStatus,3) AS LogShippingStatus,
	ISNULL(B.FullBackupStatus,3) AS FullBackupStatus,
	ISNULL(B.LogBackupStatus,3) AS LogBackupStatus,
	ISNULL(B.DiffBackupStatus,3) AS DiffBackupStatus,
	ISNULL(D.DriveStatus,3) AS DriveStatus,
	ISNULL(F.FileFreeSpaceStatus,3) AS FileFreeSpaceStatus,
	ISNULL(J.JobStatus,3) AS JobStatus,
	CASE ag.synchronization_health WHEN 0 THEN 1 WHEN 1 THEN 2 WHEN 2 THEN 4 ELSE 3 END AS AGStatus,
	dc.DetectedCorruptionDate,
	CASE WHEN err.LastError >= SSD.OldestSnapshot THEN 1 WHEN err.cnt>0 THEN 2 ELSE 4 END AS CollectionErrorStatus,
	SSD.SnapshotAgeMin,
	SSD.SnapshotAgeMax,
	DATEADD(mi,I.UtcOffSet,I.sqlserver_start_time) AS sqlserver_start_time_utc,
	I.UTCOffset,
	DATEDIFF(mi,DATEADD(mi,I.UtcOffSet,I.sqlserver_start_time),OSInfoCD.SnapshotDate) AS sqlserver_uptime,
	I.ms_ticks/60000 AS host_uptime,
	DATEADD(s,-I.ms_ticks/1000,OSInfoCD.SnapshotDate) AS host_start_time_utc
FROM dbo.Instances I 
LEFT JOIN LS ON I.InstanceID = LS.InstanceID
LEFT JOIN B ON I.InstanceID = B.InstanceID
LEFT JOIN D ON I.InstanceID = D.InstanceID
LEFT JOIN F ON I.InstanceID = F.InstanceID
LEFT JOIN J ON I.InstanceID = J.InstanceID
LEFT JOIN ag ON I.InstanceID= ag.InstanceID
LEFT JOIN dc ON I.InstanceID = dc.InstanceID
LEFT JOIN err ON I.InstanceID = err.InstanceID
LEFT JOIN SSD ON I.InstanceID = SSD.InstanceID
LEFT JOIN dbo.CollectionDates OSInfoCD ON OSInfoCD.InstanceID = I.InstanceID AND OSInfoCD.Reference='OSInfo'
WHERE EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
AND I.IsActive=1