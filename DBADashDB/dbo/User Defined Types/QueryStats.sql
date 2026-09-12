/*
    Column order matters: SqlClient binds a DataTable to a table valued parameter by ordinal, not by name.
    This must stay in step with QueryStatsTables.GetQueryStatsSchema() in the collector.
*/
CREATE TYPE dbo.QueryStats AS TABLE (
    StatementType TINYINT NOT NULL,
    database_name NVARCHAR(128) NULL,
    schema_name NVARCHAR(128) NULL,
    object_name NVARCHAR(128) NULL,
    object_id INT NULL,
    sql_handle VARBINARY(64) NULL,
    statement_start_offset INT NOT NULL,
    statement_end_offset INT NOT NULL,
    query_hash BINARY(8) NULL,
    query_plan_hash BINARY(8) NULL,
    SnapshotDate DATETIME2(3) NOT NULL,
    PeriodTime BIGINT NOT NULL,
    execution_count BIGINT NOT NULL,
    total_worker_time BIGINT NOT NULL,
    total_elapsed_time BIGINT NOT NULL,
    total_logical_reads BIGINT NOT NULL,
    total_logical_writes BIGINT NOT NULL,
    total_physical_reads BIGINT NOT NULL,
    total_clr_time BIGINT NOT NULL,
    total_rows BIGINT NULL,
    total_dop BIGINT NULL,
    total_grant_kb BIGINT NULL,
    total_used_grant_kb BIGINT NULL,
    total_spills BIGINT NULL,
    PlanCount INT NOT NULL,
    IsCompile BIT NOT NULL,
    IsOtherPlans BIT NOT NULL,
    /* An ad hoc shape's template and the batch of the variant it was made from, on one of the shape's rows */
    StatementTemplate NVARCHAR(MAX) NULL,
    ExampleBatchText NVARCHAR(MAX) NULL,
    /* The plan of the row's plan shape, GZip compressed, where the collector fetched one - see dbo.QueryStatsPlans */
    query_plan_compressed VARBINARY(MAX) NULL
);
