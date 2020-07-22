CREATE PROC [dbo].[Drives_Get](
	@InstanceIDs VARCHAR(MAX)=NULL,
	@IncludeCritical BIT=1,
	@IncludeWarning BIT=1,
	@IncludeNA BIT=0,
	@IncludeOK BIT=0
)
AS
DECLARE @StatusSQL NVARCHAR(MAX)

SELECT @StatusSQL = CASE WHEN @IncludeCritical=1 THEN ',1' ELSE '' END
	+ CASE WHEN @IncludeWarning=1 THEN ',2' ELSE '' END
	+ CASE WHEN @IncludeNA=1 THEN ',3' ELSE '' END
	+ CASE WHEN @IncludeOK=1 THEN ',4' ELSE '' END

SELECT @StatusSQL = CASE WHEN @StatusSQL='' THEN 'AND 1=2'
		ELSE 'AND D.Status IN(' + STUFF(@StatusSQL,1,1,'') + ')' END

DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT D.DriveID,D.Name,D.InstanceID,D.Label,D.TotalGB,D.FreeGB,D.DriveCheckType,D.Status,D.DriveWarningThreshold,D.DriveCriticalThreshold,D.IsInheritedThreshold,D.Instance
FROM dbo.DriveStatus D
WHERE ' + CASE WHEN @InstanceIDs IS NULL OR @InstanceIDs = '' 
			THEN '1=1' 
			ELSE 'EXISTS(SELECT 1 
						FROM STRING_SPLIT(@InstanceIDs,'','') ss
						WHERE ss.value = D.InstanceID)' END + '
' + @StatusSQL + '
ORDER BY Status DESC, PctFreeSpace DESC;'

EXEC sp_executesql @SQL,N'@InstanceIDs VARCHAR(MAX)',@InstanceIDs
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[Drives_Get] TO [Reports]
    AS [dbo];

