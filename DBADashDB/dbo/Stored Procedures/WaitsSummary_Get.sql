CREATE PROC dbo.WaitsSummary_Get(
	@InstanceID INT,
	@FromDate DATETIME2(2), /* UTC */
	@ToDate DATETIME2(2), /* UTC */
	@Use60MIN BIT=NULL,
	@UTCOffset INT=0, /* Used for Hours filter */
	@DaysOfWeek IDs READONLY, /* e.g. 1=Monday. exclude weekends:  1,2,3,4,5.  Filter applied in local timezone (@UTCOffset) */
	@Hours IDs READONLY/* e.g. 9 to 5 :  9,10,11,12,13,14,15,16. Filter applied in local timezone (@UTCOffset)*/
)
AS
SET DATEFIRST 1 /* Start week on Monday */
SET NOCOUNT ON

IF @Use60MIN IS NULL
BEGIN
	SELECT @Use60MIN = CASE WHEN DATEDIFF(hh,@FromDate,@ToDate)>24 THEN 1
						WHEN DATEPART(mi,@FromDate)+DATEPART(s,@FromDate)+DATEPART(ms,@FromDate)=0 
							AND (DATEPART(mi,@ToDate)+DATEPART(s,@ToDate)+DATEPART(ms,@ToDate)=0 
									OR @ToDate>=DATEADD(s,-2,GETUTCDATE())
								)
						THEN 1
						ELSE 0 END
END

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

DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT  WT.WaitType,
		WT.Description,
		SUM(W.wait_time_ms)/1000.0 AS TotalWaitSec,
		SUM(W.signal_wait_time_ms)/1000.0 AS SignalWaitSec,
		SUM(W.signal_wait_time_ms*1.0)/NULLIF(SUM(W.wait_time_ms),0) AS SignalWaitPct,
		SUM(W.wait_time_ms)/NULLIF(MAX(SUM(W.sample_ms_diff/1000.0)) OVER(),0) WaitTimeMsPerSec,
		SUM(W.wait_time_ms)/NULLIF(MAX(SUM(W.sample_ms_diff/1000.0)) OVER(),0)/I.scheduler_count WaitTimeMsPerCorePerSec,
		SUM(W.waiting_tasks_count) as WaitingTasksCount,
		SUM(W.wait_time_ms)/ISNULL(NULLIF(SUM(W.waiting_tasks_count),0.0),1.0) AS AvgWaitTimeMs,
		MAX(SUM(CAST(W.sample_ms_diff AS BIGINT))/1000) OVER() SampleDurationSec,
		MAX(CAST(WT.IsCriticalWait AS INT)) CriticalWait,
		I.scheduler_count
FROM dbo.Waits' + CASE WHEN @Use60MIN=1 THEN '_60MIN' ELSE '' END  + ' W
JOIN dbo.WaitType WT ON WT.WaitTypeID = W.WaitTypeID
JOIN dbo.Instances I ON I.InstanceID = W.InstanceID
WHERE W.InstanceID=@InstanceID
AND W.SnapshotDate>=@FromDate
AND W.SnapshotDate<@ToDate
AND WT.IsExcluded=0
' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, W.SnapshotDate)) IN (' + @DaysOfWeekCsv + ')' END + '
' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, W.SnapshotDate)) IN(' + @HoursCsv + ')' END + '
GROUP BY WT.WaitType,
		I.scheduler_count,
		WT.Description
ORDER BY WaitTimeMsPerSec DESC'

EXEC sp_executesql @SQL,
					N'@InstanceID INT,
					@FromDate DATETIME2(2),
					@ToDate DATETIME2(2),
					@UTCOffset INT',
					@InstanceID,
					@FromDate,
					@ToDate,
					@UTCOffset