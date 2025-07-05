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
	row_count
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
		ROW_NUMBER() OVER(PARTITION BY timestamp ORDER BY timestamp), -- just to ensure uniqueness in key
		session_id,
		context_info,
		row_count
FROM @SlowQueries SQ
LEFT JOIN dbo.Databases D ON D.database_id = SQ.database_id AND D.InstanceID = @InstanceID AND D.IsActive=1
WHERE timestamp > @MaxDate

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