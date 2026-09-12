/*
    dbo.QueryStats rolled up by the hour: the same statements and plan shapes, one row per hour rather than one per
    collection.  A window of days or months reads a twelfth of the rows a five minute schedule stores, and the
    history can be kept far longer than the raw rows, which is what this table is for.

    Kept as the rows arrive rather than built afterwards: dbo.QueryStats_Upd adds each collection's rows to their
    hour in the same transaction that stores them, so the two never disagree and nothing has to run later to catch
    up.  An hour is the collections whose snapshot falls in it, so an interval that straddles the hour belongs to
    the hour it ended in, as it does in every other _60MIN table.

    The reports read whole hours from here and only the part hours at either end of a window from dbo.QueryStats,
    so their figures are the raw rows' exactly wherever the raw rows still exist.
*/
CREATE TABLE dbo.QueryStats_60MIN (
    InstanceID INT NOT NULL,
    /* The start of the hour */
    SnapshotDate DATETIME2(3) NOT NULL,
    StatementID BIGINT NOT NULL,
    query_plan_hash BINARY(8) NOT NULL,
    execution_count BIGINT NOT NULL,
    total_worker_time BIGINT NOT NULL,
    total_elapsed_time BIGINT NOT NULL,
    total_logical_reads BIGINT NOT NULL,
    total_logical_writes BIGINT NOT NULL,
    total_physical_reads BIGINT NOT NULL,
    total_clr_time BIGINT NOT NULL,
    /* NULL where the monitored instance's version does not expose the counter, as in dbo.QueryStats */
    total_rows BIGINT NULL,
    total_dop BIGINT NULL,
    total_grant_kb BIGINT NULL,
    total_used_grant_kb BIGINT NULL,
    total_spills BIGINT NULL,
    /*
        The most plan cache rows behind the row in any one collection in the hour, rather than their total: a
        statement running every interval would otherwise count its one cache entry twelve times.  For a rollup row
        it is the most statements rolled into it, as PlanCount is in dbo.QueryStats.
    */
    MaxPlanCount INT NOT NULL,
    IsCompile BIT NOT NULL,
    IsOtherPlans BIT NOT NULL,
    /*
        Where the first interval the row ran in began and where the last ended, which the hour alone cannot say:
        the window the row's work was done in, as the grid shows it.
    */
    FirstPeriodStart DATETIME2(3) NOT NULL,
    LastSnapshotDate DATETIME2(3) NOT NULL,
    CONSTRAINT PK_QueryStats_60MIN PRIMARY KEY CLUSTERED (InstanceID ASC, SnapshotDate ASC, StatementID ASC, query_plan_hash ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_QueryStats_60MIN (SnapshotDate),
    CONSTRAINT FK_QueryStats_60MIN_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
) ON PS_QueryStats_60MIN (SnapshotDate);


GO
/*
    Whether anything still refers to a statement, for dbo.PurgeQueryStatements.  On this table rather than on
    dbo.QueryStats: every collection's rows are rolled up into it as they are stored, and it is kept longer, so it
    answers for both - and it is written once per statement, plan and hour rather than once per collection.
*/
CREATE NONCLUSTERED INDEX IX_QueryStats_60MIN_StatementID
ON dbo.QueryStats_60MIN (StatementID, SnapshotDate)
WITH (DATA_COMPRESSION = PAGE) ON PS_QueryStats_60MIN (SnapshotDate);
