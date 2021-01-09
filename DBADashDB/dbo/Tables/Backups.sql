CREATE TABLE [dbo].[Backups] (
    [DatabaseID] INT           NOT NULL,
    [type]       CHAR (1)      NOT NULL,
    [LastBackup] DATETIME2 (1) NOT NULL,
    CONSTRAINT [PK_Backups] PRIMARY KEY CLUSTERED ([DatabaseID] ASC, [type] ASC),
    CONSTRAINT [FK_Backups_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID])
);



