CREATE PROC dbo.CustomCheck_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@IncludeCritical BIT=1,
	@IncludeWarning BIT=1,
	@IncludeNA BIT=1,
	@IncludeOK BIT=1,
	@Context NVARCHAR(128)=NULL,
	@Test NVARCHAR(128)=NULL
)
AS
DECLARE @StatusSQL NVARCHAR(MAX)

SELECT @StatusSQL = CASE WHEN @IncludeCritical=1 THEN ',1' ELSE '' END
	+ CASE WHEN @IncludeWarning=1 THEN ',2' ELSE '' END
	+ CASE WHEN @IncludeNA=1 THEN ',3' ELSE '' END
	+ CASE WHEN @IncludeOK=1 THEN ',4' ELSE '' END

SELECT @StatusSQL = CASE WHEN @StatusSQL='' THEN '1=2'
		ELSE 'cc.Status IN(' + STUFF(@StatusSQL,1,1,'') + ')' END

DECLARE @SQL NVARCHAR(MAX) 
SET @SQL = N'
SELECT I.ConnectionID,
        cc.Test,
        cc.Context,
        cc.Status,
        cc.Info,
        cc.SnapshotDate
FROM dbo.CustomChecks cc
JOIN dbo.Instances I ON	 I.InstanceID = cc.InstanceID
WHERE ' + @StatusSQL + '
' + CASE WHEN @Test IS NULL THEN '' ELSE 'AND cc.Test = @Test' END + '
' + CASE WHEN @Context IS NULL THEN '' ELSE 'AND cc.Context = @Context' END + '
' + CASE WHEN @InstanceIDs IS NULL THEN '' ELSE 'AND EXISTS(SELECT 1 FROM STRING_SPLIT(@InstanceIDs,'','') ss WHERE ss.Value = I.InstanceID)' END

EXEC sp_executesql @SQL,N'@InstanceIDs VARCHAR(MAX),@Test NVARCHAR(128),@Context NVARCHAR(128)',@InstanceIDs,@Test,@Context