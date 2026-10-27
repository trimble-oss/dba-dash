CREATE PROC Alert.ServiceBrokerAlert_Upd
AS
/*
	Service Broker alerts from the checks the Service Broker tab shows (ServiceBrokerQueueInfo, ServiceBrokerDatabaseInfo & ServiceBrokerInstanceInfo).
	One alert per check per instance (alert key "ServiceBroker <check>") listing the queues or databases that fail it with their issues, critical first.
	Each check can be turned on or off, and resolves on its own once nothing fails it.  An object starting or stopping failing a check that already has
	an alert updates the alert's message - it's a new notification only if the priority escalates.

	Details - the level to alert at for each check: Off, Critical or WarningOrCritical.  Thresholds come from the Service Broker threshold configuration.
		Queues:
			QueueDisabled		Disabled queue - usually a poison message.  Always critical.  Default Critical.
			QueueActivation		Activation procedure missing (critical with messages waiting), activation disabled with messages waiting or activation
								notified but nothing receiving (warnings).  Default WarningOrCritical.
			QueueAge			Age of the oldest message waiting vs the queue age thresholds.  Default Critical.
			QueueDepth			Messages waiting vs the queue depth thresholds.  Default Critical.
		Databases:
			TransmissionQueue	Age of the oldest message waiting to be sent vs the transmission queue thresholds.  Default Critical.
			OpenConversations	Open conversations vs the conversation thresholds.  Default WarningOrCritical (there's no critical threshold by default).
			ErrorConversations	Conversations in the error state.  Warning.  Default Off.
			AbortedActivations	Activation tasks aborted in the last hour.  Warning.  Default WarningOrCritical.
		Instance (rules without a DatabaseName only):
			Endpoint			Service Broker endpoint not started or failing logins.  Warning.  Default WarningOrCritical.
			CorruptedMessages	Corrupted messages in the last hour.  Warning.  Default WarningOrCritical.
	DatabaseName: Databases to apply to.  Supports LIKE syntax.  Blank for all.
	RecentlyDisabledHours: A queue that was disabled more than this many hours ago (or before it was first collected) is left out of all the checks -
		a queue disabled for months is usually deliberate or already known, where one disabled a minute ago needs attention, and its old messages
		would keep the age alert going.  A queue that was disabled recently during the instance's open Disabled Queues alert stays in until it's
		enabled, so the alert doesn't resolve while it's still disabled.  Default 24, 0 = always.

	Instances where the collection has been disabled are excluded by the views, so their alerts are resolved.
*/
SET NOCOUNT ON
DECLARE @Type VARCHAR(50) = 'ServiceBroker'

/* Check if we have any rules to process */
IF NOT EXISTS(
	SELECT 1
	FROM Alert.Rules
	WHERE Type = @Type
)
BEGIN
	PRINT CONCAT('No rules of type ', @Type, ' to process')
	RETURN;
END
PRINT CONCAT('Processing alerts of type ', @Type)

/* The worst status to alert at for each check: 0 = Off, 1 = Critical, 2 = Warning or critical */
CREATE TABLE #ServiceBrokerApplicable(
	InstanceID INT NOT NULL,
	Priority TINYINT NOT NULL,
	DatabaseName NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
	RecentlyDisabledHours INT NOT NULL,
	QueueDisabled TINYINT NOT NULL,
	QueueActivation TINYINT NOT NULL,
	QueueAge TINYINT NOT NULL,
	QueueDepth TINYINT NOT NULL,
	TransmissionQueue TINYINT NOT NULL,
	OpenConversations TINYINT NOT NULL,
	ErrorConversations TINYINT NOT NULL,
	AbortedActivations TINYINT NOT NULL,
	Endpoint TINYINT NOT NULL,
	CorruptedMessages TINYINT NOT NULL,
	RuleID INT NOT NULL,
	GroupID INT NOT NULL DEFAULT(0)
)

INSERT INTO #ServiceBrokerApplicable(
	InstanceID,
	Priority,
	DatabaseName,
	RecentlyDisabledHours,
	QueueDisabled,
	QueueActivation,
	QueueAge,
	QueueDepth,
	TransmissionQueue,
	OpenConversations,
	ErrorConversations,
	AbortedActivations,
	Endpoint,
	CorruptedMessages,
	RuleID,
	GroupID
)
SELECT I.InstanceID,
	R.Priority,
	NULLIF(JSON_VALUE(R.Details,'$.DatabaseName'),''),
	ISNULL(TRY_CAST(JSON_VALUE(R.Details,'$.RecentlyDisabledHours') AS INT),24),
	Lvl.QueueDisabled,
	Lvl.QueueActivation,
	Lvl.QueueAge,
	Lvl.QueueDepth,
	Lvl.TransmissionQueue,
	Lvl.OpenConversations,
	Lvl.ErrorConversations,
	Lvl.AbortedActivations,
	Lvl.Endpoint,
	Lvl.CorruptedMessages,
	R.RuleID,
	R.GroupID
