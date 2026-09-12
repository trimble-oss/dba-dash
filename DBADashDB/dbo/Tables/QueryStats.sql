/*
    Statement level deltas: one row per statement and plan shape per collection, plus the rollups.

    Rolled up by the hour into dbo.QueryStats_60MIN as the rows are stored.  The reports read whole hours from
    there, so the rows here are only read for windows, or the ends of windows, shorter than an hour - by instance
    and time, which the clustered index serves.  That is why it has no other index: one on StatementID would add
    half again to every collection's writes for reads the rollup already answers, and dbo.PurgeQueryStatements
    asks the rollup whether a statement is still referred to.
*/
CREATE TABLE dbo.QueryStats (
    InstanceID INT NOT NULL,
    SnapshotDate DATETIME2 (3) NOT NULL,
    StatementID BIGINT NOT NULL,
    /*
        Plan level detail costs one column rather than a second fact table.  A statement that ran under two
        plan shapes in an interval is two rows, which is what makes a plan regression visible rather than
        inferred.  0x0 means the row is a rollup: either of the plans beyond the per statement cap
        (IsOtherPlans), or of statements that have no single plan of their own.
    */
    query_plan_hash BINARY(8) NOT NULL,
    /*
        Microseconds since the previous successful collection for this instance, not a fixed interval
        length.  A missed collection therefore describes itself: the next delta covers the whole gap and
        says how long that gap was, rather than leaving a hole that looks like idle time.
    */
    PeriodTime BIGINT NOT NULL,
    execution_count BIGINT NOT NULL,
    total_worker_time BIGINT NOT NULL,
    total_elapsed_time BIGINT NOT NULL,
    total_logical_reads BIGINT NOT NULL,
    total_logical_writes BIGINT NOT NULL,
    total_physical_reads BIGINT NOT NULL,
    total_clr_time BIGINT NOT NULL,
    /* NULL where the monitored instance's version does not expose the counter, which is not the same as zero */
    total_rows BIGINT NULL,
    total_dop BIGINT NULL,
    total_grant_kb BIGINT NULL,
    total_used_grant_kb BIGINT NULL,
    total_spills BIGINT NULL,
    /* Plan cache rows that contributed, or for a rollup row, the number of statements rolled into it */
    PlanCount INT NOT NULL,
    /*
        The plan compiled inside this interval, so the whole of its counters was taken as the interval's
        work rather than diffed.  Accurate, but worth surfacing: a query that is always IsCompile is a
        query whose plan never stays cached.
    */
    IsCompile BIT NOT NULL,
    IsOtherPlans BIT NOT NULL,
    PeriodStartTime AS (DATEADD(day, -([PeriodTime] / (86400000000.)), DATEADD(millisecond, -(([PeriodTime] % (86400000000.)) / (1000)), [SnapshotDate]))),
    PeriodEndTime AS ([SnapshotDate]),
    AvgElapsedTime AS ([total_elapsed_time] / NULLIF([execution_count], (0))),
    AvgWorkerTime AS ([total_worker_time] / NULLIF([execution_count], (0))),
    CONSTRAINT PK_QueryStats PRIMARY KEY CLUSTERED (InstanceID ASC, SnapshotDate ASC, StatementID ASC, query_plan_hash ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_QueryStats (SnapshotDate),
    CONSTRAINT FK_QueryStats_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
) ON PS_QueryStats (SnapshotDate);
