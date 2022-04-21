SET QUOTED_IDENTIFIER ON
SET NOCOUNT ON
/*********************************************************************
	
	DBA Dash Performance Counter Alert
	
	This is an example alert that you can configure based on the 
	data collected by DBA Dash.  Send an alert if a performance counter
	exceeds a specified threshold for the last 3 collections.
	This code should run from a SQL Agent job in the context of 
	the DBA Dash repository database.  DB mail needs to be configured
	to receive alert notifications.  Run the agent job on a suitable 
	schedule.

*********************************************************************/
DECLARE @Thresholds TABLE(
    object_name NVARCHAR(128) NOT NULL,
    counter_name NVARCHAR(128) NOT NULL,
    instance_name NVARCHAR(128) NULL,
	ThresholdFrom DECIMAL(28,9) NULL,
	ThresholdTo DECIMAL(28,9) NULL
);
/*********************************************************************
			Configuration Section					     
**********************************************************************/
DECLARE @DelayBetweenAlertsMins INT = 10 /* Delay between responses to avoid sending a flood of notifications*/
DECLARE @recipients VARCHAR(MAX)='your_email_here' /* Recipient email address */
DECLARE @Subject VARCHAR(255)= 'DBA Dash Performance Counter Alert' /* Email subject */
/* Enter a tag name and value to alert for instances matching a specific tag.  Leave NULL to alert for all instances */
DECLARE @TagName NVARCHAR(50) -- = 'Role' 
DECLARE @TagValue NVARCHAR(50) -- = 'Production'

/* Add thresholds here. Any counter can be used from here:

SELECT object_name,counter_name,instance_name
FROM dbo.Counters
*/
INSERT INTO @Thresholds
(
    object_name,
    counter_name,
    instance_name,
    ThresholdFrom,
    ThresholdTo
)
SELECT 'sys.dm_os_sys_memory','System Low Memory Signal State',null,1,1
UNION ALL
SELECT 'sys.dm_os_nodes','Count of Nodes reporting thread resources low',NULL,1,NULL
UNION ALL
SELECT 'sys.dm_os_sys_memory','Available Physical Memory (KB)',NULL,NULL,524288
UNION ALL
SELECT 'Resource Pool Stats','Pending memory grants count',NULL,1,NULL
UNION ALL
SELECT 'General Statistics','Temp Tables For Destruction',NULL,10000,NULL;

/*********************************************************************/

/*	Create a table in tempdb to track when the alert was last generated.  (Change to use a regular DB if you prefer)
	Used to implement a delay between alerts.  Can be shared with other DBA Dash alerts using AlertType 
*/
DECLARE @AlertType VARCHAR(50)='PerformanceCounterAlert'
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

CREATE TABLE #InstanceThresholds(
    InstanceID INT NOT NULL,
    ConnectionID NVARCHAR(128) NOT NULL,
    CounterID INT NOT NULL,
    Counter NVARCHAR(386) NOT NULL,
    ThresholdFrom DECIMAL(28, 9) NULL,
    ThresholdTo DECIMAL(28, 9) NULL,
	PRIMARY KEY(InstanceID,CounterID)
);

INSERT INTO #InstanceThresholds
(
    InstanceID,
    ConnectionID,
    CounterID,
    Counter,
    ThresholdFrom,
    ThresholdTo
)
SELECT I.InstanceID,
	I.ConnectionID,
	C.CounterID,
	CONCAT(C.object_name,'\' + C.counter_name,'\' + NULLIF(C.instance_name,'')) AS Counter,
	T.ThresholdFrom,
	T.ThresholdTo
FROM dbo.InstancesMatchingTags(@TagID) I 
JOIN dbo.InstanceCounters IC ON IC.InstanceID = I.InstanceID
JOIN dbo.Counters C ON C.CounterID = IC.CounterID
JOIN @Thresholds T ON T.object_name = C.object_name AND T.counter_name = C.counter_name AND (T.instance_name = C.instance_name OR T.instance_name IS NULL)
WHERE I.IsActive=1

DECLARE @ThresholdExceeded TABLE(
	Instance NVARCHAR(128) NOT NULL,
	Counter NVARCHAR(386) NOT NULL,
	CurrentValue DECIMAL(28,9) NOT NULL
);

-- Get instances where thresholds have been exceeded for the last 3 collections
WITH T AS (
	SELECT T.ConnectionID,
		PC.Value,
		T.Counter,	
		T.ThresholdFrom,
		T.ThresholdTo,
		CASE WHEN PC.Value>=ISNULL(T.ThresholdFrom,PC.Value) AND PC.Value<=ISNULL(T.ThresholdTo,PC.Value) THEN 1 ELSE 0 END AS Threshold,
		ROW_NUMBER() OVER(PARTITION BY PC.InstanceID,PC.CounterID ORDER BY PC.SnapshotDate DESC) rnum
	FROM dbo.PerformanceCounters PC
	JOIN #InstanceThresholds T ON T.CounterID = PC.CounterID AND T.InstanceID = PC.InstanceID
	WHERE PC.SnapshotDate >=CAST(DATEADD(mi,-10,GETUTCDATE()) AS DATETIME2(2)) -- Collected in the last 10mins
	AND PC.SnapshotDate < CAST(GETUTCDATE() AS DATETIME2(2))
)
INSERT INTO @ThresholdExceeded
(
    Instance,
    Counter,
    CurrentValue
)
SELECT T.ConnectionID,
		T.Counter,
		MAX(CASE WHEN rnum=1 THEN Value ELSE NULL END) AS CurrentValue
FROM T 
WHERE Threshold = 1
AND rnum<=3
GROUP BY ConnectionID,T.Counter
HAVING COUNT(*)=3 -- Each of the last 3 collections exceeds threshold

IF @@ROWCOUNT>0
BEGIN
	DECLARE @Msg NVARCHAR(MAX)
	SET @Msg = 'DBA Dash Performance Counter Alert
	Performance counter exceeded threshold for last 3 collections
	'
	SET @Msg += (
	SELECT '* '+ Instance + ' ' + Counter + ': ' + CAST(CurrentValue AS VARCHAR(MAX)) + '
	'
	FROM @ThresholdExceeded
	FOR XML PATH(''),TYPE
	).value('.','NVARCHAR(MAX)')

	PRINT @Msg

	EXEC msdb.dbo.sp_send_dbmail @recipients=@recipients,@body=@Msg,@subject=@subject

	UPDATE tempdb.dbo.DBADash_alerts
		SET AlertSentDate = GETDATE()
	WHERE AlertType = @AlertType
END
ELSE
BEGIN
	PRINT 'OK'
END

DROP TABLE #InstanceThresholds