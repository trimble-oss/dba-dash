CREATE TABLE [dbo].[DBFileSnapshot] (
    [SnapshotDate] DATETIME2 (2) NOT NULL,
    [FileID]       INT           NOT NULL,
    [Size]         BIGINT        NOT NULL,
    [space_used]   BIGINT        NULL,
    CONSTRAINT [PK_DBFileSnapshot] PRIMARY KEY CLUSTERED ([SnapshotDate] ASC, [FileID] ASC) WITH (DATA_COMPRESSION = PAGE),
    CONSTRAINT [FK_DBFileSnapshot_DBFiles] FOREIGN KEY ([FileID]) REFERENCES [dbo].[DBFiles] ([FileID])
);






GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_DBFileSnapshot_FileID_SnapshotDate]
    ON [dbo].[DBFileSnapshot]([FileID] ASC, [SnapshotDate] ASC)
    INCLUDE([Size], [space_used]) WITH (DATA_COMPRESSION = PAGE);

