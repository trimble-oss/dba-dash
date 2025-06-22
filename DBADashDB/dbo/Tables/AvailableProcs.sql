CREATE TABLE dbo.AvailableProcs(
	InstanceID INT,
	database_name SYSNAME,
	from_master BIT,
	schema_name SYSNAME,
	object_name SYSNAME,
	parameters XML,
	CONSTRAINT PK_AvailableProcs PRIMARY KEY(InstanceID,schema_name,object_name)
)
