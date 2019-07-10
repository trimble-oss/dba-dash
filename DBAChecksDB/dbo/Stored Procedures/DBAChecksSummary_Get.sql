CREATE PROC [dbo].[DBAChecksSummary_Get]
AS
WITH LS AS (
	SELECT InstanceID,MIN(Status) as LogShippingStatus
	FROM LogShippingStatus
	WHERE Status<>3
	GROUP BY InstanceID
)
, B as (
	SELECT InstanceID,
			MIN(NULLIF(FullBackupStatus,3)) as FullBackupStatus,
			MIN(NULLIF(LogBackupStatus,3)) as LogBackupStatus,
			MIN(NULLIF(DiffBackupStatus,3)) as DiffBackupStatus
	FROM dbo.BackupStatus
	GROUP BY InstanceID
)
, D AS (
	SELECT InstanceID, MIN(Status) as DriveStatus
	FROM dbo.DriveStatus
	WHERE Status<>3
	GROUP BY InstanceID
),
 F AS (
	SELECT InstanceID,MIN(FreeSpaceStatus) AS FileFreeSpaceStatus
	FROM dbo.DBFileStatus
	WHERE FreeSpaceStatus<>3
	GROUP BY InstanceID
)
SELECT I.InstanceID,
	I.Instance,
	LS.LogShippingStatus,
	B.FullBackupStatus,
	B.LogBackupStatus,
	B.DiffBackupStatus,
	D.DriveStatus,
	F.FileFreeSpaceStatus
FROM dbo.Instances I 
LEFT JOIN LS ON I.InstanceID = LS.InstanceID
LEFT JOIN B ON I.InstanceID = B.InstanceID
LEFT JOIN D ON I.InstanceID = D.InstanceID
LEFT JOIN F ON I.InstanceID = F.InstanceID
WHERE I.IsActive=1

