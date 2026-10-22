/*
Operator counters from sys.dm_exec_query_profiles for the long running requests in a running queries snapshot, so the
collection can keep how far each one had got - see LiveProfileCollector.

@Requests lists the requests as the snapshot saw them:
<requests><request session_id="" start_time_utc="" statement_start_offset="" statement_end_offset="" query_plan_hash="" sql_handle=""/></requests>
with the hashes and handles as hex, without 0x.

The counters are read between two reads of each request, as the live query profile message does, and only returned
when both find it on the statement from the snapshot: the same start time, offsets, plan hash and batch.  The DMVs can't
be read together atomically, and counters read as the request moved on would be written into the operators of the
wrong plan.  A request that has finished or moved on is left out.

sys.dm_exec_query_profiles is read one request at a time, by session and request id.  Joined to the list it can be read
for every running query on the instance, which is the cost the threshold is there to avoid.  It doesn't exist before
SQL 2014, and actual_read_row_count isn't in every version that has it, so it is read with dynamic SQL.  Without
lightweight query profiling it has no rows, and nothing is returned.

Returns the requests still running the snapshot's statement, then their counters.
*/
SET NOCOUNT ON;
DECLARE @UTCOffset INT = CAST(ROUND(DATEDIFF(s,GETDATE(),GETUTCDATE())/60.0,0) AS INT);

CREATE TABLE #Requests(
    session_id INT NOT NULL,
    start_time_utc DATETIME NOT NULL,
    statement_start_offset INT NOT NULL,
    statement_end_offset INT NOT NULL,
    query_plan_hash BINARY(8) NOT NULL,
    sql_handle VARBINARY(64) NOT NULL,
    request_id INT NULL,
    cpu_time INT NULL,
    total_elapsed_time INT NULL
);

CREATE TABLE #Profiles(
    session_id INT,
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

INSERT INTO #Requests(session_id,start_time_utc,statement_start_offset,statement_end_offset,query_plan_hash,sql_handle)
SELECT  r.value('@session_id','INT'),
        r.value('@start_time_utc','DATETIME'),
        r.value('@statement_start_offset','INT'),
        r.value('@statement_end_offset','INT'),
        r.value('xs:hexBinary(@query_plan_hash)','BINARY(8)'),
        r.value('xs:hexBinary(@sql_handle)','VARBINARY(64)')
FROM @Requests.nodes('/requests/request') AS n(r);

IF OBJECT_ID('sys.dm_exec_query_profiles') IS NOT NULL
BEGIN
    /* First read: still running the statement the snapshot saw */
    UPDATE t
        SET t.request_id = r.request_id,
            t.cpu_time = r.cpu_time,
            t.total_elapsed_time = r.total_elapsed_time
    FROM #Requests t
    JOIN sys.dm_exec_requests r ON r.session_id = t.session_id
    WHERE DATEADD(mi,@UTCOffset,r.start_time) = t.start_time_utc
    AND r.statement_start_offset = t.statement_start_offset
    AND r.statement_end_offset = t.statement_end_offset
    AND r.query_plan_hash = t.query_plan_hash
    AND r.sql_handle = t.sql_handle;

    DECLARE @SQL NVARCHAR(MAX) = N'
    INSERT INTO #Profiles
    SELECT  @SessionID,
            node_id,
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

    DECLARE @SessionID INT, @RequestID INT;
    DECLARE cRequests CURSOR LOCAL FAST_FORWARD FOR
        SELECT session_id, request_id
        FROM #Requests
        WHERE request_id IS NOT NULL;
    OPEN cRequests;
    FETCH NEXT FROM cRequests INTO @SessionID, @RequestID;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC sp_executesql @SQL, N'@SessionID INT, @RequestID INT', @SessionID = @SessionID, @RequestID = @RequestID;
        FETCH NEXT FROM cRequests INTO @SessionID, @RequestID;
    END
    CLOSE cRequests;
    DEALLOCATE cRequests;
END

/* Second read: on a different statement or finished by now, the counters could be for either */
DELETE t
FROM #Requests t
WHERE t.request_id IS NULL
OR NOT EXISTS(
        SELECT 1
        FROM sys.dm_exec_requests r
        WHERE r.session_id = t.session_id
        AND r.request_id = t.request_id
        AND DATEADD(mi,@UTCOffset,r.start_time) = t.start_time_utc
        AND r.statement_start_offset = t.statement_start_offset
        AND r.statement_end_offset = t.statement_end_offset
        AND r.query_plan_hash = t.query_plan_hash
        AND r.sql_handle = t.sql_handle
        );

SELECT  session_id,
        cpu_time,
        total_elapsed_time,
        SYSUTCDATETIME() AS captured_utc
FROM #Requests;

SELECT P.*
FROM #Profiles P
WHERE EXISTS(SELECT 1 FROM #Requests t WHERE t.session_id = P.session_id);
