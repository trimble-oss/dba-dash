CREATE PROC dbo.TableSizeHistory_Get(
	@ObjectID INT,
	@Top INT = 1000
)
AS
DECLARE @InstanceID INT
DECLARE @DatabaseID INT

SELECT @DatabaseID = D.DatabaseID,
	@InstanceID = D.InstanceID
FROM dbo.DBObjects O
JOIN dbo.Databases D ON O.DatabaseID = D.DatabaseID
WHERE ObjectID = @ObjectID

SELECT TOP(@Top)
		I.InstanceDisplayName AS Instance,
		D.name AS [DB],
		O.SchemaName,
		O.ObjectName,
		TS.[SnapshotDate],
		TS.row_count AS [Rows],
		TS.reserved_pages*8 AS Reserved_KB,
		TS.used_pages*8 as Used_KB,
		TS.data_pages*8 Data_KB,
		(TS.index_pages)*8 aS Index_KB,
		TS.row_count - LAG(TS.row_count) OVER(ORDER BY TS.SnapshotDate ASC) as New_Rows,
		(TS.used_pages - LAG(TS.used_pages) OVER(ORDER BY TS.SnapshotDate ASC))*8 as New_KB,
		((TS.row_count - LAG(TS.row_count) OVER(ORDER BY TS.SnapshotDate ASC)) *3600.0) / DATEDIFF(s,LAG(TS.SnapshotDate) OVER(ORDER BY TS.SnapshotDate ASC),TS.SnapshotDate) AS Rows_Per_Hour,
		((TS.used_pages - LAG(TS.used_pages) OVER(ORDER BY TS.SnapshotDate ASC)) *8 *3600.0) / DATEDIFF(s,LAG(TS.SnapshotDate) OVER(ORDER BY TS.SnapshotDate ASC),TS.SnapshotDate) AS KB_Per_Hour
FROM dbo.TableSize TS
JOIN dbo.Instances I ON I.InstanceID = TS.InstanceID
JOIN dbo.Databases D ON D.DatabaseID = TS.DatabaseID
JOIN dbo.DBObjects O ON TS.ObjectID = O.ObjectID AND O.DatabaseID = TS.DatabaseID
WHERE TS.ObjectID = @ObjectID
AND TS.DatabaseID = @DatabaseID
AND TS.InstanceID = @InstanceID 
ORDER BY TS.SnapshotDate DESC
