CREATE PROC dbo.MemoryClerkUsage_Get(
	@InstanceID INT,
	@FromDate DATETIME=NULL,
	@ToDate DATETIME=NULL,
	@MemoryClerkType NVARCHAR(60)
)
AS
SELECT MCT.MemoryClerkType,
       MU.pages_kb,
       MU.virtual_memory_reserved_kb,
       MU.virtual_memory_committed_kb,
       MU.awe_allocated_kb,
       MU.shared_memory_reserved_kb,
       MU.shared_memory_committed_kb,
	   MU.SnapshotDate
FROM dbo.MemoryUsage MU
JOIN dbo.MemoryClerkType MCT ON MCT.MemoryClerkTypeID = MU.MemoryClerkTypeID
WHERE MCT.MemoryClerkType = @MemoryClerkType
AND MU.SnapshotDate >= @FromDate
AND MU.SnapshotDate < @ToDate
AND MU.InstanceID = @InstanceID
ORDER BY MU.SnapshotDate