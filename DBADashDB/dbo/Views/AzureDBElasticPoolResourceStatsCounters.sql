CREATE VIEW dbo.AzureDBElasticPoolResourceStatsCounters
AS
/*
	Replicates dbo.PerformanceCounters table for use in dbo.PerformanceCountersBetweenDates function
*/
SELECT unpvt.InstanceID,
	C.CounterID,
	unpvt.end_time AS SnapshotDate,
	unpvt.value,
	1 AS SampleCount
FROM (
	SELECT	InstanceID,
			EP.elastic_pool_name,
			EPRS.end_time,
			CAST(EPRS.avg_allocated_storage_percent AS DECIMAL(28,9)) AS avg_allocated_storage_percent,
			CAST(EPRS.avg_cpu_percent AS DECIMAL(28,9)) AS avg_cpu_percent,
			CAST(EPRS.avg_data_io_percent AS DECIMAL(28,9)) AS avg_data_io_percent,
			CAST(EPRS.avg_log_write_percent AS DECIMAL(28,9)) AS avg_log_write_percent,
			CAST(EPRS.avg_storage_percent AS DECIMAL(28,9)) AS avg_storage_percent,
			CAST(EPRS.max_worker_percent AS DECIMAL(28,9)) AS max_worker_percent ,
			CAST(EPRS.max_session_percent AS DECIMAL(28,9)) AS max_session_percent,
			CAST(EPRS.elastic_pool_dtu_limit AS DECIMAL(28,9)) AS elastic_pool_dtu_limit,
			CAST(EPRS.elastic_pool_storage_limit_mb AS DECIMAL(28,9)) AS elastic_pool_storage_limit_mb,
			CAST(EPRS.elastic_pool_cpu_limit AS DECIMAL(28,9)) AS elastic_pool_cpu_limit
	FROM dbo.AzureDBElasticPool EP
	JOIN dbo.AzureDBElasticPoolResourceStats EPRS ON EP.PoolID = EPRS.PoolID
) PoolStats
UNPIVOT(
	value FOR CounterName IN(
	avg_allocated_storage_percent,
	avg_cpu_percent,
	avg_data_io_percent,
	avg_log_write_percent,
	avg_storage_percent,
	max_worker_percent,
	max_session_percent,
	elastic_pool_dtu_limit,
	elastic_pool_storage_limit_mb,
	elastic_pool_cpu_limit
	)
) unpvt
JOIN dbo.Counters C ON C.counter_name = unpvt.CounterName AND C.CounterType = 3 AND C.instance_name = elastic_pool_name AND C.object_name = 'sys.elastic_pool_resource_stats'
