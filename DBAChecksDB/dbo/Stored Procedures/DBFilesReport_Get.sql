CREATE PROC DBFilesReport_Get(@InstanceID INT=NULL,@FilterLevel TINYINT=2)
AS
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
FROM dbo.DBFileStatus
WHERE (InstanceID=@InstanceID OR @InstanceID IS NULL)
AND (FreeSpaceStatus<=@FilterLevel OR @FilterLevel IS NULL)