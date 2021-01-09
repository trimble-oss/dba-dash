CREATE TABLE [dbo].[DBConfigOptions] (
    [configuration_id] INT            NOT NULL,
    [name]             NVARCHAR (50)  NOT NULL,
    [default_value]    NVARCHAR (128) NULL,
    CONSTRAINT [PK_DBConfigOptions] PRIMARY KEY CLUSTERED ([configuration_id] ASC)
);



