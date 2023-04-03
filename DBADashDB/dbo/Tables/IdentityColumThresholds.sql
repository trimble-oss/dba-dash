CREATE TABLE dbo.IdentityColumnThresholds(
	InstanceID INT NOT NULL,
	DatabaseID INT NOT NULL,
	object_name NVARCHAR(128) NOT NULL,
	PctUsedWarningThreshold DECIMAL(9,3) NULL,
	PctUsedCriticalThreshold DECIMAL(9,3) NULL,
	CONSTRAINT PK_IdentityColumnThresholds PRIMARY KEY(InstanceID,DatabaseID,object_name)
)