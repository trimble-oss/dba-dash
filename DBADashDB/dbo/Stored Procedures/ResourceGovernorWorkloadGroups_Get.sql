CREATE PROC dbo.ResourceGovernorWorkloadGroups_Get(
	@InstanceID INT,
    @FromDate DATETIME2(3)=NULL, /* UTC */
	@ToDate DATETIME2(3)=NULL /* UTC */
)
AS
WITH Metrics AS (
    SELECT  M.WorkloadGroupID,
		    SUM(M.diff_cpu_usage_ms*1.0) AS period_cpu_usage_ms,
		    SUM(M.diff_cpu_usage_ms*1.0)/SUM(M.PeriodTimeMs) AS period_cpu_cores,
		    SUM(M.diff_cpu_usage_ms*1.0/M.cpu_count)/SUM(M.PeriodTimeMs) AS period_cpu_percent,
            SUM(M.diff_cpu_usage_ms*1.0)/SUM(SUM(M.diff_cpu_usage_ms)) OVER() AS period_cpu_share_percent,
		    SUM(M.diff_request_count)/SUM(M.PeriodTimeMs/60000.0) AS period_requests_per_min,
		    SUM(M.diff_queued_request_count)/SUM(M.PeriodTimeMs/60000.0) AS period_queued_request_count_per_min,
		    SUM(M.diff_cpu_limit_violation_count)/SUM(M.PeriodTimeMs/60000.0) AS period_cpu_limit_violations_per_min,
		    SUM(M.diff_lock_wait_count)/SUM(M.PeriodTimeMs/60000.0) AS period_lock_waits_per_min,
		    SUM(M.diff_lock_wait_time_ms)/SUM(M.PeriodTimeMs/1000.0) AS period_lock_wait_time_ms_per_sec,
		    SUM(M.diff_query_optimization_count)/SUM(M.PeriodTimeMs/60000.0) AS period_query_optimizations_per_min,
		    SUM(M.diff_suboptimal_plan_generation_count)/SUM(M.PeriodTimeMs/60000.0)  AS period_suboptimal_plan_generation_count_per_min,
		    SUM(M.diff_reduced_memgrant_count) /SUM(M.PeriodTimeMs/60000.0) AS period_reduced_memgrant_count_per_min,
		    SUM(M.diff_cpu_usage_preemptive_ms)/SUM(M.PeriodTimeMs/60000.0) AS period_cpu_usage_preemptive_ms_per_min,
		    SUM(M.diff_tempdb_data_limit_violation_count) /SUM(M.PeriodTimeMs/60000.0) AS period_tempdb_data_limit_violations_per_min,
		    AVG(M.active_request_count) AS period_avg_active_request_count,
		    AVG(M.queued_request_count) AS period_avg_queued_request_count,
		    AVG(M.blocked_task_count) AS period_avg_blocked_task_count,
		    AVG(M.active_parallel_thread_count) AS period_avg_active_parallel_thread_count,
		    AVG(M.tempdb_data_space_kb) AS period_avg_tempdb_data_space_kb
    FROM dbo.ResourceGovernorWorkloadGroupsMetrics M
    WHERE M.InstanceID = @InstanceID
    AND M.SnapshotDate >= @FromDate
    AND M.SnapshotDate < @ToDate
    GROUP BY M.WorkloadGroupID
)
SELECT WG.InstanceID,
       WG.group_id,
       WG.name,
       WG.pool_id,
       WG.pool_name,
       WG.external_pool_id,
       WG.statistics_start_time,
       WG.total_request_count,
       WG.total_queued_request_count,
       WG.active_request_count,
       WG.queued_request_count,
       WG.total_cpu_limit_violation_count,
       WG.total_cpu_usage_ms,
       WG.max_request_cpu_time_ms,
       WG.blocked_task_count,
       WG.total_lock_wait_count,
       WG.total_lock_wait_time_ms,
       WG.total_query_optimization_count,
       WG.total_suboptimal_plan_generation_count,
       WG.total_reduced_memgrant_count,
       WG.max_request_grant_memory_kb,
       WG.active_parallel_thread_count,
       WG.importance,
       WG.request_max_memory_grant_percent,
       WG.request_max_cpu_time_sec,
       WG.request_memory_grant_timeout_sec,
       WG.group_max_requests,
       WG.max_dop,
       WG.effective_max_dop,
       WG.total_cpu_usage_preemptive_ms,
       WG.request_max_memory_grant_percent_numeric,
       WG.tempdb_data_space_kb,
       WG.peak_tempdb_data_space_kb,
       WG.total_tempdb_data_limit_violation_count,
       WG.SnapshotDate,
       Metrics.period_cpu_usage_ms,
       Metrics.period_cpu_cores,
       Metrics.period_cpu_percent,
       Metrics.period_cpu_share_percent,
       Metrics.period_requests_per_min,
       Metrics.period_queued_request_count_per_min,
       Metrics.period_cpu_limit_violations_per_min,
       Metrics.period_lock_waits_per_min,
       Metrics.period_lock_wait_time_ms_per_sec,
       Metrics.period_query_optimizations_per_min,
       Metrics.period_suboptimal_plan_generation_count_per_min,
       Metrics.period_reduced_memgrant_count_per_min,
       Metrics.period_cpu_usage_preemptive_ms_per_min,
       Metrics.period_tempdb_data_limit_violations_per_min,
       Metrics.period_avg_active_request_count,
       Metrics.period_avg_queued_request_count,
       Metrics.period_avg_blocked_task_count,
       Metrics.period_avg_active_parallel_thread_count,
       Metrics.period_avg_tempdb_data_space_kb
FROM dbo.ResourceGovernorWorkloadGroups WG
LEFT JOIN Metrics ON WG.WorkloadGroupID = Metrics.WorkloadGroupID
WHERE WG.InstanceID = @InstanceID

