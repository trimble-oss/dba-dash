CREATE TABLE [dbo].[CollectionDates] (
    [InstanceID]   INT           NOT NULL,
    [Reference]    VARCHAR (30)  NOT NULL,
    [SnapshotDate] DATETIME2 (2) NOT NULL,
    CONSTRAINT [PK_CollectionDates] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [Reference] ASC),
    CONSTRAINT [FK_CollectionDates] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);

