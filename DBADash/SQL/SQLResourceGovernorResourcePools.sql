DECLARE @SQL NVARCHAR(MAX)
SET @SQL = '
SELECT SYSUTCDATETIME() AS SnapshotDate,
       RP.pool_id,
       RP.name,
       TODATETIMEOFFSET(RP.statistics_start_time, DATENAME(TzOffset, SYSDATETIMEOFFSET())) AS statistics_start_time,
       RP.total_cpu_usage_ms,
       RP.cache_memory_kb,
       RP.compile_memory_kb,
       RP.used_memgrant_kb,
       RP.total_memgrant_count,
       RP.total_memgrant_timeout_count,
       RP.active_memgrant_count,
       RP.active_memgrant_kb,
       RP.memgrant_waiter_count,
       RP.max_memory_kb,
       RP.used_memory_kb,
       RP.target_memory_kb,
       RP.out_of_memory_count,
       RP.min_cpu_percent,
       RP.max_cpu_percent,
       RP.min_memory_percent,
       RP.max_memory_percent,
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'cap_cpu_percent','ColumnID') IS NULL
                THEN 'CAST(NULL AS INT) AS cap_cpu_percent,'
                ELSE 'RP.cap_cpu_percent,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'min_iops_per_volume','ColumnID') IS NULL
                THEN 'CAST(NULL AS INT) AS min_iops_per_volume,'
                ELSE 'RP.min_iops_per_volume,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'max_iops_per_volume','ColumnID') IS NULL
                THEN 'CAST(NULL AS INT) AS max_iops_per_volume,'
                ELSE 'RP.max_iops_per_volume,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'read_io_queued_total','ColumnID') IS NULL
                THEN 'CAST(NULL AS INT) AS read_io_queued_total,'
                ELSE 'RP.read_io_queued_total,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'read_io_issued_total','ColumnID') IS NULL
                THEN 'CAST(NULL AS INT) AS read_io_issued_total,'
                ELSE 'RP.read_io_issued_total,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'read_io_completed_total','ColumnID') IS NULL
                THEN 'CAST(NULL AS INT) AS read_io_completed_total,'
                ELSE 'RP.read_io_completed_total,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'read_io_throttled_total','ColumnID') IS NULL
                THEN 'CAST(NULL AS INT) AS read_io_throttled_total,'
                ELSE 'RP.read_io_throttled_total,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'read_bytes_total','ColumnID') IS NULL
                THEN 'CAST(NULL AS BIGINT) AS read_bytes_total,'
                ELSE 'RP.read_bytes_total,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'read_io_stall_total_ms','ColumnID') IS NULL
                THEN 'CAST(NULL AS BIGINT) AS read_io_stall_total_ms,'
                ELSE 'RP.read_io_stall_total_ms,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'read_io_stall_queued_ms','ColumnID') IS NULL
                THEN 'CAST(NULL AS BIGINT) AS read_io_stall_queued_ms,'
                ELSE 'RP.read_io_stall_queued_ms,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'write_io_queued_total','ColumnID') IS NULL
                THEN 'CAST(NULL AS INT) AS write_io_queued_total,'
                ELSE 'RP.write_io_queued_total,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'write_io_issued_total','ColumnID') IS NULL
                THEN 'CAST(NULL AS INT) AS write_io_issued_total,'
                ELSE 'RP.write_io_issued_total,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'write_io_completed_total','ColumnID') IS NULL
                THEN 'CAST(NULL AS INT) AS write_io_completed_total,'
                ELSE 'RP.write_io_completed_total,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'write_io_throttled_total','ColumnID') IS NULL
                THEN 'CAST(NULL AS INT) AS write_io_throttled_total,'
                ELSE 'RP.write_io_throttled_total,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'write_bytes_total','ColumnID') IS NULL
                THEN 'CAST(NULL AS BIGINT) AS write_bytes_total,'
                ELSE 'RP.write_bytes_total,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'write_io_stall_total_ms','ColumnID') IS NULL
                THEN 'CAST(NULL AS BIGINT) AS write_io_stall_total_ms,'
                ELSE 'RP.write_io_stall_total_ms,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'write_io_stall_queued_ms','ColumnID') IS NULL
                THEN 'CAST(NULL AS BIGINT) AS write_io_stall_queued_ms,'
                ELSE 'RP.write_io_stall_queued_ms,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'io_issue_violations_total','ColumnID') IS NULL
                THEN 'CAST(NULL AS INT) AS io_issue_violations_total,'
                ELSE 'RP.io_issue_violations_total,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'io_issue_delay_total_ms','ColumnID') IS NULL
                THEN 'CAST(NULL AS BIGINT) AS io_issue_delay_total_ms,'
                ELSE 'RP.io_issue_delay_total_ms,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'io_issue_delay_non_throttled_total_ms','ColumnID') IS NULL
                THEN 'CAST(NULL AS BIGINT) AS io_issue_delay_non_throttled_total_ms,'
                ELSE 'RP.io_issue_delay_non_throttled_total_ms,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'total_cpu_delayed_ms','ColumnID') IS NULL
                THEN 'CAST(NULL AS BIGINT) AS total_cpu_delayed_ms,'
                ELSE 'RP.total_cpu_delayed_ms,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'total_cpu_active_ms','ColumnID') IS NULL
                THEN 'CAST(NULL AS BIGINT) AS total_cpu_active_ms,'
                ELSE 'RP.total_cpu_active_ms,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'total_cpu_violation_delay_ms','ColumnID') IS NULL
                THEN 'CAST(NULL AS BIGINT) AS total_cpu_violation_delay_ms,'
                ELSE 'RP.total_cpu_violation_delay_ms,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'total_cpu_violation_sec','ColumnID') IS NULL
                THEN 'CAST(NULL AS BIGINT) AS total_cpu_violation_sec,'
                ELSE 'RP.total_cpu_violation_sec,'
           END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.dm_resource_governor_resource_pools'),'total_cpu_usage_preemptive_ms','ColumnID') IS NULL
                THEN 'CAST(NULL AS BIGINT) AS total_cpu_usage_preemptive_ms'
                ELSE 'RP.total_cpu_usage_preemptive_ms'
           END + '
FROM sys.dm_resource_governor_resource_pools RP'

EXEC sp_executesql @SQL