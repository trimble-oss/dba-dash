CREATE VIEW dbo.RunningQueriesPerformanceCounters
AS
SELECT	unpvt.InstanceID,
		C.CounterID,
		unpvt.SnapshotDateUTC AS SnapshotDate,	
		unpvt.Value
FROM (
	SELECT	InstanceID,
			SnapshotDateUTC,
			CAST(RunningQueries AS DECIMAL(28,9)) AS [Running Query Count],
			CAST(BlockedQueries AS DECIMAL(28,9)) AS [Blocked Query Count],
			CAST(BlockedQueriesWaitMs AS DECIMAL(28,9)) AS [Blocked Queries Wait (ms)],
			CAST(MaxMemoryGrant*8 AS DECIMAL(28,9)) AS [Max Memory Grant KB],
			CAST(SumMemoryGrant*8 AS DECIMAL(28,9)) AS [Sum Memory Grant KB],
			CAST(LongestRunningQueryMs AS DECIMAL(28,9)) AS [Longest Running Query (ms)],
			CAST(CriticalWaitCount AS DECIMAL(28,9)) AS [Critical Wait Count],
			CAST(CriticalWaitTime AS DECIMAL(28,9)) AS [Critical Wait Time (ms)],
			CAST(TempDBWaitCount AS DECIMAL(28,9)) AS [TempDB Wait Count],
			CAST(TempDBWaitTimeMs AS DECIMAL(28,9)) AS [TempDB Wait Time (ms)],
			CAST(SleepingSessionsCount AS DECIMAL(28,9)) AS [Sleeping Sessions Count],
			CAST(SleepingSessionsMaxIdleTimeMs AS DECIMAL(28,9)) AS [Sleeping Sessions Max Idle Time (ms)],
			CAST(ISNULL(OldestTransactionMs,0) AS DECIMAL(28,9)) AS [Oldest Transaction (ms)]
	FROM dbo.RunningQueriesSummary
	) RQS
UNPIVOT( 
	Value FOR CounterName IN([Running Query Count],
							[Blocked Query Count], 
							[Blocked Queries Wait (ms)], 
							[Max Memory Grant KB], 
							[Sum Memory Grant KB], 
							[Longest Running Query (ms)], 
							[Critical Wait Count], 
							[Critical Wait Time (ms)], 
							[TempDB Wait Count], 
							[TempDB Wait Time (ms)], 
							[Sleeping Sessions Count], 
							[Sleeping Sessions Max Idle Time (ms)],
							[Oldest Transaction (ms)]
							)
) unpvt
JOIN dbo.Counters C ON C.counter_name = unpvt.CounterName AND C.object_name = 'Running Queries' AND C.instance_name = '' 
WHERE C.CounterType = 2