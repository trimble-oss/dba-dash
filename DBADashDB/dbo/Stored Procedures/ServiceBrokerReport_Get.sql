CREATE PROC dbo.ServiceBrokerReport_Get(
	@InstanceIDs IDs READONLY,
	@DatabaseID INT = NULL,
	@FromDate DATETIME2(2) = NULL, /* UTC */
	@ToDate DATETIME2(2) = NULL, /* UTC */
	@DateGroupingMin INT = NULL,
	@TopN INT = 5,
	@IncludeCritical BIT = 1,
	@IncludeWarning BIT = 1,
	@IncludeNA BIT = 1,
	@IncludeOK BIT = 1
)
AS
/*
	Service Broker report.
	0  Instances - endpoint, activity & errors in the last hour, overall status
	1  Databases - transmission queue, conversations (with counts by state) & activation
	2  Queues
	3  Transmission queue - messages waiting to be sent by target service and status
	4  Queue depth over time - the top @TopN queues and transmission queues (one per database) by maximum depth
	5  Activity over time - per second.  Messages sent & received, summed for the instances selected.  For a database (@DatabaseID), activations.
	6  Open conversations over time - the top @TopN databases by maximum open conversations.  A steady climb is a conversation leak.

	The status filter applies to the queues and databases.  Instances are always shown.
	The collection is off by default.  If it hasn't run for any of the instances, a message is returned instead.
*/
SET NOCOUNT ON

IF NOT EXISTS(	SELECT 1
				FROM dbo.CollectionDates CD
				JOIN @InstanceIDs T ON T.ID = CD.InstanceID
				WHERE CD.Reference = 'ServiceBroker'
				)
BEGIN
	SELECT 'Service Broker collection is disabled by default.  Enable the ServiceBroker collection in the service config tool to see data here.  Every 5 minutes is the suggested schedule.' AS Message
	RETURN
END

SET @ToDate = ISNULL(@ToDate,GETUTCDATE())
SET @FromDate = ISNULL(@FromDate,DATEADD(d,-1,@ToDate))

IF @DateGroupingMin IS NULL
BEGIN
	DECLARE @RangeMins INT = DATEDIFF(mi,@FromDate,@ToDate)
	SET @DateGroupingMin = CASE
		WHEN @RangeMins <= 180 THEN 5
		WHEN @RangeMins <= 1500 THEN 15
		WHEN @RangeMins <= 6000 THEN 60
		WHEN @RangeMins <= 36000 THEN 360
		ELSE 1440 END
END
ELSE IF @DateGroupingMin < 5
BEGIN
	/* Suggested schedule is every 5 minutes.  Smaller buckets would show gaps. */
	SET @DateGroupingMin = 5
END

DECLARE @MultipleInstances BIT = CASE WHEN (SELECT COUNT(*) FROM @InstanceIDs) > 1 THEN 1 ELSE 0 END

/* 0 - Instances */
SELECT II.InstanceID,
	II.InstanceDisplayName,
	II.ServiceBrokerStatus,
	II.Issues,
	II.queue_count,
	II.queue_message_count,
	II.transmission_queue_count,
	II.conversation_count,
	II.sends_1hr,
	II.receives_1hr,
	II.enqueued_messages_1hr,
	II.activation_errors_1hr,
	II.dropped_messages_1hr,
	II.corrupted_messages_1hr,
	II.transaction_rollbacks_1hr,
	II.ErrorStatus,
	II.endpoint_name,
	II.endpoint_state,
	II.endpoint_port,
	II.connection_count,
	II.connected_count,
	II.login_failed_count,
	II.last_activity_time,
	II.EndpointStatus,
	II.SnapshotDate
FROM dbo.ServiceBrokerInstanceInfo II
WHERE EXISTS(SELECT 1 FROM @InstanceIDs T WHERE T.ID = II.InstanceID)
ORDER BY II.ServiceBrokerStatus, II.InstanceDisplayName

/* 1 - Databases */
SELECT DI.InstanceID,
	DI.DatabaseID,
	DI.InstanceDisplayName,
	DI.DB,
	DI.DatabaseStatus,
	DI.Issues,
	DI.is_broker_enabled,
	DI.queue_count,
	DI.queue_message_count,
	DI.transmission_queue_count,
	DI.transmission_queue_error_count,
	DI.TransmissionQueueAgeMins,
	DI.TransmissionQueueStatus,
	DI.conversation_count,
	DI.ConversationStatus,
	DI.conversing_count,
	DI.disconnected_inbound_count,
	DI.disconnected_outbound_count,
	DI.error_count,
	DI.ErrorConversationStatus,
	DI.closed_count,
	DI.procedures_invoked_1hr,
	DI.tasks_aborted_1hr,
	DI.task_limit_reached_1hr,
	DI.ActivationStatus,
	DI.TransmissionQueueAgeWarningThreshold,
	DI.TransmissionQueueAgeCriticalThreshold,
	DI.ConversationCountWarningThreshold,
	DI.ConversationCountCriticalThreshold,
	DI.ThresholdConfigurationLevel,
	DI.SnapshotDate
