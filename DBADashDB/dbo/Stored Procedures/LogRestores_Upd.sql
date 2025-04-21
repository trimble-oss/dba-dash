CREATE PROC dbo.LogRestores_Upd (
	@LogRestores LogRestores READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='LogRestores'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
BEGIN TRAN

	DELETE L 
	FROM dbo.LogRestores  L
	WHERE InstanceID = @InstanceID

	INSERT INTO dbo.LogRestores(
			InstanceID,
			DatabaseID,
			restore_date,
			backup_start_date,
			last_file,
			backup_time_zone
	)
	SELECT	D.InstanceID,
			D.DatabaseID,
			L.restore_date,
			L.backup_start_date,
			L.last_file,
			L.backup_time_zone
	FROM @LogRestores L 
	JOIN dbo.Databases D ON d.name = L.database_name AND D.InstanceID= @InstanceID
	WHERE D.IsActive=1


	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate

	COMMIT

	DECLARE @MetricsInstanceID INT 
	SELECT @MetricsInstanceID = CASE WHEN EXISTS(SELECT 1 FROM dbo.RepositoryMetricsConfig WHERE InstanceID = @InstanceID AND MetricType='LogShipping') THEN @InstanceID ELSE -1 END
	DECLARE @PerformanceCounters dbo.PerformanceCounters
	INSERT INTO @PerformanceCounters(
		SnapshotDate,
		object_name,
		counter_name,
		instance_name,
		cntr_value,
		cntr_type
	)
	SELECT	@SnapshotDate,
					'Log Shipping',
					UNPVT.counter_name,
					'',
					UNPVT.cntr_value,
					65792 AS cntr_type
	FROM (
	SELECT	CAST(MaxTotalTimeBehind AS DECIMAL(28,9)) AS [Max Total Time Behind (min)],
			CAST(MinTotalTimeBehind AS DECIMAL(28,9)) AS [Min Total Time Behind (min)],
			CAST(AvgTotalTimeBehind AS DECIMAL(28,9)) AS [Avg Total Time Behind (min)],
			CAST(MaxLatencyOfLast AS DECIMAL(28,9)) AS [Max Latency Of Last (min)],
			CAST(MinLatencyOfLast AS DECIMAL(28,9)) AS [Min Latency Of Last (min)],
			CAST(AvgLatencyOfLast AS DECIMAL(28,9)) AS [Avg Latency Of Last (min)],
			CAST(MaxTimeSinceLast AS DECIMAL(28,9)) AS [Max Time Since Last (min)],
			CAST(MinTimeSinceLast AS DECIMAL(28,9)) AS [Min Time Since Last (min)],
			CAST(AvgTimeSinceLast AS DECIMAL(28,9)) AS [Avg Time Since Last (min)],
			CAST(CriticalCount AS DECIMAL(28,9)) AS [Critical Count],
			CAST(WarningCount AS DECIMAL(28,9)) AS [Warning Count],
			CAST(LogShippedDBCount AS DECIMAL(28,9)) AS [Log Shipped Database Count]
	FROM dbo.LogShippingStatusSummary
	WHERE InstanceID = @InstanceID
	) AS Summary
	UNPIVOT(cntr_value FOR counter_name IN(
			[Max Total Time Behind (min)],
			[Min Total Time Behind (min)],
			[Avg Total Time Behind (min)],
			[Max Latency Of Last (min)],
			[Min Latency Of Last (min)],
			[Avg Latency Of Last (min)],
			[Max Time Since Last (min)],
			[Min Time Since Last (min)],
			[Avg Time Since Last (min)],
			[Critical Count],
			[Warning Count],
			[Log Shipped Database Count]
			)
	 ) UNPVT
	WHERE EXISTS(	SELECT 1 
				FROM dbo.RepositoryMetricsConfig M
				WHERE M.MetricName = UNPVT.counter_name
				AND M.IsEnabled = 1
				AND M.IsAggregate = 1
				AND M.InstanceID = @MetricsInstanceID
				AND M.MetricType = 'LogShipping'
			)


	IF EXISTS(SELECT 1 FROM @PerformanceCounters)
	BEGIN
		EXEC dbo.PerformanceCounters_Upd @PerformanceCounters=@PerformanceCounters,
									@InstanceID = @InstanceID,
									@SnapshotDate=@SnapshotDate,
									@Internal=1 /* Don't clear staging table used for other metric types */


	END
END