/*
	The operator counters of long running queries, kept by the running queries collection when they meet the live
	profile threshold.  One row per request, statement and plan, replaced by each collection that sees it still running, so
	it holds the latest counters rather than a history - the history of the request's CPU, reads and waits is in
	RunningQueries.  The plan the counters belong to is in QueryPlans.
	Counters is the per operator, per thread rows from sys.dm_exec_query_profiles, compressed JSON (DECOMPRESS to
	NVARCHAR to read it).  PreviousCounters is what the collection before kept, which is how the GUI tells which
	operators were moving rows at the time.
*/
CREATE TABLE dbo.RunningQueryLiveProfiles(
	InstanceID INT NOT NULL,
	session_id SMALLINT NOT NULL,
	start_time_utc DATETIME NOT NULL,
	statement_start_offset INT NOT NULL,
	statement_end_offset INT NOT NULL,
	plan_handle VARBINARY(64) NOT NULL,
	query_plan_hash BINARY(8) NOT NULL,
	sql_handle VARBINARY(64) NOT NULL,
	FirstCapturedUTC DATETIME2(3) NOT NULL,
	LastCapturedUTC DATETIME2(3) NOT NULL,
	CaptureCount INT NOT NULL,
	cpu_time INT NULL,
	total_elapsed_time INT NULL,
	Counters VARBINARY(MAX) NOT NULL,
	PreviousCapturedUTC DATETIME2(3) NULL,
	PreviousCounters VARBINARY(MAX) NULL,
	/* Offsets are only unique within a batch, and a recompile can give a statement a new plan: the batch and plan hash say which statement and plan the counters are for */
	CONSTRAINT PK_RunningQueryLiveProfiles PRIMARY KEY(InstanceID,session_id,start_time_utc,sql_handle,statement_start_offset,statement_end_offset,query_plan_hash)
);
GO
CREATE NONCLUSTERED INDEX IX_RunningQueryLiveProfiles_LastCapturedUTC ON dbo.RunningQueryLiveProfiles(LastCapturedUTC);
