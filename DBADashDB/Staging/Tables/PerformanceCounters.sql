CREATE TABLE [Staging].[PerformanceCounters] (
    [InstanceID]    INT           NOT NULL,
    [object_name]   NCHAR (128)   NOT NULL,
    [counter_name]  NCHAR (128)   NOT NULL,
    [instance_name] NCHAR (128)   NOT NULL,
    [cntr_value]    BIGINT        NOT NULL,
    [cntr_type]     INT           NOT NULL,
    [SnapshotDate]  DATETIME2 (7) NOT NULL,
    CONSTRAINT [PK_PerformanceCounters] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [object_name] ASC, [counter_name] ASC, [instance_name] ASC)
);

