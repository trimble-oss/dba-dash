CREATE PROC dbo.ServiceBroker_Upd(
	@ServiceBrokerQueues dbo.ServiceBrokerQueues READONLY,
	@ServiceBrokerTransmissionQueue dbo.ServiceBrokerTransmissionQueue READONLY,
	@ServiceBrokerConversations dbo.ServiceBrokerConversations READONLY,
	@ServiceBrokerEndpoint dbo.ServiceBrokerEndpoint READONLY,
	@ServiceBrokerCounters dbo.ServiceBrokerCounters READONLY,
	@DatabaseNames dbo.DatabaseNames READONLY, /* Databases this collection visited but could not read */
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
/*
	Imports the Service Broker collection.  All the tables describe one snapshot so they are written together.
	- Queues: current state is updated and depth added to history.  Queues no longer found are marked inactive.
	  status_changed_date records the snapshot where a queue was first seen enabled or disabled after a change (so it's accurate to the
	  collection interval).  It's NULL for a queue that hasn't changed since it was first collected - the time it changed isn't known.
	- Transmission queue, conversations & endpoint: current state is replaced.
	- Counters: cumulative since the instance started.  The previous snapshot is held in Staging.ServiceBrokerCounters and the difference is stored.
	  Instance level counters go to ServiceBrokerStats, per database activation counters to ServiceBrokerDatabaseStats.
	- @DatabaseNames: databases the collection visited but could not read.  Their queues, transmission queue and conversations are left
	  as last collected.  Omitted (or empty) when every visit succeeded - a queue or database missing from the snapshot was dropped
	  or is no longer in scope, and is removed.  Transmission queue and conversation totals are NULL in that snapshot's history.

	Conversation counts in the stats tables exclude closed (CD) endpoints.  A closed endpoint is kept for a while after the conversation ends
	and isn't a sign of a problem, where conversations that are never ended build up in the other states.  Where there were too many endpoints
	to count by state ('??'), the total is used.
*/
SET NOCOUNT ON
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30) = 'ServiceBroker'
DECLARE @HistoryRetentionDays INT

SELECT @HistoryRetentionDays = RetentionDays
FROM dbo.DataRetention
WHERE SchemaName = 'dbo'
AND TableName = 'ServiceBrokerQueueHistory'

SET @HistoryRetentionDays = ISNULL(NULLIF(@HistoryRetentionDays,0),90)

IF EXISTS(	SELECT 1
			FROM dbo.CollectionDates
			WHERE SnapshotDate >= @SnapshotDate
			AND InstanceID = @InstanceID
			AND Reference = @Ref
			)
BEGIN
	/* Already imported, or a newer snapshot has been */
	RETURN
END

SELECT D.DatabaseID,
	Q.schema_name,
	Q.queue_name,
	Q.object_id,
	Q.is_receive_enabled,
	Q.is_enqueue_enabled,
	Q.is_retention_enabled,
	Q.is_activation_enabled,
	Q.is_poison_message_handling_enabled,
	Q.activation_procedure,
	Q.activation_procedure_exists,
	Q.max_readers,
	Q.message_count,
	Q.activated_tasks,
	Q.monitor_state,
	Q.monitor_tasks_waiting,
	Q.monitor_last_activated_time,
	Q.monitor_last_empty_rowset_time,
	Q.oldest_message_time
INTO #Queues
FROM @ServiceBrokerQueues Q
JOIN dbo.Databases D ON D.InstanceID = @InstanceID AND D.name = Q.database_name AND D.IsActive = 1

SELECT D.DatabaseID,
	T.to_service_name,
	T.transmission_status,
	T.message_count,
	T.oldest_enqueue_time
INTO #TransmissionQueue
FROM @ServiceBrokerTransmissionQueue T
JOIN dbo.Databases D ON D.InstanceID = @InstanceID AND D.name = T.database_name AND D.IsActive = 1

SELECT D.DatabaseID,
	C.state,
	SUM(C.conversation_count) AS conversation_count
INTO #Conversations
FROM @ServiceBrokerConversations C
JOIN dbo.Databases D ON D.InstanceID = @InstanceID AND D.name = C.database_name AND D.IsActive = 1
GROUP BY D.DatabaseID,
	C.state

SELECT DISTINCT D.DatabaseID
INTO #FailedDatabases
FROM @DatabaseNames F
JOIN dbo.Databases D ON D.InstanceID = @InstanceID AND D.name = F.database_name AND D.IsActive = 1

/* Change in each counter since the previous snapshot.  A counter going backwards means the instance restarted. */
SELECT C.instance_name,
	C.counter_name,
	C.cntr_value - S.cntr_value AS diff,
	S.SnapshotDate AS PreviousSnapshotDate
