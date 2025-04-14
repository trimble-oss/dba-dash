CREATE PROC dbo.AvailabilityGroupMetricsConfig_Upd(
	@InstanceID INT,
	@EnabledMetrics NVARCHAR(MAX)
)
AS
/* Update enabled status of metrics */
UPDATE C
	SET C.IsEnabled = CASE WHEN S.value IS NOT NULL THEN 1 ELSE 0 END
FROM dbo.AvailabilityGroupMetricsConfig C
LEFT JOIN STRING_SPLIT(@EnabledMetrics,',') S ON C.MetricName = LTRIM(RTRIM(S.value))
WHERE C.InstanceID = @InstanceID

IF @InstanceID >0
BEGIN
	/* Insert metrics for instance if they don't exist */
	INSERT INTO dbo.AvailabilityGroupMetricsConfig(
			InstanceID,
			MetricName,
			IsAggregate,
			IsEnabled
	)
	SELECT @InstanceID AS InstanceID,
		   RootCfg.MetricName,
		   RootCfg.IsAggregate,
		   CAST(1 AS BIT) AS IsEnabled
	FROM dbo.AvailabilityGroupMetricsConfig RootCfg
	INNER JOIN STRING_SPLIT(@EnabledMetrics,',') S ON RootCfg.MetricName = LTRIM(RTRIM(S.value)) /* Inner join as we only need to copy the enabled metrics */
	WHERE RootCfg.InstanceID = -1 /*Root Level */
	AND NOT EXISTS(SELECT 1 
					FROM dbo.AvailabilityGroupMetricsConfig InstanceCfg
					WHERE InstanceCfg.InstanceID = @InstanceID
					AND RootCfg.MetricName = InstanceCfg.MetricName
					)
	/* Remove disabled metrics at instance level*/
	DELETE dbo.AvailabilityGroupMetricsConfig
	WHERE IsEnabled = 0
	AND InstanceID = @InstanceID
	
	/* Add a dummy row if all metrics are disabled, otherwise we will inherit from the root */
	IF NOT EXISTS(SELECT 1 FROM dbo.AvailabilityGroupMetricsConfig WHERE InstanceID = @InstanceID)
	BEGIN
		INSERT INTO dbo.AvailabilityGroupMetricsConfig(
				InstanceID,
				MetricName,
				IsAggregate,
				IsEnabled
		)
		SELECT @InstanceID AS InstanceID,
			   'Dummy Row - Disabled',
			   CAST(0 AS BIT),
			   CAST(0 AS BIT)
	END
END