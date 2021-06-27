CREATE PROC dbo.DatabaseQueryStoreOptions_Get(
	@Instance NVARCHAR(128),
	@DatabaseID INT=NULL
)
AS
SELECT D.DatabaseID,
	D.name,
	QS.desired_state_desc,
	QS.actual_state_desc,
	QS.readonly_reason_desc,
	CASE WHEN QS.actual_state = 1 AND QS.readonly_reason NOT IN(0,1,8) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END IsReadOnlyErrorState,
	QS.query_capture_mode_desc,
	QS.size_based_cleanup_mode_desc,
	QS.wait_stats_capture_mode_desc,
	QS.stale_query_threshold_days,
	QS.max_plans_per_query,
	QS.interval_length_minutes,
	QS.current_storage_size_mb,
	QS.max_storage_size_mb,
	QS.actual_state_additional_info,
	QS.flush_interval_seconds,
	QS.capture_policy_execution_count,
	QS.capture_policy_stale_threshold_hours,
	QS.capture_policy_total_compile_cpu_time_ms,
	QS.capture_policy_total_execution_cpu_time_ms,
	CDS.SnapshotAge,
	CDS.SnapshotDate,
	ISNULL(CDS.Status,3) AS CollectionDateStatus
FROM dbo.DatabaseQueryStoreOptions QS
JOIN dbo.Databases D ON QS.DatabaseID = D.DatabaseID
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
LEFT JOIN dbo.CollectionDatesStatus CDS ON CDS.InstanceID = I.InstanceID AND CDS.Reference = 'DatabaseQueryStoreOptions'
WHERE I.Instance = @Instance
AND (D.DatabaseID= @DatabaseID OR @DatabaseID IS NULL)