CREATE TABLE dbo.InstanceMetadataHistory(
	InstanceID INT NOT NULL,
	Provider VARCHAR(50) NOT NULL,
	Metadata NVARCHAR(MAX) NOT NULL,
	SnapshotDate DATETIME2 NOT NULL, /* Date of first collection. */
	PreviousVersionLastSnapshotDate DATETIME2 NULL, /* The last time the previous snapshot was validated. (Can be used to narrow down when the change occurred) */
	ValidFrom DATETIME2 NOT NULL, /* Date of first collection import. */
	ValidTo DATETIME2 NOT NULL /* Date this row was replaced */,
) 
GO
CREATE CLUSTERED INDEX IX_InstanceMetadataHistory ON dbo.InstanceMetadataHistory
(
	InstanceID ASC,
	[ValidTo] ASC,
	[ValidFrom] ASC
)