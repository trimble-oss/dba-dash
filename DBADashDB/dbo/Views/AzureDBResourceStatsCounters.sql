CREATE VIEW dbo.AzureDBResourceStatsCounters
AS
SELECT  unpvt.InstanceID,
        unpvt.end_time AS SnapshotDate,
        C.CounterID,
        unpvt.CounterName,
        unpvt.value,
        1 AS SampleCount
FROM (SELECT    InstanceID,
                end_time,
                CAST(avg_cpu_percent AS DECIMAL(28,9)) AS avg_cpu_percent,
                CAST( avg_data_io_percent AS DECIMAL(28,9)) AS avg_data_io_percent,
                CAST(avg_log_write_percent AS DECIMAL(28,9)) AS avg_log_write_percent,
                CAST(avg_memory_usage_percent AS DECIMAL(28,9)) AS avg_memory_usage_percent ,
                CAST(xtp_storage_percent AS DECIMAL(28,9)) AS xtp_storage_percent,
                CAST(max_worker_percent AS DECIMAL(28,9)) AS max_worker_percent,
                CAST(max_session_percent AS DECIMAL(28,9)) AS max_session_percent,
                CAST(dtu_limit AS DECIMAL(28,9)) AS dtu_limit,
                CAST(avg_instance_cpu_percent AS DECIMAL(28,9)) AS avg_instance_cpu_percent,
                CAST(avg_instance_memory_percent AS DECIMAL(28,9)) AS avg_instance_memory_percent,
                CAST(cpu_limit AS DECIMAL(28,9)) AS cpu_limit
       FROM dbo.AzureDBResourceStats
       ) AS S
UNPIVOT( value 
        FOR CounterName IN(
            avg_cpu_percent,
            avg_data_io_percent,
            avg_log_write_percent,
            avg_memory_usage_percent,
            xtp_storage_percent,
            max_worker_percent,
            max_session_percent,
            dtu_limit,
            avg_instance_cpu_percent,
            avg_instance_memory_percent,
            cpu_limit
          )
      ) unpvt
JOIN dbo.Counters C ON C.counter_name = unpvt.CounterName AND C.CounterType = 4 AND C.object_name = 'sys.dm_db_resource_stats' AND C.instance_name = ''