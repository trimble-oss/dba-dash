CREATE TABLE dbo.BackupThresholds(
    InstanceID INT NOT NULL,
    DatabaseID INT NOT NULL,
    LogBackupWarningThreshold INT NULL,
    LogBackupCriticalThreshold INT NULL,
    FullBackupWarningThreshold INT NULL,
    FullBackupCriticalThreshold INT NULL,
    DiffBackupWarningThreshold INT NULL,
    DiffBackupCriticalThreshold INT NULL,
    ConsiderPartialBackups BIT NOT NULL,
    ConsiderFGBackups BIT NOT NULL,
    ExcludedDatabases NVARCHAR(MAX) NULL,
    MinimumAge INT NULL,
    ConsiderSnapshotBackups BIT NOT NULL CONSTRAINT DF_BackupThreshold_ConsiderSnapshotBackups DEFAULT(1),
    ConsiderCopyOnlyBackups BIT NOT NULL CONSTRAINT DF_BackupThreshold_ConsiderCopyOnlyBackups DEFAULT(1),
    CONSTRAINT PK_BackupThresholds PRIMARY KEY CLUSTERED (InstanceID ASC,DatabaseID ASC)
)