CREATE TABLE dbo.LatchStats_60MIN (
    InstanceID INT NOT NULL,
    SnapshotDate DATETIME2 (2) NOT NULL,
    LatchClassID SMALLINT NOT NULL,
    waiting_requests_count BIGINT NOT NULL,
    wait_time_ms BIGINT NOT NULL,
    CONSTRAINT PK_LatchStats_60MIN PRIMARY KEY CLUSTERED (InstanceID ASC, SnapshotDate ASC, LatchClassID ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_LatchStats_60MIN(SnapshotDate),
    CONSTRAINT FK_LatchStats_60MIN_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID),
    CONSTRAINT FK_LatchStats_60MIN_LatchClass FOREIGN KEY (LatchClassID) REFERENCES dbo.LatchClass (LatchClassID)
) ON PS_LatchStats_60MIN(SnapshotDate);
