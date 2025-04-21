CREATE PROC dbo.RepositoryMetricsConfig_Del(
	@InstanceID INT,
	@MetricType VARCHAR(20)
)
AS
IF @InstanceID >0
BEGIN
	/* Delete metrics for instance (Inherit from root) */
	DELETE C
	FROM dbo.RepositoryMetricsConfig C
	WHERE C.InstanceID = @InstanceID
	AND C.MetricType = @MetricType
END
ELSE
BEGIN
	RAISERROR('Invalid Instance ID',16,1)
END