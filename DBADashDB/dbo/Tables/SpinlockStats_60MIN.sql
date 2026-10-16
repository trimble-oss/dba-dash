CREATE TABLE dbo.SpinlockStats_60MIN (
    InstanceID INT NOT NULL,
    SnapshotDate DATETIME2 (2) NOT NULL,
    SpinlockID SMALLINT NOT NULL,
    collisions BIGINT NOT NULL,
    spins BIGINT NOT NULL,
    sleep_time BIGINT NOT NULL,
    backoffs BIGINT NOT NULL,
    CONSTRAINT PK_SpinlockStats_60MIN PRIMARY KEY CLUSTERED (InstanceID ASC, SnapshotDate ASC, SpinlockID ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_SpinlockStats_60MIN(SnapshotDate),
    CONSTRAINT FK_SpinlockStats_60MIN_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID),
    CONSTRAINT FK_SpinlockStats_60MIN_Spinlock FOREIGN KEY (SpinlockID) REFERENCES dbo.Spinlock (SpinlockID)
) ON PS_SpinlockStats_60MIN(SnapshotDate);
