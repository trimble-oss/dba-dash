CREATE PROC dbo.ResourceGovernorWorkloadGroups_Upd(
	@ResourceGovernorWorkloadGroups dbo.ResourceGovernorWorkloadGroups READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
/* 
	Note: matching is done on name rather than group_id. The group_id could change if the workload group is dropped and recreated or if the instance associated with the ConnectionID changes (migrations, failover etc).
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Ref VARCHAR(30)='ResourceGovernorWorkloadGroups'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=CAST(@SnapshotDate AS DATETIME2(2)) AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	/* Insert new records into the Metrics table based on the difference between the current snapshot and the previous snapshot.  */
	INSERT INTO dbo.ResourceGovernorWorkloadGroupsMetrics(
			InstanceID,
			WorkloadGroupID,
			SnapshotDate,
			PeriodTimeMs,
			diff_request_count,
			diff_queued_request_count,
			diff_cpu_limit_violation_count,
			diff_cpu_usage_ms,
			diff_lock_wait_count,
			diff_lock_wait_time_ms,
			diff_query_optimization_count,
			diff_suboptimal_plan_generation_count,
			diff_reduced_memgrant_count,
			diff_cpu_usage_preemptive_ms,
			diff_tempdb_data_limit_violation_count,
			active_request_count,
			queued_request_count,
			blocked_task_count,
			active_parallel_thread_count,
			tempdb_data_space_kb,
			cpu_count
		)
	SELECT	@InstanceID,
			WG.WorkloadGroupID,
			T.SnapshotDate,
			DATEDIFF_BIG(ms,WG.SnapshotDate,T.SnapshotDate) AS PeriodTimeMs,
			T.total_request_count-WG.total_request_count AS diff_request_count,
			T.total_queued_request_count - WG.total_queued_request_count AS diff_queued_request_count,
			T.total_cpu_limit_violation_count - WG.total_cpu_limit_violation_count AS diff_cpu_limit_violation_count,
			T.total_cpu_usage_ms - WG.total_cpu_usage_ms AS diff_cpu_usage_ms,
			T.total_lock_wait_count - WG.total_lock_wait_count AS diff_lock_wait_count,
			T.total_lock_wait_time_ms - WG.total_lock_wait_time_ms AS diff_lock_wait_time_ms,
			T.total_query_optimization_count - WG.total_query_optimization_count AS diff_query_optimization_count,
			T.total_suboptimal_plan_generation_count - WG.total_suboptimal_plan_generation_count AS diff_suboptimal_plan_generation_count,
			T.total_reduced_memgrant_count - WG.total_reduced_memgrant_count AS diff_reduced_memgrant_count,
			T.total_cpu_usage_preemptive_ms - WG.total_cpu_usage_preemptive_ms AS diff_cpu_usage_preemptive_ms,
			T.total_tempdb_data_limit_violation_count - WG.total_tempdb_data_limit_violation_count AS diff_tempdb_data_limit_violation_count,
			T.active_request_count,
			T.queued_request_count,
			T.blocked_task_count,
			T.active_parallel_thread_count,
			T.tempdb_data_space_kb,
			I.cpu_count
	FROM dbo.ResourceGovernorWorkloadGroups WG
	JOIN dbo.Instances I ON WG.InstanceID = I.InstanceID
	JOIN @ResourceGovernorWorkloadGroups T ON WG.name = T.name 
												AND T.statistics_start_time = WG.statistics_start_time 
	WHERE DATEDIFF_BIG(ms,WG.SnapshotDate,T.SnapshotDate) BETWEEN 0 AND 14400000 /* 4 hours in ms.  Skip recording if the gap between collections is too large. */
	AND I.InstanceID = @InstanceID
	
	/* Update the current snapshot in the main table.  */
	UPDATE WG
	SET		WG.SnapshotDate = T.SnapshotDate,
			WG.group_id = T.group_id,
			WG.pool_id = T.pool_id,
			WG.pool_name = T.pool_name,
			WG.external_pool_id = T.external_pool_id,
			WG.statistics_start_time = T.statistics_start_time,
			WG.total_request_count = T.total_request_count,
			WG.total_queued_request_count = T.total_queued_request_count,
			WG.active_request_count = T.active_request_count,
			WG.queued_request_count = T.queued_request_count,
			WG.total_cpu_limit_violation_count = T.total_cpu_limit_violation_count,
			WG.total_cpu_usage_ms = T.total_cpu_usage_ms,
			WG.max_request_cpu_time_ms = T.max_request_cpu_time_ms,
			WG.blocked_task_count = T.blocked_task_count,
			WG.total_lock_wait_count = T.total_lock_wait_count,
			WG.total_lock_wait_time_ms = T.total_lock_wait_time_ms,
			WG.total_query_optimization_count = T.total_query_optimization_count,
			WG.total_suboptimal_plan_generation_count = T.total_suboptimal_plan_generation_count,
			WG.total_reduced_memgrant_count = T.total_reduced_memgrant_count,
			WG.max_request_grant_memory_kb = T.max_request_grant_memory_kb,
			WG.active_parallel_thread_count = T.active_parallel_thread_count,
			WG.importance = T.importance,
			WG.request_max_memory_grant_percent = T.request_max_memory_grant_percent,
			WG.request_max_cpu_time_sec = T.request_max_cpu_time_sec,
			WG.request_memory_grant_timeout_sec = T.request_memory_grant_timeout_sec,
			WG.group_max_requests = T.group_max_requests,
			WG.max_dop = T.max_dop,
			WG.effective_max_dop = T.effective_max_dop,
			WG.total_cpu_usage_preemptive_ms = T.total_cpu_usage_preemptive_ms,
			WG.request_max_memory_grant_percent_numeric = T.request_max_memory_grant_percent_numeric,
			WG.tempdb_data_space_kb = T.tempdb_data_space_kb,
			WG.peak_tempdb_data_space_kb = T.peak_tempdb_data_space_kb,
			WG.total_tempdb_data_limit_violation_count = T.total_tempdb_data_limit_violation_count
	FROM dbo.ResourceGovernorWorkloadGroups WG
	JOIN @ResourceGovernorWorkloadGroups T ON WG.name = T.name AND WG.InstanceID = @InstanceID

	/* Insert new records for any workload groups that don't have a match in the main table.  */
	INSERT INTO dbo.ResourceGovernorWorkloadGroups(
				InstanceID,
				SnapshotDate,
				group_id,
				name,
				pool_id,
				pool_name,
				external_pool_id,
				statistics_start_time,
				total_request_count,
				total_queued_request_count,
				active_request_count,
				queued_request_count,
				total_cpu_limit_violation_count,
				total_cpu_usage_ms,
				max_request_cpu_time_ms,
				blocked_task_count,
				total_lock_wait_count,
				total_lock_wait_time_ms,
				total_query_optimization_count,
				total_suboptimal_plan_generation_count,
				total_reduced_memgrant_count,
				max_request_grant_memory_kb,
				active_parallel_thread_count,
				importance,
				request_max_memory_grant_percent,
				request_max_cpu_time_sec,
				request_memory_grant_timeout_sec,
				group_max_requests,
				max_dop,
				effective_max_dop,
				total_cpu_usage_preemptive_ms,
				request_max_memory_grant_percent_numeric,
				tempdb_data_space_kb,
				peak_tempdb_data_space_kb,
				total_tempdb_data_limit_violation_count
		)
		SELECT 	@InstanceID,
				T.SnapshotDate,
				T.group_id,
				T.name,
				T.pool_id,
				T.pool_name,
				T.external_pool_id,
				T.statistics_start_time,
				T.total_request_count,
				T.total_queued_request_count,
				T.active_request_count,
				T.queued_request_count,
				T.total_cpu_limit_violation_count,
				T.total_cpu_usage_ms,
				T.max_request_cpu_time_ms,
				T.blocked_task_count,
				T.total_lock_wait_count,
				T.total_lock_wait_time_ms,
				T.total_query_optimization_count,
				T.total_suboptimal_plan_generation_count,
				T.total_reduced_memgrant_count,
				T.max_request_grant_memory_kb,
				T.active_parallel_thread_count,
				T.importance,
				T.request_max_memory_grant_percent,
				T.request_max_cpu_time_sec,
				T.request_memory_grant_timeout_sec,
				T.group_max_requests,
				T.max_dop,
				T.effective_max_dop,
				T.total_cpu_usage_preemptive_ms,
				T.request_max_memory_grant_percent_numeric,
				T.tempdb_data_space_kb,
				T.peak_tempdb_data_space_kb,
				T.total_tempdb_data_limit_violation_count
		FROM @ResourceGovernorWorkloadGroups T
		WHERE NOT EXISTS(	SELECT 1 
							FROM dbo.ResourceGovernorWorkloadGroups WG 
							WHERE WG.name = T.name 
							AND WG.InstanceID = @InstanceID
							);


	
	
	EXEC dbo.CollectionDates_Upd	@InstanceID = @InstanceID,  
									@Reference = @Ref,
									@SnapshotDate = @SnapshotDate;

END