FROM Alert.Rules R
CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID, R.ApplyToInstanceID, R.AlertKey, R.ApplyToHidden) I
/* The level for each check from Details, or its default.  OPENJSON rather than JSON_VALUE as a JSON path from a column needs SQL 2017. */
CROSS APPLY(SELECT	MAX(CASE WHEN D.CheckName = 'QueueDisabled' THEN L.Level END) AS QueueDisabled,
					MAX(CASE WHEN D.CheckName = 'QueueActivation' THEN L.Level END) AS QueueActivation,
					MAX(CASE WHEN D.CheckName = 'QueueAge' THEN L.Level END) AS QueueAge,
					MAX(CASE WHEN D.CheckName = 'QueueDepth' THEN L.Level END) AS QueueDepth,
					MAX(CASE WHEN D.CheckName = 'TransmissionQueue' THEN L.Level END) AS TransmissionQueue,
					MAX(CASE WHEN D.CheckName = 'OpenConversations' THEN L.Level END) AS OpenConversations,
					MAX(CASE WHEN D.CheckName = 'ErrorConversations' THEN L.Level END) AS ErrorConversations,
					MAX(CASE WHEN D.CheckName = 'AbortedActivations' THEN L.Level END) AS AbortedActivations,
					MAX(CASE WHEN D.CheckName = 'Endpoint' THEN L.Level END) AS Endpoint,
					MAX(CASE WHEN D.CheckName = 'CorruptedMessages' THEN L.Level END) AS CorruptedMessages
			FROM (VALUES('QueueDisabled',1),
						('QueueActivation',2),
						('QueueAge',1),
						('QueueDepth',1),
						('TransmissionQueue',1),
						('OpenConversations',2),
						('ErrorConversations',0),
						('AbortedActivations',2),
						('Endpoint',2),
						('CorruptedMessages',2)
					) D(CheckName,DefaultLevel)
			OUTER APPLY(SELECT J.value
						FROM OPENJSON(ISNULL(R.Details,N'{}')) J
						WHERE J.[key] COLLATE DATABASE_DEFAULT = D.CheckName
						) J
			CROSS APPLY(SELECT CASE CAST(J.value AS NVARCHAR(50)) COLLATE DATABASE_DEFAULT
								WHEN 'Off' THEN 0
								WHEN 'Critical' THEN 1
								WHEN 'WarningOrCritical' THEN 2
								ELSE D.DefaultLevel END AS Level
						) L
			) Lvl
WHERE R.Type = @Type
AND R.IsActive = 1

/* Each queue, database & instance that fails a check, for each rule that applies */
CREATE TABLE #Failed(
	InstanceID INT NOT NULL,
	Priority TINYINT NOT NULL,
	RuleID INT NOT NULL,
	GroupID INT NOT NULL,
	CheckName VARCHAR(50) COLLATE DATABASE_DEFAULT NOT NULL,
	ObjectName NVARCHAR(400) COLLATE DATABASE_DEFAULT NOT NULL,
	Status TINYINT NOT NULL,
	Issue NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NULL
)

/* Queues */
INSERT INTO #Failed(InstanceID,Priority,RuleID,GroupID,CheckName,ObjectName,Status,Issue)
SELECT QI.InstanceID,
	A.Priority,
	A.RuleID,
	A.GroupID,
	C.CheckName,
	Obj.QueueName,
	C.Status,
	C.Issue
FROM dbo.ServiceBrokerQueueInfo QI
JOIN #ServiceBrokerApplicable A ON A.InstanceID = QI.InstanceID
CROSS APPLY(SELECT CONCAT(QI.DB, '.', QI.schema_name, '.', QI.queue_name) AS QueueName) Obj
CROSS APPLY(VALUES	('Disabled Queues', QI.EnabledStatus, QI.DisabledIssue, A.QueueDisabled),
					('Queue Activation', QI.ActivationStatus, QI.ActivationIssue, A.QueueActivation),
					('Queue Oldest Message', QI.QueueAgeStatus, QI.QueueAgeIssue, A.QueueAge),
					('Queue Depth', QI.QueueDepthStatus, QI.QueueDepthIssue, A.QueueDepth)
			) C(CheckName, Status, Issue, MaxStatus)
WHERE C.Status BETWEEN 1 AND C.MaxStatus
AND (QI.DB LIKE A.DatabaseName OR A.DatabaseName IS NULL)
AND NOT (
		/*	Disabled long ago (or before it was first collected).  Unless the instance's Disabled Queues alert is active and the queue
			was disabled recently at some point during it - it was one of the queues that raised it, or was added to it. */
		QI.is_enabled = 0
		AND A.RecentlyDisabledHours > 0
		AND (QI.status_changed_date IS NULL OR QI.status_changed_date < DATEADD(hh,-A.RecentlyDisabledHours,SYSUTCDATETIME()))
		AND NOT EXISTS(	SELECT 1
						FROM Alert.ActiveAlerts AA
						WHERE AA.AlertType = @Type
						AND AA.InstanceID = QI.InstanceID
						AND AA.AlertKey = 'ServiceBroker Disabled Queues'
						AND AA.GroupID = A.GroupID
						AND AA.IsResolved = 0
						AND QI.status_changed_date >= DATEADD(hh,-A.RecentlyDisabledHours,AA.TriggerDate)
						)
	)

