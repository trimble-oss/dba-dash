CREATE TABLE dbo.LogRestoreThresholds (
    InstanceID INT NOT NULL,
    DatabaseID INT NOT NULL,
    LatencyWarningThreshold INT NULL,
    LatencyCriticalThreshold INT NULL,
    TimeSinceLastWarningThreshold INT NULL,
    TimeSinceLastCriticalThreshold INT NULL,
    NewDatabaseExcludePeriodMin INT NOT NULL CONSTRAINT DF_LogRestoreThresholds_NewDatabaseExcludePeriodMin DEFAULT(1440),
    CONSTRAINT PK_LogRestoreThresholds PRIMARY KEY CLUSTERED (
                                                                 InstanceID ASC,
                                                                 DatabaseID ASC
                                                             )
);
