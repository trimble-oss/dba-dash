CREATE PROC DBADash.AI_TableSize_Get(
    @MaxRows INT = 200,
    @InstanceFilter NVARCHAR(256) = NULL,
    @HoursBack INT = 168
)
AS
/* Result set 1: Current largest tables */
;WITH LatestSnapshot AS (
    SELECT ts.InstanceID, ts.DatabaseID, ts.ObjectID,
        ts.row_count, ts.reserved_pages, ts.used_pages, ts.data_pages,
        ts.SnapshotDate,
        ROW_NUMBER() OVER (PARTITION BY ts.InstanceID, ts.DatabaseID, ts.ObjectID ORDER BY ts.SnapshotDate DESC) AS rn
    FROM dbo.TableSize ts
    WHERE ts.SnapshotDate >= DATEADD(HOUR, -24, SYSUTCDATETIME())
)
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    d.name AS DatabaseName,
    o.SchemaName + '.' + o.ObjectName AS TableName,
    ls.row_count AS [RowCount],
    CAST(ls.reserved_pages * 8 / 1024.0 AS DECIMAL(18,2)) AS ReservedMB,
    CAST(ls.data_pages * 8 / 1024.0 AS DECIMAL(18,2)) AS DataMB,
    CAST((ls.used_pages - ls.data_pages) * 8 / 1024.0 AS DECIMAL(18,2)) AS IndexMB,
    CAST((ls.reserved_pages - ls.used_pages) * 8 / 1024.0 AS DECIMAL(18,2)) AS UnusedMB,
    ls.SnapshotDate
FROM LatestSnapshot ls
INNER JOIN dbo.Instances i ON i.InstanceID = ls.InstanceID
INNER JOIN dbo.Databases d ON d.DatabaseID = ls.DatabaseID
INNER JOIN dbo.DBObjects o ON o.ObjectID = ls.ObjectID AND o.DatabaseID = ls.DatabaseID
WHERE ls.rn = 1
  AND i.IsActive = 1
  AND d.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY ls.reserved_pages DESC

/* Result set 2: Table growth (current vs oldest snapshot in window) */
;WITH Latest AS (
    SELECT ts.InstanceID, ts.DatabaseID, ts.ObjectID,
        ts.row_count, ts.reserved_pages, ts.SnapshotDate,
        ROW_NUMBER() OVER (PARTITION BY ts.InstanceID, ts.DatabaseID, ts.ObjectID ORDER BY ts.SnapshotDate DESC) AS rn
    FROM dbo.TableSize ts
    WHERE ts.SnapshotDate >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
),
Oldest AS (
    SELECT ts.InstanceID, ts.DatabaseID, ts.ObjectID,
        ts.row_count, ts.reserved_pages, ts.SnapshotDate,
        ROW_NUMBER() OVER (PARTITION BY ts.InstanceID, ts.DatabaseID, ts.ObjectID ORDER BY ts.SnapshotDate ASC) AS rn
    FROM dbo.TableSize ts
    WHERE ts.SnapshotDate >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
)
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    d.name AS DatabaseName,
    o.SchemaName + '.' + o.ObjectName AS TableName,
    old.row_count AS OldRowCount,
    cur.row_count AS CurrentRowCount,
    cur.row_count - old.row_count AS RowGrowth,
    CAST(old.reserved_pages * 8 / 1024.0 AS DECIMAL(18,2)) AS OldReservedMB,
    CAST(cur.reserved_pages * 8 / 1024.0 AS DECIMAL(18,2)) AS CurrentReservedMB,
    CAST((cur.reserved_pages - old.reserved_pages) * 8 / 1024.0 AS DECIMAL(18,2)) AS GrowthMB,
    old.SnapshotDate AS OldSnapshotDate,
    cur.SnapshotDate AS CurrentSnapshotDate
FROM Latest cur
INNER JOIN Oldest old ON old.InstanceID = cur.InstanceID AND old.DatabaseID = cur.DatabaseID AND old.ObjectID = cur.ObjectID AND old.rn = 1
INNER JOIN dbo.Instances i ON i.InstanceID = cur.InstanceID
INNER JOIN dbo.Databases d ON d.DatabaseID = cur.DatabaseID
INNER JOIN dbo.DBObjects o ON o.ObjectID = cur.ObjectID AND o.DatabaseID = cur.DatabaseID
WHERE cur.rn = 1
  AND i.IsActive = 1
  AND d.IsActive = 1
  AND (cur.reserved_pages - old.reserved_pages) <> 0
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY ABS(cur.reserved_pages - old.reserved_pages) DESC
