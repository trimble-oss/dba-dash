CREATE PROC dbo.ResourceGovernorWorkloadGroupsMetrics_Get(
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
		WG.name,
		SUM(M.diff_cpu_usage_ms*1.0) AS cpu_usage_ms,
		SUM(M.diff_cpu_usage_ms*1.0)/SUM(M.PeriodTimeMs) AS cpu_cores,
		SUM(M.diff_cpu_usage_ms*1.0/M.cpu_count)/SUM(M.PeriodTimeMs) AS cpu_percent,
		SUM(M.diff_cpu_usage_ms*1.0)/SUM(SUM(M.diff_cpu_usage_ms)) OVER(PARTITION BY ' + @DateGroupingSQL + ') AS cpu_share_percent,
		SUM(M.diff_request_count)/SUM(M.PeriodTimeMs/60000.0) AS requests_per_min,
		SUM(M.diff_queued_request_count)/SUM(M.PeriodTimeMs/60000.0) AS queued_request_count_per_min,
		SUM(M.diff_cpu_limit_violation_count)/SUM(M.PeriodTimeMs/60000.0) AS cpu_limit_violations_per_min,
		SUM(M.diff_lock_wait_count)/SUM(M.PeriodTimeMs/60000.0) AS lock_waits_per_min,
		SUM(M.diff_lock_wait_time_ms)/SUM(M.PeriodTimeMs/1000.0) AS lock_wait_time_ms_per_sec,
		SUM(M.diff_query_optimization_count)/SUM(M.PeriodTimeMs/60000.0) AS query_optimizations_per_min,
		SUM(M.diff_suboptimal_plan_generation_count)/SUM(M.PeriodTimeMs/60000.0)  AS suboptimal_plan_generation_count_per_min,
		SUM(M.diff_reduced_memgrant_count) /SUM(M.PeriodTimeMs/60000.0) AS reduced_memgrant_count_per_min,
		SUM(M.diff_cpu_usage_preemptive_ms)/SUM(M.PeriodTimeMs/60000.0) AS cpu_usage_preemptive_ms_per_min,
		SUM(M.diff_tempdb_data_limit_violation_count) /SUM(M.PeriodTimeMs/60000.0) AS tempdb_data_limit_violations_per_min,
		AVG(M.active_request_count) AS avg_active_request_count,
		AVG(M.queued_request_count) AS avg_queued_request_count,
		AVG(M.blocked_task_count) AS avg_blocked_task_count,
		AVG(M.active_parallel_thread_count) AS avg_active_parallel_thread_count,
		AVG(M.tempdb_data_space_kb) AS avg_tempdb_data_space_kb
FROM dbo.ResourceGovernorWorkloadGroupsMetrics M 
JOIN dbo.ResourceGovernorWorkloadGroups WG ON M.WorkloadGroupID = WG.WorkloadGroupID AND M.InstanceID = WG.InstanceID
' + CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin= 0 THEN '' ELSE 'CROSS APPLY dbo.DateGroupingMins(M.SnapshotDate,@DateGroupingMin) DG' END + '
WHERE M.InstanceID = @InstanceID
AND M.SnapshotDate >= @FromDate
AND M.SnapshotDate < @ToDate
' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, M.SnapshotDate)) IN (' + @DaysOfWeekCsv + ')' END + '
' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, M.SnapshotDate)) IN(' + @HoursCsv + ')' END + '
GROUP BY ' + @DateGroupingSQL + ', wg.name
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