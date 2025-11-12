CREATE PROC dbo.RunningQueries_Get(
	@InstanceID INT,
    @SnapshotDate DATETIME2(7)=NULL OUT, /* For backward compatibility & ease of use in selecting a single snapshot.  If set, it will override @SnapshotDateFrom & @SnapshotDateTo */
    @SnapshotDateFrom DATETIME2(7)=NULL OUT, /* If NULL the latest snapshot will be returned.  Set @SnapshotDateFrom to @SnapshotDateTo to return a specific snapshot */
    @SnapshotDateTo DATETIME2(7)=NULL OUT, /* If NULL the latest snapshot will be returned. Set @SnapshotDateFrom to @SnapshotDateTo to return a specific snapshot */
	@Skip INT=0, /* Option to return the next or previous snapshot. 1= next, -1 = previous, 0 = Snapshot at @SnapshotDate (default) */
    @SessionID INT=NULL, /* For tracking a session progress over multiple snapshots */
    @JobID UNIQUEIDENTIFIER=NULL, /* For tracking a job over multiple snapshots */
    @AppName NVARCHAR(128)=NULL, /* App name to filter on (LIKE).  Overridded if @JobID is specified */
    @ObjectName NVARCHAR(128)=NULL, /* Object name to filter on (LIKE). */
    @BatchText NVARCHAR(MAX)=NULL, /* Batch Text to filter on (LIKE). */
    @Text NVARCHAR(MAX) = NULL, /* Text to filter on (LIKE). */
    @HostName NVARCHAR(128)=NULL, /* Host Name to filter on (LIKE). */
    @WaitType NVARCHAR(60)=NULL, /* Wait Type to filter on (LIKE).  e.g. LCK% */
    @WaitResource NVARCHAR(256)=NULL,  /* Wait Resource to filter on (LIKE). e.g. 2% for tempdb */
    @LoginName NVARCHAR(128)=NULL, /* Login name to filter on (LIKE). */
    @Status NVARCHAR(30)=NULL, /* Status to filter on. e.g. e.g. runnable, running, rollback, sleeping, suspended */
    @DatabaseName NVARCHAR(128)=NULL, /* Database Name to filter on. (=) */
    @MinDurationMs BIGINT=NULL, /* Return queries with a duration greater than or equal to this threshold */
    @MinReads BIGINT=NULL, /* Return queries with reads greater than or equal to this threshold */
    @MinWrites BIGINT=NULL, /* Return queries with writes greater than or equal to this threshold */
    @MinLogicalReads BIGINT=NULL, /* Return queries with logical reads greater than or equal to this threshold */
    @MinCPUMs INT=NULL, /* Return queries with CPU (ms) greater than or equal to this threshold */
    @MinMemoryGrantKB INT=NULL, /* Return queries with a memory grant (KB) greater than or equal to this threshold */
    @BlockedOrBlocking BIT=0, /* Set to 1 to return only blocked or blocking queries */
    @MinSleepingSessionIdleTimeSec INT=NULL, /* Return queries with a sleeping session idle time (sec) greater than or equal to this threshold */
    @HasOpenTransaction BIT=NULL, /* Return queries with open transactions */
    @ContextInfo VARBINARY(128)=NULL, /* Context Info to filter on. e.g. 0x4400420041004400610073006800 */
    @QueryHash VARBINARY(8)=NULL, /* Query Hash to filter on */
    @QueryPlanHash VARBINARY(8)=NULL, /* Query Plan Hash to filter on */
    @SQLHandle VARBINARY(64)=NULL, /* SQL Handle to filter for */
    @PlanHandle VARBINARY(64)=NULL, /* Plan Handle to filter for */
    @Top INT=NULL, /* Limit the number of rows returned */
    @Debug BIT=0, /* Print dynamic SQL for debugging purposes */
    @HasCursors BIT= 0 OUTPUT /* Output parameter indicating if we have any cursors associated with this snapshot */
)
AS
IF @SnapshotDate IS NOT NULL
BEGIN
    SET @SnapshotDateFrom = @SnapshotDate
    SET @SnapshotDateTo = @SnapshotDate
END
IF @SnapshotDateFrom IS NULL /* If NULL, get the latest snapshot date */
BEGIN
    SELECT TOP(1)   @SnapshotDateFrom=SnapshotDateUTC,
                    @SnapshotDateTo=SnapshotDateUTC
    FROM dbo.RunningQueriesSummary
    WHERE InstanceID = @InstanceID
    ORDER BY SnapshotDateUTC DESC
END
IF @SnapshotDateTo IS NULL /* Set to same as from date if not specified */
BEGIN
    SET @SnapshotDateTo = @SnapshotDateFrom
END
IF @Skip NOT IN(0,1,-1)
BEGIN;
	THROW 50000,'Invalid value for parameter @Skip.  Valid options: 0,1,-1',1;
END
IF @Skip <> 0 AND @SnapshotDateFrom <> @SnapshotDateTo
BEGIN;
    THROW 50000,'@Skip should be set to 0 unless @SnapshotDateFrom = @SnapshotDateTo',1;
