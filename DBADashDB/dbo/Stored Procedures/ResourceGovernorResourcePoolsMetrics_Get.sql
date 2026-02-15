CREATE PROC dbo.ResourceGovernorResourcePoolsMetrics_Get(
	@InstanceID INT,
	@FromDate DATETIME2(3)=NULL, /* UTC */
	@ToDate DATETIME2(3)=NULL, /* UTC */
	@DateGroupingMin INT=NULL, /* How many minutes to group by.  */
	@UTCOffset INT=0, /* Used for Hours filter */
	@DaysOfWeek IDs READONLY, /* e.g. 1=Monday. exclude weekends:  1,2,3,4,5.  Filter applied in local timezone (@UTCOffset) */
	@Hours IDs READONLY, /* e.g. 9 to 5 :  9,10,11,12,13,14,15,16. Filter applied in local timezone (@UTCOffset)*/
	@Debug BIT=0
)
AS
SET DATEFIRST 1 /* Start week on Monday */
IF @FromDate IS NULL
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
IF @ToDate IS NULL
	SET @ToDate = GETUTCDATE()
DECLARE @SQL NVARCHAR(MAX)
DECLARE @DateGroupingSQL NVARCHAR(MAX)
SELECT @DateGroupingSQL= CASE WHEN @DateGroupingMin = 0 OR @DateGroupingMin IS NULL THEN 'M.SnapshotDate'
			ELSE 'DG.DateGroup' END

