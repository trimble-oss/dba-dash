CREATE PROC dbo.DBFileSnapshot_Get(
		@DatabaseID INT=NULL,
		@FG SYSNAME=NULL,
		@DataSpaceID INT=NULL,
		@FromDate DATETIME2(2),
		@ToDate DATETIME2(2),
		@InstanceGroupName SYSNAME=NULL,
		@DBName SYSNAME=NULL,
		@InstanceID INT=NULL,
		@FileName SYSNAME=NULL,
		@Debug BIT=0
)
AS
IF @DatabaseID IS NULL AND @DBName IS NOT NULL
BEGIN
	SELECT @DatabaseID=D.DatabaseID
	FROM dbo.Databases D 
	JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
	WHERE D.name = @DBName
	AND I.InstanceGroupName = @InstanceGroupName
	AND I.IsActive=1
	AND D.IsActive=1
END

DECLARE @GroupDatabaseID UNIQUEIDENTIFIER
SELECT @GroupDatabaseID= group_database_id
FROM dbo.DatabasesHADR
WHERE DatabaseID=@DatabaseID 
IF @GroupDatabaseID IS NOT NULL
BEGIN 
	SET @DatabaseID=NULL
	SET @InstanceID=NULL
	SET @InstanceGroupName=NULL
	SET @DBName=NULL
END

DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
WITH ss AS (
	SELECT	F.DatabaseID,
			SS.SnapshotDate,
			SUM(SS.size)/128.0 AS SizeMB,SUM(SS.space_used) /128.0 AS UsedMB
	FROM dbo.DBFileSnapshot SS
	JOIN dbo.DBFiles F ON F.FileID = SS.FileID
	JOIN dbo.Databases D ON D.DatabaseID = F.DatabaseID
	JOIN dbo.Instances I ON D.InstanceID = I.InstanceID
	WHERE SS.SnapshotDate>=@FromDate
	AND SS.SnapshotDate<@ToDate
	AND D.source_database_id IS NULL
	' + CASE WHEN @GroupDatabaseID IS NULL THEN '' ELSE 'AND EXISTS(SELECT 1 FROM dbo.DatabasesHADR hadr WHERE hadr.DatabaseID = F.DatabaseID AND hadr.group_database_id = @GroupDatabaseID)' END + '
	' + CASE WHEN @DatabaseID IS NULL THEN '' ELSE 'AND F.DatabaseID = @DatabaseID' END + '
	' + CASE WHEN @FG IS NULL THEN '' ELSE 'AND F.filegroup_name = @FG' END + '
	' + CASE WHEN @DataSpaceID IS NULL THEN '' ELSE 'AND F.data_space_id = @DataSpaceID' END + '
	' + CASE WHEN @InstanceGroupName IS NULL THEN '' ELSE 'AND I.InstanceGroupName = @InstanceGroupName' END + '
	' + CASE WHEN @DBName IS NULL THEN '' ELSE 'AND D.name = @DBName' END + '
	' + CASE WHEN @InstanceID IS NULL THEN '' ELSE 'AND I.InstanceID = @InstanceID' END + '
	' + CASE WHEN @FileName IS NULL THEN '' ELSE ' AND F.name = @FileName' END + '
	GROUP BY SS.SnapshotDate,F.DatabaseID
)
, db as (
	SELECT ' + CASE WHEN @GroupDatabaseID IS NULL THEN 'DatabaseID,' ELSE '' END + 'CAST(ss.SnapshotDate AS DATE) SnapshotDate,MAX(SS.SizeMB) AS SizeMB,MAX(SS.UsedMB) AS UsedMB
	FROM ss
	GROUP BY CAST(ss.SnapshotDate AS DATE)' + CASE WHEN @GroupDatabaseID IS NULL THEN ',DatabaseID' ELSE '' END + '
)
SELECT	SnapshotDate,
		SUM(SizeMB) AS SizeMB,
		SUM(UsedMB) AS UsedMB
FROM db
GROUP BY SnapshotDate
ORDER BY SnapshotDate'

IF @Debug=1
BEGIN
	PRINT @SQL
END

EXEC sp_executesql @SQL,N'@DatabaseID INT,@FromDate DATETIME2(2),@ToDate DATETIME2(2),@FG SYSNAME,@GroupDatabaseID UNIQUEIDENTIFIER,@DataSpaceID INT,@InstanceGroupName SYSNAME,@DBName SYSNAME,@FileName SYSNAME,@InstanceID INT',
						@DatabaseID,@FromDate,@ToDate,@FG,@GroupDatabaseID,@DataSpaceID,@InstanceGroupName,@DBName,@FileName,@InstanceID