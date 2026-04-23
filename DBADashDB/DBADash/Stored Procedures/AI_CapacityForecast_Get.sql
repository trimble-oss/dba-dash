CREATE PROC DBADash.AI_CapacityForecast_Get(
	@MaxRows INT = 300,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = 24
)
AS
/* Result set 1: Drive capacity risk */
SELECT TOP (@MaxRows)
	ds.InstanceDisplayName,
	ds.Name AS DriveName,
	ds.TotalGB,
	ds.FreeGB,
	ds.PctFreeSpace,
	ds.Status,
	ds.SnapshotAgeMins
FROM dbo.DriveStatus ds
WHERE ds.Status IN (1,2)
  AND (@InstanceFilter IS NULL OR ds.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY ds.Status ASC, ds.PctFreeSpace ASC;

/* Result set 2: Memory pressure (aggregated from MemoryUsage clerks, latest snapshot per instance) */
WITH LatestSnapshot AS (
	SELECT InstanceID, MAX(SnapshotDate) AS SnapshotDate
	FROM dbo.MemoryUsage
	WHERE SnapshotDate >= DATEADD(HOUR,-@HoursBack,SYSUTCDATETIME())
	GROUP BY InstanceID
),
SqlMemory AS (
	SELECT mu.InstanceID, ls.SnapshotDate,
		SUM(CAST(mu.pages_kb AS BIGINT)) AS TotalPagesKb,
		SUM(CAST(mu.virtual_memory_committed_kb AS BIGINT)) AS TotalCommittedKb
	FROM dbo.MemoryUsage mu
	INNER JOIN LatestSnapshot ls
		ON ls.InstanceID = mu.InstanceID AND ls.SnapshotDate = mu.SnapshotDate
	GROUP BY mu.InstanceID, ls.SnapshotDate
)
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	CAST(i.physical_memory_kb/1024.0/1024.0 AS DECIMAL(18,2)) AS PhysicalMemoryGB,
	CAST(sm.TotalPagesKb/1024.0/1024.0 AS DECIMAL(18,2)) AS TotalServerMemoryGB,
	CAST(sm.TotalCommittedKb/1024.0/1024.0 AS DECIMAL(18,2)) AS TargetServerMemoryGB,
	CASE WHEN i.physical_memory_kb > 0
		THEN CAST(100.0 * sm.TotalPagesKb / i.physical_memory_kb AS DECIMAL(5,2))
		ELSE NULL END AS MemoryUtilizationPercent,
	sm.SnapshotDate
FROM SqlMemory sm
INNER JOIN dbo.Instances i ON i.InstanceID = sm.InstanceID
WHERE i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY MemoryUtilizationPercent DESC
