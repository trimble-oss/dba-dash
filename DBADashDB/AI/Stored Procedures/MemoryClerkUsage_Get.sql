CREATE PROC AI.MemoryClerkUsage_Get(
    @MaxRows INT = 300,
    @InstanceFilter NVARCHAR(256) = NULL,
    @HoursBack INT = 24
)
AS
SET NOCOUNT ON
/* Latest memory snapshot per instance: identify the most recent snapshot within the window, then
   return the top memory clerks (by pages) so the model can explain where buffer/plan/other memory is going. */
;WITH LatestSnapshot AS (
    SELECT mu.InstanceID, MAX(mu.SnapshotDate) AS SnapshotDate
    FROM dbo.MemoryUsage mu
    WHERE mu.SnapshotDate >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
    GROUP BY mu.InstanceID
), ClerkUsage AS (
    SELECT
        mu.InstanceID,
        mct.MemoryClerkType,
        mu.SnapshotDate,
        mu.pages_kb,
        mu.virtual_memory_committed_kb,
        ROW_NUMBER() OVER (PARTITION BY mu.InstanceID ORDER BY mu.pages_kb DESC) AS ClerkRank
    FROM dbo.MemoryUsage mu
    INNER JOIN LatestSnapshot ls ON ls.InstanceID = mu.InstanceID AND ls.SnapshotDate = mu.SnapshotDate
    INNER JOIN dbo.MemoryClerkType mct ON mct.MemoryClerkTypeID = mu.MemoryClerkTypeID
)
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    cu.MemoryClerkType,
    cu.SnapshotDate,
    cu.pages_kb / 1024.0 AS PagesMB,
    cu.virtual_memory_committed_kb / 1024.0 AS CommittedMB,
    CAST(100.0 * cu.pages_kb / NULLIF(SUM(cu.pages_kb) OVER (PARTITION BY cu.InstanceID), 0) AS DECIMAL(5,2)) AS PctOfTotalPages
FROM ClerkUsage cu
INNER JOIN dbo.Instances i ON i.InstanceID = cu.InstanceID
WHERE cu.ClerkRank <= 10
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY i.InstanceDisplayName, cu.pages_kb DESC