/* Databases */
INSERT INTO #Failed(InstanceID,Priority,RuleID,GroupID,CheckName,ObjectName,Status,Issue)
SELECT DI.InstanceID,
	A.Priority,
	A.RuleID,
	A.GroupID,
	C.CheckName,
	DI.DB,
	C.Status,
	C.Issue
FROM dbo.ServiceBrokerDatabaseInfo DI
JOIN #ServiceBrokerApplicable A ON A.InstanceID = DI.InstanceID
CROSS APPLY(VALUES	('Transmission Queue', DI.TransmissionQueueStatus, DI.TransmissionQueueIssue, A.TransmissionQueue),
					('Open Conversations', DI.ConversationStatus, DI.ConversationIssue, A.OpenConversations),
					('Error Conversations', DI.ErrorConversationStatus, DI.ErrorConversationIssue, A.ErrorConversations),
					('Aborted Activations', DI.ActivationStatus, DI.ActivationIssue, A.AbortedActivations)
			) C(CheckName, Status, Issue, MaxStatus)
WHERE C.Status BETWEEN 1 AND C.MaxStatus
AND (DI.DB LIKE A.DatabaseName OR A.DatabaseName IS NULL)

/* Instance */
INSERT INTO #Failed(InstanceID,Priority,RuleID,GroupID,CheckName,ObjectName,Status,Issue)
SELECT II.InstanceID,
	A.Priority,
	A.RuleID,
	A.GroupID,
	C.CheckName,
	'',
	C.Status,
	C.Issue
FROM dbo.ServiceBrokerInstanceInfo II
JOIN #ServiceBrokerApplicable A ON A.InstanceID = II.InstanceID
CROSS APPLY(VALUES	('Endpoint', II.EndpointStatus, II.EndpointIssue, A.Endpoint),
					('Corrupted Messages', II.ErrorStatus, II.ErrorIssue, A.CorruptedMessages)
			) C(CheckName, Status, Issue, MaxStatus)
WHERE C.Status BETWEEN 1 AND C.MaxStatus
AND A.DatabaseName IS NULL /* A rule for some databases isn't about the instance */

/* An object can fail a check for more than one rule (e.g. one for all databases & one for some) - keep the highest priority */
;WITH DeDupe AS (
	SELECT *,
		ROW_NUMBER() OVER(PARTITION BY InstanceID, GroupID, CheckName, ObjectName ORDER BY Priority, RuleID) AS rnum
	FROM #Failed
)
DELETE DeDupe
WHERE rnum > 1;

/*	One alert per check per instance listing what fails it - critical first.  Priority & RuleID come from the same (highest priority) row so the
	rule notes & closure match the alert. */
DECLARE @AlertDetails Alert.AlertDetails

;WITH Ranked AS (
	SELECT InstanceID,
		Priority,
		RuleID,
		GroupID,
		CheckName,
		COUNT(*) OVER(PARTITION BY InstanceID, GroupID, CheckName) AS FailedCount,
		MIN(Status) OVER(PARTITION BY InstanceID, GroupID, CheckName) AS WorstStatus,
		ROW_NUMBER() OVER(PARTITION BY InstanceID, GroupID, CheckName ORDER BY Priority, RuleID) AS rnum
	FROM #Failed
)
INSERT INTO @AlertDetails(
	InstanceID,
	Priority,
	Message,
	AlertKey,
	RuleID,
	GroupID
)
SELECT R.InstanceID,
	R.Priority,
	CONCAT('Service Broker ', R.CheckName, CASE WHEN R.WorstStatus = 1 THEN ' (critical)' ELSE ' (warning)' END,
		CASE WHEN R.CheckName NOT IN('Endpoint','Corrupted Messages') THEN CONCAT(': ', R.FailedCount) END,
		(SELECT CONCAT('
', CASE WHEN F.ObjectName <> '' THEN CONCAT(F.ObjectName, ' - ') END, CASE WHEN F.Status = 1 THEN 'Critical: ' ELSE 'Warning: ' END, F.Issue)
		FROM #Failed F
		WHERE F.InstanceID = R.InstanceID
		AND F.GroupID = R.GroupID
		AND F.CheckName = R.CheckName
		ORDER BY F.Status, F.ObjectName
		FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)')),
	CONCAT('ServiceBroker ', R.CheckName),
	R.RuleID,
	R.GroupID
FROM Ranked R
WHERE R.rnum = 1

EXEC Alert.ActiveAlerts_Upd @AlertDetails = @AlertDetails, @AlertType = @Type
