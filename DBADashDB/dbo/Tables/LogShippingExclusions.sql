CREATE TABLE dbo.LogShippingExclusions(
	InstanceID INT NOT NULL, /* -1 for root level.  Otherwise the secondary instance */
	DatabaseName NVARCHAR(128) NOT NULL, /* % can be used as a wildcard */
	CONSTRAINT PK_LogShippingExclusions PRIMARY KEY CLUSTERED (InstanceID, DatabaseName)
)