FROM dbo.ServiceBrokerDatabaseInfo DI
WHERE EXISTS(SELECT 1 FROM @InstanceIDs T WHERE T.ID = DI.InstanceID)
AND (DI.DatabaseID = @DatabaseID OR @DatabaseID IS NULL)
AND DI.DatabaseStatus IN(CASE WHEN @IncludeCritical = 1 THEN 1 ELSE NULL END,
						CASE WHEN @IncludeWarning = 1 THEN 2 ELSE NULL END,
						CASE WHEN @IncludeNA = 1 THEN 3 ELSE NULL END,
						CASE WHEN @IncludeOK = 1 THEN 4 ELSE NULL END)
ORDER BY DI.DatabaseStatus, DI.InstanceDisplayName, DI.DB

/* 2 - Queues */
SELECT QI.QueueID,
	QI.InstanceID,
	QI.DatabaseID,
	QI.InstanceDisplayName,
	QI.DB,
	QI.schema_name,
	QI.queue_name,
	QI.QueueStatus,
	QI.Issues,
	QI.message_count,
	QI.QueueDepthStatus,
	QI.OldestMessageAgeMins,
	QI.QueueAgeStatus,
	QI.is_enabled,
	QI.EnabledStatus,
	QI.status_changed_date,
	QI.is_activation_enabled,
	QI.activation_procedure,
	QI.max_readers,
	QI.activated_tasks,
	QI.ReadersStatus,
	QI.monitor_state,
	QI.monitor_tasks_waiting,
	QI.monitor_last_activated_time,
	QI.monitor_last_empty_rowset_time,
	QI.is_retention_enabled,
	QI.is_poison_message_handling_enabled,
	QI.is_broker_enabled,
	QI.QueueDepthWarningThreshold,
	QI.QueueDepthCriticalThreshold,
	QI.QueueAgeWarningThreshold,
	QI.QueueAgeCriticalThreshold,
	QI.ThresholdConfigurationLevel,
	QI.SnapshotDate
FROM dbo.ServiceBrokerQueueInfo QI
WHERE EXISTS(SELECT 1 FROM @InstanceIDs T WHERE T.ID = QI.InstanceID)
AND (QI.DatabaseID = @DatabaseID OR @DatabaseID IS NULL)
AND QI.QueueStatus IN(CASE WHEN @IncludeCritical = 1 THEN 1 ELSE NULL END,
					CASE WHEN @IncludeWarning = 1 THEN 2 ELSE NULL END,
					CASE WHEN @IncludeNA = 1 THEN 3 ELSE NULL END,
					CASE WHEN @IncludeOK = 1 THEN 4 ELSE NULL END)
ORDER BY QI.QueueStatus, QI.message_count DESC, QI.InstanceDisplayName, QI.DB, QI.schema_name, QI.queue_name

/* 3 - Transmission queue */
SELECT T.InstanceID,
	T.DatabaseID,
	I.InstanceDisplayName,
	D.name AS DB,
	T.to_service_name,
	T.transmission_status,
	T.message_count,
	T.oldest_enqueue_time,
	DATEDIFF(mi,T.oldest_enqueue_time,T.SnapshotDate) AS AgeMins,
	T.SnapshotDate
FROM dbo.ServiceBrokerTransmissionQueue T
JOIN dbo.Instances I ON I.InstanceID = T.InstanceID
JOIN dbo.Databases D ON D.DatabaseID = T.DatabaseID
WHERE EXISTS(SELECT 1 FROM @InstanceIDs IDs WHERE IDs.ID = T.InstanceID)
AND (T.DatabaseID = @DatabaseID OR @DatabaseID IS NULL)
AND I.IsActive = 1
AND D.IsActive = 1
/* As the status views - current state is no longer current once the collection is disabled */
AND NOT EXISTS(	SELECT 1
				FROM dbo.ScheduleInfo SI
				WHERE SI.InstanceID = T.InstanceID
				AND SI.Reference = 'ServiceBroker'
				AND SI.IsEnabled = 0
				)
ORDER BY T.oldest_enqueue_time

/*	4 - Queue depth over time.  Queues and each database's transmission queue are ranked together by maximum depth, so @TopN is the number
	of lines on the chart - one for every database with messages waiting to be sent could otherwise make the legend bigger than the chart. */
