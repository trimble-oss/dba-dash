CREATE PROC DBADash.AI_WorkloadPressure_Get(
	@MaxRows INT = 300,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = 24
)
AS
/* Result set 1: Waits by instance and type */
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	wt.WaitType,
	SUM(w.wait_time_ms)/1000.0 AS TotalWaitSec
FROM dbo.Waits_60MIN w
INNER JOIN dbo.WaitType wt ON wt.WaitTypeID = w.WaitTypeID
INNER JOIN dbo.Instances i ON i.InstanceID = w.InstanceID
WHERE w.SnapshotDate >= DATEADD(HOUR,-@HoursBack,SYSUTCDATETIME())
  AND wt.IsExcluded = 0
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
GROUP BY i.InstanceDisplayName, wt.WaitType
ORDER BY TotalWaitSec DESC

/* Result set 2: Blocking by instance */
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	SUM(bs.BlockedSessionCount) AS BlockedSessions,
	SUM(bs.BlockedWaitTime) AS BlockedWaitMs
FROM dbo.BlockingSnapshotSummary bs
INNER JOIN dbo.Instances i ON i.InstanceID = bs.InstanceID
WHERE bs.SnapshotDateUTC >= DATEADD(HOUR,-@HoursBack,SYSUTCDATETIME())
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
GROUP BY i.InstanceDisplayName
ORDER BY BlockedWaitMs DESC

/* Result set 3: Deadlocks by instance */
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	SUM(CAST(ROUND(((pc.Value_Total / NULLIF(pc.SampleCount,0))*60.0),0) AS BIGINT)) AS DeadlockCountEstimate
FROM dbo.PerformanceCounters_60MIN pc
INNER JOIN dbo.Counters c ON c.CounterID = pc.CounterID
INNER JOIN dbo.Instances i ON i.InstanceID = pc.InstanceID
WHERE pc.SnapshotDate >= DATEADD(HOUR,-@HoursBack,SYSUTCDATETIME())
  AND c.counter_name = 'Number of Deadlocks/sec'
  AND c.object_name = 'Locks'
  AND c.instance_name = '_Total'
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
GROUP BY i.InstanceDisplayName
ORDER BY DeadlockCountEstimate DESC

/* Result set 4: Slow queries by instance */
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	SUM(sq.duration)/1000000.0 AS TotalDurationSec,
	SUM(sq.cpu_time)/1000000.0 AS TotalCpuSec,
	COUNT_BIG(*) AS SlowQueryExecCount
FROM dbo.SlowQueries sq
INNER JOIN dbo.Instances i ON i.InstanceID = sq.InstanceID
WHERE sq.timestamp >= DATEADD(HOUR,-@HoursBack,SYSUTCDATETIME())
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
GROUP BY i.InstanceDisplayName
ORDER BY TotalDurationSec DESC
