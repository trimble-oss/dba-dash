CREATE PROC [Report].[Alerts_Get](
	@InstanceIDs VARCHAR(MAX)=NULL,
	@LastOccurrenceFrom DATETIME2(3)=NULL, 
	@LastOccurrenceTo DATETIME2(3)=NULL, 
	@IsCritical BIT=NULL
)
AS
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT Item
	FROM dbo.SplitStrings(@InstanceIDs,',')
END

SELECT InstanceID,
       Instance,
       ConnectionID,
       id,
       name,
       message_id,
       severity,
       enabled,
       delay_between_responses,
       last_occurrence,
       last_occurrence_utc,
       last_response,
       last_response_utc,
       notification_message,
       include_event_description,
       database_name,
       event_description_keyword,
       occurrence_count,
       count_reset,
       count_reset_utc,
       job_id,
       JobName,
       has_notification,
       category_id,
       performance_condition,
       IsCriticalAlert,
       UTCOffset 
FROM dbo.SysAlerts A
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = A.InstanceID)
AND (A.last_occurrence_utc>=@LastOccurrenceFrom OR @LastOccurrenceFrom IS NULL)
AND (A.last_occurrence_utc< @LastOccurrenceTo OR @LastOccurrenceTo IS NULL)
AND (A.IsCriticalAlert=@IsCritical OR @IsCritical IS NULL)