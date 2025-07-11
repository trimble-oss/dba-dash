CREATE TABLE dbo.InstanceMetadata(
	InstanceID INT CONSTRAINT PK_InstanceMetadata PRIMARY KEY,
	Provider VARCHAR(50) NOT NULL,
	Metadata NVARCHAR(MAX) NOT NULL,
	SnapshotDate DATETIME2 NOT NULL, /* Date of first collection. */
	PreviousVersionLastSnapshotDate DATETIME2 NULL, /* The last time the previous snapshot was validated. (Can be used to narrow down when the change occurred) */
	ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL, /* Date of first collection import */
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL,
	PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo),
	CONSTRAINT FK_InstanceMetadata_InstanceID FOREIGN KEY (InstanceID) REFERENCES dbo.Instances(InstanceID) 
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.InstanceMetadataHistory));;