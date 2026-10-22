/*
Checks the same request is still running then returns its live plan, from sys.dm_exec_query_statistics_xml, and which
statement it is for - see LivePlanMessage.  Parameters: @SessionID, @ExpectedStartUtc (the request start time from the
snapshot, UTC) and @SnapshotUtcOffset (the instance UTC offset in minutes collected with the snapshot).

The DMVs can't be read together atomically: the request could finish (and another start on the session) between
checking it and taking the plan, or move on to its next statement between taking the plan and reading which statement
is running.  A plan put down to the wrong statement would have the next statement's counters written into it on
refresh.  So the request - start time, statement offsets, plan hash and batch (sql_handle) - is read before and after
the plan is taken.  If the statement changed in between, the plan can't be put down to either, and the user is asked to
try again.  One that has finished by the second read keeps the plan: it was taken from the request checked, and there
are no more statements to confuse it with.

The DMV doesn't exist before SQL 2016 SP1 so it's queried with sp_executesql to allow the batch to compile on older
versions.  The live start time is converted to UTC using the same whole-minute offset the collector uses.  It matches
the snapshot exactly, unless the instance offset has changed by at most an hour since the snapshot, in which case it
matches shifted by that change.
*/
SET NOCOUNT ON;
DECLARE @UTCOffset INT = CAST(ROUND(DATEDIFF(s,GETDATE(),GETUTCDATE())/60.0,0) AS INT);
/* The snapshot start was made UTC with @SnapshotUtcOffset. The same local start converted now differs by exactly
   the change in that offset, and only when the change is a daylight saving shift of at most an hour. */
DECLARE @OffsetDelta INT = @UTCOffset - @SnapshotUtcOffset;
IF ABS(@OffsetDelta) > 60 SET @OffsetDelta = 0;
DECLARE @Expected DATETIME = DATEADD(mi, @OffsetDelta, @ExpectedStartUtc);
DECLARE @msg NVARCHAR(400), @QueryPlan NVARCHAR(MAX);
DECLARE @RequestID INT, @startUtc DATETIME, @StartOffset INT, @EndOffset INT, @PlanHash BINARY(8), @SqlHandle VARBINARY(64), @CpuTime INT, @ElapsedTime INT;
DECLARE @startUtc2 DATETIME, @StartOffset2 INT, @EndOffset2 INT, @PlanHash2 BINARY(8), @SqlHandle2 VARBINARY(64);

IF OBJECT_ID('sys.dm_exec_query_statistics_xml') IS NULL
BEGIN
    THROW 50000, N'Live query plans require SQL Server 2016 SP1 or later.', 1;
END

/* A session can have several requests (MARS). The running-queries snapshot keeps the highest-CPU one, so the request
   whose start time is the snapshot's is taken and its request_id is what the plan and the second read use. Highest
   CPU, then request_id, breaks a tie where two requests share a start time. */
SELECT TOP(1) @RequestID = r.request_id,
        @startUtc = DATEADD(mi,@UTCOffset,r.start_time),
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

IF @startUtc IS NULL
BEGIN
    SET @msg = CONCAT(N'Session ', @SessionID, N' is no longer running a request.  The query may have completed.');
    THROW 50000, @msg, 1;
END
IF @startUtc <> @Expected
BEGIN
    SET @msg = CONCAT(N'Session ', @SessionID, N' is no longer running the same request (the request start time has changed).  The query may have completed.');
    THROW 50000, @msg, 1;
END

EXEC sp_executesql N'SELECT @QueryPlan = CAST(query_plan AS NVARCHAR(MAX)) FROM sys.dm_exec_query_statistics_xml(@SessionID) WHERE request_id = @RequestID',
    N'@SessionID INT, @RequestID INT, @QueryPlan NVARCHAR(MAX) OUTPUT',
    @SessionID = @SessionID, @RequestID = @RequestID, @QueryPlan = @QueryPlan OUTPUT;

SELECT @startUtc2 = DATEADD(mi,@UTCOffset,r.start_time),
        @StartOffset2 = r.statement_start_offset,
        @EndOffset2 = r.statement_end_offset,
        @PlanHash2 = r.query_plan_hash,
        @SqlHandle2 = r.sql_handle
FROM sys.dm_exec_requests r
WHERE r.session_id = @SessionID
AND r.request_id = @RequestID;

/* Still running and on a different statement or batch: the plan could be for either. Offsets are relative to the
   current batch and the plan hash is the shape, so the batch (sql_handle) is part of which statement this is.
   A comparison with NULL is unknown rather than false, and NOT unknown does not enter an IF, so the match has to
   be positively true. Anything else - a hash or handle going to or from NULL included - is a different statement. */
DECLARE @SameStatement BIT = CASE
    WHEN @startUtc2 = @startUtc
        AND (@StartOffset2 = @StartOffset OR (@StartOffset2 IS NULL AND @StartOffset IS NULL))
        AND (@EndOffset2 = @EndOffset OR (@EndOffset2 IS NULL AND @EndOffset IS NULL))
        AND (@PlanHash2 = @PlanHash OR (@PlanHash2 IS NULL AND @PlanHash IS NULL))
        AND (@SqlHandle2 = @SqlHandle OR (@SqlHandle2 IS NULL AND @SqlHandle IS NULL))
    THEN 1 ELSE 0 END;

IF @startUtc2 IS NOT NULL AND @SameStatement = 0
BEGIN
    SET @msg = CONCAT(N'Session ', @SessionID, N' moved on to another statement while its live plan was being captured.  Try again.');
    THROW 50000, @msg, 1;
END

IF @QueryPlan IS NULL
BEGIN
    SET @msg = CONCAT(N'A live plan is not available for session ', @SessionID, N'.  Some statements have no plan to show - WAITFOR, BACKUP, DBCC and the like.  Otherwise lightweight query profiling is required - it''s on by default from SQL 2019 (LIGHTWEIGHT_QUERY_PROFILING database scoped configuration) and needs trace flag 7412 on SQL 2016 SP1 and 2017.  The query might also have completed.');
END

/* The statistics XML often leaves StatementText blank. The statement is cut out of the batch the same way the running queries view does, so the plan viewer has a query to show. */
DECLARE @StatementText NVARCHAR(MAX);
SELECT @StatementText = SUBSTRING(t.text,
        ISNULL((NULLIF(@StartOffset, -1) / 2) + 1, 0),
        ISNULL((NULLIF(NULLIF(@EndOffset, -1), 0) - NULLIF(@StartOffset, -1)) / 2 + 1, 2147483647))
FROM sys.dm_exec_sql_text(@SqlHandle) t;

/* The statement the plan is for, as read either side of taking it, so a live refresh can tell when the request has moved on to another one.
   A statement with no plan is still this request: query_plan is null and unavailable_reason says why, so a refresh can skip it and keep polling. */
SELECT @SessionID AS session_id,
    @startUtc AS start_time_utc,
    SYSUTCDATETIME() AS captured_utc,
    @StartOffset AS statement_start_offset,
    @EndOffset AS statement_end_offset,
    @PlanHash AS query_plan_hash,
    @SqlHandle AS sql_handle,
    @CpuTime AS cpu_time,
    @ElapsedTime AS total_elapsed_time,
    @QueryPlan AS query_plan,
    @StatementText AS statement_text,
    @msg AS unavailable_reason;
