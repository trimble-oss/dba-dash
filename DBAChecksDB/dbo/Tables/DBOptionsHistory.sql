CREATE TABLE [dbo].[DBOptionsHistory] (
    [DatabaseID] INT           NOT NULL,
    [Setting]    [sysname]     NOT NULL,
    [OldValue]   SQL_VARIANT   NULL,
    [NewValue]   SQL_VARIANT   NULL,
    [ChangeDate] DATETIME2 (2) NOT NULL,
    CONSTRAINT [PK_DBOptionsHistory] PRIMARY KEY CLUSTERED ([DatabaseID] ASC, [Setting] ASC, [ChangeDate] ASC),
    CONSTRAINT [FK_DBOptionsHistory_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID])
);

