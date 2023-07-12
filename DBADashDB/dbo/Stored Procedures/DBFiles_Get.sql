CREATE PROC dbo.DBFiles_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@DatabaseID INT=NULL,
	@IncludeCritical BIT=1,
	@IncludeWarning BIT=1,
	@IncludeNA BIT=0,
	@IncludeOK BIT=0,
	@FilegroupLevel BIT=1,
    @Types VARCHAR(50)=NULL,
    @ShowHidden BIT=1,
    @DriveName NVARCHAR(256)=NULL
)
AS
DECLARE @StatusSQL NVARCHAR(MAX)

SELECT @StatusSQL = STUFF(CASE WHEN @IncludeCritical=1 THEN ',1' ELSE '' END
	+ CASE WHEN @IncludeWarning=1 THEN ',2' ELSE '' END
	+ CASE WHEN @IncludeNA=1 THEN ',3' ELSE '' END
	+ CASE WHEN @IncludeOK=1 THEN ',4' ELSE '' END,1,1,'')

SELECT @StatusSQL = CASE WHEN @StatusSQL='' THEN 'AND 1=2'
		ELSE 'AND (F.FreeSpaceStatus IN(' + @StatusSQL + ') OR F.PctMaxSizeStatus IN(' + @StatusSQL + ') OR FilegroupAutogrowStatus IN(' + @StatusSQL + '))' END

DECLARE @SQL NVARCHAR(MAX)

SET @SQL = CAST('' AS NVARCHAR(MAX)) + N'
SELECT FileID,
       InstanceID,
       DatabaseID,
       data_space_id,
       Instance,
       InstanceDisplayName,
       InstanceGroupName,
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
       FileSnapshotAgeStatus,
	   FilegroupMaxSizeMB,
	   FilegroupPctOfMaxSize,
	   FilegroupUsedPctOfMaxSize,
	   PctMaxSizeStatus,
	   MaxSizeExcludedReason,
	   FilegroupAutogrowFileCount,
	   FilegroupAutogrowStatus,
       type,
       type_desc' 
	   + CASE WHEN @FilegroupLevel=0 THEN ',
	   max_size,
	   MaxSizeMB,
	   GrowthMB,
	   GrowthPct,
	   growth,
	   is_percent_growth' ELSE '' END + '
FROM ' + CASE WHEN @FilegroupLevel = 1 THEN 'dbo.FilegroupStatus' ELSE 'dbo.FileStatus' END + ' AS F
WHERE 1=1
' + CASE WHEN @InstanceIDs IS NULL THEN '' ELSE 'AND EXISTS (SELECT 1
			FROM STRING_SPLIT(@InstanceIDs,'','') ss
			WHERE ss.value = F.InstanceID
			)' END + '
' + CASE WHEN @Types IS NULL THEN '' ELSE 'AND EXISTS (SELECT 1
			FROM STRING_SPLIT(@Types,'','') ss
			WHERE CAST(ss.value as TINYINT) = F.type
			)' END + '
' + @StatusSQL + '
' + CASE WHEN @DatabaseID IS NULL THEN '' ELSE 'AND F.DatabaseID = @DatabaseID' END + '
' + CASE WHEN @ShowHidden=1 THEN '' ELSE 'AND F.ShowInSummary=1' END + '
' + CASE WHEN @DriveName IS NULL THEN '' ELSE 'AND F.physical_name LIKE @DriveName + ''%''' END

PRINT @SQL
EXEC sp_executesql @SQL,N'@InstanceIDs VARCHAR(MAX), @DatabaseID INT, @Types VARCHAR(50),@DriveName NVARCHAR(256)', @InstanceIDs, @DatabaseID, @Types, @DriveName