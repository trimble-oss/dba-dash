/*
	dbo.LatchStatsCollection rolled up by the hour, for the hours dbo.LatchStats_60MIN holds.
	Keep its retention at least as long as dbo.LatchStats_60MIN's.
*/
CREATE TABLE dbo.LatchStatsCollection_60MIN (
    InstanceID INT NOT NULL,
    /* The start of the hour */
    SnapshotDate DATETIME2 (2) NOT NULL,
    /* The time the hour's snapshots covered */
    sample_ms_diff INT NOT NULL,
    CONSTRAINT PK_LatchStatsCollection_60MIN PRIMARY KEY CLUSTERED (InstanceID ASC, SnapshotDate ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_LatchStatsCollection_60MIN(SnapshotDate),
    CONSTRAINT FK_LatchStatsCollection_60MIN_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
) ON PS_LatchStatsCollection_60MIN(SnapshotDate);
