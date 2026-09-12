/*
    dbo.QueryStatsCollection rolled up by the hour, for the hours dbo.QueryStats_60MIN holds: what each hour's
    collections covered and what they could not account for.  A rate over months then divides by the time that was
    actually watched, as a rate over minutes does.

    Kept the same way as dbo.QueryStats_60MIN, in the same transaction.  Keep its retention at least as long as that
    table's: every rate the reports show for the hours it holds divides by the time recorded here.
*/
CREATE TABLE dbo.QueryStatsCollection_60MIN (
    InstanceID INT NOT NULL,
    /* The start of the hour */
    SnapshotDate DATETIME2(3) NOT NULL,
    /* The time the hour's collections covered, in microseconds: their PeriodTime, less the skipped collections' */
    CoveredTime BIGINT NOT NULL,
    Collections INT NOT NULL,
    SkippedCollections INT NOT NULL,
    FirstCollections INT NOT NULL,
    UnattributedWorkerTime BIGINT NOT NULL,
    UnattributedElapsedTime BIGINT NOT NULL,
    UnattributedExecutions BIGINT NOT NULL,
    BaselineEvictions INT NOT NULL,
    MaxReadDurationMs INT NOT NULL,
    CONSTRAINT PK_QueryStatsCollection_60MIN PRIMARY KEY CLUSTERED (InstanceID ASC, SnapshotDate ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_QueryStatsCollection_60MIN (SnapshotDate),
    CONSTRAINT FK_QueryStatsCollection_60MIN_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
) ON PS_QueryStatsCollection_60MIN (SnapshotDate);
