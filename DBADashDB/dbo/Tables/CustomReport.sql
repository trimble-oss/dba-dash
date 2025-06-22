CREATE TABLE dbo.CustomReport(
	SchemaName NVARCHAR(128) NOT NULL,
	ProcedureName NVARCHAR(128) NOT NULL,
	MetaData NVARCHAR(MAX) NOT NULL,
	Type VARCHAR(50) NOT NULL CONSTRAINT DF_CustomReport_Type DEFAULT('CustomReport'),
	CONSTRAINT PK_CustomReport PRIMARY KEY(SchemaName, ProcedureName, Type)
)