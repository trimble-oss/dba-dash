CREATE TYPE [dbo].[DDLSnapshot] AS TABLE (
    [ObjectName]         [sysname]       NOT NULL,
    [SchemaName]         [sysname]       NOT NULL,
    [ObjectType]         CHAR (3)        NOT NULL,
    [OBJECT_ID]          INT             NOT NULL,
    [DDLHash]            BINARY (32) NOT NULL,
    [DDL]                VARBINARY (MAX) NOT NULL,
    [ObjectDateCreated]  DATETIME2 (3)   NULL,
    [ObjectDateModified] DATETIME2 (3)   NULL,
    PRIMARY KEY CLUSTERED ([ObjectName] ASC, [SchemaName] ASC, [ObjectType] ASC));

