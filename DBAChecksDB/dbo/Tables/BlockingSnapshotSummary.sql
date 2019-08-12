CREATE TABLE [dbo].[BlockingSnapshotSummary] (
    [BlockingSnapshotID]  INT      IDENTITY (1, 1) NOT NULL,
    [InstanceID]          INT      NOT NULL,
    [SnapshotDateUTC]     DATETIME2(2) NOT NULL,
    [BlockedSessionCount] INT      NULL,
    [BlockedWaitTime]     BIGINT   NULL,
    [UTCOffset]           INT      NOT NULL,
    CONSTRAINT [PK_BlockingSnapshotSummary] PRIMARY KEY NONCLUSTERED ([BlockingSnapshotID] ASC)
);




GO
CREATE UNIQUE CLUSTERED INDEX [IX_BlockingSnapshotSummary_SnapshotDateUTC_InstanceID]
    ON [dbo].[BlockingSnapshotSummary]([SnapshotDateUTC] ASC, [InstanceID] ASC);

