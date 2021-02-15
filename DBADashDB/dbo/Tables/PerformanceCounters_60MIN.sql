CREATE TABLE [dbo].[PerformanceCounters_60MIN] (
    [InstanceID]   INT             NOT NULL,
    [CounterID]    INT             NOT NULL,
    [SnapshotDate] DATETIME2 (2)   NOT NULL,
    [Value_Total]  DECIMAL (28, 9) NOT NULL,
    [Value_Min]    DECIMAL (28, 9) NOT NULL,
    [Value_Max]    DECIMAL (28, 9) NOT NULL,
    [SampleCount]  SMALLINT        NOT NULL,
    CONSTRAINT [PK_PerformanceCounters_60MIN] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [CounterID] ASC, [SnapshotDate] ASC) ON [PS_PerformanceCounters_60MIN] ([SnapshotDate]),
    CONSTRAINT [FK_PerformanceCounters_60MIN_Counters] FOREIGN KEY ([CounterID]) REFERENCES [dbo].[Counters] ([CounterID]),
    CONSTRAINT [FK_PerformanceCounters_60MIN_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
) ON [PS_PerformanceCounters_60MIN] ([SnapshotDate]);

