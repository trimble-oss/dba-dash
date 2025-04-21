CREATE PROC dbo.RepositoryMetricsConfig_Get(
	@InstanceID INT,
	@MetricType VARCHAR(20)
)
AS
DECLARE @MetricsInstanceID INT 
SELECT @MetricsInstanceID = CASE WHEN EXISTS(	SELECT 1 
												FROM dbo.RepositoryMetricsConfig 
												WHERE InstanceID = @InstanceID 
												AND MetricType = @MetricType
											) THEN @InstanceID 
											ELSE -1 
											END

SELECT InstanceID,
	   MetricName,
	   CASE WHEN IsAggregate=1 THEN 'Instance Level' ELSE 'Per Database' END AS MetricLevel,
	   IsEnabled,
	   MetricType
FROM dbo.RepositoryMetricsConfig 
WHERE InstanceID = @MetricsInstanceID
AND MetricType = @MetricType
UNION ALL
/* List any new metrics available at root level for configuration at instance level */
SELECT @InstanceID AS InstanceID,
	   MetricName,
	   CASE WHEN IsAggregate=1 THEN 'Instance Level' ELSE 'Per Database' END AS MetricLevel,
	   CAST(0 AS BIT) AS IsEnabled,
	   RootCfg.MetricType
FROM dbo.RepositoryMetricsConfig RootCfg
WHERE InstanceID = -1
AND @MetricsInstanceID <> -1
AND MetricType = @MetricType
AND NOT EXISTS(	SELECT 1 
				FROM dbo.RepositoryMetricsConfig InstanceCfg
				WHERE InstanceCfg.InstanceID = @InstanceID
				AND RootCfg.MetricName = InstanceCfg.MetricName
				AND InstanceCfg.MetricType = @MetricType
				)
ORDER BY MetricType,MetricName