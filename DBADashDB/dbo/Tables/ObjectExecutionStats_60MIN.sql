CREATE TABLE [dbo].[ObjectExecutionStats_60MIN] (
    [InstanceID]           INT           NOT NULL,
    [ObjectID]             BIGINT        NOT NULL,
    [SnapshotDate]         DATETIME2 (3) NOT NULL,
    [PeriodTime]           BIGINT        NOT NULL,
    [total_worker_time]    BIGINT        NOT NULL,
    [total_elapsed_time]   BIGINT        NOT NULL,
    [total_logical_reads]  BIGINT        NOT NULL,
    [total_logical_writes] BIGINT        NOT NULL,
    [total_physical_reads] BIGINT        NOT NULL,
    [execution_count]      BIGINT        NOT NULL,
    [IsCompile]            BIT           NOT NULL,
    [MaxExecutionsPerMin]  DECIMAL (19, 6)   NULL,
    CONSTRAINT [PK_ObjectExecutionStats_60MIN] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [SnapshotDate] ASC, [ObjectID] ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_ObjectExecutionStats_60MIN(SnapshotDate)
) ON PS_ObjectExecutionStats_60MIN(SnapshotDate);

