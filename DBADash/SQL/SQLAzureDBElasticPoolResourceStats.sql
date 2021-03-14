SELECT start_time,
       end_time,
       elastic_pool_name,
       avg_cpu_percent,
       avg_data_io_percent,
       avg_log_write_percent,
       avg_storage_percent,
       max_worker_percent,
       max_session_percent,
       elastic_pool_dtu_limit,
       elastic_pool_storage_limit_mb,
       avg_allocated_storage_percent,
       elastic_pool_cpu_limit
FROM sys.elastic_pool_resource_stats 
WHERE start_time>=@Date