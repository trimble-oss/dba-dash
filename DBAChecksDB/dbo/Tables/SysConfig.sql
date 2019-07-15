CREATE TABLE [dbo].[SysConfig] (
    [InstanceID]       INT           NOT NULL,
    [configuration_id] INT           NOT NULL,
    [value]            SQL_VARIANT   NULL,
    [value_in_use]     SQL_VARIANT   NULL,
    [ValidFrom]        DATETIME2 (2) CONSTRAINT [DF_SysConfig_ValidFrom] DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [PK_SysConfig] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [configuration_id] ASC)
);



