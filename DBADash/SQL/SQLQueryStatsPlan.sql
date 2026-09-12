/*
	Finds a cache entry to take the plan of a query stats row from, for a plan shape the collection did not capture a
	plan for: the entry under the row's plan shape that last started an execution.

	A statement with a handle of its own - one in a module, or ad hoc with no query hash - is found by its handle and
	offsets.  An ad hoc shape is every literal variant of a query, each with a handle and a plan of its own, so it is
	found by its query hash instead.  Any entry under the plan shape runs the same operators, though its text and
	literal values are one variant's.  Either way only in the statement's own database, where it is known: the same
	text run in two databases is two statements.

	Nothing here fetches plan XML.  The row carries the plan handle and offsets the plan fetch takes, which runs next.
	No rows means the plan is no longer cached.

	OPTION(RECOMPILE) - the lookup should not add a plan to the cache it is reading.
*/
SELECT TOP (1)
		qs.plan_handle,
		qs.statement_start_offset,
		qs.statement_end_offset
FROM sys.dm_exec_query_stats qs
WHERE qs.query_plan_hash = @QueryPlanHash
AND (@SqlHandle IS NULL
	OR (qs.sql_handle = @SqlHandle
		AND qs.statement_start_offset = @StatementStart
		AND qs.statement_end_offset = @StatementEnd))
AND (@QueryHash IS NULL OR qs.query_hash = @QueryHash)
AND (@DatabaseName IS NULL
	OR EXISTS (	SELECT 1
				FROM sys.dm_exec_plan_attributes(qs.plan_handle) pa
				WHERE pa.attribute = 'dbid'
				AND CONVERT(INT, pa.value) = DB_ID(@DatabaseName)))
ORDER BY qs.last_execution_time DESC
OPTION (RECOMPILE);
