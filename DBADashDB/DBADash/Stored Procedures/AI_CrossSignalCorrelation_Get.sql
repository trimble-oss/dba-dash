CREATE PROC DBADash.AI_CrossSignalCorrelation_Get(
	@MaxRows INT = 500,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = 24
)
AS
/* Result set 1: Recent alerts */
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	aa.AlertKey,
	aa.Priority,
	aa.IsResolved,
	aa.UpdatedDate
FROM Alert.ActiveAlerts aa
INNER JOIN dbo.Instances i ON i.InstanceID = aa.InstanceID
WHERE i.IsActive = 1
  AND aa.UpdatedDate >= DATEADD(HOUR,-@HoursBack * 7,SYSUTCDATETIME())
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY aa.UpdatedDate DESC

/* Result set 2: Wait totals by instance */
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	SUM(w.wait_time_ms)/1000.0 AS TotalWaitSec
FROM dbo.Waits_60MIN w
INNER JOIN dbo.WaitType wt ON wt.WaitTypeID = w.WaitTypeID
INNER JOIN dbo.Instances i ON i.InstanceID = w.InstanceID
WHERE w.SnapshotDate >= DATEADD(HOUR,-@HoursBack,SYSUTCDATETIME())
  AND wt.IsExcluded = 0
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
GROUP BY i.InstanceDisplayName
ORDER BY TotalWaitSec DESC

/* Result set 3: Blocking totals by instance */
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	SUM(bs.BlockedWaitTime) AS BlockedWaitMs
FROM dbo.BlockingSnapshotSummary bs
INNER JOIN dbo.Instances i ON i.InstanceID = bs.InstanceID
WHERE bs.SnapshotDateUTC >= DATEADD(HOUR,-@HoursBack,SYSUTCDATETIME())
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
GROUP BY i.InstanceDisplayName
ORDER BY BlockedWaitMs DESC

/* Result set 4: Deadlock estimates by instance */
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

/* Result set 5: Drive status risk */
SELECT TOP (@MaxRows)
	ds.InstanceDisplayName,
	ds.Status,
	ds.PctFreeSpace
FROM dbo.DriveStatus ds
WHERE ds.Status IN (1,2)
  AND (@InstanceFilter IS NULL OR ds.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY ds.Status ASC, ds.PctFreeSpace ASC
