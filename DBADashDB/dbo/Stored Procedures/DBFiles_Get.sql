CREATE PROC [dbo].[DBFiles_Get](
	@InstanceIDs VARCHAR(MAX)=NULL,
	@DatabaseID INT=NULL,
	@IncludeCritical BIT=1,
	@IncludeWarning BIT=1,
	@IncludeNA BIT=0,
	@IncludeOK BIT=0,
	@FilegroupLevel BIT=1
)
AS
DECLARE @StatusSQL NVARCHAR(MAX)

SELECT @StatusSQL = CASE WHEN @IncludeCritical=1 THEN ',1' ELSE '' END
	+ CASE WHEN @IncludeWarning=1 THEN ',2' ELSE '' END
	+ CASE WHEN @IncludeNA=1 THEN ',3' ELSE '' END
	+ CASE WHEN @IncludeOK=1 THEN ',4' ELSE '' END

SELECT @StatusSQL = CASE WHEN @StatusSQL='' THEN 'AND 1=2'
		ELSE 'AND F.FreeSpaceStatus IN(' + STUFF(@StatusSQL,1,1,'') + ')' END

DECLARE @SQL NVARCHAR(MAX)

SET @SQL = N'
SELECT FileID,
       InstanceID,
       DatabaseID,
       data_space_id,
       Instance,
       ConnectionID,
       name,
       file_name,
       Filegroup_name,
       physical_name,
       FileSizeMB,
       FileUsedMB,
       FileFreeMB,
       FilePctFree,
       FilegroupSizeMB,
       FilegroupUsedMB,
       FilegroupFreeMB,
       FilegroupPctFree,
       FilegroupNumberOfFiles,
       is_read_only,
       is_db_read_only,
       is_in_standby,
       state,
       state_desc,
       ExcludedReason,
       FreeSpaceStatus,
       FreeSpaceWarningThreshold,
       FreeSpaceCriticalThreshold,
       FreeSpaceCheckType,
       ConfiguredLevel,
       FileSnapshotDate,
       FileSnapshotAge,
       FileSnapshotAgeStatus' 
	   + CASE WHEN @FilegroupLevel=0 THEN ',
	   max_size,
	   MaxSizeMB,
	   GrowthMB,
	   GrowthPct,
	   growth,
	   is_percent_growth' ELSE '' END + '
FROM ' + CASE WHEN @FilegroupLevel = 1 THEN 'dbo.FileGroupStatus' ELSE 'dbo.FileStatus' END + ' AS F
WHERE 1=1' + 
CASE WHEN @InstanceIDs IS NULL THEN '' ELSE 'AND EXISTS (SELECT 1
			FROM STRING_SPLIT(@InstanceIDs,'','') ss
			WHERE ss.value = F.InstanceID
			)' END + '
' + @StatusSQL + '
' + CASE WHEN @DatabaseID IS NULL THEN '' ELSE 'AND F.DatabaseID = @DatabaseID' END 


EXEC sp_executesql @SQL,N'@InstanceIDs VARCHAR(MAX),@DatabaseID INT',@InstanceIDs,@DatabaseID