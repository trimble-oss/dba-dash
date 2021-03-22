CREATE VIEW dbo.AzureDBElasticPoolStorageStatus
AS
SELECT P.PoolID,
	P.InstanceID,
	P.avg_allocated_storage_percent,
	P.elastic_pool_storage_limit_mb,
	P.elastic_pool_storage_limit_mb*(avg_allocated_storage_percent/100.0) AS elastic_pool_storage_used_mb,
	P.elastic_pool_storage_limit_mb*(1-avg_allocated_storage_percent/100.0) AS elastic_pool_storage_free_mb,
	CASE WHEN P.avg_allocated_storage_percent> (PT.CriticalThreshold*100) THEN 1  WHEN P.avg_allocated_storage_percent>(PT.WarningThreshold*100) THEN 2  WHEN PT.WarningThreshold IS NULL THEN 3 ELSE 4 END AS ElasticPoolStorageStatus
FROM dbo.AzureDBElasticPoolResourceStats_Current P 
OUTER APPLY(SELECT TOP(1) WarningThreshold,CriticalThreshold FROM dbo.AzureDBElasticPoolStorageThresholds T 
		WHERE (T.PoolID = P.PoolID OR T.PoolID=-1)
		ORDER BY T.PoolID DESC) PT