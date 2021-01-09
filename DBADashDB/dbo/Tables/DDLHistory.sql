CREATE TABLE [dbo].[DDLHistory] (
    [DatabaseID]         INT           NOT NULL,
    [ObjectID]           BIGINT        NOT NULL,
    [DDLID]              BIGINT        NOT NULL,
    [SnapshotValidFrom]  DATETIME2 (3) NOT NULL,
    [SnapshotValidTo]    DATETIME2 (3) NOT NULL,
    [object_id]          INT           NOT NULL,
    [ObjectDateCreated]  DATETIME2 (3) NULL,
    [ObjectDateModified] DATETIME2 (3) NULL,
    CONSTRAINT [PK_DDLHistory] PRIMARY KEY CLUSTERED ([ObjectID] ASC, [SnapshotValidTo] ASC),
    CONSTRAINT [FK_DDLHistory_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID]),
    CONSTRAINT [FK_DDLHistory_DBObjects] FOREIGN KEY ([ObjectID]) REFERENCES [dbo].[DBObjects] ([ObjectID]),
    CONSTRAINT [FK_DDLHistory_DDL] FOREIGN KEY ([DDLID]) REFERENCES [dbo].[DDL] ([DDLID])
);


GO
CREATE NONCLUSTERED INDEX [IX_DDLHistory_DatabaseID_SnapshotValidTo]
    ON [dbo].[DDLHistory]([DatabaseID] ASC, [SnapshotValidTo] ASC, [SnapshotValidFrom] ASC)
    INCLUDE([ObjectID], [DDLID]);

