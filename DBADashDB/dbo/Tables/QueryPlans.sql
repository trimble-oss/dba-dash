CREATE TABLE dbo.QueryPlans(
	plan_handle VARBINARY(64) NOT NULL,
	statement_start_offset int NOT NULL,
	statement_end_offset int NOT NULL,
	dbid SMALLINT NULL,
	object_id INT NULL,
	encrypted BIT NULL,
	query_plan_hash BINARY(8) NOT NULL,
	query_plan_compresed VARBINARY(MAX) NULL,
	SnapshotDate DATETIME2(2)
	CONSTRAINT PK_QueryPlan PRIMARY KEY(query_plan_hash,plan_handle,statement_start_offset,statement_end_offset)
)