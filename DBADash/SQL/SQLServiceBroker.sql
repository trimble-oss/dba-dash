SET NOCOUNT ON
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED
/*
	Service Broker collection.  Returns 6 result sets:
		1. Queues - one row per user queue with its configuration, depth, oldest message and activation state
		2. Transmission queue - messages waiting to be sent, grouped by database, target service and status
		3. Conversations - conversation endpoint counts by database and state
		4. Endpoint - the Service Broker endpoint and its connections (instance level, used for remote messaging)
		5. Counters - cumulative Broker Statistics & Broker Activation counters.  The repository diffs these.
		6. Databases that could not be collected.  The repository leaves their previously collected state in place.

	Only online, accessible databases with Service Broker enabled are visited.  Only catalog views are read unless the database has something to count.
	Databases are returned by name rather than database_id, which can differ between replicas (e.g. a contained availability group).

	Queue depth is taken from sys.partitions for the queue's internal table rather than COUNT(*) on the queue, which would read every message.
	Queues with messages are read for the age of the oldest message waiting (status 1 = ready to receive), up to @MaxQueueScanRows messages per queue
	and @MaxTotalQueueScanRows in total, smallest queues first.
	For queues with RETENTION = ON the internal table also holds messages that have already been received, so the depth is the count of messages
	waiting from that read instead - or NULL if the queue isn't read.

	The transmission queue is counted as there is no cheaper way to get it.  Conversation endpoints are counted by state up to @MaxConversationScanRows.
	Above that (usually conversations that are never ended) the total is taken from sys.partitions and returned with a state of '??'.

	SqlDependency creates a SqlQueryNotificationService-{GUID} queue for each listener and drops it when the listener stops.  These are returned as one
	row per database & schema rather than as individual queues.

	@MaxDatabases (parameter, CollectionConfig.ServiceBrokerMaxDatabases): the collection fails with an error if there are more databases to visit than this.  0 = no limit.

	Times are returned as UTC.  The collection is off by default.
	Column order must match the user defined table types in the repository database.
*/
DECLARE @DBName SYSNAME
DECLARE @SQL NVARCHAR(MAX)
DECLARE @PoisonColumn NVARCHAR(100)
DECLARE @MaxQueueScanRows BIGINT = 100000
DECLARE @MaxTotalQueueScanRows BIGINT = 1000000
DECLARE @TotalQueueScanRows BIGINT = 0
DECLARE @QueueMessageCount BIGINT
DECLARE @MaxConversationScanRows BIGINT = 1000000
DECLARE @DatabaseID INT
DECLARE @ObjectID INT
/* QUOTENAME doubles a closing bracket, so each name can be 258 characters. Two names and the separator need 517. */
DECLARE @QueueName NVARCHAR(517)
DECLARE @ReadyCount BIGINT
DECLARE @OldestMessageTime DATETIME
/* Broker connection times are local to the server and returned as UTC.  Queue monitor and message enqueue times are already UTC. */
DECLARE @UTCOffset INT = DATEDIFF(mi,GETDATE(),GETUTCDATE())

SET @PoisonColumn = CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.service_queues'),'is_poison_message_handling_enabled','ColumnID') IS NULL THEN 'CAST(NULL AS BIT)' ELSE 'q.is_poison_message_handling_enabled' END

CREATE TABLE #DBs(
	name SYSNAME NOT NULL PRIMARY KEY
)

CREATE TABLE #Queues(
	database_id INT NOT NULL,
	object_id INT NOT NULL,
	schema_name NVARCHAR(128) NOT NULL,
	queue_name NVARCHAR(128) NOT NULL,
	is_receive_enabled BIT NOT NULL,
	is_enqueue_enabled BIT NOT NULL,
	is_retention_enabled BIT NOT NULL,
	is_activation_enabled BIT NOT NULL,
	is_poison_message_handling_enabled BIT NULL,
	activation_procedure NVARCHAR(776) NULL,
	activation_procedure_exists BIT NULL,
	max_readers SMALLINT NULL,
	message_count BIGINT NULL,
	oldest_message_time DATETIME NULL,
	is_read BIT NOT NULL DEFAULT(0),
	PRIMARY KEY(database_id,object_id)
)

