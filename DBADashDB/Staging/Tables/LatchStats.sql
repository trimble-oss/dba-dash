CREATE TABLE Staging.LatchStats (
    InstanceID INT NOT NULL,
    SnapshotDate DATETIME2 (3) NOT NULL,
    latch_class NVARCHAR (60) NOT NULL,
    waiting_requests_count BIGINT NOT NULL,
    wait_time_ms BIGINT NOT NULL,
    /* Cumulative since the stats were last cleared - not something a delta can be taken of, so it is kept here as the latest value */
    max_wait_time_ms BIGINT NOT NULL,
    CONSTRAINT PK_Staging_LatchStats PRIMARY KEY CLUSTERED (InstanceID ASC, latch_class ASC)
);
