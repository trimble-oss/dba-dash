CREATE PROC [Report].[DBFiles_Get](@InstanceIDs VARCHAR(MAX)=NULL,@FilterLevel TINYINT=2)
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
       data_space_id,
       Instance,
       name,
       filegroup_name,
       SizeMB,
       UsedMB,
       FreeMB,
       PctFree,
       NumberOfFiles,
       is_read_only,
       is_db_read_only,
       is_in_standby,
       state,
       FreeSpaceStatus,
       FreeSpaceWarningThreshold,
       FreeSpaceCriticalThreshold,
       FreeSpaceCheckType,
	   ConfiguredLevel
FROM dbo.DBFileStatus F
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = F.InstanceID)
AND (FreeSpaceStatus<=@FilterLevel OR @FilterLevel IS NULL)