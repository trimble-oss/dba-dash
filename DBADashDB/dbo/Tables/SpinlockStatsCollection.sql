/*
	One row per spinlock stats snapshot that was diffed, whether or not any spinlock had any activity in it.  This is
	the time the collection covered, which dbo.SpinlockStats_Get divides by - an idle interval adds no rows to
	dbo.SpinlockStats, so taking the time from there would leave it out and overstate the rates.
	Keep its retention at least as long as dbo.SpinlockStats'.
*/
CREATE TABLE dbo.SpinlockStatsCollection (
    InstanceID INT NOT NULL,
    SnapshotDate DATETIME2 (2) NOT NULL,
    /* Time since the previous snapshot */
    sample_ms_diff INT NOT NULL,
    CONSTRAINT PK_SpinlockStatsCollection PRIMARY KEY CLUSTERED (InstanceID ASC, SnapshotDate ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_SpinlockStatsCollection(SnapshotDate),
    CONSTRAINT FK_SpinlockStatsCollection_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
) ON PS_SpinlockStatsCollection(SnapshotDate);
