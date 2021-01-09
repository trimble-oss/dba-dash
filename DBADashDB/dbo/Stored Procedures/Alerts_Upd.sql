
CREATE PROC [dbo].[Alerts_Upd](@Alerts dbo.Alerts READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='Alerts'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	BEGIN TRAN
	DELETE dbo.Alerts 
	WHERE InstanceID=@InstanceID
	INSERT INTO dbo.Alerts
	(
		InstanceID,
		id,
		name,
		message_id,
		severity,
		enabled,
		delay_between_responses,
		last_occurrence,
		last_response,
		notification_message,
		include_event_description,
		database_name,
		event_description_keyword,
		occurrence_count,
		count_reset,
		job_id,
		has_notification,
		category_id,
		performance_condition
	)
	SELECT @InstanceID,
			id,
			name,
			message_id,
			severity,
			enabled,
			delay_between_responses,
			last_occurrence,
			last_response,
			notification_message,
			include_event_description,
			database_name,
			event_description_keyword,
			occurrence_count,
			count_reset,
			job_id,
			has_notification,
			category_id,
			performance_condition
	FROM @Alerts

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
	COMMIT
END