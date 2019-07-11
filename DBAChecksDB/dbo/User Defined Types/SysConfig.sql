CREATE TYPE [dbo].[SysConfig] AS TABLE (
    [configuration_id] INT         NOT NULL,
    [value]            SQL_VARIANT NULL,
    [value_in_use]     SQL_VARIANT NULL,
    PRIMARY KEY CLUSTERED ([configuration_id] ASC));

