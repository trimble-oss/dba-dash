/* Columns in the order of the third result set of SQLElasticJobs.sql - table-valued parameters bind by ordinal */
CREATE TYPE dbo.ElasticJobTargetGroupMembers AS TABLE(
	target_group_id UNIQUEIDENTIFIER NOT NULL,
	target_group_name NVARCHAR(128) NOT NULL,
	target_id UNIQUEIDENTIFIER NOT NULL,
	membership_type NVARCHAR(50) NULL,
	target_type NVARCHAR(128) NULL,
	refresh_credential_name NVARCHAR(128) NULL,
	subscription_id UNIQUEIDENTIFIER NULL,
	resource_group_name NVARCHAR(128) NULL,
	server_name NVARCHAR(256) NULL,
	database_name NVARCHAR(128) NULL,
	elastic_pool_name NVARCHAR(128) NULL,
	shard_map_name NVARCHAR(128) NULL
);