END
IF @Skip=1
BEGIN
	/* Get the next snapshot */
	SELECT TOP(1) @SnapshotDateFrom=SnapshotDateUTC,
                   @SnapshotDateTo = SnapshotDateUTC
    FROM dbo.RunningQueriesSummary
    WHERE InstanceID = @InstanceID
	AND SnapshotDateUTC > @SnapshotDateFrom
    ORDER BY SnapshotDateUTC
END
ELSE IF @Skip=-1
BEGIN
	/* Get the previous snapshot */
	SELECT TOP(1) @SnapshotDateFrom=SnapshotDateUTC,
                @SnapshotDateTo = SnapshotDateUTC
    FROM dbo.RunningQueriesSummary
    WHERE InstanceID = @InstanceID
	AND SnapshotDateUTC < @SnapshotDateFrom
    ORDER BY SnapshotDateUTC DESC
END 
IF @JobID IS NOT NULL
BEGIN
    /* Get app name to filter on for agent job if job id is specified */
    SET @AppName = 'SQLAgent - TSQL JobStep (Job ' + CONVERT(VARCHAR,CAST(@JobID AS BINARY(16)),1) + '%'
END
/* Used to display the cursors button in the UI */
SELECT @HasCursors = CASE WHEN EXISTS(
                                    SELECT 1
                                    FROM dbo.RunningQueriesCursors
                                    WHERE InstanceID = @InstanceID
                                    AND SnapshotDateUTC = @SnapshotDateFrom
                                    AND SnapshotDateUTC = @SnapshotDateTo /* HasCursors returns true for a specific snapshot only (both dates the same) */
                    )
                    THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END


DECLARE @SQL NVARCHAR(MAX)

SET @SQL = N'
SELECT ' + CASE WHEN @Top IS NULL THEN '' ELSE 'TOP(@Top)' END + '
       InstanceID,
       InstanceDisplayName,
       Duration,
       batch_text,
       text,
       object_id,
       object_name,
       DBADashObjectID,
       SchemaName,
       ObjectName,
       SnapshotDateUTC,
       session_id,
       command,
       status,
       wait_time,
       wait_type,
       TopSessionWaits,
       blocking_session_id,
       BlockingHierarchy,
       BlockCountRecursive, 
	   BlockWaitTimeRecursiveMs,
       BlockWaitTimeRecursive,
	   BlockCount,
       IsRootBlocker,
	   BlockWaitTimeMs,
       BlockWaitTime,
       cpu_time,
       logical_reads,
       reads,
       writes,
       granted_query_memory_kb,
       percent_complete,
       open_transaction_count,
       transaction_isolation_level,
       login_name,
       host_name,
       database_id,
       database_name,
       database_names,
       program_name,
       job_id,
       job_name,
       client_interface_name,
       start_time_utc,
       last_request_start_time_utc,
       last_request_end_time_utc,
       last_request_duration,
       sleeping_session_idle_time_sec,
       sleeping_session_idle_time,
       sql_handle,
       plan_handle,
       query_hash,
       query_plan_hash,
       [Duration (ms)],
       wait_resource,
       wait_resource_type,
       wait_database_id,
       wait_file_id,
       wait_page_id,
       wait_object_id,
       wait_index_id,
       wait_hobt,
       wait_hash,
       wait_slot,
       wait_is_compile,
       page_type,
       wait_db,
       wait_object,
       wait_file,
       login_time_utc,
       has_plan,
       statement_start_offset,
       statement_end_offset,
       context_info,
       transaction_duration_ms,
       transaction_duration,
       is_implicit_transaction,
       is_query_store_on,
       tempdb_current_mb,
       tempdb_allocations_mb,
       total_elapsed_time,
       TaskWaits,
       dop,
       cursor_text
