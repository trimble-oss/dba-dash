CREATE PROC AI.Deadlocks_Get(
	@MaxRows INT = 200,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = 24,
	/* When the window ends, for looking at a past incident.  NULL ends it now, as before. */
	@ToDate DATETIME2(3) = NULL
)
AS
SET NOCOUNT ON
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	SUM(CAST(ROUND(((pc.Value_Total / NULLIF(pc.SampleCount, 0)) * 60.0), 0) AS BIGINT)) AS DeadlockCountEstimate,
	MAX(pc.SnapshotDate) AS LatestSnapshotDate
FROM dbo.PerformanceCounters_60MIN pc
INNER JOIN dbo.Counters c ON c.CounterID = pc.CounterID
INNER JOIN dbo.Instances i ON i.InstanceID = pc.InstanceID
WHERE pc.SnapshotDate >= DATEADD(HOUR, -@HoursBack, ISNULL(@ToDate, SYSUTCDATETIME())) AND (@ToDate IS NULL OR pc.SnapshotDate < @ToDate)
  AND c.counter_name = 'Number of Deadlocks/sec'
  AND c.object_name = 'Locks'
  AND c.instance_name = '_Total'
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
GROUP BY i.InstanceDisplayName
ORDER BY DeadlockCountEstimate DESC
