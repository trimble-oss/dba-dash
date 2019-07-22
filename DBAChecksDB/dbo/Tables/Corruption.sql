CREATE TABLE [dbo].[Corruption] (
    [DatabaseID]  INT      NOT NULL,
    [SourceTable] TINYINT  NOT NULL,
    [UpdateDate]  DATETIME NOT NULL,
    CONSTRAINT [PK_Corruption] PRIMARY KEY CLUSTERED ([DatabaseID] ASC, [SourceTable] ASC),
    CONSTRAINT [FK_Corruption_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID])
);



