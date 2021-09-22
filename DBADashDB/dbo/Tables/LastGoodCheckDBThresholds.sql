CREATE TABLE dbo.LastGoodCheckDBThresholds(
    InstanceID INT NOT NULL,
    DatabaseID INT NOT NULL,
    WarningThresholdHrs INT NULL,
    CriticalThresholdHrs INT NULL,
    MinimumAge INT NULL,
    ExcludedDatabases NVARCHAR(MAX) NULL,
    CONSTRAINT PK_LastGoodCheckDBThresholds PRIMARY KEY CLUSTERED (InstanceID ASC, DatabaseID ASC)
);

