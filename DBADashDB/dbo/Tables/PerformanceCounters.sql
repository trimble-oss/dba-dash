CREATE TABLE [dbo].[PerformanceCounters] (
    [InstanceID]   INT             NOT NULL,
    [CounterID]    INT             NOT NULL,
    [SnapshotDate] DATETIME2 (2)   NOT NULL,
    [Value]        DECIMAL (28, 9) NOT NULL,
    CONSTRAINT [PK_PerformanceCounters] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [CounterID] ASC, [SnapshotDate] ASC) ON [PS_PerformanceCounters] ([SnapshotDate]),
    CONSTRAINT [FK_PerformanceCounters_Counters] FOREIGN KEY ([CounterID]) REFERENCES [dbo].[Counters] ([CounterID]),
    CONSTRAINT [FK_PerformanceCounters_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
) ON [PS_PerformanceCounters] ([SnapshotDate]);

