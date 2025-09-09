CREATE PROC dbo.Databases_Upd(
	@Databases dbo.Databases READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
DECLARE @Ref VARCHAR(30)='Databases'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN

	DECLARE @Created INT = 0
	DECLARE @Dropped INT = 0
	DECLARE @MetricsInstanceID INT 
	SELECT @MetricsInstanceID = CASE WHEN EXISTS(SELECT 1 FROM dbo.RepositoryMetricsConfig WHERE InstanceID = @InstanceID AND MetricType='Databases') THEN @InstanceID ELSE -1 END

	CREATE TABLE #History(
		DatabaseID INT NOT NULL,
		Setting sysname NOT NULL,
		OldValue SQL_VARIANT NULL,
		NewValue SQL_VARIANT NULL,
		ChangeDate DATETIME2(2) NOT NULL
	);

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
					CAST(IsActive AS SQL_VARIANT) AS IsActive,
					CAST(is_ledger_on AS SQL_VARIANT) AS is_ledger_on,
					CAST(catalog_collation_type AS SQL_VARIANT) AS catalog_collation_type,
					CAST(is_accelerated_database_recovery_on AS SQL_VARIANT) AS is_accelerated_database_recovery_on,
					CAST(is_change_feed_enabled AS SQL_VARIANT) AS is_change_feed_enabled,
					CAST(is_event_stream_enabled AS SQL_VARIANT) AS is_event_stream_enabled,
					CAST(is_memory_optimized_enabled AS SQL_VARIANT) AS is_memory_optimized_enabled,
					CAST(is_temporal_history_retention_enabled AS SQL_VARIANT) AS is_temporal_history_retention_enabled,
					CAST(is_optimized_locking_on AS SQL_VARIANT) AS is_optimized_locking_on
			FROM dbo.Databases
			WHERE InstanceID  = @InstanceID
	),
	NewDB AS (SELECT id.DatabaseID,
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
					CAST(IsActive AS SQL_VARIANT) AS IsActive,
					CAST(is_ledger_on AS SQL_VARIANT) AS is_ledger_on,
					CAST(catalog_collation_type AS SQL_VARIANT) AS catalog_collation_type,
					CAST(is_accelerated_database_recovery_on AS SQL_VARIANT) AS is_accelerated_database_recovery_on,
					CAST(is_change_feed_enabled AS SQL_VARIANT) AS is_change_feed_enabled,
					CAST(is_event_stream_enabled AS SQL_VARIANT) AS is_event_stream_enabled,
					CAST(is_memory_optimized_enabled AS SQL_VARIANT) AS is_memory_optimized_enabled,
					CAST(is_temporal_history_retention_enabled AS SQL_VARIANT) AS is_temporal_history_retention_enabled,
					CAST(is_optimized_locking_on AS SQL_VARIANT) AS is_optimized_locking_on
		FROM @Databases t
		CROSS APPLY(SELECT DatabaseID,D.IsActive 
					FROM dbo.Databases D 
					WHERE D.name=T.name
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
			   is_mixed_page_allocation_on,
			   is_ledger_on,
			   catalog_collation_type,
			   is_accelerated_database_recovery_on,
			   is_change_feed_enabled,
			   is_event_stream_enabled,
			   is_memory_optimized_enabled,
			   is_temporal_history_retention_enabled,
			   is_optimized_locking_on)
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
			   is_mixed_page_allocation_on,
			   is_ledger_on,
			   catalog_collation_type,
			   is_accelerated_database_recovery_on,
			   is_change_feed_enabled,
			   is_event_stream_enabled,
			   is_memory_optimized_enabled,
			   is_temporal_history_retention_enabled,
			   is_optimized_locking_on)
			   ) AS upvt
	)
	INSERT INTO #History(DatabaseID,Setting,OldValue,NewValue,ChangeDate)
	SELECT o.DatabaseID,o.Setting,o.value,n.value,@SnapshotDate AS ChangeDate 
	FROM OldUnPvt o
	JOIN NewUnpvt n ON o.DatabaseID = n.DatabaseID AND o.Setting = n.Setting
	WHERE NOT EXISTS(SELECT o.value 
					INTERSECT
					SELECT n.Value)
	AND NOT (o.Setting<>'is_read_only' AND n.is_in_standby=1 AND n.Value=1)
	AND NOT (o.Setting<>'is_read_only' AND o.is_in_standby=1 AND o.Value=1)
	AND NOT (o.Setting='state' AND n.value IN(0,1,2,3) AND o.value IN(0,1,2,3));

	BEGIN TRAN;

	/*	Handle database rename 
		Update name where database_id and service_broker_guid match, but name is different and new name doesn't already exist
	*/
	UPDATE D 
		SET D.name = T.name,
		D.UpdatedDate = @SnapshotDate
	OUTPUT INSERTED.DatabaseID, 'name',DELETED.name,INSERTED.name,@SnapshotDate INTO #History(DatabaseID,Setting,OldValue,NewValue,ChangeDate)
	FROM dbo.Databases D 
	JOIN @Databases T ON D.database_id = T.database_id 
			AND D.service_broker_guid = T.service_broker_guid /* Provides some confidence it's the same database.  Note: create_date is updated on rename */
	WHERE D.InstanceID = @InstanceID 
	AND D.name <> T.name
    AND NOT EXISTS(	SELECT 1 
					FROM dbo.Databases D2 
					WHERE D2.InstanceID = @InstanceID 
					AND D2.name = T.name
					);

	UPDATE D 
		SET D.IsActive = 0,
		D.UpdatedDate = @SnapshotDate
	OUTPUT INSERTED.DatabaseID, 'IsActive',DELETED.IsActive,INSERTED.IsActive,@SnapshotDate INTO #History(DatabaseID,Setting,OldValue,NewValue,ChangeDate)
	FROM dbo.Databases D 
	WHERE InstanceID = @InstanceID
	AND NOT EXISTS(SELECT 1 
			FROM @Databases T 
			WHERE D.name = T.name
			)
	AND D.IsActive=1;
	SET @Dropped = @@ROWCOUNT

	UPDATE D SET [database_id] = T.database_id
		  ,[source_database_id] = T.source_database_id
		  ,[owner_sid] = T.owner_sid
		  ,[create_date] = T.create_date
		  ,[compatibility_level] = T.[compatibility_level]
		  ,[collation_name] = T.collation_name
		  ,[user_access] = T.user_access
		  ,[is_read_only] = T.is_read_only
		  ,[is_auto_close_on] = T.is_auto_close_on 
		  ,[is_auto_shrink_on] = T.is_auto_shrink_on
		  ,[state] = T.state
		  ,[is_in_standby] = T.is_in_standby
		  ,[is_cleanly_shutdown] = T.is_cleanly_shutdown
		  ,[is_supplemental_logging_enabled] = T.is_supplemental_logging_enabled
		  ,[snapshot_isolation_state] = T.snapshot_isolation_state
		  ,[is_read_committed_snapshot_on] = T.is_read_committed_snapshot_on
		  ,[recovery_model] = T.recovery_model
		  ,[page_verify_option] = T.page_verify_option
		  ,[is_auto_create_stats_on] = T.is_auto_create_stats_on
		  ,[is_auto_create_stats_incremental_on] = T.is_auto_create_stats_incremental_on
		  ,[is_auto_update_stats_on] = T.is_auto_update_stats_on
		  ,[is_auto_update_stats_async_on] = T.is_auto_update_stats_async_on
		  ,[is_ansi_null_default_on] = T.is_ansi_null_default_on
		  ,[is_ansi_nulls_on] = T.is_ansi_nulls_on
		  ,[is_ansi_padding_on] = T.is_ansi_padding_on
		  ,[is_ansi_warnings_on] = T.is_ansi_warnings_on
		  ,[is_arithabort_on] = T.is_arithabort_on
		  ,[is_concat_null_yields_null_on] = T.is_concat_null_yields_null_on
		  ,[is_numeric_roundabort_on] = T.is_numeric_roundabort_on
		  ,[is_quoted_identifier_on] = T.is_quoted_identifier_on
		  ,[is_recursive_triggers_on] = T.is_recursive_triggers_on
		  ,[is_cursor_close_on_commit_on] = T.is_cursor_close_on_commit_on
		  ,[is_local_cursor_default] = T.is_local_cursor_default
		  ,[is_fulltext_enabled] = T.is_fulltext_enabled
		  ,[is_trustworthy_on] = T.is_trustworthy_on
		  ,[is_db_chaining_on] = T.is_db_chaining_on
		  ,[is_parameterization_forced] = T.is_parameterization_forced
		  ,[is_master_key_encrypted_by_server] = T.is_master_key_encrypted_by_server
		  ,[is_query_store_on] = T.is_query_store_on
		  ,[is_published] = T.is_published
		  ,[is_subscribed] = T.is_subscribed
		  ,[is_merge_published] = T.is_merge_published
		  ,[is_distributor] = T.is_distributor
		  ,[is_sync_with_backup] = T.is_sync_with_backup
		  ,[is_broker_enabled] = T.is_broker_enabled
		  ,[log_reuse_wait] = T.log_reuse_wait
		  ,[is_date_correlation_on] = T.is_date_correlation_on
		  ,[is_cdc_enabled] = T.is_cdc_enabled
		  ,[is_encrypted] = T.is_encrypted
		  ,[is_honor_broker_priority_on] = T.is_honor_broker_priority_on
		  ,[replica_id] = T.replica_id
		  ,[group_database_id] = T.group_database_id
		  ,[resource_pool_id] = T.resource_pool_id
		  ,[default_language_lcid] = T.default_language_lcid
		  ,[default_language_name] = T.default_language_name
		  ,[default_fulltext_language_lcid] = T.default_fulltext_language_lcid
		  ,[default_fulltext_language_name] = T.default_fulltext_language_name
		  ,[is_nested_triggers_on] = T.is_nested_triggers_on
		  ,[is_transform_noise_words_on] = T.is_transform_noise_words_on
		  ,[two_digit_year_cutoff] = T.two_digit_year_cutoff
		  ,[containment] = T.containment
		  ,[target_recovery_time_in_seconds] = T.target_recovery_time_in_seconds
		  ,[delayed_durability] = T.delayed_durability
		  ,[is_memory_optimized_elevate_to_snapshot_on] = T.is_memory_optimized_elevate_to_snapshot_on
		  ,[is_federation_member] = T.is_federation_member
		  ,[is_remote_data_archive_enabled] = T.is_remote_data_archive_enabled
		  ,[is_mixed_page_allocation_on] = T.is_mixed_page_allocation_on
		  ,is_ledger_on = T.is_ledger_on
		  ,catalog_collation_type = T.catalog_collation_type
		  ,is_accelerated_database_recovery_on=T.is_accelerated_database_recovery_on
		  ,is_change_feed_enabled=T.is_change_feed_enabled
		  ,is_event_stream_enabled=T.is_event_stream_enabled
		  ,is_memory_optimized_enabled=T.is_memory_optimized_enabled
		  ,is_temporal_history_retention_enabled=T.is_temporal_history_retention_enabled
		  ,is_optimized_locking_on = T.is_optimized_locking_on
		  ,service_broker_guid = T.service_broker_guid
		  ,UpdatedDate = @SnapshotDate
	FROM dbo.Databases D
	JOIN @Databases T ON D.name = T.name
	WHERE D.InstanceID = @InstanceID
	AND EXISTS(SELECT 	   T.database_id,
						   T.source_database_id,
						   T.owner_sid,
						   T.create_date,
						   T.compatibility_level,
						   T.collation_name,
						   T.user_access,
						   T.is_read_only,
						   T.is_auto_close_on,
						   T.is_auto_shrink_on,
						   T.state,
						   T.is_in_standby,
						   T.is_cleanly_shutdown,
						   T.is_supplemental_logging_enabled,
						   T.snapshot_isolation_state,
						   T.is_read_committed_snapshot_on,
						   T.recovery_model,
						   T.page_verify_option,
						   T.is_auto_create_stats_on,
						   T.is_auto_create_stats_incremental_on,
						   T.is_auto_update_stats_on,
						   T.is_auto_update_stats_async_on,
						   T.is_ansi_null_default_on,
						   T.is_ansi_nulls_on,
						   T.is_ansi_padding_on,
						   T.is_ansi_warnings_on,
						   T.is_arithabort_on,
						   T.is_concat_null_yields_null_on,
						   T.is_numeric_roundabort_on,
						   T.is_quoted_identifier_on,
						   T.is_recursive_triggers_on,
						   T.is_cursor_close_on_commit_on,
						   T.is_local_cursor_default,
						   T.is_fulltext_enabled,
						   T.is_trustworthy_on,
						   T.is_db_chaining_on,
						   T.is_parameterization_forced,
						   T.is_master_key_encrypted_by_server,
						   T.is_query_store_on,
						   T.is_published,
						   T.is_subscribed,
						   T.is_merge_published,
						   T.is_distributor,
						   T.is_sync_with_backup,
						   T.is_broker_enabled,
						   T.log_reuse_wait,
						   T.is_date_correlation_on,
						   T.is_cdc_enabled,
						   T.is_encrypted,
						   T.is_honor_broker_priority_on,
						   T.replica_id,
						   T.group_database_id,
						   T.resource_pool_id,
						   T.default_language_lcid,
						   T.default_language_name,
						   T.default_fulltext_language_lcid,
						   T.default_fulltext_language_name,
						   T.is_nested_triggers_on,
						   T.is_transform_noise_words_on,
						   T.two_digit_year_cutoff,
						   T.containment,
						   T.target_recovery_time_in_seconds,
						   T.delayed_durability,
						   T.is_memory_optimized_elevate_to_snapshot_on,
						   T.is_federation_member,
						   T.is_remote_data_archive_enabled,
						   T.is_mixed_page_allocation_on ,
						   1, -- IsActive
						   T.is_ledger_on,
						   T.catalog_collation_type,
						   T.is_accelerated_database_recovery_on,
						   T.is_change_feed_enabled,
						   T.is_event_stream_enabled,
						   T.is_memory_optimized_enabled,
						   T.is_temporal_history_retention_enabled,
						   T.is_optimized_locking_on,
						   T.service_broker_guid
			EXCEPT
			SELECT 	   D.database_id,
						   D.source_database_id,
						   D.owner_sid,
						   D.create_date,
						   D.compatibility_level,
						   D.collation_name,
						   D.user_access,
						   D.is_read_only,
						   D.is_auto_close_on,
						   D.is_auto_shrink_on,
						   D.state,
						   D.is_in_standby,
						   D.is_cleanly_shutdown,
						   D.is_supplemental_logging_enabled,
						   D.snapshot_isolation_state,
						   D.is_read_committed_snapshot_on,
						   D.recovery_model,
						   D.page_verify_option,
						   D.is_auto_create_stats_on,
						   D.is_auto_create_stats_incremental_on,
						   D.is_auto_update_stats_on,
						   D.is_auto_update_stats_async_on,
						   D.is_ansi_null_default_on,
						   D.is_ansi_nulls_on,
						   D.is_ansi_padding_on,
						   D.is_ansi_warnings_on,
						   D.is_arithabort_on,
						   D.is_concat_null_yields_null_on,
						   D.is_numeric_roundabort_on,
						   D.is_quoted_identifier_on,
						   D.is_recursive_triggers_on,
						   D.is_cursor_close_on_commit_on,
						   D.is_local_cursor_default,
						   D.is_fulltext_enabled,
						   D.is_trustworthy_on,
						   D.is_db_chaining_on,
						   D.is_parameterization_forced,
						   D.is_master_key_encrypted_by_server,
						   D.is_query_store_on,
						   D.is_published,
						   D.is_subscribed,
						   D.is_merge_published,
						   D.is_distributor,
						   D.is_sync_with_backup,
						   D.is_broker_enabled,
						   D.log_reuse_wait,
						   D.is_date_correlation_on,
						   D.is_cdc_enabled,
						   D.is_encrypted,
						   D.is_honor_broker_priority_on,
						   D.replica_id,
						   D.group_database_id,
						   D.resource_pool_id,
						   D.default_language_lcid,
						   D.default_language_name,
						   D.default_fulltext_language_lcid,
						   D.default_fulltext_language_name,
						   D.is_nested_triggers_on,
						   D.is_transform_noise_words_on,
						   D.two_digit_year_cutoff,
						   D.containment,
						   D.target_recovery_time_in_seconds,
						   D.delayed_durability,
						   D.is_memory_optimized_elevate_to_snapshot_on,
						   D.is_federation_member,
						   D.is_remote_data_archive_enabled,
						   D.is_mixed_page_allocation_on ,
						   D.IsActive,
						   D.is_ledger_on,
						   D.catalog_collation_type,
						   D.is_accelerated_database_recovery_on,
						   D.is_change_feed_enabled,
						   D.is_event_stream_enabled,
						   D.is_memory_optimized_enabled,
						   D.is_temporal_history_retention_enabled,
						   D.is_optimized_locking_on,
						   D.service_broker_guid
						   )

	UPDATE D 
		SET D.IsActive = 1,
		D.UpdatedDate = SYSUTCDATETIME()
	OUTPUT INSERTED.DatabaseID, 'IsActive',DELETED.IsActive,INSERTED.IsActive,@SnapshotDate INTO #History(DatabaseID,Setting,OldValue,NewValue,ChangeDate)
	FROM dbo.Databases D
	JOIN @Databases T ON D.name = T.name
	WHERE D.InstanceID = @InstanceID
	AND D.IsActive=0;
	SET @Created = @@ROWCOUNT

	INSERT INTO dbo.Databases (
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
		   IsActive,
		   is_ledger_on,
		   catalog_collation_type,
		   is_accelerated_database_recovery_on,
		   is_change_feed_enabled,
		   is_event_stream_enabled,
		   is_memory_optimized_enabled,
		   is_temporal_history_retention_enabled,
		   is_optimized_locking_on,
		   service_broker_guid
	)
	SELECT  @InstanceID,
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
		   CAST(1 as BIT),
		   is_ledger_on,
		   catalog_collation_type,
		   is_accelerated_database_recovery_on,
		   is_change_feed_enabled,
		   is_event_stream_enabled,
		   is_memory_optimized_enabled,
		   is_temporal_history_retention_enabled,
		   is_optimized_locking_on,
		   service_broker_guid
	FROM @Databases T
	WHERE NOT EXISTS(SELECT 1 
					FROM dbo.Databases D 
					WHERE D.InstanceID = @InstanceID 
					AND D.name = T.name
					)

	SET @Created += @@ROWCOUNT

	INSERT INTO dbo.DBOptionsHistory(DatabaseID,Setting,OldValue,NewValue,ChangeDate)
	SELECT DatabaseID,Setting,OldValue,NewValue,ChangeDate
	FROM #History

	IF EXISTS(
		SELECT 1 
		FROM dbo.RepositoryMetricsConfig 
		WHERE InstanceID = @MetricsInstanceID
		AND MetricType='Databases'
		AND IsEnabled=1
	)
	-- Exclude Azure DB
	AND NOT EXISTS(
		SELECT 1 
		FROM dbo.Instances 
		WHERE EngineEdition=5 
		AND InstanceID = @InstanceID
	)
	BEGIN
		/* Log a metric for user database count */

		DECLARE @UserDatabases INT
		SELECT @UserDatabases = COUNT(*) 
		FROM @Databases
		WHERE database_id > 4;

		DECLARE @PC dbo.PerformanceCounters;

		WITH Metrics AS (
			SELECT @SnapshotDate AS SnapshotDate,'sys.databases' AS object_name,'Count of User Databases' AS counter_name,'' AS instance_name,@UserDatabases AS cntr_value,65792 AS cntr_type
			UNION ALL
			SELECT @SnapshotDate,'sys.databases','Databases Created','',@Created,65792
			UNION ALL
			SELECT @SnapshotDate,'sys.databases','Databases Dropped','',@Dropped,65792
		)
		INSERT INTO @PC(SnapshotDate,object_name,counter_name,instance_name,cntr_value,cntr_type)
		SELECT SnapshotDate,object_name,counter_name,instance_name,cntr_value,cntr_type
		FROM Metrics 
		WHERE EXISTS(
				SELECT 1 
				FROM dbo.RepositoryMetricsConfig 
				WHERE InstanceID = @MetricsInstanceID 
				AND MetricType='Databases'
				AND MetricName =Metrics.counter_name
				AND IsEnabled=1
				)

		EXEC dbo.PerformanceCounters_Upd @PerformanceCounters=@PC,
										@InstanceID = @InstanceID,
										@SnapshotDate=@SnapshotDate,
										@Internal=1 /* Don't clear staging table used for other metric types */

	END

	COMMIT

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
									 @Reference = @Ref,
									 @SnapshotDate = @SnapshotDate
END