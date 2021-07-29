CREATE TABLE dbo.QueryText(
	sql_handle VARBINARY(64) NOT NULL,
	dbid SMALLINT NULL,
	object_id INT NULL,
	encrypted BIT NULL,
	text NVARCHAR(MAX) NULL,
	SnapshotDate DATETIME2(2)
	CONSTRAINT PK_QueryText PRIMARY KEY(sql_handle)
)