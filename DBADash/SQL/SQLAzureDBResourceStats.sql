SELECT end_time,
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
       cpu_limit,
       replica_role
FROM sys.dm_db_resource_stats
WHERE end_time >= @Date