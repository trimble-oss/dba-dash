CREATE TABLE dbo.ServerServices(
	InstanceID INT NOT NULL,
	SnapshotDate DATETIME2 NOT NULL,
	servicename NVARCHAR(256) NOT NULL,
	startup_type INT NOT NULL,
	startup_type_desc NVARCHAR(256) NOT NULL,
	status INT NULL,
	status_desc NVARCHAR(256) NOT NULL,
	process_id INT NULL,
	last_startup_time DATETIMEOFFSET NULL,
	service_account NVARCHAR(256) NOT NULL,
	filename NVARCHAR(256) NOT NULL,
	is_clustered NVARCHAR(1) NOT NULL,
	cluster_nodename NVARCHAR(256) NULL,
	instant_file_initialization_enabled NVARCHAR(1) NULL,
	CONSTRAINT PK_ServerSercvies PRIMARY KEY(InstanceID,servicename),
	CONSTRAINT FK_ServerServices_InstanceID FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID)
)
WITH(DATA_COMPRESSION=PAGE);
GO