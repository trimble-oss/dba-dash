CREATE TABLE #sysdb(
	[name] [sysname] NOT NULL,
	[database_id] [int] NOT NULL,
	[source_database_id] [int] NULL,
	[owner_sid] [varbinary](85) NULL,
	[create_date] [datetime] NOT NULL,
	[compatibility_level] [tinyint] NOT NULL,
	[collation_name] [sysname] NULL,
	[user_access] [tinyint] NULL,
	[is_read_only] [bit] NULL,
	[is_auto_close_on] [bit] NOT NULL,
	[is_auto_shrink_on] [bit] NULL,
	[state] [tinyint] NULL,
	[is_in_standby] [bit] NULL,
	[is_cleanly_shutdown] [bit] NULL,
	[is_supplemental_logging_enabled] [bit] NULL,
	[snapshot_isolation_state] [tinyint] NULL,
	[is_read_committed_snapshot_on] [bit] NULL,
	[recovery_model] [tinyint] NULL,
	[page_verify_option] [tinyint] NULL,
	[is_auto_create_stats_on] [bit] NULL,
	[is_auto_create_stats_incremental_on] [bit] NULL,
	[is_auto_update_stats_on] [bit] NULL,
	[is_auto_update_stats_async_on] [bit] NULL,
	[is_ansi_null_default_on] [bit] NULL,
	[is_ansi_nulls_on] [bit] NULL,
	[is_ansi_padding_on] [bit] NULL,
	[is_ansi_warnings_on] [bit] NULL,
	[is_arithabort_on] [bit] NULL,
	[is_concat_null_yields_null_on] [bit] NULL,
	[is_numeric_roundabort_on] [bit] NULL,
	[is_quoted_identifier_on] [bit] NULL,
	[is_recursive_triggers_on] [bit] NULL,
	[is_cursor_close_on_commit_on] [bit] NULL,
	[is_local_cursor_default] [bit] NULL,
	[is_fulltext_enabled] [bit] NULL,
	[is_trustworthy_on] [bit] NULL,
	[is_db_chaining_on] [bit] NULL,
	[is_parameterization_forced] [bit] NULL,
	[is_master_key_encrypted_by_server] [bit] NOT NULL,
	[is_query_store_on] [bit] NULL,
	[is_published] [bit] NOT NULL,
	[is_subscribed] [bit] NOT NULL,
	[is_merge_published] [bit] NOT NULL,
	[is_distributor] [bit] NOT NULL,
	[is_sync_with_backup] [bit] NOT NULL,
	[is_broker_enabled] [bit] NOT NULL,
	[log_reuse_wait] [tinyint] NULL,
	[is_date_correlation_on] [bit] NOT NULL,
	[is_cdc_enabled] [bit] NULL, --
	[is_encrypted] [bit] NULL,
	[is_honor_broker_priority_on] [bit] NULL,
	[replica_id] [uniqueidentifier] NULL,
	[group_database_id] [uniqueidentifier] NULL,
	[resource_pool_id] [int] NULL,
	[default_language_lcid] [smallint] NULL,
	[default_language_name] [nvarchar](128) NULL,
	[default_fulltext_language_lcid] [int] NULL,
	[default_fulltext_language_name] [nvarchar](128) NULL,
	[is_nested_triggers_on] [bit] NULL,
	[is_transform_noise_words_on] [bit] NULL,
	[two_digit_year_cutoff] [smallint] NULL,
	[containment] [tinyint] NULL,
	[target_recovery_time_in_seconds] [int] NULL,
	[delayed_durability] [int] NULL,
	[is_memory_optimized_elevate_to_snapshot_on] [bit] NULL,
	[is_federation_member] [bit] NULL,
	[is_remote_data_archive_enabled] [bit] NULL,
	[is_mixed_page_allocation_on] [bit] NULL
) 

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

DECLARE @EditionID BIGINT
SELECT @EditionID = CAST(SERVERPROPERTY('EditionID') as bigint) 

SELECT @SQL = 'INSERT INTO #sysdb(' + @cols + ')
		SELECT ' + @cols + ' FROM sys.databases 
		' + CASE WHEN @EditionID = 1674378470 THEN ' WHERE name = DB_NAME()' ELSE '' END -- current DB only for azure

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
       is_mixed_page_allocation_on
FROM #sysdb
DROP TABLE #sysdb
DROP TABLE #sysdbcols