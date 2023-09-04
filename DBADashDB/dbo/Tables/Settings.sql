CREATE TABLE dbo.Settings (
    SettingName  VARCHAR (100) NOT NULL,
    SettingValue SQL_VARIANT  NOT NULL,
    CONSTRAINT PK_Settings PRIMARY KEY CLUSTERED (SettingName ASC)
);

