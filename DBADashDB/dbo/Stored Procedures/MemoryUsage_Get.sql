CREATE PROC dbo.MemoryUsage_Get(
	@InstanceID INT,
	@FromDate DATETIME=NULL,
	@ToDate DATETIME=NULL
)
AS
DECLARE @LatestSnapshot DATETIME2(2)
IF @FromDate IS NULL
BEGIN
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
END
IF @ToDate IS NULL 
BEGIN
	SET @ToDate = GETUTCDATE()
END
SELECT TOP(1) @LatestSnapshot = MU.SnapshotDate     
FROM dbo.MemoryUsage MU
WHERE MU.InstanceID = @InstanceID
AND MU.SnapshotDate>=@FromDate
AND MU.SnapshotDate < @ToDate
ORDER BY MU.SnapshotDate DESC;

WITH Agg AS (
    SELECT	MemoryClerkTypeID,
			MAX(pages_kb) AS max_pages_kb,
			AVG(pages_kb) AS avg_pages_kb,
			MIN(pages_kb) AS min_pages_kb,
			MAX(virtual_memory_reserved_kb) AS max_virtual_memory_reserved_kb,
			AVG(virtual_memory_reserved_kb) AS avg_virtual_memory_reserved_kb,
			MIN(virtual_memory_reserved_kb) AS min_virtual_memory_reserved_kb,
			MAX(virtual_memory_committed_kb) AS max_virtual_memory_committed_kb,
			AVG(virtual_memory_committed_kb) AS avg_virtual_memory_committed_kb,
			MIN(virtual_memory_committed_kb) AS min_virtual_memory_committed_kb,
			MAX(awe_allocated_kb) AS max_awe_allocated_kb,
			AVG(awe_allocated_kb) AS avg_awe_allocated_kb,
			MIN(awe_allocated_kb) AS min_awe_allocated_kb,
			MAX(shared_memory_reserved_kb) AS max_shared_memory_reserved_kb,
			AVG(shared_memory_reserved_kb) AS avg_shared_memory_reserved_kb,
			MIN(shared_memory_reserved_kb) AS min_shared_memory_reserved_kb,
			MAX(shared_memory_committed_kb) AS max_shared_memory_committed_kb,
			AVG(shared_memory_committed_kb) AS avg_shared_memory_committed_kb,
			MIN(shared_memory_committed_kb) AS min_shared_memory_committed_kb
    FROM dbo.MemoryUsage
    WHERE InstanceID = @InstanceID
    AND SnapshotDate >= @FromDate
    AND SnapshotDate < @ToDate
    GROUP BY MemoryClerkTypeID
)
SELECT MCT.MemoryClerkType,
       ISNULL(MCT.MemoryClerkDescription,'Description not available.') AS MemoryClerkDescription,
       MU.pages_kb,
	   Agg.max_pages_kb,
	   Agg.avg_pages_kb,
	   Agg.min_pages_kb,
       MU.virtual_memory_reserved_kb,
	   Agg.max_virtual_memory_reserved_kb,
	   Agg.avg_virtual_memory_reserved_kb,
	   Agg.min_virtual_memory_reserved_kb,
       MU.virtual_memory_committed_kb,
	   Agg.max_virtual_memory_committed_kb,
	   Agg.avg_virtual_memory_committed_kb,
	   Agg.min_virtual_memory_committed_kb,
       MU.awe_allocated_kb,
	   Agg.max_awe_allocated_kb,
	   Agg.avg_awe_allocated_kb,
	   Agg.min_awe_allocated_kb,
       MU.shared_memory_reserved_kb,
	   Agg.max_shared_memory_reserved_kb,
	   Agg.avg_shared_memory_reserved_kb,
	   Agg.min_shared_memory_reserved_kb,
       MU.shared_memory_committed_kb,
	   Agg.max_shared_memory_committed_kb,
	   Agg.avg_shared_memory_committed_kb,
	   Agg.min_shared_memory_committed_kb,
	   MU.SnapshotDate,
	   MU.pages_kb*1.0 / SUM(MU.pages_kb) OVER() AS Pct
FROM dbo.MemoryUsage MU
JOIN dbo.MemoryClerkType MCT ON MCT.MemoryClerkTypeID = MU.MemoryClerkTypeID
JOIN Agg ON Agg.MemoryClerkTypeID = MU.MemoryClerkTypeID
WHERE MU.InstanceID = @InstanceID
AND MU.SnapshotDate = @LatestSnapshot
ORDER BY MU.pages_kb DESC