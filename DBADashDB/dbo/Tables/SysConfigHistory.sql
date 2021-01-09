CREATE TABLE [dbo].[SysConfigHistory] (
    [InstanceID]       INT           NOT NULL,
    [configuration_id] INT           NOT NULL,
    [value]            SQL_VARIANT   NULL,
    [value_in_use]     SQL_VARIANT   NULL,
    [new_value]        SQL_VARIANT   NULL,
    [new_value_in_use] SQL_VARIANT   NULL,
    [ValidFrom]        DATETIME2 (2) NOT NULL,
    [ValidTo]          DATETIME2 (2) NOT NULL,
    CONSTRAINT [PK_SysConfigHistory] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [configuration_id] ASC, [ValidTo] ASC)
);

