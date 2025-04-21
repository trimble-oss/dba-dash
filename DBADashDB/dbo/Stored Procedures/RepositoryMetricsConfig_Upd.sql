CREATE PROC dbo.RepositoryMetricsConfig_Upd(
	@InstanceID INT,
	@EnabledMetrics NVARCHAR(MAX),
	@MetricType VARCHAR(20)
)
AS
/* Update enabled status of metrics */
UPDATE C
	SET C.IsEnabled = CASE WHEN S.value IS NOT NULL THEN 1 ELSE 0 END
FROM dbo.RepositoryMetricsConfig C
LEFT JOIN STRING_SPLIT(@EnabledMetrics,',') S ON C.MetricName = LTRIM(RTRIM(S.value))
WHERE C.InstanceID = @InstanceID
AND C.MetricType = @MetricType

IF @InstanceID >0
BEGIN
	/* Insert metrics for instance if they don't exist */
	INSERT INTO dbo.RepositoryMetricsConfig(
			InstanceID,
			MetricName,
			IsAggregate,
			IsEnabled,
			MetricType
	)
	SELECT @InstanceID AS InstanceID,
		   RootCfg.MetricName,
		   RootCfg.IsAggregate,
		   CAST(1 AS BIT) AS IsEnabled,
		   RootCfg.MetricType
	FROM dbo.RepositoryMetricsConfig RootCfg
	INNER JOIN STRING_SPLIT(@EnabledMetrics,',') S ON RootCfg.MetricName = LTRIM(RTRIM(S.value)) /* Inner join as we only need to copy the enabled metrics */
	WHERE RootCfg.InstanceID = -1 /*Root Level */
	AND MetricType = @MetricType
	AND NOT EXISTS(SELECT 1 
					FROM dbo.RepositoryMetricsConfig InstanceCfg
					WHERE InstanceCfg.InstanceID = @InstanceID
					AND RootCfg.MetricName = InstanceCfg.MetricName
					AND InstanceCfg.MetricType = @MetricType
					)
	/* Remove disabled metrics at instance level*/
	DELETE dbo.RepositoryMetricsConfig
	WHERE IsEnabled = 0
	AND InstanceID = @InstanceID
	AND MetricType = @MetricType
	
	/* Add a dummy row if all metrics are disabled, otherwise we will inherit from the root */
	IF NOT EXISTS(SELECT 1 FROM dbo.RepositoryMetricsConfig WHERE InstanceID = @InstanceID AND MetricType = @MetricType)
	BEGIN
		INSERT INTO dbo.RepositoryMetricsConfig(
				InstanceID,
				MetricName,
				IsAggregate,
				IsEnabled,
				MetricType
		)
		SELECT @InstanceID AS InstanceID,
			   'Dummy Row - Disabled',
			   CAST(0 AS BIT),
			   CAST(0 AS BIT),
			   @MetricType
	END
END