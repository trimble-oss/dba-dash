CREATE PROC DBADash.AI_Blocking_Get(
	@MaxRows INT = 200,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = 24
)
AS
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	bs.SnapshotDateUTC AS SnapshotDate,
	bs.BlockedSessionCount,
	bs.BlockedWaitTime
FROM dbo.BlockingSnapshotSummary bs
INNER JOIN dbo.Instances i ON i.InstanceID = bs.InstanceID
WHERE bs.SnapshotDateUTC >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY bs.SnapshotDateUTC DESC, bs.BlockedWaitTime DESC
