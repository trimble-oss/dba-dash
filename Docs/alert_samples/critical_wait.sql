SET QUOTED_IDENTIFIER ON
SET NOCOUNT ON
/*********************************************************************
	
	DBA Dash  Critical Wait Alert
	
	This is an example alert that you can configure based on the 
	data collected by DBA Dash.  Send an alert if any critical wait 
	type exceeds the specified threshold.  e.g. RESOURCE_SEMAPHORE
	would indicate that queries are waiting for memory to run.  
	This code should run from a SQL Agent job in the context of 
	the DBA Dash repository database.  DB mail needs to be configured
	to receive alert notifications.  Run the agent job on a suitable 
	schedule.

*********************************************************************/

/*********************************************************************
			Configuration Section					     
**********************************************************************/

DECLARE @Threshold INT = 1000 /* Threshold in ms/sec that will trigger the alert*/
DECLARE @SamplePeriodMins INT=5  /* The period we will evaluate in minutes */
DECLARE @DelayBetweenAlertsMins INT = 10 /* Delay between responses to avoid sending a flood of notifications*/
DECLARE @recipients VARCHAR(MAX)='your_email_here' /* Recipient email address */
DECLARE @Subject VARCHAR(255)= 'DBA Dash Critical Wait Alert' /* Email subject */
/* Enter a tag name and value to alert for instances matching a specific tag.  Leave NULL to alert for all instances */
DECLARE @TagName NVARCHAR(50) -- = 'Role' 
DECLARE @TagValue NVARCHAR(50) -- = 'Production'

/*********************************************************************/

/*	Create a table in tempdb to track when the alert was last generated.  (Change to use a regular DB if you prefer)
	Used to implement a delay between alerts.  Can be shared with other DBA Dash alerts using AlertType 
*/
DECLARE @AlertType VARCHAR(50)='CriticalWait'
IF(OBJECT_ID('tempdb.dbo.DBADash_alerts') IS NULL)
BEGIN
	CREATE TABLE tempdb.dbo.DBADash_alerts(
		AlertType VARCHAR(50) PRIMARY KEY,
		AlertSentDate DATETIME NOT NULL
	)
END
/* Ensure we have a value for the alert type in the table */
IF NOT EXISTS(SELECT 1 FROM tempdb.dbo.DBADash_alerts WHERE AlertType = @AlertType)
BEGIN
	INSERT INTO tempdb.dbo.DBADash_alerts(AlertType,AlertSentDate)
	VALUES(@AlertType,'19000101')
END
DECLARE @LastAlert DATETIME

/* Get the time the alert was last generated and check we are over the minimum delay between alerts */
SELECT @LastAlert = AlertSentDate 
FROM tempdb.dbo.DBADash_alerts
WHERE AlertType = @AlertType

IF @LastAlert > DATEADD(mi,-@DelayBetweenAlertsMins, GETDATE())
BEGIN
	PRINT 'Waiting for ' + CAST(@DelayBetweenAlertsMins AS VARCHAR(MAX)) + 'mins before sending an alert.  Next alert can be sent after ' + CONVERT(VARCHAR,DATEADD(mi,@DelayBetweenAlertsMins,@LastAlert),120) + ' (' + 
	CAST(DATEDIFF(s,GETDATE(),DATEADD(mi,@DelayBetweenAlertsMins,@LastAlert)) AS VARCHAR(MAX)) + ' seconds from now)'
	RETURN
END
/* 
	Get the tag ID that this alert will target. e.g. Use tagging just target production SQL instances
*/
DECLARE @TagID SMALLINT=-1
IF @TagName IS NOT NULL AND @TagValue IS NOT NULL
BEGIN
	SELECT @TagID = TagID 
	FROM dbo.Tags
	WHERE TagName = @TagName
	AND TagValue = @TagValue;
	IF @@ROWCOUNT=0
	BEGIN
		RAISERROR('Tag not found',11,1)
		RETURN
	END
END

DECLARE @CriticalWaits TABLE(
	Instance NVARCHAR(128) NOT NULL,
	WaitType NVARCHAR(60) NOT NULL,
	WaitTimeMsPerSec FLOAT NOT NULL
)

/* Get a list of instances with critical waits that exceed the specified threshold */
INSERT INTO @CriticalWaits(Instance,WaitType,WaitTimeMsPerSec)
SELECT I.InstanceDisplayName as Instance,
		WT.WaitType,
		SUM(W.wait_time_ms)*1000.0 / SUM(W.sample_ms_diff) as WaitTimeMsPerSec
FROM dbo.Waits W 
JOIN dbo.WaitType WT ON WT.WaitTypeID = W.WaitTypeID
JOIN dbo.InstancesMatchingTags(@TagID) I ON W.InstanceID = I.InstanceID
WHERE W.SnapshotDate>=CAST(DATEADD(mi,-@SamplePeriodMins,GETUTCDATE()) AS DATETIME2(2))
AND W.SnapshotDate < CAST(DATEADD(mi,1,GETUTCDATE()) AS DATETIME2(2))
AND WT.IsCriticalWait=1
GROUP BY I.InstanceDisplayName,WT.WaitType
HAVING SUM(W.wait_time_ms)*1000.0 / SUM(W.sample_ms_diff)  > @Threshold
OPTION(RECOMPILE)

/* If we have any instances that exceed the threshold, generate an email notification */
IF @@ROWCOUNT>0
BEGIN
	DECLARE @Msg NVARCHAR(MAX)
	SET @Msg = 'DBA Dash Alert:
	Critical wait detected on the following instances:

	'
	SET @Msg += (
	SELECT '* '+ Instance + ' ' + WaitType + ' ' + FORMAT(WaitTimeMsPerSec,'N1') + ' ms/sec
	'
	FROM @CriticalWaits
	FOR XML PATH(''),TYPE
	).value('.','NVARCHAR(MAX)')

	SET @Msg +='
	Threshold : ' + FORMAT(@Threshold,'N1') + ' ms/sec in last ' + CAST(@SamplePeriodMins AS NVARCHAR(MAX)) + 'mins
	Waiting ' + CAST(@DelayBetweenAlertsMins AS VARCHAR(MAX)) + 'mins before next alert.'

	PRINT @Msg

	EXEC msdb.dbo.sp_send_dbmail @recipients=@recipients,@body=@Msg,@subject=@subject

	UPDATE tempdb.dbo.DBADash_alerts
		SET AlertSentDate = GETDATE()
	WHERE AlertType = @AlertType
END
ELSE
BEGIN
	PRINT 'No critical wait exceeding specified threshold.'
END


