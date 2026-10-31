CREATE PROC AI.Blocking_Get(
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
	bs.SnapshotDateUTC AS SnapshotDate,
	bs.BlockedSessionCount,
	bs.BlockedWaitTime
FROM dbo.BlockingSnapshotSummary bs
INNER JOIN dbo.Instances i ON i.InstanceID = bs.InstanceID
WHERE bs.SnapshotDateUTC >= DATEADD(HOUR, -@HoursBack, ISNULL(@ToDate, SYSUTCDATETIME())) AND (@ToDate IS NULL OR bs.SnapshotDateUTC < @ToDate)
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY bs.SnapshotDateUTC DESC, bs.BlockedWaitTime DESC
