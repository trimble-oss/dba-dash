CREATE PROC [dbo].[Database_Upd](@DB SQLDB READONLY,@InstanceID INT,@SnapshotDate DATETIME)
AS
WITH T AS (
	SELECT D.* 
	FROM dbo.Databases D
	JOIN dbo.Instances I ON D.InstanceID = I.InstanceID
	WHERE I.InstanceID = @InstanceID
)
MERGE T
USING (SELECT * FROM @DB) as S ON S.database_id = T.database_id AND (S.create_date = T.create_date OR S.name=T.name)
WHEN MATCHED THEN 
	UPDATE  SET [name] = S.Name
      ,[source_database_id] = S.source_database_id
      ,[owner_sid] = S.owner_sid
      ,[create_date] = S.Create_date
      ,[compatibility_level] = S.[compatibility_level]
      ,[collation_name] = S.collation_name
      ,[user_access] = S.user_access
      ,[is_read_only] = S.is_read_only
      ,[is_auto_close_on] = S.is_auto_close_on 
      ,[is_auto_shrink_on] = S.is_auto_shrink_on
      ,[state] = S.state
      ,[is_in_standby] = S.is_in_standby
      ,[is_cleanly_shutdown] = S.is_cleanly_shutdown
      ,[is_supplemental_logging_enabled] = S.is_supplemental_logging_enabled
      ,[snapshot_isolation_state] = S.snapshot_isolation_state
      ,[is_read_committed_snapshot_on] = S.is_read_committed_snapshot_on
      ,[recovery_model] = S.recovery_model
      ,[page_verify_option] = S.page_verify_option
      ,[is_auto_create_stats_on] = S.is_auto_create_stats_on
      ,[is_auto_create_stats_incremental_on] = S.is_auto_create_stats_incremental_on
      ,[is_auto_update_stats_on] = S.is_auto_update_stats_on
      ,[is_auto_update_stats_async_on] = S.is_auto_update_stats_async_on
      ,[is_ansi_null_default_on] = S.is_ansi_null_default_on
      ,[is_ansi_nulls_on] = S.is_ansi_nulls_on
      ,[is_ansi_padding_on] = S.is_ansi_padding_on
      ,[is_ansi_warnings_on] = S.is_ansi_warnings_on
      ,[is_arithabort_on] = S.is_arithabort_on
      ,[is_concat_null_yields_null_on] = S.is_concat_null_yields_null_on
      ,[is_numeric_roundabort_on] = S.is_numeric_roundabort_on
      ,[is_quoted_identifier_on] = S.is_quoted_identifier_on
      ,[is_recursive_triggers_on] = S.is_recursive_triggers_on
      ,[is_cursor_close_on_commit_on] = S.is_cursor_close_on_commit_on
      ,[is_local_cursor_default] = S.is_local_cursor_default
      ,[is_fulltext_enabled] = S.is_fulltext_enabled
      ,[is_trustworthy_on] = S.is_trustworthy_on
      ,[is_db_chaining_on] = S.is_db_chaining_on
      ,[is_parameterization_forced] = S.is_parameterization_forced
      ,[is_master_key_encrypted_by_server] = S.is_master_key_encrypted_by_server
      ,[is_query_store_on] = S.is_query_store_on
      ,[is_published] = S.is_published
      ,[is_subscribed] = S.is_subscribed
      ,[is_merge_published] = S.is_merge_published
      ,[is_distributor] = S.is_distributor
      ,[is_sync_with_backup] = S.is_sync_with_backup
      ,[is_broker_enabled] = S.is_broker_enabled
      ,[log_reuse_wait] = S.log_reuse_wait
      ,[is_date_correlation_on] = S.is_date_correlation_on
      ,[is_cdc_enabled] = S.is_cdc_enabled
      ,[is_encrypted] = S.is_encrypted
      ,[is_honor_broker_priority_on] = S.is_honor_broker_priority_on
      ,[replica_id] = S.replica_id
      ,[group_database_id] = S.group_database_id
      ,[resource_pool_id] = S.resource_pool_id
      ,[default_language_lcid] = S.default_language_lcid
      ,[default_language_name] = S.default_language_name
      ,[default_fulltext_language_lcid] = S.default_fulltext_language_lcid
      ,[default_fulltext_language_name] = S.default_fulltext_language_name
      ,[is_nested_triggers_on] = S.is_nested_triggers_on
      ,[is_transform_noise_words_on] = S.is_transform_noise_words_on
      ,[two_digit_year_cutoff] = S.two_digit_year_cutoff
      ,[containment] = S.containment
      ,[target_recovery_time_in_seconds] = S.target_recovery_time_in_seconds
      ,[delayed_durability] = S.delayed_durability
      ,[is_memory_optimized_elevate_to_snapshot_on] = S.is_memory_optimized_elevate_to_snapshot_on
      ,[is_federation_member] = S.is_federation_member
      ,[is_remote_data_archive_enabled] = S.is_remote_data_archive_enabled
      ,[is_mixed_page_allocation_on] = S.is_mixed_page_allocation_on
      ,[IsActive] = 1
WHEN NOT MATCHED BY TARGET THEN
INSERT (
		InstanceID,
		name,
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
       is_mixed_page_allocation_on ,
	   IsActive
)
VALUES( @InstanceID,
		name,
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
       is_mixed_page_allocation_on ,
	   CAST(1 as BIT)
)
WHEN NOT MATCHED BY SOURCE THEN 
UPDATE SET T.IsActive = 0;

UPDATE dbo.SnapshotDates
SET DatabasesDate=@SnapshotDate
WHERE InstanceID=@InstanceID