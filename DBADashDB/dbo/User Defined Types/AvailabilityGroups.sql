CREATE TYPE dbo.AvailabilityGroups as TABLE(
	   group_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
       name NVARCHAR(128) NOT NULL,
       resource_id NVARCHAR(40) NOT NULL,
       resource_group_id NVARCHAR(40) NOT NULL,
       failure_condition_level INT NOT NULL,
       health_check_timeout INT NOT NULL,
       automated_backup_preference TINYINT NOT NULL,
       version SMALLINT  NOT NULL,
       basic_features BIT  NOT NULL,
       dtc_support BIT NOT NULL, 
       db_failover BIT NOT NULL,
       is_distributed BIT NOT NULL,
       cluster_type TINYINT NOT NULL,
       required_synchronized_secondaries_to_commit INT NOT NULL,
       sequence_number BIGINT NOT NULL,
       is_contained BIT NOT NULL
)