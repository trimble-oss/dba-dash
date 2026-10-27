CREATE VIEW dbo.ServiceBrokerDatabaseInfo
AS
/*
	Service Broker status for each database that has user queues, messages in the transmission queue or conversations.
	DatabaseStatus: 1=Critical, 2=Warning, 4=OK.  It doesn't include the status of the queues in the database - see ServiceBrokerQueueInfo.
	- TransmissionQueueStatus: Age of the oldest message waiting to be sent compared to the thresholds.  Messages stay in the transmission queue
	  if they can't be delivered - check the transmission status for the reason.
	- ConversationStatus: Open conversations compared to the thresholds.  Closed (CD) conversations are excluded.  A large number of conversations
	  usually means they are not being ended - e.g. "fire and forget" where the target ends the conversation but the initiator never does.
	  Where there were too many to count by state (state '??') the total is used.
	- ErrorConversationStatus: Warning if there are conversations in the error (ER) state.  These stay until the application ends them.
	- ActivationStatus: Warning if any activation tasks were aborted in the last hour.  An activation procedure ending with an error.

	Instances where the collection has been disabled are excluded as the data is no longer current.
*/
WITH DBs AS (
	SELECT InstanceID, DatabaseID FROM dbo.ServiceBrokerQueues WHERE IsActive = 1
	UNION
	SELECT InstanceID, DatabaseID FROM dbo.ServiceBrokerTransmissionQueue
	UNION
	SELECT InstanceID, DatabaseID FROM dbo.ServiceBrokerConversations
)
SELECT DBs.InstanceID,
	DBs.DatabaseID,
	I.InstanceDisplayName,
	D.name AS DB,
	D.is_broker_enabled,
	Q.queue_count,
	Q.queue_message_count,
	TQ.transmission_queue_count,
	TQ.transmission_queue_error_count,
	TQ.oldest_enqueue_time,
	Calc.TransmissionQueueAgeMins,
	CV.conversation_count,
	CV.conversing_count,
	CV.disconnected_inbound_count,
	CV.disconnected_outbound_count,
	CV.error_count,
	CV.closed_count,
	ACT.procedures_invoked_1hr,
	ACT.tasks_aborted_1hr,
	ACT.task_limit_reached_1hr,
	THRES.TransmissionQueueAgeWarningThreshold,
	THRES.TransmissionQueueAgeCriticalThreshold,
	THRES.ConversationCountWarningThreshold,
	THRES.ConversationCountCriticalThreshold,
	THRES.ThresholdConfigurationLevel,
	Calc.TransmissionQueueStatus,
	Calc.ConversationStatus,
	Calc.ErrorConversationStatus,
	Calc.ActivationStatus,
	(SELECT MIN(S.Status)
	FROM (VALUES(Calc.TransmissionQueueStatus),(Calc.ConversationStatus),(Calc.ErrorConversationStatus),(Calc.ActivationStatus),(4)) S(Status)
	WHERE S.Status <> 3
	) AS DatabaseStatus,
	/* The issue for each check - used for the alert messages, and combined for Issues */
	Iss.TransmissionQueueIssue,
	Iss.ConversationIssue,
	Iss.ErrorConversationIssue,
	Iss.ActivationIssue,
	STUFF(CONCAT('; ' + Iss.TransmissionQueueIssue, '; ' + Iss.ConversationIssue, '; ' + Iss.ErrorConversationIssue, '; ' + Iss.ActivationIssue),1,2,'') AS Issues,
	SD.SnapshotDate
FROM DBs
JOIN dbo.Instances I ON I.InstanceID = DBs.InstanceID
JOIN dbo.Databases D ON D.DatabaseID = DBs.DatabaseID
LEFT JOIN dbo.CollectionDates SD ON SD.InstanceID = DBs.InstanceID AND SD.Reference = 'ServiceBroker'
OUTER APPLY(SELECT COUNT(*) AS queue_count,
					SUM(SBQ.message_count) AS queue_message_count
			FROM dbo.ServiceBrokerQueues SBQ
			WHERE SBQ.DatabaseID = DBs.DatabaseID
			AND SBQ.IsActive = 1
			) Q
OUTER APPLY(SELECT SUM(T.message_count) AS transmission_queue_count,
					SUM(CASE WHEN T.transmission_status <> '' THEN T.message_count ELSE 0 END) AS transmission_queue_error_count,
					MIN(T.oldest_enqueue_time) AS oldest_enqueue_time,
					/* Age against the snapshot these rows were collected in.  A later instance collection that couldn't read this database leaves them in place. */
					MAX(T.SnapshotDate) AS SnapshotDate
			FROM dbo.ServiceBrokerTransmissionQueue T
			WHERE T.DatabaseID = DBs.DatabaseID
			) TQ
