CREATE TYPE [dbo].[Backups] AS TABLE (
    [database_name] [sysname]     NOT NULL,
    [type]          CHAR (1)      NULL,
    [LastBackup]    DATETIME2 (2) NULL);

