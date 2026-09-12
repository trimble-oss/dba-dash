/*
	Statement level resource usage, read from the plan cache.

	Returns one row per cached plan per statement.  That grain is deliberate: the delta has to be
	calculated at the grain the counters accumulate at, and only then aggregated.  Aggregating first
	would break whenever the set of rows contributing to a group changes between snapshots, which for
	the plan cache is constantly.

	Filtered so only rows that finished an execution since the previous collection are returned.  Rows
	excluded that way have a zero delta by definition, so nothing is lost - see Docs/QueryStats.md for why
	filtering on the cumulative counters instead would not be safe.

	The filter is on when the last execution finished, not on last_execution_time alone.  That column is
	when the last execution started, and the counters only move when an execution finishes, so an
	execution that started before the previous collection and finished after it changes the counters
	while leaving last_execution_time behind the filter.  Filtering on it alone would hide exactly the long
	executions this collection most needs to see.  The finish is last_execution_time plus
	last_elapsed_time, both of which describe the most recently finished execution.  Added in whole
	seconds, rounded up, because DATEADD takes an INT: elapsed time in microseconds overflows it after 35
	minutes, and agent style queries that wait in a loop can run for weeks.

	Column availability differs by version, so the query is built with COLUMNPROPERTY tests:
		total_rows                                          2008 R2+
		total_dop, total_grant_kb, total_used_grant_kb,
		total_spills                                        2016+

	The database and object come from the plan attributes, which belong to a plan rather than a statement:
	every statement of a procedure or batch shares its plan_handle.  So the filtered rows are materialised
	first and the attributes, and the names resolved from them, looked up once per plan rather than once
	per row - a procedure with twenty statements costs one call to sys.dm_exec_plan_attributes, not twenty.
	The materialisation also means the plan cache is scanned once, which a derived table referenced twice
	would not guarantee.

	OPTION(RECOMPILE) throughout - the collection should not add a plan to the cache it is reading,
	and the row estimates depend entirely on the parameter.
*/
DECLARE @UTCOffset INT
DECLARE @FromLocal DATETIME
DECLARE @SnapshotDateUTC DATETIME2(3)
DECLARE @SQL NVARCHAR(MAX)

SELECT @UTCOffset = CAST(ROUND(DATEDIFF(s, GETDATE(), GETUTCDATE()) / 60.0, 0) AS INT),
       @SnapshotDateUTC = GETUTCDATE()

/* last_execution_time is in server local time, the caller works in UTC */
SET @FromLocal = DATEADD(mi, -@UTCOffset, @LastExecutionTimeFromUTC)