/* Generate CSV list from list of integer values (safe from SQL injection compared to passing in a CSV string) */
DECLARE @DaysOfWeekCsv NVARCHAR(MAX)
SELECT @DaysOfWeekCsv =  STUFF((SELECT ',' + CAST(ID AS VARCHAR)
FROM @DaysOfWeek
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')

/* Generate CSV list from list of integer values (safe from SQL injection compared to passing in a CSV string) */
DECLARE @HoursCsv NVARCHAR(MAX)
SELECT @HoursCsv =  STUFF((SELECT ',' + CAST(ID AS VARCHAR)
FROM @Hours
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')

SET @SQL = N'
SELECT ' + @DateGroupingSQL + ' AS SnapshotDate,
		RP.name,
		SUM(M.diff_cpu_usage_ms*1.0) AS cpu_usage_ms,
		SUM(M.diff_cpu_usage_ms*1.0)/SUM(M.PeriodTimeMs) AS cpu_cores,
		SUM(M.diff_cpu_usage_ms*1.0/M.cpu_count)/SUM(M.PeriodTimeMs) AS cpu_percent,
		SUM(M.diff_cpu_usage_ms*1.0)/SUM(SUM(M.diff_cpu_usage_ms)) OVER(PARTITION BY ' + @DateGroupingSQL + ') AS cpu_share_percent,
		(SUM(M.diff_cpu_usage_ms*1.0/M.cpu_count)/SUM(M.PeriodTimeMs)) * 100.0 / NULLIF(MAX(M.cap_cpu_percent),0) AS cpu_cap_utilization_percent,
		SUM(CASE WHEN (M.diff_cpu_usage_ms*1.0/M.cpu_count)/M.PeriodTimeMs * 100.0 >= (M.cap_cpu_percent * 0.95) THEN M.PeriodTimeMs ELSE 0 END) / NULLIF(SUM(M.PeriodTimeMs)*1.0, 0) AS cpu_cap_near_threshold_percent,
		SUM(M.diff_memgrant_count)/SUM(M.PeriodTimeMs/60000.0) AS memgrant_count_per_min,
		SUM(M.diff_memgrant_timeout_count)/SUM(M.PeriodTimeMs/60000.0) AS memgrant_timeout_count_per_min,
		SUM(M.diff_out_of_memory_count)/SUM(M.PeriodTimeMs/60000.0) AS out_of_memory_count_per_min,
		SUM(M.diff_read_io_queued)/SUM(M.PeriodTimeMs/60000.0) AS read_io_queued_per_min,
		SUM(M.diff_read_io_issued)/SUM(M.PeriodTimeMs/60000.0) AS read_io_issued_per_min,
		SUM(M.diff_read_io_completed)/SUM(M.PeriodTimeMs/60000.0) AS read_io_completed_per_min,
		SUM(M.diff_read_io_throttled)/SUM(M.PeriodTimeMs/60000.0) AS read_io_throttled_per_min,
		SUM(M.diff_read_bytes/POWER(1024.0,2))/SUM(M.PeriodTimeMs/1000.0) AS read_mb_per_sec,
		SUM(M.diff_read_io_stall_ms)/SUM(M.PeriodTimeMs/60000.0) AS read_io_stall_ms_per_min,
		SUM(M.diff_read_io_stall_queued_ms)/SUM(M.PeriodTimeMs/60000.0) AS read_io_stall_queued_ms_per_min,
		SUM(M.diff_write_io_queued)/SUM(M.PeriodTimeMs/60000.0) AS write_io_queued_per_min,
		SUM(M.diff_write_io_issued)/SUM(M.PeriodTimeMs/60000.0) AS write_io_issued_per_min,
		SUM(M.diff_write_io_completed)/SUM(M.PeriodTimeMs/60000.0) AS write_io_completed_per_min,
		SUM(M.diff_write_io_throttled)/SUM(M.PeriodTimeMs/60000.0) AS write_io_throttled_per_min,
		SUM(M.diff_write_bytes/POWER(1024.0,2))/SUM(M.PeriodTimeMs/1000.0) AS write_mb_per_sec,
		SUM(M.diff_write_io_stall_ms)/SUM(M.PeriodTimeMs/60000.0) AS write_io_stall_ms_per_min,
		SUM(M.diff_write_io_stall_queued_ms)/SUM(M.PeriodTimeMs/60000.0) AS write_io_stall_queued_ms_per_min,
		SUM(M.diff_io_issue_delay_ms)/SUM(M.PeriodTimeMs/60000.0) AS io_issue_delay_ms_per_min,
		SUM(M.diff_io_issue_delay_non_throttled_ms)/SUM(M.PeriodTimeMs/60000.0) AS io_issue_delay_non_throttled_ms_per_min,
		SUM(M.diff_cpu_delayed_ms)/SUM(M.PeriodTimeMs/60000.0) AS cpu_delayed_ms_per_min,
		SUM(M.diff_cpu_active_ms)/SUM(M.PeriodTimeMs/60000.0) AS cpu_active_ms_per_min,
		SUM(M.diff_cpu_violation_delay_ms)/SUM(M.PeriodTimeMs/60000.0) AS cpu_violation_delay_ms_per_min,
		SUM(M.diff_cpu_violation_sec)/SUM(M.PeriodTimeMs/60000.0) AS cpu_violation_sec_per_min,
		SUM(M.diff_cpu_usage_preemptive_ms)/SUM(M.PeriodTimeMs/60000.0) AS cpu_usage_preemptive_ms_per_min
FROM dbo.ResourceGovernorResourcePoolsMetrics M 
JOIN dbo.ResourceGovernorResourcePools RP ON M.ResourcePoolID = RP.ResourcePoolID AND M.InstanceID = RP.InstanceID
' + CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin= 0 THEN '' ELSE 'CROSS APPLY dbo.DateGroupingMins(M.SnapshotDate,@DateGroupingMin) DG' END + '
WHERE M.InstanceID = @InstanceID
AND M.SnapshotDate >= @FromDate
AND M.SnapshotDate < @ToDate
' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, M.SnapshotDate)) IN (' + @DaysOfWeekCsv + ')' END + '
' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, M.SnapshotDate)) IN(' + @HoursCsv + ')' END + '
GROUP BY ' + @DateGroupingSQL + ', RP.name
ORDER BY SnapshotDate'

IF @Debug = 1
BEGIN
	PRINT @SQL
END

EXEC sp_executesql @SQL,
				N'@InstanceID INT,
				@FromDate DATETIME2,
				@ToDate DATETIME2,
				@DateGroupingMin INT,
				@UTCOffset INT',
				@InstanceID,
				@FromDate,
				@ToDate,
				@DateGroupingMin,
				@UTCOffset