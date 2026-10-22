CREATE PROC dbo.RunningQueryLiveProfile_Get(
	@InstanceID INT,
	@session_id SMALLINT,
	@start_time_utc DATETIME,
	@statement_start_offset INT,
	@statement_end_offset INT,
	@sql_handle VARBINARY(64),
	@query_plan_hash BINARY(8)
)
AS
/* The counters kept for a request, statement and plan, with the plan they go with */
SELECT	LP.LastCapturedUTC,
		LP.FirstCapturedUTC,
		LP.CaptureCount,
		LP.cpu_time,
		LP.total_elapsed_time,
		LP.Counters,
		LP.PreviousCapturedUTC,
		LP.PreviousCounters,
		LP.plan_handle,
		LP.query_plan_hash,
		LP.sql_handle,
		QP.query_plan_compresed
FROM dbo.RunningQueryLiveProfiles LP
LEFT JOIN dbo.QueryPlans QP ON QP.plan_handle = LP.plan_handle
		AND QP.query_plan_hash = LP.query_plan_hash
		AND QP.statement_start_offset = LP.statement_start_offset
		AND QP.statement_end_offset = LP.statement_end_offset
WHERE LP.InstanceID = @InstanceID
AND LP.session_id = @session_id
AND LP.start_time_utc = @start_time_utc
AND LP.statement_start_offset = @statement_start_offset
AND LP.statement_end_offset = @statement_end_offset
AND LP.sql_handle = @sql_handle
AND LP.query_plan_hash = @query_plan_hash
