CREATE TYPE dbo.AvailableProcs AS TABLE(
	database_name SYSNAME,
	from_master BIT,
	schema_name SYSNAME,
	object_name SYSNAME,
	parameters XML
)
