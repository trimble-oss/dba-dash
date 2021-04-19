CREATE TABLE dbo.DatabasesHADR (
    DatabaseID INT NOT NULL,
    group_database_id UNIQUEIDENTIFIER NOT NULL,
    is_primary_replica BIT NULL,
    synchronization_state TINYINT NULL,
    synchronization_health TINYINT NULL,
    is_suspended BIT NULL,
    suspend_reason TINYINT NULL,
    replica_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_DatabasesHADR_replica_id DEFAULT('00000000-0000-0000-0000-000000000000'),
    group_id UNIQUEIDENTIFIER NULL,
    is_commit_participant BIT NULL,
    database_state TINYINT NULL,
    is_local BIT NULL,
    secondary_lag_seconds BIGINT NULL,
    CONSTRAINT PK_DatabasesHADR PRIMARY KEY CLUSTERED (DatabaseID ASC,replica_id),
    CONSTRAINT FK_DatabasesHADR FOREIGN KEY (DatabaseID) REFERENCES dbo.Databases (DatabaseID),
    CONSTRAINT FK_DatabasesHADR_Databases FOREIGN KEY (DatabaseID) REFERENCES dbo.Databases (DatabaseID)
);

GO
CREATE NONCLUSTERED INDEX IX_DatabasesHADR_group_database_id
ON dbo.DatabasesHADR (
                         group_database_id ASC,
                         DatabaseID ASC
                     );

