CREATE PROC dbo.AvailabilityGroupMetricsConfig_Del(
	@InstanceID INT
)
AS
IF @InstanceID >0
BEGIN
	/* Delete metrics for instance (Inherit from root) */
	DELETE C
	FROM dbo.AvailabilityGroupMetricsConfig C
	WHERE C.InstanceID = @InstanceID
END
ELSE
BEGIN
	RAISERROR('Invalid Instance ID',16,1)
END