CREATE TABLE Staging.SpinlockStats (
    InstanceID INT NOT NULL,
    SnapshotDate DATETIME2 (3) NOT NULL,
    spinlock_name NVARCHAR (256) NOT NULL,
    collisions BIGINT NOT NULL,
    spins BIGINT NOT NULL,
    sleep_time BIGINT NOT NULL,
    backoffs BIGINT NOT NULL,
    CONSTRAINT PK_Staging_SpinlockStats PRIMARY KEY CLUSTERED (InstanceID ASC, spinlock_name ASC)
);
