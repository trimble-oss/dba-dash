/*
	dbo.SpinlockStatsCollection rolled up by the hour, for the hours dbo.SpinlockStats_60MIN holds.
	Its retention is dbo.SpinlockStats_60MIN's - see dbo.DataRetentionLink.
*/
CREATE TABLE dbo.SpinlockStatsCollection_60MIN (
    InstanceID INT NOT NULL,
    /* The start of the hour */
    SnapshotDate DATETIME2 (2) NOT NULL,
    /* The time the hour's snapshots covered */
    sample_ms_diff INT NOT NULL,
    CONSTRAINT PK_SpinlockStatsCollection_60MIN PRIMARY KEY CLUSTERED (InstanceID ASC, SnapshotDate ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_SpinlockStatsCollection_60MIN(SnapshotDate),
    CONSTRAINT FK_SpinlockStatsCollection_60MIN_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
) ON PS_SpinlockStatsCollection_60MIN(SnapshotDate);
