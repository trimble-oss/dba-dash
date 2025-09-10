CREATE TABLE dbo.Databases (
    DatabaseID INT IDENTITY(1, 1) NOT NULL,
    InstanceID INT NOT NULL,
    name sysname NOT NULL,
    database_id INT NOT NULL,
    source_database_id INT NULL,
    owner_sid VARBINARY(85) NULL,
    create_date DATETIME NOT NULL,
    compatibility_level TINYINT NOT NULL,
    collation_name sysname NULL,
    user_access TINYINT NULL,
    is_read_only BIT NULL,
    is_auto_close_on BIT NOT NULL,
    is_auto_shrink_on BIT NULL,
    state TINYINT NULL,
    is_in_standby BIT NULL,
    is_cleanly_shutdown BIT NULL,
    is_supplemental_logging_enabled BIT NULL,
    snapshot_isolation_state TINYINT NULL,
    is_read_committed_snapshot_on BIT NULL,
    recovery_model TINYINT NULL,
    page_verify_option TINYINT NULL,
    is_auto_create_stats_on BIT NULL,
    is_auto_create_stats_incremental_on BIT NULL,
    is_auto_update_stats_on BIT NULL,
    is_auto_update_stats_async_on BIT NULL,
    is_ansi_null_default_on BIT NULL,
    is_ansi_nulls_on BIT NULL,
    is_ansi_padding_on BIT NULL,
    is_ansi_warnings_on BIT NULL,
    is_arithabort_on BIT NULL,
    is_concat_null_yields_null_on BIT NULL,
    is_numeric_roundabort_on BIT NULL,
    is_quoted_identifier_on BIT NULL,
    is_recursive_triggers_on BIT NULL,
    is_cursor_close_on_commit_on BIT NULL,
    is_local_cursor_default BIT NULL,
    is_fulltext_enabled BIT NULL,
    is_trustworthy_on BIT NULL,
    is_db_chaining_on BIT NULL,
    is_parameterization_forced BIT NULL,
    is_master_key_encrypted_by_server BIT NOT NULL,
    is_query_store_on BIT NULL,
    is_published BIT NOT NULL,
    is_subscribed BIT NOT NULL,
    is_merge_published BIT NOT NULL,
    is_distributor BIT NOT NULL,
    is_sync_with_backup BIT NOT NULL,
    is_broker_enabled BIT NOT NULL,
    log_reuse_wait TINYINT NULL,
    is_date_correlation_on BIT NOT NULL,
    is_cdc_enabled BIT NULL,
    is_encrypted BIT NULL,
    is_honor_broker_priority_on BIT NULL,
    replica_id UNIQUEIDENTIFIER NULL,
    group_database_id UNIQUEIDENTIFIER NULL,
    resource_pool_id INT NULL,
    default_language_lcid SMALLINT NULL,
    default_language_name NVARCHAR(128) NULL,
    default_fulltext_language_lcid INT NULL,
    default_fulltext_language_name NVARCHAR(128) NULL,
    is_nested_triggers_on BIT NULL,
    is_transform_noise_words_on BIT NULL,
    two_digit_year_cutoff SMALLINT NULL,
    containment TINYINT NULL,
    target_recovery_time_in_seconds INT NULL,
    delayed_durability INT NULL,
    is_memory_optimized_elevate_to_snapshot_on BIT NULL,
    is_federation_member BIT NULL,
    is_remote_data_archive_enabled BIT NULL,
    is_mixed_page_allocation_on BIT NULL,
    IsActive BIT NOT NULL,
    LastGoodCheckDbTime DATETIME2(3) NULL,
    VLFCount INT NULL,
    is_ledger_on BIT NULL,
    catalog_collation_type INT NULL,
    is_accelerated_database_recovery_on BIT NULL,
    is_change_feed_enabled BIT NULL,
    is_event_stream_enabled BIT NULL,
    is_memory_optimized_enabled BIT NULL,
    is_temporal_history_retention_enabled BIT NULL,
    is_optimized_locking_on BIT NULL,
    service_broker_guid UNIQUEIDENTIFIER NULL,
    UpdatedDate DATETIME2(3) NULL,
    user_access_desc AS (CASE WHEN user_access = (0) THEN 'MULTI_USER'
                               WHEN user_access = (1) THEN 'SINGLE_USER'
                               WHEN user_access = (2) THEN 'RESTRICTED_USER' ELSE CONVERT(NVARCHAR(60), user_access) END),
    state_desc AS (CASE WHEN state = (0) THEN 'ONLINE'
                       WHEN state = (1) THEN 'RESTORING'
                       WHEN state = (2) THEN 'RECOVERING'
                       WHEN state = (3) THEN 'RECOVERY_PENDING'
                       WHEN state = (4) THEN 'SUSPECT'
                       WHEN state = (5) THEN 'EMERGENCY'
                       WHEN state = (6) THEN 'OFFLINE'
                       WHEN state = (7) THEN 'COPYING'
                       WHEN state = (10) THEN 'OFFLINE_SECONDARY' ELSE CONVERT(NVARCHAR(60), state)END
                  ),
    CONSTRAINT PK_Databases PRIMARY KEY CLUSTERED (DatabaseID ASC),
    CONSTRAINT FK_Databases_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
);

GO
CREATE UNIQUE NONCLUSTERED INDEX IX_Databases_InstanceID_database_id_create_date
    ON dbo.Databases(InstanceID ASC, database_id ASC, create_date ASC)
    INCLUDE(IsActive);

GO
CREATE UNIQUE NONCLUSTERED INDEX IX_Databases_InstanceID_name
    ON dbo.Databases(InstanceID ASC, name ASC)
    INCLUDE(database_id,IsActive,service_broker_guid);
