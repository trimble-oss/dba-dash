CREATE TABLE [dbo].[DDLSnapshotOptions] (
    [DDLSnapshotOptionsID] INT           IDENTITY (1, 1) NOT NULL,
    [SnapshotOptionsHash]  BINARY (32)   NOT NULL,
    [SnapshotOptions]      VARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_DDLSnapshotOptions] PRIMARY KEY CLUSTERED ([DDLSnapshotOptionsID] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_DDLSnapshotOptions_SnapshotOptionsHash]
    ON [dbo].[DDLSnapshotOptions]([SnapshotOptionsHash] ASC);

