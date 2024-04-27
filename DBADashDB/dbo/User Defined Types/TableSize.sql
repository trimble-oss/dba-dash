CREATE TYPE dbo.TableSize AS TABLE(
	SnapshotDate DATETIME2(7) NULL,
	DB NVARCHAR(128) NOT NULL,
	database_id SMALLINT NOT NULL,
	schema_name NVARCHAR(128) NOT NULL,
	object_name NVARCHAR(128) NOT NULL,
	object_id INT NOT NULL,
	type CHAR(2) NOT NULL,
	row_count BIGINT NOT NULL,
	reserved_pages BIGINT NOT NULL,
	used_pages BIGINT NOT NULL,
	data_pages BIGINT NOT NULL
)