CREATE PROC dbo.DBSpace_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@DatabaseID INT=NULL,
	@DBName SYSNAME=NULL,
	@InstanceGroupName NVARCHAR(128)=NULL
)
AS
DECLARE @SQL NVARCHAR(MAX)
DECLARE @Grp NVARCHAR(MAX) 
SET @Grp = CASE WHEN @DatabaseID IS NOT NULL OR @DBName IS NOT NULL THEN 'F.Name' 
			WHEN @InstanceIDs NOT LIKE '%,%' OR @InstanceGroupName IS NOT NULL THEN 'D.Name'
			ELSE 'I.InstanceGroupName' END 

SET @SQL = N'
SELECT ' + @Grp + ' as Grp,
	SUM(F.size*8)/POWER(1024.0,2) AS AllocatedGB,
	SUM(F.space_used*8)/POWER(1024.0,2) AS UsedGB,
	SUM(size)/SUM(SUM(size*1.0)) OVER() Pct
FROM dbo.Instances I 
JOIN dbo.Databases D ON D.InstanceID = I.InstanceID
JOIN dbo.DBFiles F ON F.DatabaseID = D.DatabaseID
WHERE I.IsActive=1
AND D.IsActive=1
AND F.IsActive=1
AND D.source_database_id IS NULL
' + CASE WHEN @InstanceIDs IS NULL THEN '' ELSE 'AND EXISTS (SELECT 1
			FROM STRING_SPLIT(@InstanceIDs,'','') ss
			WHERE ss.value = I.InstanceID
			)' END + '
' + CASE WHEN @DatabaseID IS NULL THEN '' ELSE 'AND F.DatabaseID = @DatabaseID' END + '
' + CASE WHEN @InstanceGroupName IS NULL THEN '' ELSE 'AND I.InstanceGroupName = @InstanceGroupName' END + '
' + CASE WHEN @DBName IS NULL THEN '' ELSE 'AND D.Name = @DBName' END + '
GROUP BY ' + @Grp + '
ORDER BY AllocatedGB DESC'

EXEC sp_executesql @SQL,N'@InstanceIDs VARCHAR(MAX),@DatabaseID INT,@DBName SYSNAME,@InstanceGroupName NVARCHAR(128)',@InstanceIDs,@DatabaseID,@DBName,@InstanceGroupName