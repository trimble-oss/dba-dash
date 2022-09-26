CREATE TYPE dbo.AvailabilityGroups as TABLE(
	   group_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
       name NVARCHAR(128) NOT NULL,
       resource_id NVARCHAR(40) NULL,
       resource_group_id NVARCHAR(40) NULL,
       failure_condition_level INT NULL,
       health_check_timeout INT NULL,
       automated_backup_preference TINYINT NULL,
       version SMALLINT NULL,
       basic_features BIT NULL,
       dtc_support BIT NULL, 
       db_failover BIT NULL,
       is_distributed BIT NULL,
       cluster_type TINYINT NULL,
       required_synchronized_secondaries_to_commit INT NULL,
       sequence_number BIGINT NULL,
       is_contained BIT NULL
)