CREATE TABLE [dbo].[Procs] (
    [ProcID]      INT       IDENTITY (1, 1) NOT NULL,
    [DatabaseID]  INT       NOT NULL,
    [object_name] [sysname] NOT NULL,
    [object_id]   INT       NOT NULL,
    type CHAR(2) NULL,
    [schema_name] [sysname] NULL,
    CONSTRAINT [PK_Procs] PRIMARY KEY CLUSTERED ([ProcID] ASC),
    CONSTRAINT [FK_Procs_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Procs_DatabaseID_object_name]
    ON [dbo].[Procs]([DatabaseID] ASC, [object_name] ASC, [object_id] ASC) WITH (DATA_COMPRESSION = PAGE);

