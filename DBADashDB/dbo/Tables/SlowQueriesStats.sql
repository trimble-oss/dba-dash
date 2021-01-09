CREATE TABLE [dbo].[SlowQueriesStats] (
    [InstanceID]           INT           NOT NULL,
    [SnapshotDate]         DATETIME2 (3) NOT NULL,
    [Truncated]            INT           NULL,
    [ProcessingTime]       INT           NULL,
    [TotalEventsProcessed] INT           NULL,
    [EventCount]           INT           NULL,
    [DroppedCount]         INT           NULL,
    [MemoryUsed]           INT           NULL,
    CONSTRAINT [PK_SlowQueriesStats] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [SnapshotDate] ASC) ON [PS_SlowQueries] ([SnapshotDate])
) ON [PS_SlowQueries] ([SnapshotDate]);

