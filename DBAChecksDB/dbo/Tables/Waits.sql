CREATE TABLE [dbo].[Waits] (
    [InstanceID]          INT           NOT NULL,
    [SnapshotDate]        DATETIME2 (2) NOT NULL,
    [WaitTypeID]          SMALLINT      NOT NULL,
    [waiting_tasks_count] BIGINT        NOT NULL,
    [wait_time_ms]        BIGINT        NOT NULL,
    [signal_wait_time_ms] BIGINT        NOT NULL,
    [sample_ms_diff]      INT           NOT NULL,
    CONSTRAINT [PK_Waits] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [SnapshotDate] ASC, [WaitTypeID] ASC) WITH (DATA_COMPRESSION = PAGE) ON [PS_Waits] ([SnapshotDate]),
    CONSTRAINT [FK_Waits_Instance] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID]),
    CONSTRAINT [FK_Waits_WaitType] FOREIGN KEY ([WaitTypeID]) REFERENCES [dbo].[WaitType] ([WaitTypeID])
) ON [PS_Waits] ([SnapshotDate]);








GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Waits_SnapshotDate_InstanceID_WaitTypeID]
    ON [dbo].[Waits]([SnapshotDate] ASC, [InstanceID] ASC, [WaitTypeID] ASC)
    INCLUDE([wait_time_ms]) WITH (DATA_COMPRESSION = PAGE) 
    ON [PS_Waits] ([SnapshotDate]);




GO
CREATE COLUMNSTORE INDEX [CI_Waits]
    ON [dbo].[Waits]([InstanceID], [SnapshotDate], [WaitTypeID], [waiting_tasks_count], [wait_time_ms], [signal_wait_time_ms], [sample_ms_diff])
    ON [PS_Waits] ([SnapshotDate]);

