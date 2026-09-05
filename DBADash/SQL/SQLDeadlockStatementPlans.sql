/*
Lists the cached plans for a statement captured in a deadlock graph, with the execution stats that go
with them.

A deadlock graph identifies its statements by sql handle, which is the handle of the whole batch or
module - so one handle can have several statements, and a statement can have several plans (a
recompile, or different SET options, produce another cache entry).  Returning "the" plan would hide
that, so this returns the lot and lets the caller pick: the row matching the graph's statement offset
is flagged and sorted first.

Nothing here fetches plan XML.  The rows carry the plan handle and statement offsets, which is what
the GUI's existing plan collection path takes, so the plan is only pulled for the row the user asks
for.  Handles and hashes are converted to their 0x string form for the same reason - that is what the
plan collection path expects.

No rows means the batch or module is no longer in the plan cache at all.
*/
SET NOCOUNT ON;

CREATE TABLE #plans
(
    is_deadlock_statement BIT NOT NULL,
    statement_text NVARCHAR(MAX) NULL,
    execution_count BIGINT NULL,
    avg_cpu_ms DECIMAL(28, 3) NULL,
    total_cpu_ms DECIMAL(28, 3) NULL,
    avg_duration_ms DECIMAL(28, 3) NULL,
    total_duration_ms DECIMAL(28, 3) NULL,
    avg_logical_reads BIGINT NULL,
    avg_physical_reads BIGINT NULL,
    avg_writes BIGINT NULL,
    last_execution_time DATETIME NULL,
    creation_time DATETIME NULL,
    plan_handle VARCHAR(130) NULL,
    query_hash VARCHAR(18) NULL,
    query_plan_hash VARCHAR(18) NULL,
    statement_start_offset INT NOT NULL,
    statement_end_offset INT NOT NULL,
    source VARCHAR(30) NOT NULL
);

INSERT INTO #plans
(
    source, is_deadlock_statement, statement_text, execution_count, avg_cpu_ms, total_cpu_ms,
    avg_duration_ms, total_duration_ms, avg_logical_reads, avg_physical_reads, avg_writes,
    last_execution_time, creation_time, plan_handle, query_hash, query_plan_hash,
    statement_start_offset, statement_end_offset
)
SELECT 'Plan cache',
       CASE WHEN qs.statement_start_offset = @StatementStart THEN 1 ELSE 0 END,
       SUBSTRING(
           st.text,
           (qs.statement_start_offset / 2) + 1,
           ((CASE qs.statement_end_offset
                 WHEN -1 THEN DATALENGTH(st.text)
                 ELSE qs.statement_end_offset
             END - qs.statement_start_offset) / 2) + 1),
       qs.execution_count,
       (qs.total_worker_time / 1000.0) / NULLIF(qs.execution_count, 0),
       qs.total_worker_time / 1000.0,
       (qs.total_elapsed_time / 1000.0) / NULLIF(qs.execution_count, 0),
       qs.total_elapsed_time / 1000.0,
       qs.total_logical_reads / NULLIF(qs.execution_count, 0),
       qs.total_physical_reads / NULLIF(qs.execution_count, 0),
       qs.total_logical_writes / NULLIF(qs.execution_count, 0),
       qs.last_execution_time,
       qs.creation_time,
       CONVERT(VARCHAR(130), qs.plan_handle, 1),
       CONVERT(VARCHAR(18), qs.query_hash, 1),
       CONVERT(VARCHAR(18), qs.query_plan_hash, 1),
       qs.statement_start_offset,
       qs.statement_end_offset
FROM sys.dm_exec_query_stats qs
OUTER APPLY sys.dm_exec_sql_text(qs.sql_handle) st
WHERE qs.sql_handle = @SqlHandle
OPTION (RECOMPILE); -- Plan caching is not beneficial and would pollute the cache being queried

IF NOT EXISTS (SELECT 1 FROM #plans)
BEGIN
    -- Nothing has finished executing under this handle - the statement may be running right now, in
    -- which case there is a plan to look at even though there are no stats to go with it.
    INSERT INTO #plans
    (
        source, is_deadlock_statement, statement_text, last_execution_time, plan_handle, query_hash,
        query_plan_hash, statement_start_offset, statement_end_offset
    )
    SELECT 'Executing now',
           CASE WHEN r.statement_start_offset = @StatementStart THEN 1 ELSE 0 END,
           SUBSTRING(
               st.text,
               (r.statement_start_offset / 2) + 1,
               ((CASE r.statement_end_offset
                     WHEN -1 THEN DATALENGTH(st.text)
                     ELSE r.statement_end_offset
                 END - r.statement_start_offset) / 2) + 1),
           r.start_time,
           CONVERT(VARCHAR(130), r.plan_handle, 1),
           CONVERT(VARCHAR(18), r.query_hash, 1),
           CONVERT(VARCHAR(18), r.query_plan_hash, 1),
           r.statement_start_offset,
           r.statement_end_offset
    FROM sys.dm_exec_requests r
    OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) st
    WHERE r.sql_handle = @SqlHandle
    OPTION (RECOMPILE);
END

SELECT *
FROM #plans
ORDER BY is_deadlock_statement DESC,
         last_execution_time DESC;
