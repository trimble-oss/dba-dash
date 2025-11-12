CREATE TABLE dbo.RunningQueriesCursors(
    InstanceID INT NOT NULL,
    SnapshotDateUTC DATETIME2(7) NOT NULL,
    session_id SMALLINT NOT NULL,
    uniqueifier SMALLINT NOT NULL,
    name NVARCHAR(256) NULL,
    properties NVARCHAR(256) NULL,
    sql_handle VARBINARY(64) NULL,
    statement_start_offset INT NULL,
    statement_end_offset INT NULL,
    plan_generation_num	BIGINT NULL,
    creation_time_utc DATETIME NULL,
    is_open BIT NULL,
    is_async_population BIT NULL,
    is_close_on_commit	BIT NULL,
    fetch_status INT NULL,
    fetch_buffer_size INT NULL,
    fetch_buffer_start INT NULL,
    ansi_position INT NULL,
    worker_time BIGINT NULL, 
    reads BIGINT NULL,
    writes BIGINT NULL,
    dormant_duration BIGINT NULL,
    CONSTRAINT PK_RunningQueriesCursors PRIMARY KEY(InstanceID,SnapshotDateUTC,session_id,uniqueifier) WITH (DATA_COMPRESSION = PAGE) ON PS_RunningQueriesCursors(SnapshotDateUTC)
);
GO
CREATE INDEX IX_RunningQueriesCursors_sql_handle ON dbo.RunningQueriesCursors(sql_handle) INCLUDE(SnapshotDateUTC) WITH (DATA_COMPRESSION = PAGE) ON PS_RunningQueriesCursors(SnapshotDateUTC);