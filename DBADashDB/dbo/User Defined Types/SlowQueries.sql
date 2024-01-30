﻿CREATE TYPE dbo.SlowQueries AS TABLE (
    event_type sysname NOT NULL,
    object_name sysname NULL,
    timestamp DATETIME2(3) NULL,
    duration BIGINT NULL,
    cpu_time BIGINT NULL,
    logical_reads BIGINT NULL,
    physical_reads BIGINT NULL,
    writes BIGINT NULL,
    username sysname NULL,
    batch_text NVARCHAR(MAX) NULL,
    statement NVARCHAR(MAX) NULL,
    database_id INT NULL,
    client_hostname sysname NULL,
    client_app_name sysname NULL,
    result sysname NULL,
    session_id INT NULL,
    context_info VARBINARY(128) NULL
);