CREATE TABLE #TransmissionQueue(
	database_id INT NOT NULL,
	to_service_name NVARCHAR(256) NULL,
	transmission_status NVARCHAR(4000) NULL,
	message_count BIGINT NOT NULL,
	oldest_enqueue_time DATETIME NULL
)

CREATE TABLE #Conversations(
	database_id INT NOT NULL,
	state CHAR(2) NOT NULL,
	conversation_count BIGINT NOT NULL
)

/* Visited, but the read failed.  Anything already written for that database is removed so a partial read isn't imported. */
CREATE TABLE #FailedDBs(
	name SYSNAME NOT NULL PRIMARY KEY
)

INSERT INTO #DBs(name)
SELECT name
FROM sys.databases
WHERE state = 0
AND HAS_DBACCESS(name) = 1
AND database_id <> 2 /* tempdb */
AND is_broker_enabled = 1

/* Service Broker doesn't run on an availability group secondary.  The queues will be reported by the primary. */
IF OBJECT_ID('sys.dm_hadr_database_replica_states') IS NOT NULL
BEGIN
	EXEC sp_executesql N'
	DELETE DB
	FROM #DBs DB
	WHERE EXISTS(SELECT 1
				FROM sys.dm_hadr_database_replica_states rs
				JOIN sys.dm_hadr_availability_replica_states ars ON rs.replica_id = ars.replica_id
				WHERE rs.database_id = DB_ID(DB.name)
				AND rs.is_local = 1
				AND ars.role = 2
				)'
END

