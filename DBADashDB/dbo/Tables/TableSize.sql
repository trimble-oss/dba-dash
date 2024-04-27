CREATE TABLE dbo.TableSize(
	InstanceID INT NOT NULL,
	DatabaseID INT NOT NULL,
	ObjectID BIGINT NOT NULL,
	SnapshotDate DATETIME2(7) NOT NULL,
	row_count BIGINT NOT NULL,
	reserved_pages BIGINT NOT NULL,
	used_pages BIGINT NOT NULL,
	data_pages BIGINT NOT NULL,
	index_pages  AS (used_pages-data_pages),
	CONSTRAINT PK_TableSize PRIMARY KEY CLUSTERED 
	(
		InstanceID ASC,
		SnapshotDate ASC,
		DatabaseID ASC,
		ObjectID ASC
	) WITH(DATA_COMPRESSION=PAGE) ON [PS_TableSize](SnapshotDate)
) ON [PS_TableSize](SnapshotDate)
GO