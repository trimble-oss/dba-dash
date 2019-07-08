CREATE PROC [dbo].[BackupReport_Get](@InstanceID INT=NULL,@FilterLevel TINYINT=2)
AS
SELECT * 
FROM dbo.BackupStatus
WHERE (InstanceID = @InstanceID OR @InstanceID IS NULL)
AND (FullBackupStatus<=@FilterLevel OR DiffBackupStatus<=@FilterLevel OR LogBackupStatus<= @FilterLevel)
ORDER BY FullBackupStatus

