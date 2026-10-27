CREATE VIEW dbo.ServiceBrokerInstanceInfo
AS
/*
	Service Broker status for each instance where the collection has run.
	- EndpointStatus: Warning if there is a Service Broker endpoint that isn't started, or connections have failed to log in.
	- ErrorStatus: Warning if there were corrupted messages in the hour before the last collection.
	  Activation errors and dropped messages are returned for information but don't affect the status:
	  - Activation errors are reported against the database they happened in (ServiceBrokerDatabaseInfo.ActivationStatus).
	  - Dropped messages include duplicate & out of order messages that remote brokers resend as part of normal retries.  A message that can't be
		delivered stays in the sender's transmission queue, which is where the problem shows (ServiceBrokerDatabaseInfo.TransmissionQueueStatus).
	- ServiceBrokerStatus: The overall status for the instance used on the Summary tab.  The worst of the instance, database and queue statuses.
	  Issues explains it - counts of queues and databases with a critical or warning status (detailed in their own grids), then the instance's own issues.
	  3 (N/A) where there are no queues, transmission queue messages, conversations or endpoint to report on.

	Instances where the collection has been disabled are excluded as the data is no longer current.
*/
SELECT I.InstanceID,
	I.InstanceDisplayName,
	CD.SnapshotDate,
	E.endpoint_name,
	E.state_desc AS endpoint_state,
	E.port AS endpoint_port,
	E.connection_count,
	E.connected_count,
	E.login_failed_count,
	E.last_activity_time,
	ST.sends_1hr,
	ST.receives_1hr,
	ST.enqueued_messages_1hr,
	ST.activation_errors_1hr,
	ST.dropped_messages_1hr,
	ST.corrupted_messages_1hr,
	ST.transaction_rollbacks_1hr,
	Q.queue_count,
	Q.queue_message_count,
	DB.transmission_queue_count,
	DB.conversation_count,
	Q.QueueStatus,
	DB.DatabaseStatus,
	Calc.EndpointStatus,
	Calc.ErrorStatus,
	ISNULL((SELECT MIN(S.Status)
			FROM (VALUES(Q.QueueStatus),(DB.DatabaseStatus),(Calc.EndpointStatus),(Calc.ErrorStatus)) S(Status)
			WHERE S.Status <> 3
			),3) AS ServiceBrokerStatus,
	STUFF(CONCAT(
		/* Queue and database issues are detailed in their own views - count them here so the status has an explanation */
		CASE WHEN Q.critical_count > 0 OR Q.warning_count > 0 THEN CONCAT('; Queues: ', STUFF(CONCAT(
				CASE WHEN Q.critical_count > 0 THEN CONCAT(', ', Q.critical_count, ' critical') END,
				CASE WHEN Q.warning_count > 0 THEN CONCAT(', ', Q.warning_count, ' warning') END
				),1,2,''), '.') END,
		CASE WHEN DB.critical_count > 0 OR DB.warning_count > 0 THEN CONCAT('; Databases: ', STUFF(CONCAT(
				CASE WHEN DB.critical_count > 0 THEN CONCAT(', ', DB.critical_count, ' critical') END,
				CASE WHEN DB.warning_count > 0 THEN CONCAT(', ', DB.warning_count, ' warning') END
				),1,2,''), '.') END,
		'; ' + Iss.EndpointIssue,
		'; ' + Iss.ErrorIssue
		),1,2,'') AS Issues,
	/* The instance's own issues - used for the alert messages */
	Iss.EndpointIssue,
	Iss.ErrorIssue
FROM dbo.Instances I
JOIN dbo.CollectionDates CD ON CD.InstanceID = I.InstanceID AND CD.Reference = 'ServiceBroker'
LEFT JOIN dbo.ServiceBrokerEndpoint E ON E.InstanceID = I.InstanceID
OUTER APPLY(SELECT SUM(S.sends) AS sends_1hr,
					SUM(S.receives) AS receives_1hr,
					SUM(S.enqueued_messages) AS enqueued_messages_1hr,
					SUM(S.activation_errors) AS activation_errors_1hr,
					SUM(S.dropped_messages) AS dropped_messages_1hr,
					SUM(S.corrupted_messages) AS corrupted_messages_1hr,
					SUM(S.transaction_rollbacks) AS transaction_rollbacks_1hr
			FROM dbo.ServiceBrokerStats S
			WHERE S.InstanceID = I.InstanceID
			AND S.SnapshotDate >= DATEADD(mi,-60,CD.SnapshotDate)
			) ST
OUTER APPLY(SELECT COUNT(*) AS queue_count,
					SUM(QI.message_count) AS queue_message_count,
					MIN(QI.QueueStatus) AS QueueStatus,
					SUM(CASE WHEN QI.QueueStatus = 1 THEN 1 ELSE 0 END) AS critical_count,
					SUM(CASE WHEN QI.QueueStatus = 2 THEN 1 ELSE 0 END) AS warning_count
			FROM dbo.ServiceBrokerQueueInfo QI
			WHERE QI.InstanceID = I.InstanceID
			) Q
OUTER APPLY(SELECT SUM(DI.transmission_queue_count) AS transmission_queue_count,
					SUM(DI.conversation_count) AS conversation_count,
					MIN(DI.DatabaseStatus) AS DatabaseStatus,
					SUM(CASE WHEN DI.DatabaseStatus = 1 THEN 1 ELSE 0 END) AS critical_count,
					SUM(CASE WHEN DI.DatabaseStatus = 2 THEN 1 ELSE 0 END) AS warning_count
			FROM dbo.ServiceBrokerDatabaseInfo DI
			WHERE DI.InstanceID = I.InstanceID
			) DB
CROSS APPLY(SELECT CASE WHEN E.endpoint_name IS NULL THEN 3
					WHEN E.state_desc <> 'STARTED' OR E.login_failed_count > 0 THEN 2
					ELSE 4 END AS EndpointStatus,
				CASE WHEN ST.corrupted_messages_1hr > 0 THEN 2
					WHEN ST.sends_1hr > 0 OR ST.receives_1hr > 0 OR ST.enqueued_messages_1hr > 0 THEN 4
					ELSE 3 END AS ErrorStatus
			) Calc
CROSS APPLY(SELECT NULLIF(LTRIM(CONCAT(
					CASE WHEN E.endpoint_name IS NOT NULL AND E.state_desc <> 'STARTED' THEN CONCAT('  Service Broker endpoint is ', E.state_desc, '.') END,
					CASE WHEN E.login_failed_count > 0 THEN '  Service Broker endpoint connections have failed to log in.' END
					)),'') AS EndpointIssue,
				CASE WHEN ST.corrupted_messages_1hr > 0 THEN CONCAT(FORMAT(ST.corrupted_messages_1hr,'N0'), ' corrupted messages in the last hour.') END AS ErrorIssue
			) Iss
WHERE I.IsActive = 1
AND NOT EXISTS(	SELECT 1
				FROM dbo.ScheduleInfo SI
				WHERE SI.InstanceID = I.InstanceID
				AND SI.Reference = 'ServiceBroker'
				AND SI.IsEnabled = 0
				)
