CREATE TABLE [dbo].[DBConfigOptions] (
    [configuration_id] INT           NOT NULL,
    [name]             NVARCHAR (50) NOT NULL,
    CONSTRAINT [PK_DBConfigOptions] PRIMARY KEY CLUSTERED ([configuration_id] ASC)
);

