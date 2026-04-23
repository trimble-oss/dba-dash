CREATE PROC DBADash.AI_RunningQueries_Get(
    @MaxRows INT = 300,
    @InstanceFilter NVARCHAR(256) = NULL,
    @HoursBack INT = 24
)
AS
/* Result set 1: Running queries summary trends (aggregated per snapshot) */
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    rqs.SnapshotDateUTC,
    rqs.RunningQueries,
    rqs.BlockedQueries,
    rqs.BlockedQueriesWaitMs,
    rqs.LongestRunningQueryMs,
    rqs.MaxMemoryGrant,
    rqs.SumMemoryGrant,
    rqs.CriticalWaitCount,
    rqs.CriticalWaitTime,
    rqs.SleepingSessionsCount,
    rqs.SleepingSessionsMaxIdleTimeMs,
    rqs.OldestTransactionMs,
    rqs.TempDBCurrentPageCount
FROM dbo.RunningQueriesSummary rqs
INNER JOIN dbo.Instances i ON i.InstanceID = rqs.InstanceID
WHERE rqs.SnapshotDateUTC >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY rqs.SnapshotDateUTC DESC

/* Result set 2: Recent long-running or blocked queries (detail from latest snapshots) */
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    rq.SnapshotDateUTC,
    rq.session_id,
    rq.status,
    rq.command,
    rq.wait_type,
    rq.wait_time AS WaitTimeMs,
    rq.wait_resource,
    rq.blocking_session_id,
    rq.cpu_time AS CpuTimeMs,
    rq.logical_reads,
    rq.reads AS PhysicalReads,
    rq.writes,
    rq.total_elapsed_time AS ElapsedTimeMs,
    rq.granted_query_memory AS GrantedMemoryPages,
    rq.dop,
    rq.open_transaction_count,
    rq.login_name,
    rq.host_name,
    rq.program_name,
    rq.database_id,
    rq.start_time_utc,
    rq.tempdb_alloc_page_count AS TempDBAllocPages
FROM dbo.RunningQueries rq
INNER JOIN dbo.Instances i ON i.InstanceID = rq.InstanceID
WHERE rq.SnapshotDateUTC >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
  AND (
    rq.total_elapsed_time > 30000           -- running > 30 seconds
    OR rq.blocking_session_id > 0           -- blocked
    OR rq.granted_query_memory > 100000     -- large memory grant (> ~780MB)
    OR rq.open_transaction_count > 1        -- multiple open transactions
    OR rq.wait_type IN ('LCK_M_S','LCK_M_X','LCK_M_U','LCK_M_IS','LCK_M_IX','LCK_M_SCH_M','LCK_M_BU')
  )
ORDER BY rq.total_elapsed_time DESC
