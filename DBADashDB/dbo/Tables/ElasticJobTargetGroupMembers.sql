/*
	Members of the target groups of an elastic job agent - jobs.target_group_members in its job database.
	A member is a database, a logical server (each of its databases), an elastic pool (each database in the pool) or a shard map (each of
	its shards), included in or excluded from the group.
*/
CREATE TABLE dbo.ElasticJobTargetGroupMembers(
	InstanceID INT NOT NULL,
	target_group_id UNIQUEIDENTIFIER NOT NULL,
	target_id UNIQUEIDENTIFIER NOT NULL,
	target_group_name NVARCHAR(128) NOT NULL,
	membership_type NVARCHAR(50) NULL,
	target_type NVARCHAR(128) NULL,
	refresh_credential_name NVARCHAR(128) NULL,
	subscription_id UNIQUEIDENTIFIER NULL,
	resource_group_name NVARCHAR(128) NULL,
	server_name NVARCHAR(256) NULL,
	database_name NVARCHAR(128) NULL,
	elastic_pool_name NVARCHAR(128) NULL,
	shard_map_name NVARCHAR(128) NULL,
	CONSTRAINT PK_ElasticJobTargetGroupMembers PRIMARY KEY CLUSTERED (InstanceID, target_group_id, target_id),
	CONSTRAINT FK_ElasticJobTargetGroupMembers_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
);