/* Each database visited costs some catalog reads.  Fail with a clear error rather than timing out on an instance with a very large number of them. */
DECLARE @DatabaseCount INT = (SELECT COUNT(*) FROM #DBs)
IF @MaxDatabases > 0 AND @DatabaseCount > @MaxDatabases
BEGIN
	RAISERROR('Service Broker collection: %d databases with Service Broker enabled is over the limit of %d.  Raise ServiceBrokerMaxDatabases in the service config (0 = no limit) or disable the ServiceBroker collection for this instance.',11,1,@DatabaseCount,@MaxDatabases)
	RETURN
END

DECLARE DBs CURSOR FAST_FORWARD READ_ONLY LOCAL FOR
SELECT name
FROM #DBs

OPEN DBs
FETCH NEXT FROM DBs INTO @DBName

WHILE @@FETCH_STATUS = 0
BEGIN
	/* Captured before the read so a failure can undo rows already inserted for this database. */
	SET @DatabaseID = DB_ID(@DBName)
	SET @SQL = N'USE ' + QUOTENAME(@DBName) + N'
	INSERT INTO #Queues(database_id,object_id,schema_name,queue_name,is_receive_enabled,is_enqueue_enabled,is_retention_enabled,is_activation_enabled,is_poison_message_handling_enabled,activation_procedure,activation_procedure_exists,max_readers,message_count)
	SELECT DB_ID(),
		q.object_id,
		SCHEMA_NAME(q.schema_id),
		q.name,
		q.is_receive_enabled,
		q.is_enqueue_enabled,
		q.is_retention_enabled,
		q.is_activation_enabled,
		' + @PoisonColumn + N',
		q.activation_procedure,
		CASE WHEN q.activation_procedure IS NULL THEN NULL WHEN OBJECT_ID(q.activation_procedure) IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END,
		q.max_readers,
		(SELECT SUM(p.rows)
		FROM sys.internal_tables it
		JOIN sys.partitions p ON p.object_id = it.object_id AND p.index_id IN(0,1)
		WHERE it.parent_object_id = q.object_id
		AND it.internal_type = 201 /* QUEUE_MESSAGES */
		)
	FROM sys.service_queues q
	WHERE q.is_ms_shipped = 0

	IF EXISTS(SELECT 1 FROM sys.transmission_queue)
	BEGIN
		/*	The largest 49 target service & status groups.  The rest are combined, split by whether transmission_status
			is set.  Pending messages have a NULL status, not an empty string.  Combining them with the failures would count
			them as errors.  Totals and the age of the oldest message stay complete either way. */
		;WITH G AS (
			SELECT to_service_name,
				transmission_status,
				COUNT_BIG(*) AS message_count,
				MIN(enqueue_time) AS oldest_enqueue_time,
				ROW_NUMBER() OVER(ORDER BY COUNT_BIG(*) DESC) AS rn,
				COUNT(*) OVER() AS group_count
			FROM sys.transmission_queue
			GROUP BY to_service_name,transmission_status
		),
		Grp AS (
			SELECT *,
				CASE WHEN group_count <= 50 OR rn < 50 THEN rn WHEN ISNULL(transmission_status,N'''') = N'''' THEN 50 ELSE 51 END AS grp
			FROM G
		)
		INSERT INTO #TransmissionQueue(database_id,to_service_name,transmission_status,message_count,oldest_enqueue_time)
		SELECT DB_ID(),
			/* grp 50 is the combined pending bucket only when there are more than 50 groups. With exactly 50 it is the last real group. */
			CASE WHEN MAX(group_count) <= 50 OR grp < 50 THEN MAX(to_service_name) ELSE CONCAT(N''('', COUNT(*), N'' other target service / status groups)'') END,
			CASE WHEN MAX(group_count) <= 50 OR grp < 50 THEN MAX(transmission_status) WHEN grp = 50 THEN N'''' ELSE N''(various)'' END,
			SUM(message_count),
			MIN(oldest_enqueue_time)
		FROM Grp
		GROUP BY grp
	END

	/* sys.conversation_endpoints is backed by sysdercv - one row per endpoint.  Get the total cheaply before deciding whether to count by state. */
	DECLARE @ConversationCount BIGINT
	SELECT @ConversationCount = SUM(p.rows)
	FROM sys.partitions p
	WHERE p.object_id = OBJECT_ID(''sys.sysdercv'')
	AND p.index_id IN(0,1)

	IF @ConversationCount > @MaxConversationScanRows
	BEGIN
		INSERT INTO #Conversations(database_id,state,conversation_count)
		VALUES(DB_ID(),''??'',@ConversationCount)
	END
	ELSE IF EXISTS(SELECT 1 FROM sys.conversation_endpoints)
	BEGIN
		INSERT INTO #Conversations(database_id,state,conversation_count)
		SELECT DB_ID(),
			state,
			COUNT_BIG(*)
		FROM sys.conversation_endpoints
		GROUP BY state
	END
	'
	BEGIN TRY
		EXEC sp_executesql @SQL, N'@MaxConversationScanRows BIGINT', @MaxConversationScanRows
	END TRY
	BEGIN CATCH
		/*	e.g. database went offline or access was lost between building the list and visiting it.  Don't fail the collection for
			other databases, and don't leave a partial read to be imported as if it were complete. */
		DELETE #Queues WHERE database_id = @DatabaseID
		DELETE #TransmissionQueue WHERE database_id = @DatabaseID
		DELETE #Conversations WHERE database_id = @DatabaseID
		INSERT INTO #FailedDBs(name) VALUES(@DBName)
		PRINT 'Error collecting Service Broker data for ' + QUOTENAME(@DBName) + ': ' + ERROR_MESSAGE()
	END CATCH

	FETCH NEXT FROM DBs INTO @DBName
END

CLOSE DBs
DEALLOCATE DBs

/*	Oldest message waiting in each queue with messages.  Reads the queue, so only for queues up to @MaxQueueScanRows, and up to
	@MaxTotalQueueScanRows messages across all the queues - an instance with a lot of databases could otherwise read millions of messages
	every collection.  Smallest queues first so as many as possible get an age.  The large queues left over are flagged by queue depth. */
DECLARE Queues CURSOR FAST_FORWARD READ_ONLY LOCAL FOR
SELECT database_id,
	object_id,
	QUOTENAME(schema_name) + N'.' + QUOTENAME(queue_name),
	message_count
FROM #Queues
WHERE message_count BETWEEN 1 AND @MaxQueueScanRows
AND queue_name NOT LIKE N'SqlQueryNotificationService-%'
ORDER BY message_count

OPEN Queues
FETCH NEXT FROM Queues INTO @DatabaseID,@ObjectID,@QueueName,@QueueMessageCount

WHILE @@FETCH_STATUS = 0
BEGIN
	SET @TotalQueueScanRows += @QueueMessageCount
	IF @TotalQueueScanRows > @MaxTotalQueueScanRows
	BEGIN
		/* Queues are in size order, so the rest are bigger */
		BREAK
	END

	SET @SQL = N'USE ' + QUOTENAME(DB_NAME(@DatabaseID)) + N'
	SELECT @ReadyCount = COUNT_BIG(*),
		@OldestMessageTime = MIN(message_enqueue_time)
	FROM ' + @QueueName + N'
	WHERE status = 1 /* Ready to receive */'
	BEGIN TRY
		EXEC sp_executesql @SQL, N'@ReadyCount BIGINT OUTPUT, @OldestMessageTime DATETIME OUTPUT', @ReadyCount OUTPUT, @OldestMessageTime OUTPUT

		UPDATE #Queues
			SET oldest_message_time = @OldestMessageTime,
			message_count = CASE WHEN is_retention_enabled = 1 THEN @ReadyCount ELSE message_count END,
			is_read = 1
		WHERE database_id = @DatabaseID
		AND object_id = @ObjectID
	END TRY
	BEGIN CATCH
		/* e.g. no SELECT permission on the queue.  The age is left NULL. */
		PRINT 'Error reading Service Broker queue ' + @QueueName + ' in ' + QUOTENAME(DB_NAME(@DatabaseID)) + ': ' + ERROR_MESSAGE()
	END CATCH

	FETCH NEXT FROM Queues INTO @DatabaseID,@ObjectID,@QueueName,@QueueMessageCount
END

CLOSE Queues
DEALLOCATE Queues

/* Retained messages make the depth from sys.partitions meaningless.  Unknown if the queue couldn't be read. */
UPDATE #Queues
	SET message_count = NULL
WHERE is_retention_enabled = 1
AND is_read = 0
AND message_count > 0

/* 1. Queues */
;WITH QD AS (
	SELECT Q.database_id,
		Q.object_id,
		Q.schema_name,
		Q.queue_name,
		Q.is_receive_enabled,
		Q.is_enqueue_enabled,
		Q.is_retention_enabled,
		Q.is_activation_enabled,
		Q.is_poison_message_handling_enabled,
		Q.activation_procedure,
		Q.activation_procedure_exists,
		Q.max_readers,
		Q.message_count,
		ISNULL(T.activated_tasks,0) AS activated_tasks,
		QM.state AS monitor_state,
		QM.tasks_waiting AS monitor_tasks_waiting,
		NULLIF(QM.last_activated_time,'19000101') AS monitor_last_activated_time,
		NULLIF(QM.last_empty_rowset_time,'19000101') AS monitor_last_empty_rowset_time,
		Q.oldest_message_time,
		CASE WHEN Q.queue_name LIKE N'SqlQueryNotificationService-%' THEN 1 ELSE 0 END AS is_sql_dependency
	FROM #Queues Q
	OUTER APPLY(SELECT COUNT(*) AS activated_tasks
				FROM sys.dm_broker_activated_tasks AT
				WHERE AT.database_id = Q.database_id
				AND AT.queue_id = Q.object_id
				) T
	LEFT JOIN sys.dm_broker_queue_monitors QM ON QM.database_id = Q.database_id AND QM.queue_id = Q.object_id
)
SELECT DB_NAME(database_id) AS database_name,
	object_id,
	schema_name,
	queue_name,
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
	oldest_message_time
FROM QD
WHERE is_sql_dependency = 0
UNION ALL
/* SqlDependency queues rolled up.  The worst state of any of them, and their total depth & running readers. */
SELECT DB_NAME(database_id) AS database_name,
	-1 AS object_id,
	schema_name,
	N'SqlQueryNotificationService-*' AS queue_name,
	CAST(MIN(CAST(is_receive_enabled AS INT)) AS BIT),
	CAST(MIN(CAST(is_enqueue_enabled AS INT)) AS BIT),
	CAST(MAX(CAST(is_retention_enabled AS INT)) AS BIT),
	CAST(MAX(CAST(is_activation_enabled AS INT)) AS BIT),
	CAST(MIN(CAST(is_poison_message_handling_enabled AS INT)) AS BIT),
	CASE WHEN MAX(activation_procedure) IS NOT NULL THEN N'SqlQueryNotificationStoredProcedure-*' END AS activation_procedure,
	CAST(MIN(CAST(activation_procedure_exists AS INT)) AS BIT),
	MAX(max_readers),
	SUM(message_count),
	SUM(activated_tasks),
	NULL AS monitor_state,
	NULL AS monitor_tasks_waiting,
	MAX(monitor_last_activated_time),
	NULL AS monitor_last_empty_rowset_time,
	NULL AS oldest_message_time
FROM QD
WHERE is_sql_dependency = 1
GROUP BY database_id,
	schema_name

/* 2. Transmission queue */
SELECT DB_NAME(database_id) AS database_name,
	to_service_name,
	transmission_status,
	message_count,
	oldest_enqueue_time
FROM #TransmissionQueue

/* 3. Conversations */
SELECT DB_NAME(database_id) AS database_name,
	state,
	conversation_count
FROM #Conversations

/* 4. Endpoint */
SELECT E.name AS endpoint_name,
	E.state_desc,
	TE.port,
	C.connection_count,
	C.connected_count,
	C.login_failed_count,
	C.last_activity_time
FROM sys.service_broker_endpoints E
LEFT JOIN sys.tcp_endpoints TE ON TE.endpoint_id = E.endpoint_id
OUTER APPLY(SELECT COUNT(*) AS connection_count,
					SUM(CASE WHEN BC.state_desc = 'CONNECTED' THEN 1 ELSE 0 END) AS connected_count,
					SUM(CASE WHEN BC.login_state_desc LIKE '%FAIL%' OR BC.login_state_desc LIKE '%ERROR%' THEN 1 ELSE 0 END) AS login_failed_count,
					DATEADD(mi,@UTCOffset,MAX(BC.last_activity_time)) AS last_activity_time
			FROM sys.dm_broker_connections BC
			) C

/* 5. Counters.  Cumulative since the instance started - including the /sec counters, which are stored as running totals. */
SELECT RTRIM(instance_name) AS instance_name,
	RTRIM(counter_name) AS counter_name,
	cntr_value
FROM sys.dm_os_performance_counters
WHERE (object_name LIKE '%:Broker Statistics%'
		AND counter_name IN('Enqueued Messages Total',
							'SQL SEND Total',
							'SQL RECEIVE Total',
							'Activation Errors Total',
							'Corrupted Messages Total',
							'Dropped Messages Total',
							'Broker Transaction Rollbacks',
							'Dequeued TransmissionQ Msgs/sec')
		)
	OR (object_name LIKE '%:Broker Activation%'
		AND RTRIM(instance_name) <> '_Total'
		AND counter_name IN('Stored Procedures Invoked/sec',
							'Task Limit Reached/sec',
							'Tasks Aborted/sec')
		)

/* 6. Databases that could not be collected.  Empty when every visit succeeded. */
SELECT name AS database_name
FROM #FailedDBs
