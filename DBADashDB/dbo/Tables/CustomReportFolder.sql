CREATE TABLE dbo.CustomReportFolder(
	SchemaName NVARCHAR(128) NOT NULL,
	ProcedureName NVARCHAR(128) NOT NULL,
	FolderPath NVARCHAR(400) NOT NULL,
	CONSTRAINT PK_CustomReportFolder PRIMARY KEY(SchemaName, ProcedureName)
)
