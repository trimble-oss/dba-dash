CREATE PROC dbo.RunningQueriesSummary_Get(
	@InstanceID INT,
	@FromDate DATETIME2,
	@ToDate DATETIME2,
	@MaxRows INT=2000
)
AS
SELECT TOP(@MaxRows) I.InstanceID,
	   I.ConnectionID AS Instance,
       I.InstanceDisplayName,
	   S.SnapshotDateUTC,
       S.RunningQueries,
       S.BlockedQueries,
       S.BlockedQueriesWaitMs,
       BlockedHD.HumanDuration AS BlockedQueriesWait,
       S.MaxMemoryGrant*8 AS MaxMemoryGrantKB,
       S.SumMemoryGrant*8 AS SumMemoryGrantKB,
       S.LongestRunningQueryMs,
       LongestHD.HumanDuration AS LongestRunningQuery,
       S.CriticalWaitCount,
       S.CriticalWaitTime,
       S.TempDBWaitCount,
       S.TempDBWaitTimeMs,
       TempDBHD.HumanDuration AS TempDBWaitTime,
       S.SleepingSessionsCount,
       S.SleepingSessionsMaxIdleTimeMs,
       MaxIdleHD.HumanDuration as MaxIdleTime,
       S.OldestTransactionMs,
       OldestTranHD.HumanDuration AS OldestTransaction,
       S.TempDBCurrentPageCount / 128.0 AS TempDBCurrentMB
FROM dbo.RunningQueriesSummary S
JOIN dbo.Instances I ON I.InstanceID = S.InstanceID
CROSS APPLY dbo.MillisecondsToHumanDuration (S.LongestRunningQueryMs) LongestHD
CROSS APPLY dbo.MillisecondsToHumanDuration (S.BlockedQueriesWaitMs) BlockedHD
CROSS APPLY dbo.MillisecondsToHumanDuration (S.TempDBWaitTimeMs) TempDBHD
CROSS APPLY dbo.MillisecondsToHumanDuration (S.SleepingSessionsMaxIdleTimeMs) MaxIdleHD
CROSS APPLY dbo.MillisecondsToHumanDuration (S.OldestTransactionMs) OldestTranHD
WHERE S.InstanceID = @InstanceID
AND S.SnapshotDateUTC >=@FromDate
AND S.SnapshotDateUTC < @ToDate
ORDER BY S.SnapshotDateUTC DESC