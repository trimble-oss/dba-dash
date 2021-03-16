CREATE TYPE dbo.DatabaseMirroring AS TABLE(
    database_id INT NOT NULL PRIMARY KEY,
    [mirroring_guid] UNIQUEIDENTIFIER,
    [mirroring_state] TINYINT,
    [mirroring_role] TINYINT,
    [mirroring_role_sequence] INT,
    [mirroring_safety_level] TINYINT,
    [mirroring_safety_sequence] INT,
    [mirroring_partner_name] NVARCHAR(128),
    [mirroring_partner_instance] NVARCHAR(128),
    [mirroring_witness_name] NVARCHAR(128),
    [mirroring_witness_state] TINYINT,
    [mirroring_failover_lsn] DECIMAL(25, 0),
    [mirroring_connection_timeout] INT,
    [mirroring_redo_queue] INT,
    [mirroring_redo_queue_type] NVARCHAR(60),
    [mirroring_end_of_log_lsn] DECIMAL(25, 0),
    [mirroring_replication_lsn] DECIMAL(25, 0)
);

