CREATE PROC dbo.RunningQueriesSummary_Get(
	@InstanceID INT,
	@FromDate DATETIME2,
	@ToDate DATETIME2,
	@MaxRows INT=2000
)
AS
SELECT TOP(@MaxRows) I.InstanceID,
		   I.ConnectionID AS Instance,
		 S.SnapshotDateUTC,
       S.RunningQueries,
       S.BlockedQueries,
       S.BlockedQueriesWaitMs,
       S.MaxMemoryGrant*8 as MaxMemoryGrantKB,
       S.LongestRunningQueryMs,
       S.CriticalWaitCount,
       S.CriticalWaitTime 
FROM dbo.RunningQueriesSummary S
JOIN dbo.Instances I ON I.InstanceID = S.InstanceID
WHERE S.InstanceID = @InstanceID
AND S.SnapshotDateUTC >=@FromDate
AND S.SnapshotDateUTC < @ToDate
ORDER BY S.SnapshotDateUTC DESC