/*
	One row per latch stats snapshot that was diffed, whether or not any latch class was waited on in it.  This is
	the time the collection covered, which dbo.LatchStats_Get divides by - an idle interval adds no rows to
	dbo.LatchStats, so taking the time from there would leave it out and overstate the rates.
	Keep its retention at least as long as dbo.LatchStats'.
*/
CREATE TABLE dbo.LatchStatsCollection (
    InstanceID INT NOT NULL,
    SnapshotDate DATETIME2 (2) NOT NULL,
    /* Time since the previous snapshot */
    sample_ms_diff INT NOT NULL,
    CONSTRAINT PK_LatchStatsCollection PRIMARY KEY CLUSTERED (InstanceID ASC, SnapshotDate ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_LatchStatsCollection(SnapshotDate),
    CONSTRAINT FK_LatchStatsCollection_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
) ON PS_LatchStatsCollection(SnapshotDate);
