CREATE TABLE dbo.LatchStats (
    InstanceID INT NOT NULL,
    SnapshotDate DATETIME2 (2) NOT NULL,
    LatchClassID SMALLINT NOT NULL,
    waiting_requests_count BIGINT NOT NULL,
    wait_time_ms BIGINT NOT NULL,
    sample_ms_diff INT NOT NULL,
    CONSTRAINT PK_LatchStats PRIMARY KEY CLUSTERED (InstanceID ASC, SnapshotDate ASC, LatchClassID ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_LatchStats(SnapshotDate),
    CONSTRAINT FK_LatchStats_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID),
    CONSTRAINT FK_LatchStats_LatchClass FOREIGN KEY (LatchClassID) REFERENCES dbo.LatchClass (LatchClassID)
) ON PS_LatchStats(SnapshotDate);
