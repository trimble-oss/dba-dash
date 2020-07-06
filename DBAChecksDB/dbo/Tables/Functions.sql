CREATE TABLE [dbo].[Functions] (
    [FunctionID]  INT       IDENTITY (1, 1) NOT NULL,
    [DatabaseID]  INT       NOT NULL,
    [object_name] [sysname] NOT NULL,
    [object_id]   INT       NOT NULL,
    [type]        CHAR(2)   NULL,
    schema_name   SYSNAME   NULL,
    CONSTRAINT [PK_Functions] PRIMARY KEY CLUSTERED ([FunctionID] ASC),
    CONSTRAINT [FK_Functions_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Functions_DatabaseID_object_name]
    ON [dbo].[Functions]([DatabaseID] ASC, [object_name] ASC, [object_id] ASC);

