CREATE PROC [Report].[Backups_Get](@InstanceIDs VARCHAR(MAX)=NULL,@FilterLevel TINYINT=2)
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
END

SELECT InstanceID,
       DatabaseID,
       Instance,
       name,
       recovery_model,
       LastFull,
       LastDiff,
       LastLog,
       LastFG,
       LastFGDiff,
       LastPartial,
       LastPartialDiff,
       FullBackupStatus,
       LogBackupStatus,
       DiffBackupStatus,
       LogBackupWarningThreshold,
       LogBackupCriticalThreshold,
       FullBackupWarningThreshold,
       FullBackupCriticalThreshold,
       DiffBackupWarningThreshold,
       DiffBackupCriticalThreshold,
       ConsiderPartialBackups,
       ConsiderFGBackups,
	   SnapshotDate,
	   SnapshotAge
FROM dbo.BackupStatus BS
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = BS.InstanceID)
AND (FullBackupStatus<=@FilterLevel OR DiffBackupStatus<=@FilterLevel OR LogBackupStatus<= @FilterLevel)
ORDER BY FullBackupStatus