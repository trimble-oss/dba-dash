CREATE TABLE [Staging].[Waits] (
    [InstanceID]          INT           NOT NULL,
    [SnapshotDate]        DATETIME2 (3) NOT NULL,
    [wait_type]           NVARCHAR (60) NOT NULL,
    [waiting_tasks_count] BIGINT        NOT NULL,
    [wait_time_ms]        BIGINT        NOT NULL,
    [signal_wait_time_ms] BIGINT        NOT NULL,
    CONSTRAINT [PK_Staging_Waits] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [wait_type] ASC)
);

