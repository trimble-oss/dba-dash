CREATE TABLE dbo.SlowQueries(
    InstanceID INT NOT NULL,
    DatabaseID INT NULL,
    event_type sysname NOT NULL,
    object_name sysname NULL,
    timestamp DATETIME2(3) NOT NULL,
    duration BIGINT NULL,
    cpu_time BIGINT NULL,
    logical_reads BIGINT NULL,
    physical_reads BIGINT NULL,
    writes BIGINT NULL,
    username sysname NULL,
    text NVARCHAR(MAX) NULL,
    client_hostname sysname NULL,
    client_app_name sysname NULL,
    result sysname NULL,
    Uniqueifier SMALLINT NOT NULL,
    session_id INT NULL,
    context_info VARBINARY(128) NULL,
    row_count BIGINT NULL,
    WorkloadGroupID INT NULL,
    ResourcePoolID INT NULL,
    CONSTRAINT PK_SlowQueries PRIMARY KEY CLUSTERED (InstanceID ASC, timestamp ASC, Uniqueifier ASC) ON PS_SlowQueries([timestamp]),
    CONSTRAINT FK_SlowQueries_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
) ON PS_SlowQueries (timestamp);



