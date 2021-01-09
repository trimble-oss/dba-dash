CREATE TABLE [dbo].[DBConfigHistory] (
    [DatabaseID]              INT            NOT NULL,
    [configuration_id]        INT            NOT NULL,
    [value]                   NVARCHAR (128) NULL,
    [value_for_secondary]     NVARCHAR (128) NULL,
    [new_value]               NVARCHAR (128) NULL,
    [new_value_for_secondary] NVARCHAR (128) NULL,
    [ValidFrom]               DATETIME2 (2)  NOT NULL,
    [ValidTo]                 DATETIME2 (2)  NOT NULL,
    CONSTRAINT [PK_DBConfigHistory] PRIMARY KEY CLUSTERED ([DatabaseID] ASC, [configuration_id] ASC, [ValidTo] ASC)
);

