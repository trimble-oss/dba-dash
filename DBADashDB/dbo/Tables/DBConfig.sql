CREATE TABLE [dbo].[DBConfig] (
    [DatabaseID]          INT            NOT NULL,
    [configuration_id]    INT            NOT NULL,
    [value]               NVARCHAR (128) NULL,
    [value_for_secondary] NVARCHAR (128) NULL,
    [ValidFrom]           DATETIME2 (2)  NOT NULL,
    CONSTRAINT [PK_DBConfig] PRIMARY KEY CLUSTERED ([DatabaseID] ASC, [configuration_id] ASC),
    CONSTRAINT [FK_DBConfig_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID]),
    CONSTRAINT [FK_DBConfig_DBConfigOptions] FOREIGN KEY ([configuration_id]) REFERENCES [dbo].[DBConfigOptions] ([configuration_id])
);



