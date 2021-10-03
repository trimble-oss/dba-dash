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
ORDER BY MU.SnapshotDate DESC

SELECT MCT.MemoryClerkType,
       ISNULL(MCT.MemoryClerkDescription,'Description not available.') AS MemoryClerkDescription,
       MU.pages_kb,
       MU.virtual_memory_reserved_kb,
       MU.virtual_memory_committed_kb,
       MU.awe_allocated_kb,
       MU.shared_memory_reserved_kb,
       MU.shared_memory_committed_kb,
	   MU.SnapshotDate,
	   MU.pages_kb*1.0 / SUM(MU.pages_kb) OVER() AS Pct
FROM dbo.MemoryUsage MU
    JOIN dbo.MemoryClerkType MCT
        ON MCT.MemoryClerkTypeID = MU.MemoryClerkTypeID
WHERE MU.InstanceID = @InstanceID
AND MU.SnapshotDate = @LatestSnapshot
ORDER BY MU.pages_kb DESC