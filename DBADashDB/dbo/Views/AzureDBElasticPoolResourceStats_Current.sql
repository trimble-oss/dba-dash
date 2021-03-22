CREATE VIEW dbo.AzureDBElasticPoolResourceStats_Current
AS
SELECT P.PoolID,
       P.InstanceID,
       P.elastic_pool_name,
       LastRS.start_time,
       LastRS.end_time,
       LastRS.avg_cpu_percent,
       LastRS.avg_data_io_percent,
       LastRS.avg_log_write_percent,
       LastRS.avg_storage_percent,
       LastRS.max_worker_percent,
       LastRS.max_session_percent,
       LastRS.elastic_pool_dtu_limit,
       LastRS.elastic_pool_storage_limit_mb,
       LastRS.avg_allocated_storage_percent,
       LastRS.elastic_pool_cpu_limit
FROM dbo.AzureDBElasticPool P 
CROSS APPLY(SELECT TOP(1) * FROM dbo.AzureDBElasticPoolResourceStats RS WHERE P.PoolID = RS.PoolID ORDER BY RS.end_time DESC) LastRS