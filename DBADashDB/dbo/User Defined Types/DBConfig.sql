CREATE TYPE [dbo].[DBConfig] AS TABLE (
    [database_id]         INT            NOT NULL,
    [configuration_id]    INT            NOT NULL,
    [name]                NVARCHAR (60)  NOT NULL,
    [value]               NVARCHAR (128) NULL,
    [value_for_secondary] NVARCHAR (128) NULL,
    PRIMARY KEY CLUSTERED ([database_id] ASC, [configuration_id] ASC));