FROM dbo.RunningQueriesInfo Q
WHERE Q.InstanceID = @InstanceID
' + CASE WHEN @SnapshotDateFrom = @SnapshotDateTo THEN 'AND Q.SnapshotDateUTC = @SnapshotDateFrom' ELSE 'AND Q.SnapshotDateUTC >= @SnapshotDateFrom AND Q.SnapshotDateUTC < @SnapshotDateTo' END + '
' + CASE WHEN @SessionID IS NULL THEN '' ELSE 'AND Q.session_id = @SessionID' END + '
' + CASE WHEN @AppName IS NULL THEN '' ELSE 'AND Q.program_name LIKE @AppName' END + '
' + CASE WHEN @ObjectName IS NULL THEN '' ELSE 'AND Q.object_name LIKE @ObjectName' END + '
' + CASE WHEN @Text IS NULL THEN '' ELSE 'AND Q.text LIKE @Text' END + '
' + CASE WHEN @BatchText IS NULL THEN '' ELSE 'AND Q.batch_text LIKE @BatchText' END + '
' + CASE WHEN @HostName IS NULL THEN '' ELSE 'AND Q.host_name LIKE @HostName' END + '
' + CASE WHEN @WaitType IS NULL THEN '' ELSE 'AND Q.wait_type LIKE @WaitType' END + '
' + CASE WHEN @WaitResource IS NULL THEN '' ELSE 'AND Q.wait_resource LIKE @WaitResource' END + '
' + CASE WHEN @LoginName IS NULL THEN '' ELSE 'AND Q.login_name LIKE @LoginName' END + '
' + CASE WHEN @Status IS NULL THEN '' ELSE 'AND Q.status = @Status' END + '
' + CASE WHEN @DatabaseName IS NULL THEN '' ELSE 'AND Q.database_name = @DatabaseName' END + '
' + CASE WHEN @MinDurationMs IS NULL THEN '' ELSE 'AND Q.[Duration (ms)]>=@MinDurationMs' END + '
' + CASE WHEN @MinReads IS NULL THEN '' ELSE 'AND Q.reads>=@MinReads' END + '
' + CASE WHEN @MinWrites IS NULL THEN '' ELSE 'AND Q.writes>=@MinWrites' END + '
' + CASE WHEN @MinLogicalReads IS NULL THEN '' ELSE 'AND Q.logical_reads>=@MinLogicalReads' END + '
' + CASE WHEN @MinCPUMs IS NULL THEN '' ELSE 'AND Q.cpu_time>=@MinCPUMs' END + '
' + CASE WHEN @MinMemoryGrantKB IS NULL THEN '' ELSE 'AND Q.granted_query_memory_kb>=@MinMemoryGrantKB' END + '
' + CASE WHEN @MinMemoryGrantKB IS NULL THEN '' ELSE 'AND Q.granted_query_memory_kb>=@MinMemoryGrantKB' END + '
' + CASE WHEN @MinSleepingSessionIdleTimeSec IS NULL THEN '' ELSE  'AND Q.sleeping_session_idle_time_sec >= @MinSleepingSessionIdleTimeSec' END + '
' + CASE WHEN @HasOpenTransaction IS NULL THEN '' WHEN @HasOpenTransaction=1 THEN 'AND Q.open_transaction_count>0' ELSE 'AND Q.open_transaction_count=0' END + '
' + CASE WHEN @ContextInfo IS NULL THEN '' ELSE 'AND Q.context_info = @ContextInfo' END + '
' + CASE WHEN @QueryHash IS NULL THEN '' ELSE 'AND Q.query_hash_bin = @QueryHash' END + '
' + CASE WHEN @QueryPlanHash IS NULL THEN '' ELSE 'AND Q.query_plan_hash_bin = @QueryPlanHash' END + '
' + CASE WHEN @SQLHandle IS NULL THEN '' ELSE 'AND Q.sql_handle_bin = @SQLHandle' END + '
' + CASE WHEN @PlanHandle IS NULL THEN '' ELSE 'AND Q.plan_handle_bin = @PlanHandle' END + '
' + CASE WHEN @BlockedOrBlocking=1 THEN 'AND (Q.blocking_session_id<>0 OR Q.BlockCount>0)' ELSE '' END + '
ORDER BY Q.SnapshotDateUTC,Q.Duration DESC'

IF @Debug=1
BEGIN
    EXEC dbo.PrintMax @SQL
END

EXEC sp_executesql @SQL,N'@InstanceID INT,
                         @SnapshotDateFrom DATETIME2(7),
                         @SnapshotDateTo DATETIME2(7),
                         @SessionID INT,
                         @AppName NVARCHAR(128),
                         @ObjectName NVARCHAR(128),
                         @Text NVARCHAR(MAX),
                         @BatchText NVARCHAR(MAX),
                         @WaitType NVARCHAR(60),
                         @WaitResource NVARCHAR(256),
                         @LoginName NVARCHAR(128),
                         @Status NVARCHAR(30),
                         @DatabaseName NVARCHAR(128),
                         @MinDurationMs BIGINT,
                         @MinReads BIGINT,
                         @MinWrites BIGINT,
                         @MinLogicalReads BIGINT,
                         @MinCPUMs INT,
                         @MinMemoryGrantKB INT,
                         @MinSleepingSessionIdleTimeSec NUMERIC(26,6),
                         @ContextInfo VARBINARY(128),
                         @QueryHash VARBINARY(8),
                         @QueryPlanHash VARBINARY(8),
                         @SQLHandle VARBINARY(64),
                         @PlanHandle VARBINARY(64),
                         @HostName NVARCHAR(128),
                         @Top INT',
                         @InstanceID,
                         @SnapshotDateFrom,
                         @SnapshotDateTo,
                         @SessionID,
                         @AppName,
                         @ObjectName,
                         @Text,
                         @BatchText,
                         @WaitType,
                         @WaitResource,
                         @LoginName,
                         @Status,
                         @DatabaseName,
                         @MinDurationMs,

                         @MinReads,
                         @MinWrites,
                         @MinLogicalReads,
                         @MinCPUMs,
                         @MinMemoryGrantKB,
                         @MinSleepingSessionIdleTimeSec,
                         @ContextInfo,
                         @QueryHash,
                         @QueryPlanHash,
                         @SQLHandle,
                         @PlanHandle,
                         @HostName,
                         @Top