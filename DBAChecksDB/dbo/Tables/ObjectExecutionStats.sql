CREATE TABLE [dbo].[ObjectExecutionStats] (
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
    [MaxExecutionsPerMin]  AS            ([execution_count]/(nullif([PeriodTime],(0))/(60000000.0))),
    CONSTRAINT [PK_ObjectExecutionStats] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [SnapshotDate] ASC, [ObjectID] ASC) WITH (DATA_COMPRESSION = PAGE) ON [PS_ObjectExecutionStats] ([SnapshotDate]),
    CONSTRAINT [FK_ObjectExecutionStats_DBObjects] FOREIGN KEY ([ObjectID]) REFERENCES [dbo].[DBObjects] ([ObjectID]),
    CONSTRAINT [FK_ObjectExecutionStats_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
) ON [PS_ObjectExecutionStats] ([SnapshotDate]);

