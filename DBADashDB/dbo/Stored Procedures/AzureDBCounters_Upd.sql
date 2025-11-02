CREATE PROC dbo.AzureDBCounters_Upd(
	@InstanceID INT=NULL
)
AS
/*
	Create virtual counters for Azure SQL Database Elastic Pool resource stats & Azure SQL Database resource stats.
	dbo.AzureDBElasticPoolResourceStatsCounters, dbo.AzureDBElasticPoolResourceStatsCounters_60MIN, AzureDBResourceStatsCounters & AzureDBResourceStatsCounters_60MIN views replicate dbo.PerformanceCounters and dbo.PerformanceCounters_60MIN tables.
	dbo.PerformanceCountersBetweenDates & dbo.PerformanceCountersBetweenDates_60MIN functions incorporate these views when CounterType=3 (Elastic Pool) or CounterType=4 (Azure DB resource stats).
	
*/
INSERT INTO dbo.Counters(object_name,counter_name,instance_name,CounterType)
SELECT 'sys.elastic_pool_resource_stats',counter_name,elastic_pool_name,3
FROM (	SELECT DISTINCT elastic_pool_name 
		FROM dbo.AzureDBElasticPool
		WHERE (InstanceID = @InstanceID OR @InstanceID IS NULL)
		) pools
CROSS APPLY (
VALUES('avg_allocated_storage_percent'),
	('avg_cpu_percent'),
	('avg_data_io_percent'),
	('avg_log_write_percent'),
	('avg_storage_percent'),
	('max_worker_percent'),
	('max_session_percent'),
	('elastic_pool_dtu_limit'),
	('elastic_pool_storage_limit_mb'),
	('elastic_pool_cpu_limit')
	) T (counter_name)
WHERE NOT EXISTS(
		SELECT 1 
		FROM dbo.Counters C
		WHERE C.object_name='sys.elastic_pool_resource_stats'
		AND C.CounterType = 3
		AND C.instance_name = pools.elastic_pool_name
		AND C.counter_name = T.counter_name
		)
OPTION(RECOMPILE)
	
INSERT INTO dbo.InstanceCounters(InstanceID,CounterID)
SELECT P.InstanceID, C.CounterID
FROM dbo.Counters C
JOIN dbo.AzureDBElasticPool P ON C.instance_name = P.elastic_pool_name
WHERE C.CounterType = 3
AND C.object_name = 'sys.elastic_pool_resource_stats'
AND (P.InstanceID = @InstanceID OR @InstanceID IS NULL)
AND NOT EXISTS(	SELECT 1 
				FROM InstanceCounters IC
				WHERE IC.InstanceID = P.InstanceID
				AND IC.CounterID = C.CounterID
				)
OPTION(RECOMPILE)

INSERT INTO dbo.InstanceCounters(InstanceID,CounterID)
SELECT  I.InstanceID,C.CounterID
FROM dbo.Instances I
CROSS JOIN dbo.Counters C
WHERE EngineEdition = 5
AND C.CounterType = 4
AND NOT EXISTS(
			SELECT 1 
			FROM dbo.InstanceCounters IC 
			WHERE IC.CounterID = C.CounterID 
			AND IC.InstanceID = I.InstanceID
			)
AND (I.InstanceID = @InstanceID OR @InstanceID IS NULL)
OPTION(RECOMPILE)