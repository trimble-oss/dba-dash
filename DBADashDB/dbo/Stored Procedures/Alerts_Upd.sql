CREATE PROC dbo.Alerts_Upd(
		@Alerts dbo.Alerts READONLY,
		@InstanceID INT,
		@SnapshotDate DATETIME2(3)
)
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='Alerts'
IF NOT EXISTS(	SELECT 1 
				FROM dbo.CollectionDates 
				WHERE SnapshotDate>=@SnapshotDate 
				AND InstanceID = @InstanceID 
				AND Reference=@Ref
			)
BEGIN
	BEGIN TRAN

	DELETE A 
	FROM dbo.Alerts A
	WHERE A.InstanceID=@InstanceID
	AND NOT EXISTS(	SELECT 1 
					FROM @Alerts T 
					WHERE T.id = A.id
					)

	UPDATE A 
		SET A.name = T.name,
			A.message_id = T.message_id,
			A.severity= T.severity,
			A.enabled = T.enabled,
			A.delay_between_responses = T.delay_between_responses,
			A.last_occurrence = T.last_occurrence,
			A.last_response = T.last_response,
			A.notification_message = T.notification_message,
			A.include_event_description = T.include_event_description,
			A.database_name = T.database_name,
			A.event_description_keyword = T.event_description_keyword,
			A.occurrence_count = T.occurrence_count,
			A.count_reset = T.count_reset,
			A.job_id = T.job_id,
			A.has_notification = T.has_notification,
			A.category_id = T.category_id,
			A.performance_condition = T.performance_condition
	FROM dbo.Alerts A
	JOIN @Alerts T ON A.id = T.id 
	WHERE A.InstanceID = @InstanceID

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
	FROM @Alerts T
	WHERE NOT EXISTS(	SELECT 1
						FROM dbo.Alerts A 
						WHERE A.id = T.id 
						AND A.InstanceID = @InstanceID
					)

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
	COMMIT
END