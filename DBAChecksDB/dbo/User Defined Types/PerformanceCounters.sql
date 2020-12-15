CREATE TYPE [dbo].[PerformanceCounters] AS TABLE (
    [SnapshotDate]  DATETIME2 (7) NOT NULL,
    [object_name]   NCHAR (128)   NOT NULL,
    [counter_name]  NCHAR (128)   NOT NULL,
    [instance_name] NCHAR (128)   NOT NULL,
    [cntr_value]    DECIMAL(28,9)        NOT NULL,
    [cntr_type]     INT           NOT NULL,
    PRIMARY KEY CLUSTERED ([SnapshotDate],[cntr_type] ASC, [object_name] ASC, [counter_name] ASC, [instance_name] ASC));

