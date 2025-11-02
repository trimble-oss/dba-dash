CREATE VIEW dbo.AzureDBElasticPoolResourceStatsCounters_60MIN
AS
/*
	Replicates dbo.PerformanceCounters_60MIN table for use in dbo.PerformanceCountersBetweenDates_60MIN function
*/
SELECT unpvt.InstanceID,
	C.CounterID,
	calc.CounterName,
	unpvt.end_time AS SnapshotDate,
    MAX(CASE WHEN Calc.IsMaxCounter=1 THEN unpvt.value ELSE NULL END) AS max_value,
    MAX(CASE WHEN Calc.IsMaxCounter=0 THEN unpvt.value ELSE NULL  END) AS value,
    MAX(unpvt.SampleCount) AS SampleCount
FROM (
	SELECT	InstanceID,
			EP.elastic_pool_name,
			EPRS.end_time,
			CAST(EPRS.avg_allocated_storage_percent AS DECIMAL(28,9)) AS avg_allocated_storage_percent,
			CAST(EPRS.avg_cpu_percent AS DECIMAL(28,9)) AS avg_cpu_percent,
			CAST(EPRS.max_cpu_percent AS DECIMAL(28,9)) AS max_cpu_percent,
			CAST(EPRS.avg_data_io_percent AS DECIMAL(28,9)) AS avg_data_io_percent,
			CAST(EPRS.max_data_io_percent AS DECIMAL(28,9)) AS max_data_io_percent,
			CAST(EPRS.avg_log_write_percent AS DECIMAL(28,9)) AS avg_log_write_percent,
			CAST(EPRS.max_log_write_percent AS DECIMAL(28,9)) AS max_log_write_percent,
			CAST(EPRS.avg_storage_percent AS DECIMAL(28,9)) AS avg_storage_percent,
			CAST(EPRS.max_storage_percent AS DECIMAL(28,9)) AS max_storage_percent,
			CAST(EPRS.max_worker_percent AS DECIMAL(28,9)) AS max_worker_percent ,
			CAST(EPRS.max_session_percent AS DECIMAL(28,9)) AS max_session_percent,
			CAST(EPRS.elastic_pool_dtu_limit AS DECIMAL(28,9)) AS elastic_pool_dtu_limit,
			CAST(EPRS.elastic_pool_storage_limit_mb AS DECIMAL(28,9)) AS elastic_pool_storage_limit_mb,
			CAST(EPRS.elastic_pool_cpu_limit AS DECIMAL(28,9)) AS elastic_pool_cpu_limit,
			EPRS.CPU10+EPRS.CPU20+EPRS.CPU30+EPRS.CPU40+EPRS.CPU50+EPRS.CPU60+EPRS.CPU70+EPRS.CPU80+EPRS.CPU90+EPRS.CPU100  AS SampleCount
	FROM dbo.AzureDBElasticPool EP
	JOIN dbo.AzureDBElasticPoolResourceStats_60MIN EPRS ON EP.PoolID = EPRS.PoolID
) PoolStats
UNPIVOT(
	value FOR CounterName IN(
	avg_allocated_storage_percent,
	avg_cpu_percent,
	max_cpu_percent,
	avg_data_io_percent,
	max_data_io_percent,
	avg_log_write_percent,
	max_log_write_percent,
	avg_storage_percent,
	max_storage_percent,
	max_worker_percent,
	max_session_percent,
	elastic_pool_dtu_limit,
	elastic_pool_storage_limit_mb,
	elastic_pool_cpu_limit
	)
) unpvt
OUTER APPLY(SELECT CASE WHEN CounterName = 'max_cpu_percent' THEN 'avg_cpu_percent' 
                   WHEN CounterName = 'max_data_io_percent' THEN 'avg_data_io_percent'
                   WHEN CounterName = 'max_log_write_percent' THEN 'avg_log_write_percent'
				   WHEN CounterName = 'max_storage_percent' THEN 'avg_storage_percent'
                    ELSE CounterName END AS CounterName,
                CASE WHEN unpvt.CounterName IN('max_cpu_percent','max_data_io_percent','max_log_write_percent','max_storage_percent','max_worker_percent','max_session_percent')
                    THEN 1 ELSE 0 END AS IsMaxCounter
            ) AS calc
JOIN dbo.Counters C ON C.counter_name = calc.CounterName AND C.CounterType = 3 AND C.instance_name = elastic_pool_name AND C.object_name = 'sys.elastic_pool_resource_stats'
GROUP BY  unpvt.InstanceID,
        unpvt.end_time,
        C.CounterID,
        calc.CounterName