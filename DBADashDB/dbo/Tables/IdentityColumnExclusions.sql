CREATE TABLE dbo.IdentityColumnExclusions(
	InstanceID INT NOT NULL, /* -1 for root level */
	DatabaseID INT NOT NULL, /* -1 for root or instance level */
	ObjectName NVARCHAR(257) NOT NULL, /* Table name or schema.table.  % can be used as a wildcard */
	CONSTRAINT PK_IdentityColumnExclusions PRIMARY KEY CLUSTERED (InstanceID, DatabaseID, ObjectName)
)
