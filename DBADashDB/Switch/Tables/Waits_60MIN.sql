CREATE TABLE [Switch].[Waits_60MIN] (
    [InstanceID]          INT           NOT NULL,
    [SnapshotDate]        DATETIME2 (2) NOT NULL,
    [WaitTypeID]          SMALLINT      NOT NULL,
    [waiting_tasks_count] BIGINT        NOT NULL,
    [wait_time_ms]        BIGINT        NOT NULL,
    [signal_wait_time_ms] BIGINT        NOT NULL,
    [sample_ms_diff]      INT           NOT NULL,
    CONSTRAINT [PK_Waits_60MIN] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [SnapshotDate] ASC, [WaitTypeID] ASC),
    CONSTRAINT [FK_Waits_60MIN_Instance] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID]),
    CONSTRAINT [FK_Waits_60MIN_WaitType] FOREIGN KEY ([WaitTypeID]) REFERENCES [dbo].[WaitType] ([WaitTypeID])
);