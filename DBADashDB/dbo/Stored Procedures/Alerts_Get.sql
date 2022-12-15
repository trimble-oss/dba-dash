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
       name AS [Alert Name],
       message_id AS [Message ID],
       severity AS [Severity],
       enabled AS [Enabled],
       delay_between_responses AS [Delay Between Responses],
       last_occurrence_utc AS [Last Occurrence],
	   DATEDIFF(d,A.last_occurrence_utc,GETUTCDATE()) AS [Days since Last Occurrence],
       last_response_utc AS [Last Response],
       notification_message AS [Notification Message],
       include_event_description AS [Include Event Description],
       database_name AS [Database Name],
       event_description_keyword AS [Event Description Keyword],
       occurrence_count AS [Occurrence Count],
       count_reset_utc AS [Count Reset],
       job_id AS [Job ID],
       JobName AS [Job Name],
       has_notification AS [Has Notification],
       category_id AS [Category ID],
       performance_condition AS [Performance Condition],
       IsCriticalAlert AS [Is Critical Alert]
FROM dbo.SysAlerts A
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = A.InstanceID)
AND (A.last_occurrence_utc>=@LastOccurrenceFrom OR @LastOccurrenceFrom IS NULL)
AND (A.last_occurrence_utc< @LastOccurrenceTo OR @LastOccurrenceTo IS NULL)
AND (A.IsCriticalAlert=@IsCritical OR @IsCritical IS NULL)
AND (A.ShowInSummary=1 OR @ShowHidden=1)
ORDER BY [Last Occurrence] DESC