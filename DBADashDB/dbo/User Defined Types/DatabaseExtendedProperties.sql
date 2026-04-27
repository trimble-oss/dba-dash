CREATE TYPE [dbo].[DatabaseExtendedProperties] AS TABLE (
    [database_id] INT     NOT NULL,
    [name]        SYSNAME NOT NULL,
    [value]       NVARCHAR(MAX) NULL,
    PRIMARY KEY CLUSTERED ([database_id] ASC, [name] ASC)
)
