CREATE TABLE Staging.ObjectExecutionStats (
    InstanceID INT NOT NULL,
    object_id INT NOT NULL,
    database_id INT NOT NULL,
    database_name NVARCHAR(128) NOT NULL CONSTRAINT DF_Staging_ObjectExecutionStats_database_name DEFAULT(CAST(NEWID() AS NVARCHAR(128))),
    object_name NVARCHAR(128) NULL,
    total_worker_time BIGINT NOT NULL,
    total_elapsed_time BIGINT NOT NULL,
    total_logical_reads BIGINT NOT NULL,
    total_logical_writes BIGINT NOT NULL,
    total_physical_reads BIGINT NOT NULL,
    cached_time DATETIME NOT NULL,
    execution_count BIGINT NOT NULL,
    current_time_utc DATETIME2(3) NOT NULL,
    CONSTRAINT PK_Staging_ObjectExecutionStats PRIMARY KEY CLUSTERED (InstanceID ASC, object_id ASC, database_name ASC, cached_time ASC)
);