OUTER APPLY(SELECT SUM(CASE WHEN C.state <> 'CD' THEN C.conversation_count ELSE 0 END) AS conversation_count,
					SUM(CASE WHEN C.state = 'CO' THEN C.conversation_count ELSE 0 END) AS conversing_count,
					SUM(CASE WHEN C.state = 'DI' THEN C.conversation_count ELSE 0 END) AS disconnected_inbound_count,
					SUM(CASE WHEN C.state = 'DO' THEN C.conversation_count ELSE 0 END) AS disconnected_outbound_count,
					SUM(CASE WHEN C.state = 'ER' THEN C.conversation_count ELSE 0 END) AS error_count,
					SUM(CASE WHEN C.state = 'CD' THEN C.conversation_count ELSE 0 END) AS closed_count
			FROM dbo.ServiceBrokerConversations C
			WHERE C.DatabaseID = DBs.DatabaseID
			) CV
OUTER APPLY(SELECT SUM(DS.procedures_invoked) AS procedures_invoked_1hr,
					SUM(DS.tasks_aborted) AS tasks_aborted_1hr,
					SUM(DS.task_limit_reached) AS task_limit_reached_1hr
			FROM dbo.ServiceBrokerDatabaseStats DS
			WHERE DS.InstanceID = DBs.InstanceID
			AND DS.DatabaseID = DBs.DatabaseID
			AND DS.SnapshotDate >= DATEADD(mi,-60,SD.SnapshotDate)
			) ACT
OUTER APPLY(SELECT TOP(1) T.TransmissionQueueAgeWarningThreshold,
						T.TransmissionQueueAgeCriticalThreshold,
						T.ConversationCountWarningThreshold,
						T.ConversationCountCriticalThreshold,
						CASE WHEN T.DatabaseID <> -1 THEN 'Database' WHEN T.InstanceID <> -1 THEN 'Instance' ELSE 'Root' END AS ThresholdConfigurationLevel
			FROM dbo.ServiceBrokerThresholds T
			WHERE (T.InstanceID = DBs.InstanceID OR T.InstanceID = -1)
			AND (T.DatabaseID = DBs.DatabaseID OR T.DatabaseID = -1)
			AND T.queue_name = ''
			ORDER BY T.InstanceID DESC, T.DatabaseID DESC
			) THRES
CROSS APPLY(SELECT DATEDIFF(mi,TQ.oldest_enqueue_time,ISNULL(TQ.SnapshotDate,SD.SnapshotDate)) AS TransmissionQueueAgeMins) Age
CROSS APPLY(SELECT Age.TransmissionQueueAgeMins,
				CASE WHEN Age.TransmissionQueueAgeMins >= THRES.TransmissionQueueAgeCriticalThreshold THEN 1
					WHEN Age.TransmissionQueueAgeMins >= THRES.TransmissionQueueAgeWarningThreshold THEN 2
					WHEN THRES.TransmissionQueueAgeWarningThreshold IS NULL AND THRES.TransmissionQueueAgeCriticalThreshold IS NULL THEN 3
					ELSE 4 END AS TransmissionQueueStatus,
				CASE WHEN CV.conversation_count >= THRES.ConversationCountCriticalThreshold THEN 1
					WHEN CV.conversation_count >= THRES.ConversationCountWarningThreshold THEN 2
					WHEN THRES.ConversationCountWarningThreshold IS NULL AND THRES.ConversationCountCriticalThreshold IS NULL THEN 3
					ELSE 4 END AS ConversationStatus,
				CASE WHEN CV.error_count > 0 THEN 2 ELSE 4 END AS ErrorConversationStatus,
				CASE WHEN ACT.tasks_aborted_1hr > 0 THEN 2 ELSE 4 END AS ActivationStatus
			) Calc
CROSS APPLY(SELECT CASE WHEN Calc.TransmissionQueueStatus IN(1,2) THEN CONCAT('Oldest message in the transmission queue has been waiting ', Calc.TransmissionQueueAgeMins, ' mins.  See the transmission status for why it hasn''t been sent.') END AS TransmissionQueueIssue,
				CASE WHEN Calc.ConversationStatus IN(1,2) THEN CONCAT(FORMAT(CV.conversation_count,'N0'), ' open conversations is over the ', CASE WHEN Calc.ConversationStatus = 1 THEN 'critical' ELSE 'warning' END, ' threshold.  Check conversations are being ended.') END AS ConversationIssue,
				CASE WHEN Calc.ErrorConversationStatus = 2 THEN CONCAT(FORMAT(CV.error_count,'N0'), ' conversations in the error state.  These stay until ended with END CONVERSATION.') END AS ErrorConversationIssue,
				CASE WHEN Calc.ActivationStatus = 2 THEN CONCAT(FORMAT(ACT.tasks_aborted_1hr,'N0'), ' activation tasks aborted in the last hour - the activation procedure ended with an error.') END AS ActivationIssue
			) Iss
WHERE I.IsActive = 1
AND D.IsActive = 1
AND NOT EXISTS(	SELECT 1
				FROM dbo.ScheduleInfo SI
				WHERE SI.InstanceID = DBs.InstanceID
				AND SI.Reference = 'ServiceBroker'
				AND SI.IsEnabled = 0
				)
