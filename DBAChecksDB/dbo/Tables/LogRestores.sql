CREATE TABLE [dbo].[LogRestores] (
    [DatabaseID]        INT            NOT NULL,
    [restore_date]      DATETIME       NULL,
    [backup_start_date] DATETIME       NULL,
    [last_file]         NVARCHAR (260) NULL,
    CONSTRAINT [FK_LogRestores_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID])
);