CREATE TABLE #DepthSeries(
	QueueID INT NULL, /* NULL for a transmission queue */
	InstanceID INT NOT NULL,
	DatabaseID INT NOT NULL,
	MaxMessageCount BIGINT NOT NULL
)

INSERT INTO #DepthSeries(QueueID,InstanceID,DatabaseID,MaxMessageCount)
SELECT H.QueueID,
	Q.InstanceID,
	Q.DatabaseID,
	MAX(H.message_count)
FROM dbo.ServiceBrokerQueueHistory H
JOIN @InstanceIDs IDs ON IDs.ID = H.InstanceID
JOIN dbo.ServiceBrokerQueues Q ON Q.QueueID = H.QueueID
WHERE H.SnapshotDate >= @FromDate
AND H.SnapshotDate < @ToDate
AND (Q.DatabaseID = @DatabaseID OR @DatabaseID IS NULL)
GROUP BY H.QueueID,
	Q.InstanceID,
	Q.DatabaseID
HAVING MAX(H.message_count) > 0
UNION ALL
SELECT NULL,
	DS.InstanceID,
	DS.DatabaseID,
	MAX(DS.transmission_queue_count)
FROM dbo.ServiceBrokerDatabaseStats DS
JOIN @InstanceIDs IDs ON IDs.ID = DS.InstanceID
WHERE DS.SnapshotDate >= @FromDate
AND DS.SnapshotDate < @ToDate
AND (DS.DatabaseID = @DatabaseID OR @DatabaseID IS NULL)
GROUP BY DS.InstanceID,
	DS.DatabaseID
HAVING MAX(DS.transmission_queue_count) > 0

SELECT TOP(@TopN) QueueID,
	InstanceID,
	DatabaseID
INTO #TopDepthSeries
FROM #DepthSeries
ORDER BY MaxMessageCount DESC

SELECT DG.DateGroup AS SnapshotDate,
	CONCAT(CASE WHEN @MultipleInstances = 1 THEN I.InstanceDisplayName + ' \ ' END, D.name, '.', Q.schema_name, '.', Q.queue_name) AS Queue,
	MAX(H.message_count) AS MessageCount
