CREATE VIEW dbo.PartitionConfiguration
AS
SELECT	SchemaName,
		TableName,
		PeriodType,
		PeriodCount,
		IsSystem
FROM (
VALUES -- Daily Partitions
	('dbo','Waits','d',14,CAST(1 AS BIT)),
	('dbo','CPU','d',14,CAST(1 AS BIT)),
	('dbo','AzureDBResourceStats','d',14,CAST(1 AS BIT)),
	('dbo','AzureDBElasticPoolResourceStats','d',14,CAST(1 AS BIT)),
	('dbo','ObjectExecutionStats','d',14,CAST(1 AS BIT)),
	('dbo','DBIOStats','d',14,CAST(1 AS BIT)),
	('dbo','SlowQueries','d',14,CAST(1 AS BIT)),
	('dbo','CustomChecksHistory','d',14,CAST(1 AS BIT)),
	('dbo','PerformanceCounters','d',14,CAST(1 AS BIT)),
	('dbo','JobHistory','d',14,CAST(1 AS BIT)),
	('dbo','RunningQueries','d',14,CAST(1 AS BIT)),
	('dbo','RunningQueriesCursors','d',14,CAST(1 AS BIT)),
	('dbo','MemoryUsage','d',14,CAST(1 AS BIT)),
	('dbo','SessionWaits','d',14,CAST(1 AS BIT)),
	('dbo','FailedLogins','d',14,CAST(1 AS BIT)),
	('dbo','ResourceGovernorWorkloadGroupsMetrics','d',14,CAST(1 AS BIT)),
	('dbo','ResourceGovernorResourcePoolsMetrics','d',14,CAST(1 AS BIT)),
	-- Monthly Partitions
	('dbo','AzureDBElasticPoolResourceStats_60MIN','m',3,CAST(1 AS BIT)),
	('dbo','AzureDBResourceStats_60MIN','m',3,CAST(1 AS BIT)),
	('dbo','CPU_60MIN','m',3,CAST(1 AS BIT)),
	('dbo','DBIOStats_60MIN','m',3,CAST(1 AS BIT)),
	('dbo','Waits_60MIN','m',3,CAST(1 AS BIT)),
	('dbo','ObjectExecutionStats_60MIN','m',3,CAST(1 AS BIT)),
	('dbo','PerformanceCounters_60MIN','m',3,CAST(1 AS BIT)),
	('dbo','JobStats_60MIN','m',3,CAST(1 AS BIT)),
	('dbo','IdentityColumnsHistory','m',3,CAST(1 AS BIT)),
	('dbo','TableSize','m',3,CAST(1 AS BIT))
	) T(SchemaName,TableName,PeriodType,PeriodCount,IsSystem)
UNION ALL
SELECT	SchemaName,
		TableName, 
		CASE WHEN RetentionDays > 365 THEN 'm' ELSE 'd' END AS PeriodType,
		CASE WHEN RetentionDays > 365 THEN 3 ELSE 14 END AS PeriodCount,
		CAST(0 AS BIT) AS IsSytem
FROM dbo.DataRetention
WHERE SchemaName = 'UserData'
