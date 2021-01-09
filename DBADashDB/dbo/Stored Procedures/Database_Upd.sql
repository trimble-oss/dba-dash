
CREATE PROC [dbo].[Database_Upd](@DB SQLDB READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
DECLARE @Ref VARCHAR(30)='Database'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	BEGIN TRAN;
	WITH OldDB AS (
		SELECT DatabaseID,
				CAST(owner_sid AS SQL_VARIANT) AS owner_sid,
				CAST(compatibility_level AS SQL_VARIANT) AS compatibility_level,
				CAST(collation_name AS SQL_VARIANT) AS collation_name,
				CAST(user_access AS SQL_VARIANT) AS user_access,
				CAST(is_read_only AS SQL_VARIANT) AS is_read_only,
				CAST(is_auto_close_on AS SQL_VARIANT) AS is_auto_close_on,
				CAST(is_auto_shrink_on AS SQL_VARIANT) AS is_auto_shrink_on,
				CAST(state AS SQL_VARIANT) AS state,
				CAST(is_in_standby AS SQL_VARIANT) AS is_in_standby,
				CAST(is_cleanly_shutdown AS SQL_VARIANT) AS is_cleanly_shutdown,
				CAST(is_supplemental_logging_enabled AS SQL_VARIANT) AS is_supplemental_logging_enabled,
				CAST(snapshot_isolation_state AS SQL_VARIANT) AS snapshot_isolation_state,
				CAST(is_read_committed_snapshot_on AS SQL_VARIANT) AS is_read_committed_snapshot_on,
				CAST(recovery_model AS SQL_VARIANT) AS recovery_model,
				CAST(page_verify_option AS SQL_VARIANT) AS page_verify_option,
				CAST(is_auto_create_stats_on AS SQL_VARIANT) AS is_auto_create_stats_on,
				CAST(is_auto_create_stats_incremental_on AS SQL_VARIANT) AS is_auto_create_stats_incremental_on,
				CAST(is_auto_update_stats_on AS SQL_VARIANT) AS is_auto_update_stats_on,
				CAST(is_auto_update_stats_async_on AS SQL_VARIANT) AS is_auto_update_stats_async_on,
				CAST(is_ansi_null_default_on AS SQL_VARIANT) AS is_ansi_null_default_on,
				CAST(is_ansi_nulls_on AS SQL_VARIANT) AS is_ansi_nulls_on,
				CAST(is_ansi_padding_on AS SQL_VARIANT) AS is_ansi_padding_on,
				CAST(is_ansi_warnings_on AS SQL_VARIANT) AS is_ansi_warnings_on,
				CAST(is_arithabort_on AS SQL_VARIANT) AS is_arithabort_on,
				CAST(is_concat_null_yields_null_on AS SQL_VARIANT) AS is_concat_null_yields_null_on,
				CAST(is_numeric_roundabort_on AS SQL_VARIANT) AS is_numeric_roundabort_on,
				CAST(is_quoted_identifier_on AS SQL_VARIANT) AS is_quoted_identifier_on,
				CAST(is_recursive_triggers_on AS SQL_VARIANT) AS is_recursive_triggers_on,
				CAST(is_cursor_close_on_commit_on AS SQL_VARIANT) AS is_cursor_close_on_commit_on,
				CAST(is_local_cursor_default AS SQL_VARIANT) AS is_local_cursor_default,
				CAST(is_fulltext_enabled AS SQL_VARIANT) AS is_fulltext_enabled,
				CAST(is_trustworthy_on AS SQL_VARIANT) AS is_trustworthy_on,
				CAST(is_db_chaining_on AS SQL_VARIANT) AS is_db_chaining_on,
				CAST(is_parameterization_forced AS SQL_VARIANT) AS is_parameterization_forced,
				CAST(is_master_key_encrypted_by_server AS SQL_VARIANT) AS is_master_key_encrypted_by_server,
				CAST(is_query_store_on AS SQL_VARIANT) AS is_query_store_on,
				CAST(is_published AS SQL_VARIANT) AS is_published,
				CAST(is_subscribed AS SQL_VARIANT) AS is_subscribed,
				CAST(is_merge_published AS SQL_VARIANT) AS is_merge_published,
				CAST(is_distributor AS SQL_VARIANT) AS is_distributor,
				CAST(is_sync_with_backup AS SQL_VARIANT) AS is_sync_with_backup,
				CAST(is_broker_enabled AS SQL_VARIANT) AS is_broker_enabled,
				CAST(is_date_correlation_on AS SQL_VARIANT) AS is_date_correlation_on,
				CAST(is_cdc_enabled AS SQL_VARIANT) AS is_cdc_enabled,
				CAST(is_encrypted AS SQL_VARIANT) AS is_encrypted,
				CAST(is_honor_broker_priority_on AS SQL_VARIANT) AS is_honor_broker_priority_on,
				CAST(replica_id AS SQL_VARIANT) AS replica_id,
				CAST(group_database_id AS SQL_VARIANT) AS group_database_id,
				CAST(resource_pool_id AS SQL_VARIANT) AS resource_pool_id,
				CAST(default_language_lcid AS SQL_VARIANT) AS default_language_lcid,
				CAST(default_language_name AS SQL_VARIANT) AS default_language_name,
				CAST(default_fulltext_language_lcid AS SQL_VARIANT) AS default_fulltext_language_lcid,
				CAST(default_fulltext_language_name AS SQL_VARIANT) AS default_fulltext_language_name,
				CAST(is_nested_triggers_on AS SQL_VARIANT) AS is_nested_triggers_on,
				CAST(is_transform_noise_words_on AS SQL_VARIANT) AS is_transform_noise_words_on,
				CAST(two_digit_year_cutoff AS SQL_VARIANT) AS two_digit_year_cutoff,
				CAST(containment AS SQL_VARIANT) AS containment,
				CAST(target_recovery_time_in_seconds AS SQL_VARIANT) AS target_recovery_time_in_seconds,
				CAST(delayed_durability AS SQL_VARIANT) AS delayed_durability,
				CAST(is_memory_optimized_elevate_to_snapshot_on AS SQL_VARIANT) AS is_memory_optimized_elevate_to_snapshot_on,
				CAST(is_federation_member AS SQL_VARIANT) AS is_federation_member,
				CAST(is_remote_data_archive_enabled AS SQL_VARIANT) AS is_remote_data_archive_enabled,
				CAST(is_mixed_page_allocation_on AS SQL_VARIANT) AS is_mixed_page_allocation_on,
				CAST(IsActive AS SQL_VARIANT) AS IsActive
		FROM dbo.Databases
		WHERE InstanceID  = @InstanceID
	),
	NewDB AS (SELECT DatabaseID,
		CAST(owner_sid AS SQL_VARIANT) AS owner_sid,
		CAST(compatibility_level AS SQL_VARIANT) AS compatibility_level,
		CAST(collation_name AS SQL_VARIANT) AS collation_name,
		CAST(user_access AS SQL_VARIANT) AS user_access,
		CAST(is_read_only AS SQL_VARIANT) AS is_read_only,
		CAST(is_auto_close_on AS SQL_VARIANT) AS is_auto_close_on,
		CAST(is_auto_shrink_on AS SQL_VARIANT) AS is_auto_shrink_on,
		CAST(state AS SQL_VARIANT) AS state,
		CAST(is_in_standby AS SQL_VARIANT) AS is_in_standby,
		CAST(is_cleanly_shutdown AS SQL_VARIANT) AS is_cleanly_shutdown,
		CAST(is_supplemental_logging_enabled AS SQL_VARIANT) AS is_supplemental_logging_enabled,
		CAST(snapshot_isolation_state AS SQL_VARIANT) AS snapshot_isolation_state,
		CAST(is_read_committed_snapshot_on AS SQL_VARIANT) AS is_read_committed_snapshot_on,
		CAST(recovery_model AS SQL_VARIANT) AS recovery_model,
		CAST(page_verify_option AS SQL_VARIANT) AS page_verify_option,
		CAST(is_auto_create_stats_on AS SQL_VARIANT) AS is_auto_create_stats_on,
		CAST(is_auto_create_stats_incremental_on AS SQL_VARIANT) AS is_auto_create_stats_incremental_on,
		CAST(is_auto_update_stats_on AS SQL_VARIANT) AS is_auto_update_stats_on,
		CAST(is_auto_update_stats_async_on AS SQL_VARIANT) AS is_auto_update_stats_async_on,
		CAST(is_ansi_null_default_on AS SQL_VARIANT) AS is_ansi_null_default_on,
		CAST(is_ansi_nulls_on AS SQL_VARIANT) AS is_ansi_nulls_on,
		CAST(is_ansi_padding_on AS SQL_VARIANT) AS is_ansi_padding_on,
		CAST(is_ansi_warnings_on AS SQL_VARIANT) AS is_ansi_warnings_on,
		CAST(is_arithabort_on AS SQL_VARIANT) AS is_arithabort_on,
		CAST(is_concat_null_yields_null_on AS SQL_VARIANT) AS is_concat_null_yields_null_on,
		CAST(is_numeric_roundabort_on AS SQL_VARIANT) AS is_numeric_roundabort_on,
		CAST(is_quoted_identifier_on AS SQL_VARIANT) AS is_quoted_identifier_on,
		CAST(is_recursive_triggers_on AS SQL_VARIANT) AS is_recursive_triggers_on,
		CAST(is_cursor_close_on_commit_on AS SQL_VARIANT) AS is_cursor_close_on_commit_on,
		CAST(is_local_cursor_default AS SQL_VARIANT) AS is_local_cursor_default,
		CAST(is_fulltext_enabled AS SQL_VARIANT) AS is_fulltext_enabled,
		CAST(is_trustworthy_on AS SQL_VARIANT) AS is_trustworthy_on,
		CAST(is_db_chaining_on AS SQL_VARIANT) AS is_db_chaining_on,
		CAST(is_parameterization_forced AS SQL_VARIANT) AS is_parameterization_forced,
		CAST(is_master_key_encrypted_by_server AS SQL_VARIANT) AS is_master_key_encrypted_by_server,
		CAST(is_query_store_on AS SQL_VARIANT) AS is_query_store_on,
		CAST(is_published AS SQL_VARIANT) AS is_published,
		CAST(is_subscribed AS SQL_VARIANT) AS is_subscribed,
		CAST(is_merge_published AS SQL_VARIANT) AS is_merge_published,
		CAST(is_distributor AS SQL_VARIANT) AS is_distributor,
		CAST(is_sync_with_backup AS SQL_VARIANT) AS is_sync_with_backup,
		CAST(is_broker_enabled AS SQL_VARIANT) AS is_broker_enabled,
		CAST(is_date_correlation_on AS SQL_VARIANT) AS is_date_correlation_on,
		CAST(is_cdc_enabled AS SQL_VARIANT) AS is_cdc_enabled,
		CAST(is_encrypted AS SQL_VARIANT) AS is_encrypted,
		CAST(is_honor_broker_priority_on AS SQL_VARIANT) AS is_honor_broker_priority_on,
		CAST(replica_id AS SQL_VARIANT) AS replica_id,
		CAST(group_database_id AS SQL_VARIANT) AS group_database_id,
		CAST(resource_pool_id AS SQL_VARIANT) AS resource_pool_id,
		CAST(default_language_lcid AS SQL_VARIANT) AS default_language_lcid,
		CAST(default_language_name AS SQL_VARIANT) AS default_language_name,
		CAST(default_fulltext_language_lcid AS SQL_VARIANT) AS default_fulltext_language_lcid,
		CAST(default_fulltext_language_name AS SQL_VARIANT) AS default_fulltext_language_name,
		CAST(is_nested_triggers_on AS SQL_VARIANT) AS is_nested_triggers_on,
		CAST(is_transform_noise_words_on AS SQL_VARIANT) AS is_transform_noise_words_on,
		CAST(two_digit_year_cutoff AS SQL_VARIANT) AS two_digit_year_cutoff,
		CAST(containment AS SQL_VARIANT) AS containment,
		CAST(target_recovery_time_in_seconds AS SQL_VARIANT) AS target_recovery_time_in_seconds,
		CAST(delayed_durability AS SQL_VARIANT) AS delayed_durability,
		CAST(is_memory_optimized_elevate_to_snapshot_on AS SQL_VARIANT) AS is_memory_optimized_elevate_to_snapshot_on,
		CAST(is_federation_member AS SQL_VARIANT) AS is_federation_member,
		CAST(is_remote_data_archive_enabled AS SQL_VARIANT) AS is_remote_data_archive_enabled,
		CAST(is_mixed_page_allocation_on AS SQL_VARIANT) AS is_mixed_page_allocation_on,
		CAST(IsActive AS SQL_VARIANT) AS IsActive
		FROM @DB t
		CROSS APPLY(SELECT DatabaseID,D.IsActive 
					FROM dbo.Databases D 
					WHERE D.database_id = T.database_id AND (D.create_date = T.create_date OR D.name=T.name)
					AND D.InstanceID = @InstanceID
		) id
	),
	OldUnPvt AS (
		SELECT DatabaseID, Setting,Value,is_in_standby    
		FROM OldDB
		UNPIVOT(Value FOR Setting IN(
			   owner_sid,
			   compatibility_level,
			   collation_name,
			   user_access,
			   is_read_only,
			   is_auto_close_on,
			   is_auto_shrink_on,
			   state,
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
			   is_mixed_page_allocation_on)
			   ) AS upvt
	),
	NewUnPvt AS (
		SELECT DatabaseID, Setting,Value, is_in_standby    
		FROM NewDB
		UNPIVOT(Value FOR Setting IN(
			   owner_sid,
			   compatibility_level,
			   collation_name,
			   user_access,
			   is_read_only,
			   is_auto_close_on,
			   is_auto_shrink_on,
			   state,
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
			   is_mixed_page_allocation_on)
			   ) AS upvt
	)
	INSERT INTO dbo.DBOptionsHistory(DatabaseID,Setting,OldValue,NewValue,ChangeDate)
	SELECT o.DatabaseID,o.Setting,o.value,n.value,@SnapshotDate AS ChangeDate 
	FROM OldUnPvt o
	JOIN NewUnpvt n ON o.DatabaseID = n.DatabaseID AND o.Setting = n.Setting
	WHERE NOT EXISTS(SELECT o.value 
					INTERSECT
					SELECT n.Value)
	AND NOT (o.Setting<>'is_read_only' AND n.is_in_standby=1 AND n.Value=1)
	AND NOT (o.Setting<>'is_read_only' AND o.is_in_standby=1 AND o.Value=1)
	AND NOT (o.Setting='state' AND n.value IN(0,1,2,3) AND o.value IN(0,1,2,3));

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

	COMMIT

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
									 @Reference = @Ref,
									 @SnapshotDate = @SnapshotDate
END