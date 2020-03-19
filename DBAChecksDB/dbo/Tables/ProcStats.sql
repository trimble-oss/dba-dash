CREATE TABLE [dbo].[ProcStats] (
    [InstanceID]           INT           NOT NULL,
    [ProcID]               INT           NOT NULL,
    [SnapshotDate]         DATETIME2 (3) NOT NULL,
    [PeriodTime]           BIGINT        NOT NULL,
    [total_worker_time]    BIGINT        NOT NULL,
    [total_elapsed_time]   BIGINT        NOT NULL,
    [total_logical_reads]  BIGINT        NOT NULL,
    [total_logical_writes] BIGINT        NOT NULL,
    [total_physical_reads] BIGINT        NOT NULL,
    [execution_count]      BIGINT        NOT NULL,
    [IsCompile]            BIT           NOT NULL,
    CONSTRAINT [PK_ProcStats] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [SnapshotDate] ASC, [ProcID] ASC) WITH (DATA_COMPRESSION = PAGE) ON [PS_ProcStats] ([SnapshotDate]),
    CONSTRAINT [FK_ProcStats_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID]),
    CONSTRAINT [FK_ProcStats_Procs] FOREIGN KEY ([ProcID]) REFERENCES [dbo].[Procs] ([ProcID])
) ON [PS_ProcStats] ([SnapshotDate]);





