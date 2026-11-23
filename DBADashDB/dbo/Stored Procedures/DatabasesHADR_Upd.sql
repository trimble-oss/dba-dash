CREATE PROC dbo.DatabasesHADR_Upd(
		@DatabasesHADR DatabasesHADR READONLY,
		@InstanceID INT,
		@SnapshotDate DATETIME2(2)
)
AS
SET XACT_ABORT ON;
DECLARE @Ref VARCHAR(30)='DatabasesHADR'
IF NOT EXISTS(	SELECT 1 
				FROM dbo.CollectionDates 
				WHERE SnapshotDate>=@SnapshotDate 
				AND InstanceID = @InstanceID 
				AND Reference=@Ref
			 )
BEGIN
	/*	Map each database_id in the collection to a database in the repository.  Databases are matched by name (the natural key for
		dbo.Databases).  database_id isn't reliable - in a contained AG the DMV reports the node's database_id for the AG's master while
		sys.databases (and dbo.Databases) has master as database_id 1.  Older agents don't collect database_name, so fall back to database_id.
	*/
	DECLARE @DatabaseMap TABLE(
		database_id INT NOT NULL,
		DatabaseID INT NOT NULL,
		name SYSNAME NOT NULL,
		INDEX IX_DatabaseMap CLUSTERED(database_id) /* Not unique - matching on database_id (older agents) can match more than one database, as before */
	);
	INSERT INTO @DatabaseMap(database_id, DatabaseID, name)
	SELECT hadr.database_id, d.DatabaseID, d.name
	FROM (SELECT DISTINCT database_id, database_name FROM @DatabasesHADR WHERE database_name IS NOT NULL) hadr
	JOIN dbo.Databases d ON d.InstanceID = @InstanceID AND d.name = hadr.database_name
	WHERE d.IsActive = 1
	UNION ALL
	SELECT hadr.database_id, d.DatabaseID, d.name
	FROM (SELECT DISTINCT database_id FROM @DatabasesHADR WHERE database_name IS NULL) hadr
	JOIN dbo.Databases d ON d.InstanceID = @InstanceID AND d.database_id = hadr.database_id
	WHERE d.IsActive = 1;

	BEGIN TRAN;

	DELETE hadr
	FROM dbo.DatabasesHADR hadr
	WHERE InstanceID = @InstanceID

	INSERT INTO dbo.DatabasesHADR
	(
		InstanceID,
		DatabaseID,
		group_database_id,
		is_primary_replica,
		synchronization_state,
		synchronization_health,
		is_suspended,
		suspend_reason,
		replica_id,
		group_id,
		is_commit_participant,
		database_state,
		is_local,
		secondary_lag_seconds,
		last_sent_time,
		last_received_time,
		last_hardened_time,
		last_redone_time,
		log_send_queue_size,
		log_send_rate,
		redo_queue_size,
		redo_rate,	
		filestream_send_rate,
		last_commit_time
	)
	SELECT @InstanceID,
		   map.DatabaseID,
		   hadr.group_database_id,
		   hadr.is_primary_replica,
		   hadr.synchronization_state,
		   hadr.synchronization_health,
		   hadr.is_suspended,
		   hadr.suspend_reason,
		   ISNULL(hadr.replica_id,'00000000-0000-0000-0000-000000000000'),
		   hadr.group_id,
		   hadr.is_commit_participant,
		   hadr.database_state,
		   hadr.is_local,
		   hadr.secondary_lag_seconds,
		   hadr.last_sent_time,
		   hadr.last_received_time,
		   hadr.last_hardened_time,
		   hadr.last_redone_time,
		   hadr.log_send_queue_size,
		   hadr.log_send_rate,
		   hadr.redo_queue_size,
		   hadr.redo_rate,	
		   hadr.filestream_send_rate,
		   hadr.last_commit_time
	FROM @DatabasesHADR hadr
	JOIN @DatabaseMap map ON map.database_id = hadr.database_id;

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
	COMMIT;

	DECLARE @MetricsInstanceID INT 
	SELECT @MetricsInstanceID = CASE WHEN EXISTS(SELECT 1 FROM dbo.RepositoryMetricsConfig WHERE InstanceID = @InstanceID AND MetricType='AG') THEN @InstanceID ELSE -1 END

	DECLARE @PerformanceCounters dbo.PerformanceCounters
	IF EXISTS(SELECT 1 
			FROM dbo.RepositoryMetricsConfig 
			WHERE IsEnabled = 1
			AND IsAggregate = 1
			AND InstanceID = @MetricsInstanceID
			)
	BEGIN
		INSERT INTO @PerformanceCounters(
			SnapshotDate,
			object_name,
			counter_name,
			instance_name,
			cntr_value,
			cntr_type
		)
		SELECT	@SnapshotDate,
				'Availability Group Metrics',
				UNPVT.counter_name,
				'',
				UNPVT.cntr_value,
				65792 AS cntr_type
		FROM (	SELECT	CAST(MAX(CASE WHEN HADR.is_primary_replica = 1 THEN -1 WHEN HADR.synchronization_state=2 THEN 0 ELSE ISNULL(DATEDIFF(ss, HADR.last_commit_time, PrimaryHADR.last_commit_time), -2) END) AS DECIMAL(28,9)) [Max Estimated Data Loss (sec)],
						CAST(MAX(CASE WHEN HADR.is_primary_replica = 1 THEN -1 WHEN HADR.redo_queue_size IS NULL THEN -2 WHEN HADR.redo_queue_size = 0 THEN 0 WHEN HADR.redo_rate IS NULL OR HADR.redo_rate = 0 THEN -2 ELSE CAST(HADR.redo_queue_size AS DECIMAL(28,9)) / HADR.redo_rate END) AS DECIMAL(28,9)) AS [Max Estimated Recovery Time (sec)],
						CAST(MAX(HADR.secondary_lag_seconds) AS DECIMAL(28,9)) AS [Max Secondary Lag (sec)],
						CAST(SUM(HADR.redo_queue_size) AS DECIMAL(28,9)) AS [Total Redo Queue Size (KB)],
						CAST(AVG(HADR.redo_queue_size) AS DECIMAL(28,9)) AS [Avg Redo Queue Size (KB)],
						CAST(SUM(HADR.log_send_queue_size) AS DECIMAL(28,9)) AS [Total Log Send Queue Size (KB)],
						CAST(AVG(HADR.log_send_queue_size) AS DECIMAL(28,9)) AS [Avg Log Send Queue Size (KB)]
				FROM @DatabasesHADR HADR
				LEFT JOIN @DatabasesHADR PrimaryHADR ON PrimaryHADR.group_database_id = HADR.group_database_id 
											 AND PrimaryHADR.is_primary_replica = 1
											 AND PrimaryHADR.is_local = 1     
				) HA
		UNPIVOT(cntr_value
				FOR counter_name IN([Max Estimated Data Loss (sec)],
									[Max Estimated Recovery Time (sec)],
									[Max Secondary Lag (sec)],
									[Total Redo Queue Size (KB)],
									[Avg Redo Queue Size (KB)],
									[Total Log Send Queue Size (KB)],
									[Avg Log Send Queue Size (KB)]
									)
		) UNPVT
		WHERE EXISTS(	SELECT 1 
						FROM dbo.RepositoryMetricsConfig M
						WHERE M.MetricName = UNPVT.counter_name
						AND M.IsEnabled = 1
						AND M.IsAggregate = 1
						AND M.InstanceID = @MetricsInstanceID
						AND M.MetricType = 'AG'
					)
	END
	IF EXISTS(SELECT 1 
			FROM dbo.RepositoryMetricsConfig 
			WHERE IsEnabled = 1
			AND IsAggregate = 0
			)
	BEGIN	
		INSERT INTO @PerformanceCounters(
			SnapshotDate,
			object_name,
			counter_name,
			instance_name,
			cntr_value,
			cntr_type
		)
		SELECT	@SnapshotDate,
				'Availability Group Metrics',
				UNPVT.counter_name,
				UNPVT.instance_name,
				UNPVT.cntr_value,
				65792 AS cntr_type
		FROM (	SELECT	CASE WHEN HADR.is_local=1 THEN '' ELSE AR.replica_server_name + ' \ ' END + map.name AS instance_name,
						CAST(CASE WHEN HADR.is_primary_replica = 1 THEN NULL WHEN HADR.synchronization_state=2 THEN 0 ELSE ISNULL(DATEDIFF(ss, HADR.last_commit_time, PrimaryHADR.last_commit_time), -2) END AS DECIMAL(28,9)) [Estimated Data Loss (sec)],
						CAST(CASE WHEN HADR.is_primary_replica = 1 THEN NULL WHEN HADR.redo_queue_size IS NULL THEN -2 WHEN HADR.redo_queue_size = 0 THEN 0 WHEN HADR.redo_rate IS NULL OR HADR.redo_rate = 0 THEN -2 ELSE CAST(HADR.redo_queue_size AS DECIMAL(28,9)) / HADR.redo_rate END AS DECIMAL(28,9)) AS [Estimated Recovery Time (sec)],
						CAST(HADR.secondary_lag_seconds AS DECIMAL(28,9)) AS [Secondary Lag (sec)],
						CAST(HADR.redo_queue_size AS DECIMAL(28,9)) AS [Redo Queue Size (KB)],
						CAST(HADR.log_send_queue_size AS DECIMAL(28,9)) AS [Log Send Queue Size (KB)]
				FROM @DatabasesHADR HADR
				JOIN @DatabaseMap map ON map.database_id = HADR.database_id
				JOIN dbo.AvailabilityReplicas AR ON AR.InstanceID = @InstanceID AND AR.replica_id = HADR.replica_id
				LEFT JOIN @DatabasesHADR PrimaryHADR ON PrimaryHADR.group_database_id = HADR.group_database_id 
											 AND PrimaryHADR.is_primary_replica = 1
											 AND PrimaryHADR.is_local = 1     
				) HA
		UNPIVOT(cntr_value
				FOR counter_name IN([Estimated Data Loss (sec)],
									[Estimated Recovery Time (sec)],
									[Secondary Lag (sec)],
									[Redo Queue Size (KB)],
									[Log Send Queue Size (KB)]
									)
		) UNPVT
		WHERE EXISTS(	SELECT 1 
						FROM dbo.RepositoryMetricsConfig M
						WHERE M.MetricName = UNPVT.counter_name
						AND M.IsEnabled = 1
						AND M.IsAggregate = 0
						AND M.InstanceID = @MetricsInstanceID
						AND M.MetricType = 'AG'
					)
	END
	IF EXISTS(SELECT 1 FROM @PerformanceCounters)
	BEGIN
		EXEC dbo.PerformanceCounters_Upd @PerformanceCounters=@PerformanceCounters,
									@InstanceID = @InstanceID,
									@SnapshotDate=@SnapshotDate,
									@Internal=1 /* Don't clear staging table used for other metric types */
	END
END