SET @SQL = CAST(N'
SELECT	qs.sql_handle,
		qs.statement_start_offset,
		qs.statement_end_offset,
		qs.plan_handle,
		qs.query_hash,
		qs.query_plan_hash,
		qs.plan_generation_num,
		DATEADD(mi, @UTCOffset, qs.creation_time) AS creation_time_utc,
		DATEADD(mi, @UTCOffset, qs.last_execution_time) AS last_execution_time_utc,
		/*
			The last execution finished inside the interval - see the filter - so these describe the one
			execution the interval is known to hold even where the counters cannot be diffed.  The maximum
			spans the life of the plan, so it only describes the interval where it has risen since the
			previous collection or the plan compiled inside it.
		*/
		qs.last_elapsed_time,
		qs.last_worker_time,
		qs.max_elapsed_time,
		qs.execution_count,
		qs.total_worker_time,
		qs.total_elapsed_time,
		qs.total_logical_reads,
		qs.total_logical_writes,
		qs.total_physical_reads,
		qs.total_clr_time' AS NVARCHAR(MAX))
	+ CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_query_stats'), 'total_rows', 'ColumnId') IS NULL
		THEN N',
		CAST(NULL AS BIGINT) AS total_rows' ELSE N',
		qs.total_rows' END
	+ CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_query_stats'), 'total_dop', 'ColumnId') IS NULL
		THEN N',
		CAST(NULL AS BIGINT) AS total_dop' ELSE N',
		qs.total_dop' END
	+ CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_query_stats'), 'total_grant_kb', 'ColumnId') IS NULL
		THEN N',
		CAST(NULL AS BIGINT) AS total_grant_kb' ELSE N',
		qs.total_grant_kb' END
	+ CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_query_stats'), 'total_used_grant_kb', 'ColumnId') IS NULL
		THEN N',
		CAST(NULL AS BIGINT) AS total_used_grant_kb' ELSE N',
		qs.total_used_grant_kb' END
	+ CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_exec_query_stats'), 'total_spills', 'ColumnId') IS NULL
		THEN N',
		CAST(NULL AS BIGINT) AS total_spills' ELSE N',
		qs.total_spills' END
	+ N'
INTO #QueryStats
FROM sys.dm_exec_query_stats qs
WHERE DATEADD(SECOND, CONVERT(INT, qs.last_elapsed_time / 1000000) + 1, qs.last_execution_time) >= @FromLocal
AND qs.execution_count > 0
OPTION (RECOMPILE);

/*
	The database is only available from the plan attributes.  sys.dm_exec_sql_text returns a NULL dbid for
	an ad hoc or prepared plan, and retrieving the text for every plan would cost far more than this apply.
	Once per plan, because the attributes are the same for every statement of one.  OUTER APPLY rather than
	CROSS so a plan evicted between the scan and the attribute lookup does not silently drop its rows.
*/
SELECT	P.plan_handle,
		A.dbid,
		DB_NAME(A.dbid) AS database_name,
		/*
			For an ad hoc plan the objectid attribute holds a hash of the batch text rather than an object,
			so it is only meaningful when the handle says this is module code (0x03 prefix).  A plan belongs
			to one batch, so its statements all agree.
		*/
		CASE WHEN P.IsModule = 1 THEN A.objectid END AS object_id,
		CASE WHEN P.IsModule = 1 THEN OBJECT_SCHEMA_NAME(A.objectid, A.dbid) END AS schema_name,
		CASE WHEN P.IsModule = 1 THEN OBJECT_NAME(A.objectid, A.dbid) END AS object_name,
		/*
			Whether QUOTED_IDENTIFIER was on is in here, and it decides whether a double quoted token in the
			text is an identifier or a string - which the collector needs to know to take the literal values out
			of an ad hoc query text.
		*/
		A.set_options
INTO #Plans
FROM (	SELECT	plan_handle,
				MAX(CASE WHEN SUBSTRING(sql_handle, 1, 1) = 0x03 THEN 1 ELSE 0 END) AS IsModule
		FROM #QueryStats
		GROUP BY plan_handle
	) P
OUTER APPLY (	SELECT	MAX(CASE WHEN pa.attribute = ''dbid'' THEN CONVERT(INT, pa.value) END) AS dbid,
						MAX(CASE WHEN pa.attribute = ''objectid'' THEN CONVERT(INT, pa.value) END) AS objectid,
						MAX(CASE WHEN pa.attribute = ''set_options'' THEN CONVERT(INT, pa.value) END) AS set_options
				FROM sys.dm_exec_plan_attributes(P.plan_handle) pa
				WHERE pa.attribute IN (''dbid'', ''objectid'', ''set_options'')
			) A
OPTION (RECOMPILE);

SELECT	@SnapshotDateUTC AS SnapshotDateUTC,
		Q.*,
		P.dbid,
		P.database_name,
		P.object_id,
		P.schema_name,
		P.object_name,
		P.set_options
FROM #QueryStats Q
LEFT JOIN #Plans P ON P.plan_handle = Q.plan_handle
OPTION (RECOMPILE);'

EXEC sp_executesql @SQL,
	N'@UTCOffset INT,@SnapshotDateUTC DATETIME2(3),@FromLocal DATETIME',
	@UTCOffset, @SnapshotDateUTC, @FromLocal
