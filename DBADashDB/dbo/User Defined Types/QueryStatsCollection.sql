/*
    Column order matters: SqlClient binds a DataTable to a table valued parameter by ordinal, not by name.
    This must stay in step with QueryStatsTables.GetCollectionSchema() in the collector.
*/
CREATE TYPE dbo.QueryStatsCollection AS TABLE (
    SnapshotDate DATETIME2(3) NOT NULL,
    PeriodTime BIGINT NOT NULL,
    DMVRowCount INT NOT NULL,
    RowsWithDelta INT NOT NULL,
    RowsUnattributed INT NOT NULL,
    UnattributedWorkerTime BIGINT NOT NULL,
    UnattributedElapsedTime BIGINT NOT NULL,
    UnattributedExecutions BIGINT NOT NULL,
    FamilyCount INT NOT NULL,
    FamiliesKept INT NOT NULL,
    StatementsKept INT NOT NULL,
    RowsPersisted INT NOT NULL,
    BaselineCount INT NOT NULL,
    BaselineEvictions INT NOT NULL,
    ReadDurationMs INT NOT NULL,
    ProcessingDurationMs INT NOT NULL,
    IsFirstCollection BIT NOT NULL,
    IsSkipped BIT NOT NULL
);
