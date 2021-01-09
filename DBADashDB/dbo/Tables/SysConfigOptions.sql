CREATE TABLE [dbo].[SysConfigOptions] (
    [configuration_id] INT            NOT NULL,
    [name]             NVARCHAR (35)  NOT NULL,
    [description]      NVARCHAR (255) NOT NULL,
    [is_dynamic]       BIT            NOT NULL,
    [is_advanced]      BIT            NOT NULL,
    [default_value]    SQL_VARIANT    NULL,
    [minimum]          SQL_VARIANT    NULL,
    [maximum]          SQL_VARIANT    NULL,
    CONSTRAINT [PK_SysConfigOptions] PRIMARY KEY CLUSTERED ([configuration_id] ASC)
);

