CREATE VIEW dbo.AzureDBResourceStatsCounters_60MIN
AS
/*
	Replicates dbo.PerformanceCounters_60MIN table for use in dbo.PerformanceCountersBetweenDates_60MIN function
*/
SELECT  unpvt.InstanceID,
        unpvt.end_time AS SnapshotDate,
        C.CounterID,
        calc.CounterName,
        MAX(CASE WHEN Calc.IsMaxCounter=1 THEN unpvt.value ELSE NULL END) AS max_value,
        MAX(CASE WHEN Calc.IsMaxCounter=0 THEN unpvt.value ELSE NULL  END) AS value,
        MAX(unpvt.SampleCount) AS SampleCount
FROM (SELECT    RS.InstanceID,
                RS.end_time,
                CAST(RS.avg_cpu_percent AS DECIMAL(28,9)) AS avg_cpu_percent,
                CAST(RS.max_cpu_percent AS DECIMAL(28,9)) AS max_cpu_percent,
                CAST(RS.avg_data_io_percent AS DECIMAL(28,9)) AS avg_data_io_percent,
                CAST(RS.max_data_io_percent AS DECIMAL(28,9)) AS max_data_io_percent,
                CAST(RS.avg_log_write_percent AS DECIMAL(28,9)) AS avg_log_write_percent,
                CAST(RS.max_log_write_percent AS DECIMAL(28,9)) AS max_log_write_percent,
                CAST(RS.avg_memory_usage_percent AS DECIMAL(28,9)) AS avg_memory_usage_percent ,
                CAST(RS.max_memory_usage_percent AS DECIMAL(28,9)) AS max_memory_usage_percent,
                CAST(RS.xtp_storage_percent AS DECIMAL(28,9)) AS xtp_storage_percent,
                CAST(RS.max_xtp_storage_percent AS DECIMAL(28,9)) AS max_xtp_storage_percent,
                CAST(RS.max_worker_percent AS DECIMAL(28,9)) AS max_worker_percent,
                CAST(RS.max_session_percent AS DECIMAL(28,9)) AS max_session_percent,
                CAST(RS.dtu_limit AS DECIMAL(28,9)) AS dtu_limit,
                CAST(RS.avg_instance_cpu_percent AS DECIMAL(28,9)) AS avg_instance_cpu_percent,
                CAST(RS.max_instance_cpu_percent AS DECIMAL(28,9)) AS max_instance_cpu_percent,
                CAST(RS.avg_instance_memory_percent AS DECIMAL(28,9)) AS avg_instance_memory_percent,
                CAST(RS.max_instance_memory_percent AS DECIMAL(28,9)) AS max_instance_memory_percent,
                CAST(cpu_limit AS DECIMAL(28,9)) AS cpu_limit,
                RS.CPU10+RS.CPU20+RS.CPU30+RS.CPU40+RS.CPU50+RS.CPU60+RS.CPU70+RS.CPU80+RS.CPU90+RS.CPU100  AS SampleCount
       FROM dbo.AzureDBResourceStats_60MIN RS
       ) AS S
UNPIVOT( value 
        FOR CounterName IN(
            avg_cpu_percent,
            max_cpu_percent,
            avg_data_io_percent,
            max_data_io_percent,
            avg_log_write_percent,
            max_log_write_percent,
            avg_memory_usage_percent,
            max_memory_usage_percent,
            xtp_storage_percent,
            max_xtp_storage_percent,
            max_worker_percent,
            max_session_percent,
            dtu_limit,
            avg_instance_cpu_percent,
            max_instance_cpu_percent,
            avg_instance_memory_percent,
            max_instance_memory_percent,
            cpu_limit
          )
      ) unpvt
OUTER APPLY(SELECT CASE WHEN CounterName = 'max_cpu_percent' THEN 'avg_cpu_percent' 
                    WHEN CounterName = 'max_data_io_percent' THEN 'avg_data_io_percent'
                    WHEN CounterName = 'max_log_write_percent' THEN 'avg_log_write_percent'
                    WHEN CounterName = 'max_memory_usage_percent' THEN 'avg_memory_usage_percent'
                    WHEN CounterName = 'max_xtp_storage_percent' THEN 'xtp_storage_percent'
                    WHEN CounterName = 'max_instance_cpu_percent' THEN 'avg_instance_cpu_percent'
                    WHEN CounterName = 'max_instance_memory_percent' THEN 'avg_instance_memory_percent'
                    ELSE CounterName END AS CounterName,
                CASE WHEN unpvt.CounterName IN('max_cpu_percent','max_data_io_percent','max_log_write_percent','max_memory_usage_percent','max_xtp_storage_percent','max_instance_cpu_percent','max_instance_memory_percent','max_worker_percent','max_session_percent')
                    THEN 1 ELSE 0 END AS IsMaxCounter
            ) AS calc
JOIN dbo.Counters C ON C.counter_name = calc.CounterName AND C.CounterType = 4 AND C.object_name = 'sys.dm_db_resource_stats' AND C.instance_name = ''
GROUP BY  unpvt.InstanceID,
        unpvt.end_time,
        C.CounterID,
        calc.CounterName