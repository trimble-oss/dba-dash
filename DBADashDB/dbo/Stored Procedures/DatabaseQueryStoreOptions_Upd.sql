CREATE PROCEDURE dbo.DatabaseQueryStoreOptions_Upd
	@DatabaseQueryStoreOptions dbo.DatabaseQueryStoreOptions READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
AS
BEGIN TRAN
DELETE QSO 
FROM dbo.DatabaseQueryStoreOptions QSO
WHERE EXISTS(SELECT 1 
			FROM dbo.Databases D 
			WHERE D.DatabaseID = QSO.DatabaseID 
			AND D.InstanceID = @InstanceID)

 INSERT INTO dbo.DatabaseQueryStoreOptions(
    DatabaseID,
    desired_state,
    actual_state,
    readonly_reason,
    current_storage_size_mb,
    flush_interval_seconds,
    interval_length_minutes,
    max_storage_size_mb,
    stale_query_threshold_days,
    max_plans_per_query,
    query_capture_mode,
    size_based_cleanup_mode,
    actual_state_additional_info,
    wait_stats_capture_mode,
    capture_policy_execution_count,
    capture_policy_total_compile_cpu_time_ms,
    capture_policy_total_execution_cpu_time_ms,
    capture_policy_stale_threshold_hours
)
SELECT DatabaseID,
       desired_state,
       actual_state,
       readonly_reason,
       current_storage_size_mb,
       flush_interval_seconds,
       interval_length_minutes,
       max_storage_size_mb,
       stale_query_threshold_days,
       max_plans_per_query,
       query_capture_mode,
       size_based_cleanup_mode,
       actual_state_additional_info,
       wait_stats_capture_mode,
       capture_policy_execution_count,
       capture_policy_total_compile_cpu_time_ms,
       capture_policy_total_execution_cpu_time_ms,
       capture_policy_stale_threshold_hours
FROM @DatabaseQueryStoreOptions t 
JOIN dbo.Databases D ON t.database_id = D.database_id
WHERE D.InstanceID = @InstanceID

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID, 
                             @Reference = 'DatabaseQueryStoreOptions', 
                             @SnapshotDate = @SnapshotDate

COMMIT