CREATE TYPE dbo.SessionWaits AS TABLE(
	SnapshotDateUTC DATETIME2(7) NOT NULL,
	session_id SMALLINT NOT NULL,
	wait_type NVARCHAR(60) NOT NULL,
	waiting_tasks_count BIGINT NOT NULL,
	wait_time_ms BIGINT NOT NULL,
	max_wait_time_ms BIGINT NOT NULL,
	signal_wait_time_ms BIGINT NOT NULL,
	login_time_utc DATETIME NOT NULL,
	PRIMARY KEY(SnapshotDateUTC,session_id,wait_type)
)