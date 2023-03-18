CREATE PROC dbo.Alerts_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@LastOccurrenceFrom DATETIME2(3)=NULL, 
	@LastOccurrenceTo DATETIME2(3)=NULL, 
	@IsCritical BIT=NULL,
	@ShowHidden BIT=1
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
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END

SELECT A.InstanceDisplayName AS Instance,
       A.name AS [Alert Name],
       A.message_id AS [Message ID],
       A.severity AS [Severity],
       A.enabled AS [Enabled],
       A.delay_between_responses AS [Delay Between Responses],
       A.last_occurrence_utc AS [Last Occurrence],
	   DATEDIFF(d,A.last_occurrence_utc,GETUTCDATE()) AS [Days since Last Occurrence],
       A.last_response_utc AS [Last Response],
       A.notification_message AS [Notification Message],
       A.include_event_description AS [Include Event Description],
       A.database_name AS [Database Name],
       A.event_description_keyword AS [Event Description Keyword],
       A.occurrence_count AS [Occurrence Count],
       A.count_reset_utc AS [Count Reset],
       A.job_id AS [Job ID],
       A.JobName AS [Job Name],
       A.has_notification AS [Has Notification],
       A.category_id AS [Category ID],
       A.performance_condition AS [Performance Condition],
       A.IsCriticalAlert AS [Is Critical Alert],
	   A.AlertLevel,
       A.DefaultAlertLevel,
       A.ActualAlertLevel,
       A.NotificationPeriodHrs,
       A.DefaultNotificationPeriodHrs,
       A.ActualNotificationPeriodHrs,
       A.AlertStatus,
       A.id,
       A.InstanceID,
       A.AcknowledgeDate
FROM dbo.SysAlerts A
WHERE EXISTS(   
                SELECT 1 
                FROM @Instances I 
                WHERE I.InstanceID = A.InstanceID
            )
AND (A.last_occurrence_utc>=@LastOccurrenceFrom OR @LastOccurrenceFrom IS NULL)
AND (A.last_occurrence_utc< @LastOccurrenceTo OR @LastOccurrenceTo IS NULL)
AND (A.IsCriticalAlert=@IsCritical OR @IsCritical IS NULL)
AND (A.ShowInSummary=1 OR @ShowHidden=1)
ORDER BY [Last Occurrence] DESC