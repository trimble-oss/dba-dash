CREATE TYPE dbo.DatabasesHADR AS TABLE(
    database_id INT NOT NULL,
    group_database_id UNIQUEIDENTIFIER NOT NULL,
    is_primary_replica BIT NULL,
    synchronization_state TINYINT NULL,
    synchronization_health TINYINT NULL,
    is_suspended BIT NULL,
    suspend_reason TINYINT NULL,
    replica_id UNIQUEIDENTIFIER NULL,
    group_id UNIQUEIDENTIFIER NULL,
    is_commit_participant BIT NULL,
    database_state TINYINT NULL,
    is_local BIT NULL,
    secondary_lag_seconds BIGINT NULL
);

