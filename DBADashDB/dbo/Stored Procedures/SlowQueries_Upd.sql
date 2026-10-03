CREATE PROC dbo.SlowQueries_Upd(
		@SlowQueries dbo.SlowQueries READONLY,
		@InstanceID INT,
		@SnapshotDate DATETIME2(3)
)
AS
DECLARE @AzureDatabaseID INT 
DECLARE @MinDate DATETIME2(3)
DECLARE @Ref VARCHAR(30)='SlowQueries'
DECLARE @MaxDate DATETIME2(3)

SELECT @MinDate =MIN(timestamp) 
FROM @SlowQueries

SELECT @MaxDate = ISNULL(MAX(timestamp),'19000101')
FROM dbo.SlowQueries
WHERE InstanceID = @InstanceID
AND timestamp>=@MinDate

/*	Events are inserted when they are newer than the latest already stored, which discards anything collected
	twice - a re-read of an event file, or the overlap between the dual ring buffer sessions.  The exception is
	the latest millisecond itself: a later read can return an event from that same millisecond that wasn't
	collected before (it was dispatched after the previous read, or the read stopped part way through it).
	Those are inserted unless an identical event is already stored, numbered after the existing rows so the key
	stays unique.  Only done when the batch has rows in that millisecond, and only reads the rows stored in it. */
DECLARE @Boundary TABLE(
	event_type SYSNAME NOT NULL,
	session_id INT NULL,
	duration BIGINT NULL,
	cpu_time BIGINT NULL,
	logical_reads BIGINT NULL
)
DECLARE @BoundaryUniqueifier SMALLINT = 0

IF EXISTS(SELECT 1 FROM @SlowQueries WHERE timestamp = @MaxDate)
BEGIN
	INSERT INTO @Boundary(event_type, session_id, duration, cpu_time, logical_reads)
	SELECT event_type, session_id, duration, cpu_time, logical_reads
	FROM dbo.SlowQueries
	WHERE InstanceID = @InstanceID
	AND timestamp = @MaxDate

	SELECT @BoundaryUniqueifier = ISNULL(MAX(Uniqueifier),0)
	FROM dbo.SlowQueries
	WHERE InstanceID = @InstanceID
	AND timestamp = @MaxDate
END

/* For AzureDB there is a 1:1 mapping between dbo.Instances and dbo.Databases.  Get the associated DatabaseID */
SELECT @AzureDatabaseID = D.DatabaseID
FROM dbo.Instances I
JOIN dbo.Databases D ON I.InstanceID = D.InstanceID
WHERE I.EngineEdition=5
AND I.InstanceID = @InstanceID
AND I.IsActive=1
AND D.IsActive=1

INSERT INTO dbo.SlowQueries
(
	InstanceID,
	DatabaseID,
	event_type,
	object_name,
	timestamp,
	duration,
	cpu_time,
	logical_reads,
	physical_reads,
	writes,
	username,
	text,
	client_hostname,
	client_app_name,
	result,
	Uniqueifier,
	session_id,
	context_info,
	row_count,
	WorkloadGroupID,
    ResourcePoolID
)
SELECT @InstanceID,
		ISNULL(D.DatabaseID,@AzureDatabaseID), /* For AzureDB, use @AzureDatabaseID if the database_id from the extended event doesn't match for some reason. (Issue #481) */
		event_type,
		object_name,
		timestamp,
		duration,
		cpu_time,
		logical_reads,
		physical_reads,
		writes,
		username,
		ISNULL(batch_text,statement),
		client_hostname,
		client_app_name,
		result,
		ROW_NUMBER() OVER(PARTITION BY timestamp ORDER BY timestamp) -- just to ensure uniqueness in key
			+ CASE WHEN timestamp = @MaxDate THEN @BoundaryUniqueifier ELSE 0 END, -- after rows already stored in the boundary millisecond
		session_id,
		context_info,
		row_count,
		WG.WorkloadGroupID,
		RP.ResourcePoolID
FROM @SlowQueries SQ
LEFT JOIN dbo.Databases D ON D.database_id = SQ.database_id AND D.InstanceID = @InstanceID AND D.IsActive=1
LEFT JOIN dbo.ResourceGovernorWorkloadGroups WG ON WG.group_id = SQ.session_resource_group_id AND WG.InstanceID = @InstanceID AND WG.IsActive=1
LEFT JOIN dbo.ResourceGovernorResourcePools RP ON RP.pool_id = SQ.session_resource_pool_id AND RP.InstanceID = @InstanceID AND RP.IsActive=1
WHERE (timestamp > @MaxDate
	OR (timestamp = @MaxDate
		AND NOT EXISTS(
			/* INTERSECT so NULLs compare as equal */
			SELECT 1
			FROM @Boundary B
			WHERE B.event_type = SQ.event_type
			AND EXISTS(	SELECT B.session_id, B.duration, B.cpu_time, B.logical_reads
						INTERSECT
						SELECT SQ.session_id, SQ.duration, SQ.cpu_time, SQ.logical_reads)
			)
		)
	)

DECLARE @MetricsInstanceID INT 
SELECT @MetricsInstanceID = CASE WHEN EXISTS(SELECT 1 FROM dbo.RepositoryMetricsConfig WHERE InstanceID = @InstanceID AND MetricType='SlowQueries') THEN @InstanceID ELSE -1 END

IF EXISTS(SELECT * 
			FROM dbo.RepositoryMetricsConfig 
			WHERE IsEnabled = 1
			AND IsAggregate = 1
			AND InstanceID = @MetricsInstanceID
			)
BEGIN
	DECLARE @PC dbo.PerformanceCounters
	
	INSERT INTO @PC(SnapshotDate,object_name,counter_name,instance_name,cntr_value,cntr_type)
	SELECT @SnapshotDate AS SnapshotDate,
		'Slow Queries' AS object_name,
		counter_name,
		'' AS instance_name,
		cntr_value,
		65792 AS cntr_type
	FROM (
		SELECT	ISNULL(SUM(CASE WHEN result='2 - Abort' THEN 1 ELSE 0 END),0) AS [Abort Count],
				ISNULL(SUM(CASE WHEN result='1 - Error' THEN 1 ELSE 0 END),0) AS [Error Count],
				COUNT(*) AS [Total Queries]
		FROM @SlowQueries
		) AGG
	UNPIVOT( cntr_value FOR counter_name IN([Abort Count],[Error Count],[Total Queries])
	) U
	WHERE EXISTS(	SELECT 1 
					FROM dbo.RepositoryMetricsConfig M
					WHERE M.MetricName = U.counter_name
					AND M.IsEnabled = 1
					AND M.IsAggregate = 1
					AND M.InstanceID = @MetricsInstanceID
					AND M.MetricType = 'SlowQueries'
					)

	IF EXISTS(SELECT 1 FROM @PC)
	BEGIN
		EXEC dbo.PerformanceCounters_Upd @PerformanceCounters=@PC,
									@InstanceID = @InstanceID,
									@SnapshotDate=@SnapshotDate,
									@Internal=1 /* Don't clear staging table used for other metric types */
	END
END

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										@Reference = @Ref,
										@SnapshotDate = @SnapshotDate