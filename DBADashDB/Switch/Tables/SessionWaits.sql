CREATE TABLE Switch.SessionWaits(
	SnapshotDateUTC DATETIME2(7) NOT NULL,
	InstanceID INT NOT NULL,
	session_id SMALLINT NOT NULL,
	WaitTypeID INT NOT NULL,	
	waiting_tasks_count BIGINT NOT NULL,
	wait_time_ms BIGINT NOT NULL,
	max_wait_time_ms BIGINT NOT NULL,
	signal_wait_time_ms BIGINT NOT NULL,
	login_time_utc DATETIME NOT NULL,
	CONSTRAINT PK_SessionWaits PRIMARY KEY(InstanceID,SnapshotDateUTC,session_id,WaitTypeID) WITH(DATA_COMPRESSION=PAGE)
) 