INTO #CounterDiff
FROM @ServiceBrokerCounters C
JOIN Staging.ServiceBrokerCounters S ON S.InstanceID = @InstanceID
									AND S.instance_name = C.instance_name
									AND S.counter_name = C.counter_name
WHERE C.cntr_value >= S.cntr_value
AND S.SnapshotDate < @SnapshotDate
AND DATEDIFF(mi,S.SnapshotDate,@SnapshotDate) < 2160

BEGIN TRAN

/*	Queues.  Imports for different instances run in parallel, and each holds its locks until commit.
	A scan of ServiceBrokerQueues reads every instance's rows.  The update and the purge take a U lock
	on each row they examine, so one import waits on the other's locks while holding its own and the two
	deadlock.  A small table makes that scan the cheapest plan, so every access below is forced to seek:
	current rows by the queue's natural key, and rows being deactivated or purged by instance. */
UPDATE SBQ
	SET SBQ.object_id = Q.object_id,
	SBQ.is_receive_enabled = Q.is_receive_enabled,
	SBQ.is_enqueue_enabled = Q.is_enqueue_enabled,
	SBQ.is_retention_enabled = Q.is_retention_enabled,
	SBQ.is_activation_enabled = Q.is_activation_enabled,
	SBQ.is_poison_message_handling_enabled = Q.is_poison_message_handling_enabled,
	SBQ.activation_procedure = Q.activation_procedure,
	SBQ.activation_procedure_exists = Q.activation_procedure_exists,
	SBQ.max_readers = Q.max_readers,
	SBQ.message_count = Q.message_count,
	SBQ.activated_tasks = Q.activated_tasks,
	SBQ.monitor_state = Q.monitor_state,
	SBQ.monitor_tasks_waiting = Q.monitor_tasks_waiting,
	SBQ.monitor_last_activated_time = Q.monitor_last_activated_time,
	SBQ.monitor_last_empty_rowset_time = Q.monitor_last_empty_rowset_time,
	SBQ.oldest_message_time = Q.oldest_message_time,
	/* When the queue was enabled or disabled - compared with the values being replaced */
	SBQ.status_changed_date = CASE WHEN SBQ.is_receive_enabled <> Q.is_receive_enabled OR SBQ.is_enqueue_enabled <> Q.is_enqueue_enabled THEN @SnapshotDate ELSE SBQ.status_changed_date END,
	SBQ.SnapshotDate = @SnapshotDate,
	SBQ.IsActive = 1
FROM dbo.ServiceBrokerQueues SBQ WITH (FORCESEEK (IX_ServiceBrokerQueues_DatabaseID_schema_name_queue_name (DatabaseID, schema_name, queue_name)))
JOIN #Queues Q ON SBQ.DatabaseID = Q.DatabaseID AND SBQ.schema_name = Q.schema_name AND SBQ.queue_name = Q.queue_name

INSERT INTO dbo.ServiceBrokerQueues(
	InstanceID,
	DatabaseID,
	schema_name,
	queue_name,
	object_id,
	is_receive_enabled,
	is_enqueue_enabled,
	is_retention_enabled,
	is_activation_enabled,
	is_poison_message_handling_enabled,
	activation_procedure,
	activation_procedure_exists,
	max_readers,
	message_count,
	activated_tasks,
	monitor_state,
	monitor_tasks_waiting,
	monitor_last_activated_time,
	monitor_last_empty_rowset_time,
	oldest_message_time,
	SnapshotDate,
	IsActive
)
SELECT @InstanceID,
	Q.DatabaseID,
	Q.schema_name,
	Q.queue_name,
	Q.object_id,
	Q.is_receive_enabled,
	Q.is_enqueue_enabled,
	Q.is_retention_enabled,
	Q.is_activation_enabled,
	Q.is_poison_message_handling_enabled,
	Q.activation_procedure,
	Q.activation_procedure_exists,
	Q.max_readers,
	Q.message_count,
	Q.activated_tasks,
	Q.monitor_state,
	Q.monitor_tasks_waiting,
	Q.monitor_last_activated_time,
	Q.monitor_last_empty_rowset_time,
	Q.oldest_message_time,
	@SnapshotDate,
	1
FROM #Queues Q
WHERE NOT EXISTS(	SELECT 1
					FROM dbo.ServiceBrokerQueues SBQ WITH (FORCESEEK (IX_ServiceBrokerQueues_DatabaseID_schema_name_queue_name (DatabaseID, schema_name, queue_name)))
					WHERE SBQ.DatabaseID = Q.DatabaseID
					AND SBQ.schema_name = Q.schema_name
					AND SBQ.queue_name = Q.queue_name
					)

UPDATE SBQ
	SET SBQ.IsActive = 0
