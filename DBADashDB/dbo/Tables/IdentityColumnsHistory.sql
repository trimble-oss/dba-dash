CREATE TABLE dbo.IdentityColumnsHistory(
	InstanceID INT NOT NULL REFERENCES dbo.Instances(InstanceID),
	DatabaseID INT NOT NULL REFERENCES dbo.Databases(DatabaseID),
	object_id INT NOT NULL,
	SnapshotDate DATETIME2(2) NOT NULL,
	last_value BIGINT NULL,
	row_count BIGINT NULL,
	CONSTRAINT PK_IdentityColumnsHistory PRIMARY KEY(InstanceID,DatabaseID,object_id,SnapshotDate) WITH(DATA_COMPRESSION=PAGE) ON PS_IdentityColumnsHistory(SnapshotDate) 
) ON PS_IdentityColumnsHistory(SnapshotDate)