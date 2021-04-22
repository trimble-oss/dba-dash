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
    synchronization_state_desc AS CASE synchronization_state WHEN  0 THEN N'NOT SYNCHRONIZING' WHEN 1 THEN N'SYNCHRONIZING' WHEN 2 THEN N'SYNCHRONIZED' WHEN 3 THEN N'REVERTING' WHEN 4 THEN N'INITIALIZING' ELSE CAST(synchronization_state AS NVARCHAR(60)) END,
    synchronization_health_desc AS CASE synchronization_health WHEN 0 THEN N'NOT_HEALTHY'  WHEN 1 THEN N'PARTIALLY_HEALTHY' WHEN 2 THEN N'HEALTHY' ELSE CAST(synchronization_health AS NVARCHAR(60)) END,
    database_state_desc AS CASE database_state WHEN 0 THEN N'ONLINE' WHEN 1 THEN N'RESTORING' WHEN 2 THEN N'RECOVERING' WHEN 3 THEN N'RECOVERY_PENDING' WHEN 4 THEN N'SUSPECT' WHEN 5 THEN N'EMERGENCY' WHEN 6 THEN N'OFFLINE' ELSE  CAST(database_state AS NVARCHAR(60)) END,
    suspend_reason_desc AS CASE suspend_reason WHEN 0 THEN N'SUSPEND_FROM_USER' WHEN 1 THEN N'SUSPEND_FROM_PARTNER' WHEN 2 THEN N'SUSPEND_FROM_REDO' WHEN 3 THEN N'SUSPEND_FROM_CAPTURE' WHEN 4 THEN N'SUSPEND_FROM_APPLY' WHEN 5 THEN N'SUSPEND_FROM_RESTART' WHEN 6 THEN N'SUSPEND_FROM_UNDO' WHEN 7 THEN N'SUSPEND_FROM_REVALIDATION' WHEN 8 THEN N'SUSPEND_FROM_XRF_UPDATE'  ELSE CAST(suspend_reason as NVARCHAR(60)) END
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

