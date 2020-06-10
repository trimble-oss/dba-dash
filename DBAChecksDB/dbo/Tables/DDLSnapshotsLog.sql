CREATE TABLE [dbo].[DDLSnapshotsLog] (
    [DatabaseID]        INT           NOT NULL,
    [SnapshotDate]      DATETIME2 (3) NOT NULL,
    [ValidatedSnapshot] DATETIME2 (3) NULL,
    [EndDate]           DATETIME2 (3) NOT NULL,
    [Duration]          INT           NOT NULL,
    CONSTRAINT [PK_DDLSnapshotsLog] PRIMARY KEY CLUSTERED ([DatabaseID] ASC, [SnapshotDate] ASC)
);

