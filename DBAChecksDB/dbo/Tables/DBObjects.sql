CREATE TABLE [dbo].[DBObjects] (
    [ObjectID]             BIGINT        IDENTITY (1, 1) NOT NULL,
    [DatabaseID]           INT           NOT NULL,
    [ObjectType]           CHAR (3)      NOT NULL,
    [object_id]            INT           NOT NULL,
    [SchemaName]           [sysname]     NOT NULL,
    [ObjectName]           [sysname]     NOT NULL,
    [DDLID]                BIGINT        NOT NULL,
    [SnapshotDateCreated]  DATETIME2 (3) NOT NULL,
    [SnapshotDateModified] DATETIME2 (3) NOT NULL,
    [ObjectDateCreated]    DATETIME2 (3) NULL,
    [ObjectDateModified]   DATETIME2 (3) NULL,
    [RevisionCount]        INT           NOT NULL,
    [IsActive]             BIT           NOT NULL,
    CONSTRAINT [PK_DBObjects] PRIMARY KEY CLUSTERED ([ObjectID] ASC),
    CONSTRAINT [FK_DBObjects_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID]),
    CONSTRAINT [FK_DBObjects_DBObjectDDL] FOREIGN KEY ([DDLID]) REFERENCES [dbo].[DDL] ([DDLID])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_DBObjects_DatabaseId_ObjectName_SchemaName_ObjectType]
    ON [dbo].[DBObjects]([DatabaseID] ASC, [ObjectName] ASC, [SchemaName] ASC, [ObjectType] ASC);

