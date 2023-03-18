CREATE PROC dbo.AlertThresholds_Upd(
	@InstanceID INT,
	@id INT,
	@AlertLevel TINYINT,
	@NotificationPeriodHrs SMALLINT
)
AS
UPDATE dbo.Alerts
	SET NotificationPeriodHrs=@NotificationPeriodHrs,
	AlertLevel = @AlertLevel
WHERE @InstanceID = @InstanceID
AND id = @id