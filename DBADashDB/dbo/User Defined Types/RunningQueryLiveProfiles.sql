CREATE TYPE dbo.RunningQueryLiveProfiles AS TABLE(
	session_id SMALLINT NOT NULL,
	start_time_utc DATETIME NOT NULL,
	statement_start_offset INT NOT NULL,
	statement_end_offset INT NOT NULL,
	plan_handle VARBINARY(64) NOT NULL,
	query_plan_hash BINARY(8) NOT NULL,
	sql_handle VARBINARY(64) NOT NULL,
	captured_utc DATETIME2(3) NOT NULL,
	cpu_time INT NULL,
	total_elapsed_time INT NULL,
	counters VARBINARY(MAX) NOT NULL
)
