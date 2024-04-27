CREATE PROC dbo.TableSize_Get(
	@InstanceIDs IDs READONLY,
	@GrowthDays INT=30,
	@Top INT = 1000,
	@DatabaseID INT
)
AS
DECLARE @MinSnapshotDate DATETIME2
SELECT @MinSnapshotDate = DATEADD(d,-@GrowthDays,SYSUTCDATETIME());

WITH T AS (
	SELECT	I.InstanceID,
			I.InstanceDisplayName AS Instance,
			TS.[SnapshotDate],
			TS.DatabaseID,
			TS.ObjectID,
			TS.[row_count],
			TS.[reserved_pages],
			TS.[used_pages],
			TS.[data_pages],
			TS.index_pages,
			RANK() OVER(PARTITION BY TS.ObjectID ORDER BY TS.SnapshotDate DESC) Latest,
			RANK() OVER(PARTITION BY TS.ObjectID ORDER BY TS.SnapshotDate ASC) Oldest
	FROM dbo.TableSize TS
	JOIN dbo.Instances I ON I.InstanceID = TS.InstanceID
	WHERE EXISTS(SELECT 1
				FROM @InstanceIDs T
				WHERE T.ID = TS.InstanceID
				)
	AND TS.SnapshotDate>=@MinSnapshotDate
	AND (TS.DatabaseID = @DatabaseID OR @DatabaseID IS NULL)
)
SELECT TOP(@Top) 
	Latest.ObjectID,
	Latest.InstanceID,
	Latest.Instance,
	Latest.[SnapshotDate],
	ISNULL(SSD.Status,3) AS SnapshotStatus,
	D.name AS [DB],
	O.SchemaName,
	O.ObjectName,
	Latest.row_count AS [Rows],
	Latest.reserved_pages*8 AS Reserved_KB,
	Latest.used_pages*8 as Used_KB,
	Latest.data_pages*8 Data_KB,
	(Latest.index_pages)*8 aS Index_KB,
	(Latest.row_count - Oldest.row_count)*1440.0 / NULLIF(DATEDIFF(mi,Oldest.SnapshotDate,Latest.SnapshotDate),0) AS Avg_Rows_Per_Day,
	(Latest.used_pages - Oldest.used_pages)*8*1440.0 / NULLIF(DATEDIFF(mi,Oldest.SnapshotDate,Latest.SnapshotDate),0) AS Avg_KB_Per_Day,
	NULLIF(DATEDIFF(mi,Oldest.SnapshotDate,Latest.SnapshotDate),0)/1440.0 AS CalcDays	
FROM T Latest
JOIN dbo.Databases D ON D.DatabaseID = Latest.DatabaseID
JOIN dbo.DBObjects O ON Latest.ObjectID = O.ObjectID AND O.DatabaseID = Latest.DatabaseID
LEFT JOIN T Oldest ON Latest.Instance = Oldest.Instance AND Latest.DatabaseID = Oldest.DatabaseID AND Latest.ObjectID = Oldest.ObjectID AND Oldest.Oldest = 1
LEFT JOIN dbo.CollectionDatesStatus SSD ON SSD.InstanceID = Latest.InstanceID AND SSD.Reference='TableSize'
WHERE Latest.Latest = 1
ORDER BY Latest.reserved_pages DESC
OPTION(RECOMPILE)