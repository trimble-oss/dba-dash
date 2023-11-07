CREATE TABLE dbo.DataRetention (
    SchemaName SYSNAME NOT NULL CONSTRAINT DF_DataRetention_SchemaName DEFAULT('dbo'),
    TableName SYSNAME NOT NULL,
    RetentionDays INT NULL,
    CONSTRAINT PK_DataRetention PRIMARY KEY CLUSTERED (TableName, SchemaName)
);

