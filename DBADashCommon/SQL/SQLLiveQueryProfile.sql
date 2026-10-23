/*
The operator counters of a request that is still running, from sys.dm_exec_query_profiles - see
LiveQueryProfileMessage.  Parameters: @SessionID, @ExpectedStartUtc (the request start time from the snapshot, UTC) and
@SnapshotUtcOffset (the instance UTC offset in minutes collected with the snapshot).

The counters are read between two reads of the request - start time, statement offsets, plan hash and batch
(sql_handle) - and only returned when both agree.  The DMVs can't be read together atomically, and counters taken as the
request moved on to its next statement would be put down to the statement read before them: written into the operators
of the wrong plan.  A request that moved is reported as Moving with no counters, so the GUI skips that refresh rather
than failing - the next one sees the new statement and fetches its plan.  One that has finished by the second read is
Completed.

The request is the one whose start time matches the snapshot, not the lowest request_id: a MARS session can have
several, and the running-queries snapshot keeps the highest-CPU one.  That request_id is what the counters and the
second read use.  The start time matches exactly, unless the instance offset has changed by at most an hour since the
snapshot, in which case it matches shifted by that change.

actual_read_row_count isn't in every version that has sys.dm_exec_query_profiles, so the counters are selected with
dynamic SQL that includes it where the column exists.  The live start time is converted to UTC using the same
whole-minute offset the collector uses.
*/
SET NOCOUNT ON;
DECLARE @UTCOffset INT = CAST(ROUND(DATEDIFF(s,GETDATE(),GETUTCDATE())/60.0,0) AS INT);
/* Same start-time match as LivePlanMessage: exact, or shifted by a daylight saving change of at most an hour */
DECLARE @OffsetDelta INT = @UTCOffset - @SnapshotUtcOffset;
IF ABS(@OffsetDelta) > 60 SET @OffsetDelta = 0;
DECLARE @Expected DATETIME = DATEADD(mi, @OffsetDelta, @ExpectedStartUtc);
DECLARE @Status VARCHAR(20);
DECLARE @startUtc DATETIME, @RequestID INT, @StartOffset INT, @EndOffset INT, @PlanHash BINARY(8), @SqlHandle VARBINARY(64), @CpuTime INT, @ElapsedTime INT;
DECLARE @startUtc2 DATETIME, @StartOffset2 INT, @EndOffset2 INT, @PlanHash2 BINARY(8), @SqlHandle2 VARBINARY(64);

CREATE TABLE #Profiles(
    node_id INT,
    thread_id INT,
    row_count BIGINT,
    rewind_count BIGINT,
    rebind_count BIGINT,
    end_of_scan_count BIGINT,
    elapsed_time_ms BIGINT,
    cpu_time_ms BIGINT,
    scan_count BIGINT,
    logical_read_count BIGINT,
    physical_read_count BIGINT,
    read_ahead_count BIGINT,
    lob_logical_read_count BIGINT,
    lob_physical_read_count BIGINT,
    actual_read_row_count BIGINT,
    open_time BIGINT,
    close_time BIGINT
);

DECLARE @SQL NVARCHAR(MAX) = N'
INSERT INTO #Profiles
SELECT  node_id,
        thread_id,
        row_count,
        rewind_count,
        rebind_count,
        end_of_scan_count,
        elapsed_time_ms,
        cpu_time_ms,
        scan_count,
        logical_read_count,
        physical_read_count,
        read_ahead_count,
        lob_logical_read_count,
        lob_physical_read_count,
        ' + CASE WHEN COL_LENGTH('sys.dm_exec_query_profiles','actual_read_row_count') IS NULL THEN N'CAST(NULL AS BIGINT)' ELSE N'actual_read_row_count' END + N' AS actual_read_row_count,
        open_time,
        close_time
FROM sys.dm_exec_query_profiles
WHERE session_id = @SessionID
AND request_id = @RequestID
OPTION(RECOMPILE)';

SELECT  TOP(1) @startUtc = DATEADD(mi,@UTCOffset,r.start_time),
        @RequestID = r.request_id,
        @StartOffset = r.statement_start_offset,
        @EndOffset = r.statement_end_offset,
        @PlanHash = r.query_plan_hash,
        @SqlHandle = r.sql_handle,
        @CpuTime = r.cpu_time,
        @ElapsedTime = r.total_elapsed_time
FROM sys.dm_exec_requests r
WHERE r.session_id = @SessionID
ORDER BY CASE WHEN DATEADD(mi,@UTCOffset,r.start_time) = @Expected THEN 0 ELSE 1 END,
         r.cpu_time DESC,
         r.request_id;

/* Exact, apart from a daylight saving change - see LivePlanMessage */
IF @startUtc IS NULL OR @startUtc <> @Expected
BEGIN
    SET @Status = 'Completed';
END
ELSE
BEGIN
    EXEC sp_executesql @SQL, N'@SessionID INT, @RequestID INT', @SessionID = @SessionID, @RequestID = @RequestID;

    SELECT  @startUtc2 = DATEADD(mi,@UTCOffset,r.start_time),
            @StartOffset2 = r.statement_start_offset,
            @EndOffset2 = r.statement_end_offset,
            @PlanHash2 = r.query_plan_hash,
            @SqlHandle2 = r.sql_handle
    FROM sys.dm_exec_requests r
    WHERE r.session_id = @SessionID
    AND r.request_id = @RequestID;

    /* On a different statement or batch by the second read, the counters could be for either. Same positive match
       as LivePlanMessage: NULL = NULL is unknown, so each field matches when equal or both NULL, and anything else
       is Moving rather than a match that failed open. */
    SET @Status = CASE WHEN @startUtc2 IS NULL THEN 'Completed'
                    WHEN @startUtc2 = @startUtc
                        AND (@StartOffset2 = @StartOffset OR (@StartOffset2 IS NULL AND @StartOffset IS NULL))
                        AND (@EndOffset2 = @EndOffset OR (@EndOffset2 IS NULL AND @EndOffset IS NULL))
                        AND (@PlanHash2 = @PlanHash OR (@PlanHash2 IS NULL AND @PlanHash IS NULL))
                        AND (@SqlHandle2 = @SqlHandle OR (@SqlHandle2 IS NULL AND @SqlHandle IS NULL)) THEN 'Running'
                    ELSE 'Moving' END;
END

IF @Status <> 'Running'
BEGIN
    SELECT @Status AS Status, SYSUTCDATETIME() AS captured_utc;
    RETURN;
END

SELECT  @Status AS Status,
        SYSUTCDATETIME() AS captured_utc,
        @StartOffset AS statement_start_offset,
        @EndOffset AS statement_end_offset,
        @PlanHash AS query_plan_hash,
        @SqlHandle AS sql_handle,
        @CpuTime AS cpu_time,
        @ElapsedTime AS total_elapsed_time;

SELECT * FROM #Profiles;
