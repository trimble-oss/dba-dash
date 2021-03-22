CREATE TABLE dbo.AzureDBElasticPoolStorageThresholds
(
    PoolID INT NOT NULL,
    WarningThreshold DECIMAL(9, 3) NULL,
    CriticalThreshold DECIMAL(9, 3) NULL,
    CONSTRAINT PK_AzureDBElasticPoolStorageThresholds  PRIMARY KEY CLUSTERED (PoolID)
);
