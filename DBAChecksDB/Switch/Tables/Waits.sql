CREATE TABLE [Switch].[Waits] (
    [InstanceID]          INT           NOT NULL,
    [SnapshotDate]        DATETIME2 (2) NOT NULL,
    [WaitTypeID]          SMALLINT      NOT NULL,
    [waiting_tasks_count] BIGINT        NOT NULL,
    [wait_time_ms]        BIGINT        NOT NULL,
    [signal_wait_time_ms] BIGINT        NOT NULL,
    [sample_ms_diff]      INT           NOT NULL,
    CONSTRAINT [PK_Waits] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [SnapshotDate] ASC, [WaitTypeID] ASC) WITH (DATA_COMPRESSION = PAGE)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Waits_SnapshotDate_InstanceID_WaitTypeID]
    ON [Switch].[Waits]([SnapshotDate] ASC, [InstanceID] ASC, [WaitTypeID] ASC)
    INCLUDE([wait_time_ms]) WITH (DATA_COMPRESSION = PAGE);

