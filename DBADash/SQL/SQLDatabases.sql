CREATE TABLE #sysdb (
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
    is_cdc_enabled BIT NULL, --
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
    is_ledger_on BIT NULL
);

SELECT *
INTO #sysdbcols
FROM sys.databases 
WHERE 1=2

DECLARE @SQL NVARCHAR(MAX)
DECLARE @cols NVARCHAR(MAX)
SET @cols = 
STUFF((SELECT ',' + name  
FROM (
	SELECT name  FROM tempdb.sys.columns WHERE OBJECT_ID = OBJECT_ID('tempdb..#sysdb')
	INTERSECT
	SELECT name FROM tempdb.sys.columns WHERE OBJECT_ID = OBJECT_ID('tempdb..#sysdbcols')
	) T
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')

DECLARE @IsAzureDB BIT
SELECT @IsAzureDB = CASE WHEN CAST(SERVERPROPERTY('EngineEdition') AS INT) =5 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END

SELECT @SQL = 'INSERT INTO #sysdb(' + @cols + ')
		SELECT ' + @cols + ' FROM sys.databases 
		' + CASE WHEN @IsAzureDB=CAST(1 AS BIT) THEN ' WHERE name = DB_NAME()' ELSE '' END -- current DB only for azure

EXEC sp_executesql @SQL

SELECT name,
       database_id,
       source_database_id,
       owner_sid,
       create_date,
       compatibility_level,
       collation_name,
       user_access,
       is_read_only,
       is_auto_close_on,
       is_auto_shrink_on,
       state,
       is_in_standby,
       is_cleanly_shutdown,
       is_supplemental_logging_enabled,
       snapshot_isolation_state,
       is_read_committed_snapshot_on,
       recovery_model,
       page_verify_option,
       is_auto_create_stats_on,
       is_auto_create_stats_incremental_on,
       is_auto_update_stats_on,
       is_auto_update_stats_async_on,
       is_ansi_null_default_on,
       is_ansi_nulls_on,
       is_ansi_padding_on,
       is_ansi_warnings_on,
       is_arithabort_on,
       is_concat_null_yields_null_on,
       is_numeric_roundabort_on,
       is_quoted_identifier_on,
       is_recursive_triggers_on,
       is_cursor_close_on_commit_on,
       is_local_cursor_default,
       is_fulltext_enabled,
       is_trustworthy_on,
       is_db_chaining_on,
       is_parameterization_forced,
       is_master_key_encrypted_by_server,
       is_query_store_on,
       is_published,
       is_subscribed,
       is_merge_published,
       is_distributor,
       is_sync_with_backup,
       is_broker_enabled,
       log_reuse_wait,
       is_date_correlation_on,
       is_cdc_enabled,
       is_encrypted,
       is_honor_broker_priority_on,
       replica_id,
       group_database_id,
       resource_pool_id,
       default_language_lcid,
       default_language_name,
       default_fulltext_language_lcid,
       default_fulltext_language_name,
       is_nested_triggers_on,
       is_transform_noise_words_on,
       two_digit_year_cutoff,
       containment,
       target_recovery_time_in_seconds,
       delayed_durability,
       is_memory_optimized_elevate_to_snapshot_on,
       is_federation_member,
       is_remote_data_archive_enabled,
       is_mixed_page_allocation_on,
	   is_ledger_on
FROM #sysdb

DROP TABLE #sysdb
DROP TABLE #sysdbcols