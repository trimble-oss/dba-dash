CREATE TABLE dbo.CustomReport(
	SchemaName NVARCHAR(128) NOT NULL,
	ProcedureName NVARCHAR(128) NOT NULL,
	MetaData NVARCHAR(MAX) NOT NULL,
	CONSTRAINT PK_CustomReport PRIMARY KEY(SchemaName, ProcedureName)
)