FROM dbo.ServiceBrokerQueues SBQ WITH (FORCESEEK (IX_ServiceBrokerQueues_InstanceID_IsActive_SnapshotDate (InstanceID, IsActive)))
WHERE SBQ.InstanceID = @InstanceID
AND SBQ.IsActive = 1
AND NOT EXISTS(	SELECT 1
				FROM #Queues Q
				WHERE SBQ.DatabaseID = Q.DatabaseID
				AND SBQ.schema_name = Q.schema_name
				AND SBQ.queue_name = Q.queue_name
				)
AND NOT EXISTS(	SELECT 1
				FROM #FailedDatabases FD
				WHERE FD.DatabaseID = SBQ.DatabaseID
				)

/*	Remove queues that no longer exist once their history has been purged.
	Some applications create and drop queues frequently.  SqlDependency queues are rolled up by the collection, but others may not be. */
DELETE SBQ
FROM dbo.ServiceBrokerQueues SBQ WITH (FORCESEEK (IX_ServiceBrokerQueues_InstanceID_IsActive_SnapshotDate (InstanceID, IsActive, SnapshotDate)))
WHERE SBQ.InstanceID = @InstanceID
AND SBQ.IsActive = 0
AND SBQ.SnapshotDate < DATEADD(d,-@HistoryRetentionDays,GETUTCDATE())
AND NOT EXISTS(	SELECT 1
				FROM dbo.ServiceBrokerQueueHistory H
				WHERE H.InstanceID = SBQ.InstanceID
				AND H.QueueID = SBQ.QueueID
				)

INSERT INTO dbo.ServiceBrokerQueueHistory(
	InstanceID,
	SnapshotDate,
	QueueID,
	message_count
)
SELECT @InstanceID,
	@SnapshotDate,
	SBQ.QueueID,
	Q.message_count
FROM #Queues Q
JOIN dbo.ServiceBrokerQueues SBQ WITH (FORCESEEK (IX_ServiceBrokerQueues_DatabaseID_schema_name_queue_name (DatabaseID, schema_name, queue_name))) ON SBQ.DatabaseID = Q.DatabaseID AND SBQ.schema_name = Q.schema_name AND SBQ.queue_name = Q.queue_name

/* Transmission queue.  A database that could not be read keeps the rows from the last successful visit. */
DELETE TQ
FROM dbo.ServiceBrokerTransmissionQueue TQ
WHERE TQ.InstanceID = @InstanceID
AND NOT EXISTS(	SELECT 1
				FROM #FailedDatabases FD
				WHERE FD.DatabaseID = TQ.DatabaseID
				)

INSERT INTO dbo.ServiceBrokerTransmissionQueue(
	InstanceID,
	DatabaseID,
	to_service_name,
	transmission_status,
	message_count,
	oldest_enqueue_time,
	SnapshotDate
)
SELECT @InstanceID,
	T.DatabaseID,
	T.to_service_name,
	T.transmission_status,
	T.message_count,
	T.oldest_enqueue_time,
	@SnapshotDate
FROM #TransmissionQueue T

/* Conversations.  Same as the transmission queue - a failed visit does not clear what was collected before. */
DELETE C
FROM dbo.ServiceBrokerConversations C
WHERE C.InstanceID = @InstanceID
AND NOT EXISTS(	SELECT 1
				FROM #FailedDatabases FD
				WHERE FD.DatabaseID = C.DatabaseID
				)

INSERT INTO dbo.ServiceBrokerConversations(
	InstanceID,
	DatabaseID,
	state,
	conversation_count,
	SnapshotDate
)
SELECT @InstanceID,
	C.DatabaseID,
	C.state,
	C.conversation_count,
	@SnapshotDate
FROM #Conversations C

/* Endpoint */
DELETE dbo.ServiceBrokerEndpoint
WHERE InstanceID = @InstanceID

INSERT INTO dbo.ServiceBrokerEndpoint(
	InstanceID,
	endpoint_name,
	state_desc,
	port,
	connection_count,
	connected_count,
	login_failed_count,
	last_activity_time,
	SnapshotDate
)
SELECT TOP(1) @InstanceID,
	E.endpoint_name,
	E.state_desc,
	E.port,
	E.connection_count,
	E.connected_count,
	E.login_failed_count,
	E.last_activity_time,
	@SnapshotDate
FROM @ServiceBrokerEndpoint E

