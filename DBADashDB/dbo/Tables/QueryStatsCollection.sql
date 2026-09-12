/*
    One row per query stats collection, whether or not it produced any fact rows.

    This is what makes the data quality of a window answerable instead of assumed.  The collection keeps
    only the top families and rolls the rest up, and a plan cache row seen for the first time with an old
    compile time cannot be attributed to the interval at all - so the honest answer to "is this window
    complete" lives here rather than being inferred from the absence of rows.

    Partitioned on a scheme of its own rather than dbo.QueryStats', so partition maintenance purges it by its
    own retention.  Sharing the function left it to dbo.QueryStats' cleanup, which merges the boundaries of the
    partitions it truncates, so this table's old rows were merged into the first partition and never removed.
    Keep its retention at least as long as dbo.QueryStats': every rate the reports show for a window divides by
    the time recorded here.
*/
CREATE TABLE dbo.QueryStatsCollection (
    InstanceID INT NOT NULL,
    SnapshotDate DATETIME2 (3) NOT NULL,
    PeriodTime BIGINT NOT NULL,
    /* Rows returned by the DMV: those whose last execution finished since the previous collection */
    DMVRowCount INT NOT NULL,
    RowsWithDelta INT NOT NULL,
    /*
        Rows that could not be diffed: no baseline, and compiled before the interval.  Their work is not
        attributed to statements, and the three totals below are an estimate of the interval's share of it rather
        than the plans' whole life in cache: the last execution, which finished inside the interval, plus the
        earlier ones prorated over the time since the plan compiled.
    */
    RowsUnattributed INT NOT NULL,
    UnattributedWorkerTime BIGINT NOT NULL,
    UnattributedElapsedTime BIGINT NOT NULL,
    UnattributedExecutions BIGINT NOT NULL,
    FamilyCount INT NOT NULL,
    FamiliesKept INT NOT NULL,
    StatementsKept INT NOT NULL,
    RowsPersisted INT NOT NULL,
    BaselineCount INT NOT NULL,
    /* Baseline rows dropped to stay within the configured cap.  Non-zero means the cap is costing accuracy */
    BaselineEvictions INT NOT NULL,
    ReadDurationMs INT NOT NULL,
    ProcessingDurationMs INT NOT NULL,
    IsFirstCollection BIT NOT NULL,
    /* The read was deliberately skipped, so this interval has no detail by design rather than by failure */
    IsSkipped BIT NOT NULL,
    /* Where the interval began: the previous collection's snapshot, as dbo.QueryStats has it */
    PeriodStartTime AS (DATEADD(day, -([PeriodTime] / (86400000000.)), DATEADD(millisecond, -(([PeriodTime] % (86400000000.)) / (1000)), [SnapshotDate]))),
    CONSTRAINT PK_QueryStatsCollection PRIMARY KEY CLUSTERED (InstanceID ASC, SnapshotDate ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_QueryStatsCollection (SnapshotDate),
    CONSTRAINT FK_QueryStatsCollection_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
) ON PS_QueryStatsCollection (SnapshotDate);
