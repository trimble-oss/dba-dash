CREATE PROC dbo.DBOptionsHistory_Get(
		@InstanceIDs VARCHAR(MAX)=NULL,
		@DatabaseID INT=NULL,
		@ExcludeStateChanges BIT=1,
		@ShowHidden BIT=1
)
AS
DECLARE @SQL NVARCHAR(MAX) 
SET @SQL = N'
SELECT I.Instance, 
		I.InstanceDisplayName,
		D.name AS DB,
		H.Setting,
		H.OldValue,
		H.NewValue,
		H.ChangeDate
FROM dbo.DBOptionsHistory H
JOIN dbo.Databases D ON H.DatabaseID = D.DatabaseID 
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
WHERE 1=1
' + CASE WHEN @InstanceIDs IS NULL THEN '' ELSE ' AND EXISTS(SELECT 1 
			FROM STRING_SPLIT(@InstanceIDs,'','') ss 
			WHERE ss.value = D.InstanceID
			)' END +'
' + CASE WHEN @DatabaseID IS NULL THEN 'AND D.IsActive=1' ELSE 'AND D.DatabaseID = @DatabaseID' END + '
' + CASE WHEN @ExcludeStateChanges=1 THEN 'AND H.Setting NOT IN(''state'',''is_read_only'',''is_in_standby'')' ELSE '' END + '
' + CASE WHEN @ShowHidden=1 THEN '' ELSE 'AND I.ShowInSummary=1' END + '
ORDER BY H.ChangeDate DESC'

EXEC sp_executesql @SQL,N'@InstanceIDs VARCHAR(MAX),@DatabaseID INT',@InstanceIDs,@DatabaseID