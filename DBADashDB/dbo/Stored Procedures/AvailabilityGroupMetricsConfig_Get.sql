CREATE PROC dbo.AvailabilityGroupMetricsConfig_Get(
	@InstanceID INT
)
AS
DECLARE @MetricsInstanceID INT 
SELECT @MetricsInstanceID = CASE WHEN EXISTS(SELECT 1 FROM dbo.AvailabilityGroupMetricsConfig WHERE InstanceID = @InstanceID) THEN @InstanceID ELSE -1 END

SELECT InstanceID,
	   MetricName,
	   CASE WHEN IsAggregate=1 THEN 'Instance Level' ELSE 'Per Database' END AS MetricType,
	   IsEnabled
FROM dbo.AvailabilityGroupMetricsConfig 
WHERE InstanceID = @MetricsInstanceID
UNION ALL
/* List any new metrics available at root level for configuration at instance level */
SELECT @InstanceID AS InstanceID,
	   MetricName,
	   CASE WHEN IsAggregate=1 THEN 'Instance Level' ELSE 'Per Database' END AS MetricType,
	   CAST(0 AS BIT) AS IsEnabled
FROM dbo.AvailabilityGroupMetricsConfig RootCfg
WHERE InstanceID = -1
AND @MetricsInstanceID <> -1
AND NOT EXISTS(	SELECT 1 
				FROM dbo.AvailabilityGroupMetricsConfig InstanceCfg
				WHERE InstanceCfg.InstanceID = @InstanceID
				AND RootCfg.MetricName = InstanceCfg.MetricName
				)
ORDER BY MetricType,MetricName