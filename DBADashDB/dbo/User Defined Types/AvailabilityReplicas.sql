CREATE TYPE dbo.AvailabilityReplicas AS TABLE(
	   replica_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
       group_id UNIQUEIDENTIFIER NOT NULL,
       replica_metadata_id INT NULL,
       replica_server_name NVARCHAR(256) NOT NULL,
       endpoint_url NVARCHAR(128) NULL,
       availability_mode TINYINT NOT NULL,
       failover_mode TINYINT NOT NULL,
       session_timeout INT NOT NULL,
       primary_role_allow_connections TINYINT NOT NULL,
       secondary_role_allow_connections TINYINT NOT NULL,
       create_date DATETIME NULL,
       modify_date DATETIME NULL,
       backup_priority INT NOT NULL,
       read_only_routing_url NVARCHAR(256) NULL,
       seeding_mode TINYINT NULL,
       read_write_routing_url NVARCHAR(256) NULL
)
