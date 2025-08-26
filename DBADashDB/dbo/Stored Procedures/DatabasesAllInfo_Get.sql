CREATE PROC dbo.DatabasesAllInfo_Get(
		@InstanceIDs VARCHAR(MAX)=NULL,
		@DatabaseID INT=NULL,
        @ShowHidden BIT=1,
        @InstanceGroupName NVARCHAR(128)=NULL
)
AS
DECLARE @SQL NVARCHAR(MAX) 
SET @SQL = N'
SELECT  I.InstanceGroupName AS Instance,
		D.DatabaseID,
        D.InstanceID,
        D.name AS DatabaseName,
        D.database_id,
        D.source_database_id,
        CONVERT(VARCHAR(MAX),D.owner_sid,1) owner_sid,
        D.create_date,
        D.compatibility_level,
        I.MaxSupportedCompatibilityLevel,
        D.collation_name,
        D.user_access,
        D.user_access_desc,
        D.is_read_only,
        D.is_auto_close_on,
        D.is_auto_shrink_on,
        D.state,
        D.state_desc,
        D.is_in_standby,
        D.is_cleanly_shutdown,
        D.is_supplemental_logging_enabled,
        D.snapshot_isolation_state,
        D.is_read_committed_snapshot_on,
        D.recovery_model,
        D.page_verify_option,
		CASE WHEN D.page_verify_option=0 THEN ''NONE'' WHEN D.page_verify_option=1 THEN ''TORN_PAGE_DETECTION'' WHEN D.page_verify_option=2 THEN ''CHECKSUM'' ELSE NULL END as page_verify_option_desc,
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
        D.is_mixed_page_allocation_on,
        D.is_ledger_on, 
        D.LastGoodCheckDbTime,
        LG.status as LastGoodCheckDBStatus,
		D.VLFCount,
        D.catalog_collation_type,
        D.is_accelerated_database_recovery_on,
        D.is_change_feed_enabled,
        D.is_event_stream_enabled,
        D.is_memory_optimized_enabled,
        D.is_temporal_history_retention_enabled,
        D.is_optimized_locking_on
FROM dbo.Databases D
JOIN dbo.InstanceInfo I ON I.InstanceID = D.InstanceID
JOIN dbo.LastGoodCheckDB LG ON I.InstanceID = LG.InstanceID AND D.DatabaseID = LG.DatabaseID
WHERE 1=1
AND D.IsActive=1
AND I.IsActive=1
' + CASE WHEN @InstanceGroupName IS NULL THEN '' ELSE 'AND I.InstanceGroupName = @InstanceGroupName' END + '
' + CASE WHEN @InstanceIDs IS NULL THEN '' ELSE ' AND EXISTS(SELECT 1 
			FROM STRING_SPLIT(@InstanceIDs,'','') ss 
			WHERE ss.value = D.InstanceID
			)
' END + '
' + CASE WHEN @DatabaseID IS NULL THEN '' ELSE 'AND D.DatabaseID = @DatabaseID' END + '
' + CASE WHEN @ShowHidden=1 THEN '' ELSE 'AND I.ShowInSummary=1' END

EXEC sp_executesql @SQL,N'@InstanceIDs VARCHAR(MAX),@DatabaseID INT,@InstanceGroupName NVARCHAR(128)',@InstanceIDs,@DatabaseID,@InstanceGroupName