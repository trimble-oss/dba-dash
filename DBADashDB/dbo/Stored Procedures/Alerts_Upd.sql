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
	DECLARE @Type VARCHAR(50)='SQLAgentAlert'
	DECLARE @AlertDetails Alert.AlertDetails
	DECLARE @AlertDetailCount INT=0

	/* Check if we have any DBA Dash alert rules that might apply */
	IF EXISTS(	SELECT 1 
				FROM Alert.Rules 
				WHERE Type = @Type 
				AND IsActive=1
				)
	BEGIN
		CREATE TABLE #NewAlerts(
			name NVARCHAR(MAX),
			message_id INT,
			severity INT,
			notification_message NVARCHAR(512)
		)
		/* Get new alerts - alerts that have been triggered since the last collection */
		INSERT INTO #NewAlerts(
			name,
			message_id,
			severity,
			notification_message
		)
		SELECT	T.name,
				T.message_id,
				T.severity,
				T.notification_message
		FROM @Alerts T
		WHERE NOT EXISTS(
						SELECT 1 
						FROM dbo.Alerts A
						WHERE A.InstanceID = @InstanceID
						AND A.last_occurrence >=T.last_occurrence 
						AND A.id = t.id
						)
		AND T.last_occurrence >=DATEADD(d,-1,GETUTCDATE()) /* Put a limit on last occurance date (for new instances) */

		IF @@ROWCOUNT >0
		BEGIN
			/* Filter the new sql agent alerts based on the DBA Dash alert rules */
			INSERT INTO @AlertDetails(
					InstanceID,
					Priority,
					AlertKey,
					Message,
					RuleID
			)
			SELECT @InstanceID,
					R.Priority,
					REPLACE(R.AlertKey,'{AlertName}',name) AS AlertKey,
					ISNULL(T.notification_message,name + ' alert triggered'),
					R.RuleID
			FROM Alert.Rules R
			CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID,R.ApplyToInstanceID,R.AlertKey,R.ApplyToHidden) I
			OUTER APPLY(SELECT 	TRY_CAST(JSON_VALUE(R.Details,'$.SeverityFrom') AS INT) AS SeverityFrom,
								TRY_CAST(JSON_VALUE(R.Details,'$.SeverityTo') AS INT) AS SeverityTo,
								ISNULL(JSON_VALUE(R.Details,'$.AlertName'),'%') AS AlertName
						) calc
			CROSS APPLY #NewAlerts T
			WHERE R.Type = @Type
			AND R.IsActive=1
			AND I.InstanceID = @InstanceID
			AND (T.severity>=calc.SeverityFrom OR calc.SeverityFrom IS NULL)
			AND (T.severity<=calc.SeverityTo OR calc.SeverityTo IS NULL)
			AND (T.name LIKE calc.AlertName)
			AND (	
					EXISTS(	
							SELECT 1
							FROM OPENJSON(R.Details,'$.MessageIDList') List 
							WHERE List.Value = T.message_id
						)
					OR JSON_QUERY(R.Details,'$.MessageIDList') ='[]'
				)
			SET @AlertDetailCount = @@ROWCOUNT
		END
	END
		

	BEGIN TRAN

	IF @AlertDetailCount >0 /* Update DBA Dash alerts if needed */
	BEGIN
		EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AlertDetails,@AlertType=@Type,@ResolveAlertsOfType=0
	END

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