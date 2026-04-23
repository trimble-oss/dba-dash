CREATE PROC DBADash.AI_Waits_Get(
	@MaxRows INT = 200,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = 24
)
AS
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	wt.WaitType,
	wt.Description,
	SUM(w.wait_time_ms) / 1000.0 AS TotalWaitSec,
	SUM(w.signal_wait_time_ms) / 1000.0 AS SignalWaitSec,
	SUM(w.waiting_tasks_count) AS WaitingTasksCount
FROM dbo.Waits_60MIN w
INNER JOIN dbo.WaitType wt ON wt.WaitTypeID = w.WaitTypeID
INNER JOIN dbo.Instances i ON i.InstanceID = w.InstanceID
WHERE w.SnapshotDate >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
  AND wt.IsExcluded = 0
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
GROUP BY i.InstanceDisplayName, wt.WaitType, wt.Description
ORDER BY TotalWaitSec DESC
