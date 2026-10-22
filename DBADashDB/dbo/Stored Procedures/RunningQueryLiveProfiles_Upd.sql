CREATE PROC dbo.RunningQueryLiveProfiles_Upd(
	@RunningQueryLiveProfiles dbo.RunningQueryLiveProfiles READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(7)
)
AS
/*
	Latest counters only: a request seen again on the same statement of the same batch, with the same plan, replaces what was kept for it, moving the counters it
	had to PreviousCounters.  A capture no newer than the one kept is ignored, so a collection imported late or twice
	doesn't go back in time.
	Statement offsets are only unique within a batch, and a recompile can give the same statement a new plan, so the batch
	(sql_handle) and plan hash are part of which capture this is.  The plan handle is updated: a recompile to the same
	shape keeps the same counters but can be cached under another handle.
*/
SET XACT_ABORT ON

UPDATE LP
	SET LP.LastCapturedUTC = t.captured_utc,
		LP.CaptureCount = LP.CaptureCount + 1,
		LP.cpu_time = t.cpu_time,
		LP.total_elapsed_time = t.total_elapsed_time,
		LP.plan_handle = t.plan_handle,
		LP.PreviousCapturedUTC = LP.LastCapturedUTC,
		LP.PreviousCounters = LP.Counters,
		LP.Counters = t.counters
FROM dbo.RunningQueryLiveProfiles LP
JOIN @RunningQueryLiveProfiles t ON LP.session_id = t.session_id
		AND LP.start_time_utc = t.start_time_utc
		AND LP.statement_start_offset = t.statement_start_offset
		AND LP.statement_end_offset = t.statement_end_offset
		AND LP.sql_handle = t.sql_handle
		AND LP.query_plan_hash = t.query_plan_hash
WHERE LP.InstanceID = @InstanceID
AND t.captured_utc > LP.LastCapturedUTC

INSERT INTO dbo.RunningQueryLiveProfiles(
	InstanceID,
	session_id,
	start_time_utc,
	statement_start_offset,
	statement_end_offset,
	plan_handle,
	query_plan_hash,
	sql_handle,
	FirstCapturedUTC,
	LastCapturedUTC,
	CaptureCount,
	cpu_time,
	total_elapsed_time,
	Counters
)
SELECT	@InstanceID,
		t.session_id,
		t.start_time_utc,
		t.statement_start_offset,
		t.statement_end_offset,
		t.plan_handle,
		t.query_plan_hash,
		t.sql_handle,
		t.captured_utc,
		t.captured_utc,
		1,
		t.cpu_time,
		t.total_elapsed_time,
		t.counters
FROM @RunningQueryLiveProfiles t
WHERE NOT EXISTS(SELECT 1
				FROM dbo.RunningQueryLiveProfiles LP
				WHERE LP.InstanceID = @InstanceID
				AND LP.session_id = t.session_id
				AND LP.start_time_utc = t.start_time_utc
				AND LP.statement_start_offset = t.statement_start_offset
				AND LP.statement_end_offset = t.statement_end_offset
				AND LP.sql_handle = t.sql_handle
				AND LP.query_plan_hash = t.query_plan_hash
				)
