CREATE TABLE dbo.LogRestoreThresholds (
    InstanceID INT NOT NULL,
    DatabaseID INT NOT NULL,
    LatencyWarningThreshold INT NULL,
    LatencyCriticalThreshold INT NULL,
    TimeSinceLastWarningThreshold INT NULL,
    TimeSinceLastCriticalThreshold INT NULL,
    NewDatabaseExcludePeriodMin INT NOT NULL CONSTRAINT DF_LogRestoreThresholds_NewDatabaseExcludePeriodMin DEFAULT(1440),
    NotOnPrimaryStatus TINYINT NULL, /* Status override for databases not on the configured primary.  NULL to use the thresholds.  1=Critical, 2=Warning, 3=N/A */
    CONSTRAINT CK_LogRestoreThresholds_NotOnPrimaryStatus CHECK (NotOnPrimaryStatus IN (1,2,3)),
    CONSTRAINT PK_LogRestoreThresholds PRIMARY KEY CLUSTERED (
                                                                 InstanceID ASC,
                                                                 DatabaseID ASC
                                                             )
);