FROM dbo.ServiceBrokerQueueHistory H
JOIN @InstanceIDs IDs ON IDs.ID = H.InstanceID
JOIN dbo.ServiceBrokerQueues Q ON Q.QueueID = H.QueueID
JOIN dbo.Databases D ON D.DatabaseID = Q.DatabaseID
JOIN dbo.Instances I ON I.InstanceID = Q.InstanceID
CROSS APPLY dbo.DateGroupingMins(H.SnapshotDate,@DateGroupingMin) DG
WHERE H.QueueID IN(SELECT T.QueueID FROM #TopDepthSeries T WHERE T.QueueID IS NOT NULL)
AND H.SnapshotDate >= @FromDate
AND H.SnapshotDate < @ToDate
GROUP BY DG.DateGroup,
	I.InstanceDisplayName,
	D.name,
	Q.schema_name,
	Q.queue_name
UNION ALL
SELECT DG.DateGroup AS SnapshotDate,
	CONCAT('Transmission Queue: ', CASE WHEN @MultipleInstances = 1 THEN I.InstanceDisplayName + ' \ ' END, D.name) AS Queue,
	MAX(DS.transmission_queue_count) AS MessageCount
FROM dbo.ServiceBrokerDatabaseStats DS
JOIN dbo.Databases D ON D.DatabaseID = DS.DatabaseID
JOIN dbo.Instances I ON I.InstanceID = DS.InstanceID
CROSS APPLY dbo.DateGroupingMins(DS.SnapshotDate,@DateGroupingMin) DG
WHERE EXISTS(	SELECT 1
				FROM #TopDepthSeries T
				WHERE T.QueueID IS NULL
				AND T.InstanceID = DS.InstanceID
				AND T.DatabaseID = DS.DatabaseID
				)
AND DS.SnapshotDate >= @FromDate
AND DS.SnapshotDate < @ToDate
GROUP BY DG.DateGroup,
	I.InstanceDisplayName,
	D.name
ORDER BY SnapshotDate

/*	5 - Activity over time.  Each collection's deltas cover sample_ms_diff (since the previous collection), which can be longer than the bucket
	with a longer schedule or a gap in collection.  Rates are calculated per instance from the time sampled, then summed across instances.
	Errors aren't charted - they would be lost at the bottom of the scale next to the message rates and are highlighted in the grids. */
IF @DatabaseID IS NULL
BEGIN
	;WITH Activity AS (
		SELECT DG.DateGroup AS SnapshotDate,
			S.InstanceID,
			SUM(CAST(S.sample_ms_diff AS BIGINT)) AS sample_ms,
			SUM(S.sends) AS sends,
			SUM(S.receives) AS receives,
			SUM(S.enqueued_messages) AS enqueued_messages,
			SUM(S.dequeued_transmission_queue_messages) AS dequeued_transmission_queue_messages
		FROM dbo.ServiceBrokerStats S
		JOIN @InstanceIDs IDs ON IDs.ID = S.InstanceID
		CROSS APPLY dbo.DateGroupingMins(S.SnapshotDate,@DateGroupingMin) DG
		WHERE S.SnapshotDate >= @FromDate
		AND S.SnapshotDate < @ToDate
		AND S.sample_ms_diff > 0
		GROUP BY DG.DateGroup,
			S.InstanceID
	)
	SELECT A.SnapshotDate,
		M.Measure,
		SUM(M.Value * 1000.0 / A.sample_ms) AS ValuePerSec,
		SUM(M.Value) AS Value
	FROM Activity A
	CROSS APPLY(VALUES	('SENDs/sec',A.sends),
						('RECEIVEs/sec',A.receives),
						('Enqueued Messages/sec',A.enqueued_messages),
						('Transmission Queue Sent/sec',A.dequeued_transmission_queue_messages)
				) M(Measure,Value)
	WHERE M.Value IS NOT NULL
	GROUP BY A.SnapshotDate,
		M.Measure
	ORDER BY A.SnapshotDate, M.Measure
END
ELSE
BEGIN
	/* The Broker Statistics counters are instance wide.  Only the activation counters are per database. */
	;WITH Activity AS (
		SELECT DG.DateGroup AS SnapshotDate,
			SUM(CAST(DS.sample_ms_diff AS BIGINT)) AS sample_ms,
			SUM(DS.procedures_invoked) AS procedures_invoked,
			SUM(DS.task_limit_reached) AS task_limit_reached
		FROM dbo.ServiceBrokerDatabaseStats DS
		JOIN @InstanceIDs IDs ON IDs.ID = DS.InstanceID
		CROSS APPLY dbo.DateGroupingMins(DS.SnapshotDate,@DateGroupingMin) DG
		WHERE DS.SnapshotDate >= @FromDate
		AND DS.SnapshotDate < @ToDate
		AND DS.DatabaseID = @DatabaseID
		AND DS.sample_ms_diff > 0
		GROUP BY DG.DateGroup
	)
	SELECT A.SnapshotDate,
		M.Measure,
		M.Value * 1000.0 / A.sample_ms AS ValuePerSec,
		M.Value
	FROM Activity A
	CROSS APPLY(VALUES	('Activations/sec',A.procedures_invoked),
						('Task Limit Reached/sec',A.task_limit_reached)
				) M(Measure,Value)
	WHERE M.Value IS NOT NULL
	ORDER BY A.SnapshotDate, M.Measure
END

/* 6 - Open conversations over time */
SELECT DS.InstanceID,
	DS.DatabaseID,
	MAX(DS.conversation_count) AS MaxConversationCount
INTO #ConversationHistory
FROM dbo.ServiceBrokerDatabaseStats DS
JOIN @InstanceIDs IDs ON IDs.ID = DS.InstanceID
WHERE DS.SnapshotDate >= @FromDate
AND DS.SnapshotDate < @ToDate
AND (DS.DatabaseID = @DatabaseID OR @DatabaseID IS NULL)
GROUP BY DS.InstanceID,
	DS.DatabaseID
HAVING MAX(DS.conversation_count) > 0

SELECT DG.DateGroup AS SnapshotDate,
	CONCAT(CASE WHEN @MultipleInstances = 1 THEN I.InstanceDisplayName + ' \ ' END, D.name) AS DB,
	MAX(DS.conversation_count) AS ConversationCount
FROM dbo.ServiceBrokerDatabaseStats DS
JOIN dbo.Databases D ON D.DatabaseID = DS.DatabaseID
JOIN dbo.Instances I ON I.InstanceID = DS.InstanceID
CROSS APPLY dbo.DateGroupingMins(DS.SnapshotDate,@DateGroupingMin) DG
WHERE EXISTS(	SELECT 1
				FROM (	SELECT TOP(@TopN) CH.DatabaseID
						FROM #ConversationHistory CH
						ORDER BY CH.MaxConversationCount DESC
						) T
				WHERE T.DatabaseID = DS.DatabaseID
				)
AND DS.SnapshotDate >= @FromDate
AND DS.SnapshotDate < @ToDate
GROUP BY DG.DateGroup,
	I.InstanceDisplayName,
	D.name
ORDER BY SnapshotDate
