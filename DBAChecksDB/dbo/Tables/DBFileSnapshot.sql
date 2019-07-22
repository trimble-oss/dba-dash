CREATE TABLE [dbo].[DBFileSnapshot] (
    [SnapshotDate] DATETIME2 (2) NOT NULL,
    [FileID]       INT           NOT NULL,
    [Size]         BIGINT        NOT NULL,
    [space_used]   BIGINT        NULL,
    CONSTRAINT [PK_DBFileSnapshot] PRIMARY KEY CLUSTERED ([SnapshotDate] ASC, [FileID] ASC),
    CONSTRAINT [FK_DBFileSnapshot_DBFiles] FOREIGN KEY ([FileID]) REFERENCES [dbo].[DBFiles] ([FileID])
);



