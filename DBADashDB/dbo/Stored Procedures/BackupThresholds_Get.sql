CREATE PROC BackupThresholds_Get(@InstanceID INT =NULL,@DatabaseID INT=NULL)
AS
SELECT BT.InstanceID,
       BT.DatabaseID,
	   CASE WHEN BT.InstanceID=-1 THEN '{ALL}' ELSE I.Instance END AS Instance,
	   CASE WHEN BT.DatabaseID =-1 THEN '{ALL}' ELSE D.name END AS [Database],
       BT.LogBackupWarningThreshold,
       BT.LogBackupCriticalThreshold,
       BT.FullBackupWarningThreshold,
       BT.FullBackupCriticalThreshold,
       BT.DiffBackupWarningThreshold,
       BT.DiffBackupCriticalThreshold,
       BT.ConsiderPartialBackups,
       BT.ConsiderFGBackups,
       BT.ExcludedDatabases,
       BT.MinimumAge
FROM dbo.BackupThresholds BT
LEFT JOIN dbo.Databases D ON BT.DatabaseID = D.DatabaseID
LEFT JOIN dbo.Instances I ON BT.InstanceID = I.InstanceID
WHERE (BT.DatabaseID = @DatabaseID OR @DatabaseID IS NULL)
AND (BT.InstanceID=@InstanceID OR @InstanceID IS NULL)
OPTION (RECOMPILE)