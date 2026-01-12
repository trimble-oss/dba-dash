CREATE TYPE dbo.AvailabilityReplicas AS TABLE(
	   replica_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
       group_id UNIQUEIDENTIFIER NOT NULL,
       replica_metadata_id INT NULL,
       replica_server_name NVARCHAR(256) NULL,
       endpoint_url NVARCHAR(256) NULL,
       availability_mode TINYINT NULL,
       failover_mode TINYINT NULL,
       session_timeout INT NULL,
       primary_role_allow_connections TINYINT NULL,
       secondary_role_allow_connections TINYINT NULL,
       create_date DATETIME NULL,
       modify_date DATETIME NULL,
       backup_priority INT NULL,
       read_only_routing_url NVARCHAR(256) NULL,
       seeding_mode TINYINT NULL,
       read_write_routing_url NVARCHAR(256) NULL
)