/* Instance level stats */
INSERT INTO dbo.ServiceBrokerStats(
	InstanceID,
	SnapshotDate,
	sample_ms_diff,
	enqueued_messages,
	sends,
	receives,
	dequeued_transmission_queue_messages,
	activation_errors,
	corrupted_messages,
	dropped_messages,
	transaction_rollbacks
)
SELECT @InstanceID,
	@SnapshotDate,
	DATEDIFF(ms,MAX(D.PreviousSnapshotDate),@SnapshotDate),
	SUM(CASE WHEN D.counter_name = 'Enqueued Messages Total' THEN D.diff END),
	SUM(CASE WHEN D.counter_name = 'SQL SEND Total' THEN D.diff END),
	SUM(CASE WHEN D.counter_name = 'SQL RECEIVE Total' THEN D.diff END),
	SUM(CASE WHEN D.counter_name = 'Dequeued TransmissionQ Msgs/sec' THEN D.diff END),
	SUM(CASE WHEN D.counter_name = 'Activation Errors Total' THEN D.diff END),
	SUM(CASE WHEN D.counter_name = 'Corrupted Messages Total' THEN D.diff END),
	SUM(CASE WHEN D.counter_name = 'Dropped Messages Total' THEN D.diff END),
	SUM(CASE WHEN D.counter_name = 'Broker Transaction Rollbacks' THEN D.diff END)
FROM (SELECT 1 AS One) X
LEFT JOIN #CounterDiff D ON D.counter_name IN('Enqueued Messages Total',
											'SQL SEND Total',
											'SQL RECEIVE Total',
											'Dequeued TransmissionQ Msgs/sec',
											'Activation Errors Total',
											'Corrupted Messages Total',
											'Dropped Messages Total',
											'Broker Transaction Rollbacks')

/* Database level stats.  Broker Activation counters are per database, with the database name as the counter instance. */
;WITH Activation AS (
	SELECT DB.DatabaseID,
		DATEDIFF(ms,MAX(D.PreviousSnapshotDate),@SnapshotDate) AS sample_ms_diff,
		SUM(CASE WHEN D.counter_name = 'Stored Procedures Invoked/sec' THEN D.diff END) AS procedures_invoked,
		SUM(CASE WHEN D.counter_name = 'Task Limit Reached/sec' THEN D.diff END) AS task_limit_reached,
		SUM(CASE WHEN D.counter_name = 'Tasks Aborted/sec' THEN D.diff END) AS tasks_aborted
	FROM #CounterDiff D
	JOIN dbo.Databases DB ON DB.InstanceID = @InstanceID AND DB.name = D.instance_name AND DB.IsActive = 1
	WHERE D.counter_name IN('Stored Procedures Invoked/sec','Task Limit Reached/sec','Tasks Aborted/sec')
	GROUP BY DB.DatabaseID
),
DBs AS (
	SELECT DatabaseID FROM #Queues
	UNION
	SELECT DatabaseID FROM #TransmissionQueue
	UNION
	SELECT DatabaseID FROM #Conversations
	UNION
	SELECT DatabaseID FROM Activation
)
INSERT INTO dbo.ServiceBrokerDatabaseStats(
	InstanceID,
	SnapshotDate,
	DatabaseID,
	sample_ms_diff,
	procedures_invoked,
	task_limit_reached,
	tasks_aborted,
	transmission_queue_count,
	conversation_count
)
SELECT @InstanceID,
	@SnapshotDate,
	DBs.DatabaseID,
	A.sample_ms_diff,
	A.procedures_invoked,
	A.task_limit_reached,
	A.tasks_aborted,
	/* NULL, not 0, when this visit didn't read the database - a 0 would be a dip on the chart for data we don't have. */
	CASE WHEN EXISTS(SELECT 1 FROM #FailedDatabases FD WHERE FD.DatabaseID = DBs.DatabaseID) THEN NULL
		ELSE ISNULL((SELECT SUM(T.message_count) FROM #TransmissionQueue T WHERE T.DatabaseID = DBs.DatabaseID),0) END,
	CASE WHEN EXISTS(SELECT 1 FROM #FailedDatabases FD WHERE FD.DatabaseID = DBs.DatabaseID) THEN NULL
		ELSE ISNULL((SELECT SUM(C.conversation_count) FROM #Conversations C WHERE C.DatabaseID = DBs.DatabaseID AND C.state <> 'CD'),0) END
FROM DBs
LEFT JOIN Activation A ON A.DatabaseID = DBs.DatabaseID

/* Keep this snapshot of the counters to diff against next time */
DELETE Staging.ServiceBrokerCounters
WHERE InstanceID = @InstanceID

INSERT INTO Staging.ServiceBrokerCounters(
	InstanceID,
	instance_name,
	counter_name,
	cntr_value,
	SnapshotDate
)
SELECT @InstanceID,
	instance_name,
	counter_name,
	MAX(cntr_value),
	@SnapshotDate
FROM @ServiceBrokerCounters
GROUP BY instance_name,
	counter_name

/* Same transaction as the snapshot. A failure here must roll the history back, or a retry passes the guard above and hits the history primary keys. */
EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
							@Reference = @Ref,
							@SnapshotDate = @SnapshotDate

COMMIT
