SET QUOTED_IDENTIFIER ON
SET NOCOUNT ON
/*********************************************************************
	
	DBA Dash Blocking Alert
	
	This is an example alert that you can configure based on the 
	data collected by DBA Dash.  Send an alert if blocking exceeds
	a certain wait time and number of blocked sessions.  Based on 
	running query snapshot.
	This code should run from a SQL Agent job in the context of 
	the DBA Dash repository database.  DB mail needs to be configured
	to receive alert notifications.  Run the agent job on a suitable 
	schedule.

*********************************************************************/

/*********************************************************************
			Configuration Section					     
**********************************************************************/

DECLARE @ThresholdMs INT = 60000 /* Threshold in ms that will trigger the alert*/
DECLARE @MinBlockedSessions INT=5 /* Minimum number of blocked sessions required to generate an alert */
DECLARE @DelayBetweenAlertsMins INT = 10 /* Delay between responses to avoid sending a flood of notifications*/
DECLARE @recipients VARCHAR(MAX)='your_email_here' /* Recipient email address */
DECLARE @Subject VARCHAR(255)= 'DBA Dash Blocking Alert' /* Email subject */
/* Enter a tag name and value to alert for instances matching a specific tag.  Leave NULL to alert for all instances */
DECLARE @TagName NVARCHAR(50) -- = 'Role' 
DECLARE @TagValue NVARCHAR(50) -- = 'Production'

/*********************************************************************/

/*	Create a table in tempdb to track when the alert was last generated.  (Change to use a regular DB if you prefer)
	Used to implement a delay between alerts.  Can be shared with other DBA Dash alerts using AlertType 
*/
DECLARE @AlertType VARCHAR(50)='LockWait'
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

DECLARE @Blocking TABLE(
	Instance NVARCHAR(128) NOT NULL,
	BlockedWaitTime BIGINT NOT NULL,
	BlockedSessionCount INT NOT NULL
);

WITH CurrentBlocking AS (
	SELECT	I.InstanceDisplayName,
			BSS.BlockedWaitTime,
			BSS.BlockedSessionCount,
			BSS.SnapshotDateUTC,
			ROW_NUMBER() OVER(PARTITION BY BSS.InstanceID ORDER BY BSS.SnapshotDateUTC DESC) rnum
	FROM dbo.BlockingSnapshotSummary BSS 
	JOIN dbo.InstancesMatchingTags(@TagID) I ON BSS.InstanceID = I.InstanceID
	WHERE BSS.SnapshotDateUTC >=DATEADD(mi,-5,GETUTCDATE())
)
INSERT INTO @Blocking(Instance,BlockedWaitTime, BlockedSessionCount)
SELECT	InstanceDisplayName, 
		BlockedWaitTime,
		BlockedSessionCount 
FROM CurrentBlocking
WHERE rnum=1 -- Latest blocking snapshot
AND BlockedSessionCount>@MinBlockedSessions
AND BlockedWaitTime>@ThresholdMs

IF @@ROWCOUNT>0
BEGIN
	DECLARE @Msg NVARCHAR(MAX)
	SET @Msg = 'DBA Dash Alert:
	Significant blocking detected on the following instances:

	'
	SET @Msg += (
	SELECT '* '+ Instance + ' Sessions Blocked: ' + FORMAT(BlockedSessionCount,'N0') + '.  Wait Time: ' + FORMAT(BlockedWaitTime/1000.0,'N1') + ' seconds
	'
	FROM @Blocking
	FOR XML PATH(''),TYPE
	).value('.','NVARCHAR(MAX)')

	SET @Msg +='
	Threshold : ' + FORMAT(@ThresholdMs,'N1') + ' ms AND ' + CAST(@MinBlockedSessions AS NVARCHAR(MAX)) + ' sessions blocked
	Waiting ' + CAST(@DelayBetweenAlertsMins AS VARCHAR(MAX)) + 'mins before next alert.'

	PRINT @Msg

	EXEC msdb.dbo.sp_send_dbmail @recipients=@recipients,@body=@Msg,@subject=@subject

	UPDATE tempdb.dbo.DBADash_alerts
		SET AlertSentDate = GETDATE()
	WHERE AlertType = @AlertType
END
ELSE
BEGIN
	PRINT 'No significant blocking detected.'
END