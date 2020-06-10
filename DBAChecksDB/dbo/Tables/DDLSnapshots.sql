CREATE TABLE [dbo].[DDLSnapshots] (
    [DatabaseID]    INT           NOT NULL,
    [SnapshotDate]  DATETIME2 (3) NOT NULL,
    [ValidatedDate] DATETIME2 (3) NOT NULL,
    [DiffCount]     INT           NOT NULL,
    CONSTRAINT [PK_DDLSnapshots] PRIMARY KEY CLUSTERED ([DatabaseID] ASC, [SnapshotDate] ASC),
    CONSTRAINT [FK_DBSnapshots_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID])
);

