CREATE TABLE dbo.SpinlockStats (
    InstanceID INT NOT NULL,
    SnapshotDate DATETIME2 (2) NOT NULL,
    SpinlockID SMALLINT NOT NULL,
    collisions BIGINT NOT NULL,
    spins BIGINT NOT NULL,
    sleep_time BIGINT NOT NULL,
    backoffs BIGINT NOT NULL,
    sample_ms_diff INT NOT NULL,
    CONSTRAINT PK_SpinlockStats PRIMARY KEY CLUSTERED (InstanceID ASC, SnapshotDate ASC, SpinlockID ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_SpinlockStats(SnapshotDate),
    CONSTRAINT FK_SpinlockStats_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID),
    CONSTRAINT FK_SpinlockStats_Spinlock FOREIGN KEY (SpinlockID) REFERENCES dbo.Spinlock (SpinlockID)
) ON PS_SpinlockStats(SnapshotDate);
