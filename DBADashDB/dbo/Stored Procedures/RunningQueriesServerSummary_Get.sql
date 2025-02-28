CREATE PROC dbo.RunningQueriesServerSummary_Get(
	@InstanceIDs NVARCHAR(MAX)=NULL,
	@ShowHidden BIT=1
)
AS
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;
WITH T AS (
	SELECT I.InstanceID,
		   I.InstanceDisplayName,
		   S.SnapshotDateUTC,
		   S.RunningQueries,
		   S.BlockedQueries,
		   S.BlockedQueriesWaitMs,
		   S.MaxMemoryGrant*8 as MaxMemoryGrantKB,
		   S.SumMemoryGrant*8 as SumMemoryGrantKB,
		   S.LongestRunningQueryMs,
		   S.CriticalWaitCount,
		   S.CriticalWaitTime,
		   S.TempDBWaitCount,
		   S.TempDBWaitTimeMs,
		   S.SleepingSessionsCount,
		   S.SleepingSessionsMaxIdleTimeMs,
		   S.OldestTransactionMs,
		   ROW_NUMBER() OVER(PARTITION BY S.InstanceID ORDER BY S.SnapshotDateUTC DESC) rnum
	FROM dbo.RunningQueriesSummary S 
	JOIN dbo.Instances I ON I.InstanceID = S.InstanceID
	WHERE EXISTS(SELECT 1 FROM @Instances t WHERE t.InstanceID = I.InstanceID)
	AND S.SnapshotDateUTC>=DATEADD(mi,-15,GETUTCDATE())
	AND S.SnapshotDateUTC< DATEADD(mi,1,GETUTCDATE())
	AND (I.ShowInSummary=1 OR @ShowHidden=1)
)
SELECT T.InstanceID,
       T.InstanceDisplayName,
       T.SnapshotDateUTC,
       T.RunningQueries,
       T.BlockedQueries,
       T.BlockedQueriesWaitMs,
	   BlockedHD.HumanDuration AS BlockedQueriesWait,
       T.MaxMemoryGrantKB,
	   T.SumMemoryGrantKB,
       T.LongestRunningQueryMs,
	   LongestHD.HumanDuration AS LongestRunningQuery,
       T.CriticalWaitCount,
       T.CriticalWaitTime,
	   T.TempDBWaitCount,
	   T.TempDBWaitTimeMs,
	   TempDBHD.HumanDuration AS TempDBWaitTime,
	   T.SleepingSessionsCount,
       T.SleepingSessionsMaxIdleTimeMs,
       MaxIdleHD.HumanDuration as MaxIdleTime,
	   T.OldestTransactionMs,
	   OldestTranHD.HumanDuration AS OldestTransaction
FROM T 
CROSS APPLY dbo.MillisecondsToHumanDuration (T.LongestRunningQueryMs) LongestHD
CROSS APPLY dbo.MillisecondsToHumanDuration (T.BlockedQueriesWaitMs) BlockedHD
CROSS APPLY dbo.MillisecondsToHumanDuration (T.TempDBWaitTimeMs) TempDBHD
CROSS APPLY dbo.MillisecondsToHumanDuration (T.SleepingSessionsMaxIdleTimeMs) MaxIdleHD
CROSS APPLY dbo.MillisecondsToHumanDuration (T.OldestTransactionMs) OldestTranHD
WHERE T.rnum = 1
ORDER BY T.RunningQueries DESC