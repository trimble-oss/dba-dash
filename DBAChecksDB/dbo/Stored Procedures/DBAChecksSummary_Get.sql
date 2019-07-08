CREATE PROC [dbo].[DBAChecksSummary_Get]
AS
WITH LS AS (
	SELECT InstanceID,MIN(Status) as LogShippingStatus
	FROM LogShippingStatus
	GROUP BY InstanceID
)
, B as (
	SELECT InstanceID,
			MIN(FullBackupStatus) as FullBackupStatus,
			MIN(LogBackupStatus) as LogBackupStatus,
			MIN(DiffBackupStatus) as DiffBackupStatus
	FROM dbo.BackupStatus
	GROUP BY InstanceID
)
, D AS (
	SELECT InstanceID, MIN(Status) as DriveStatus
	FROM dbo.DriveStatus
	GROUP BY InstanceID
)
SELECT I.InstanceID,
	I.Instance,
	LS.LogShippingStatus,
	B.FullBackupStatus,
	B.LogBackupStatus,
	B.DiffBackupStatus,
	D.DriveStatus
FROM dbo.Instances I 
LEFT JOIN LS ON I.InstanceID = LS.InstanceID
LEFT JOIN B ON I.InstanceID = B.InstanceID
LEFT JOIN D ON I.InstanceID = D.InstanceID
WHERE I.IsActive=1

