CREATE TYPE [dbo].[SlowQueriesStats] AS TABLE (
    [Truncated]            INT NULL,
    [ProcessingTime]       INT NULL,
    [TotalEventsProcessed] INT NULL,
    [EventCount]           INT NULL,
    [DroppedCount]         INT NULL,
    [MemoryUsed]           INT NULL);

