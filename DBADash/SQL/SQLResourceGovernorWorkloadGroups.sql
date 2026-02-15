DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT  SYSUTCDATETIME() AS SnapshotDate,
        WG.group_id,
        WG.name,
        WG.pool_id,
        P.name as pool_name,
        ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_workload_groups'),'external_pool_id','ColumnID') IS NULL THEN 'CAST(NULL AS INT) AS external_pool_id,' ELSE 'WG.external_pool_id,' END + '
        TODATETIMEOFFSET(WG.statistics_start_time, DATENAME(TzOffset, SYSDATETIMEOFFSET())) AS statistics_start_time,
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
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_workload_groups'),'effective_max_dop','ColumnID') IS NULL THEN 'CAST(NULL AS INT) AS effective_max_dop,' ELSE 'WG.effective_max_dop,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_workload_groups'),'total_cpu_usage_preemptive_ms','ColumnID') IS NULL THEN 'CAST(NULL AS BIGINT) AS total_cpu_usage_preemptive_ms,' ELSE 'WG.total_cpu_usage_preemptive_ms,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_workload_groups'),'request_max_memory_grant_percent_numeric','ColumnID') IS NULL THEN 'CAST(NULL AS FLOAT) AS request_max_memory_grant_percent_numeric,' ELSE 'WG.request_max_memory_grant_percent_numeric,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_workload_groups'),'tempdb_data_space_kb','ColumnID') IS NULL THEN 'CAST(NULL AS BIGINT) AS tempdb_data_space_kb,' ELSE 'WG.tempdb_data_space_kb,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_workload_groups'),'peak_tempdb_data_space_kb','ColumnID') IS NULL THEN 'CAST(NULL AS BIGINT) AS peak_tempdb_data_space_kb,' ELSE 'WG.peak_tempdb_data_space_kb,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_workload_groups'),'total_tempdb_data_limit_violation_count','ColumnID') IS NULL THEN 'CAST(NULL AS BIGINT) AS total_tempdb_data_limit_violation_count' ELSE 'WG.total_tempdb_data_limit_violation_count' END + '
FROM sys.dm_resource_governor_workload_groups AS WG
JOIN sys.resource_governor_resource_pools P ON WG.pool_id = P.pool_id;'

EXEC sp_executesql @SQL