CREATE TABLE [dbo].[CollectionDates] (
    [InstanceID]    INT           NOT NULL,
    [Reference]     VARCHAR (100)  NOT NULL,
    [SnapshotDate]  DATETIME2 (2) NOT NULL,
    -- Last time the collection *ran*, even when change detection meant no data was written (e.g. Jobs).
    -- For collections without change detection this tracks SnapshotDate.  NULL for rows written by an
    -- older service version that predates heartbeats - the status view falls back to SnapshotDate.
    [HeartbeatDate] DATETIME2 (2) NULL,
    CONSTRAINT [PK_CollectionDates] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [Reference] ASC),
    CONSTRAINT [FK_CollectionDates] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);

