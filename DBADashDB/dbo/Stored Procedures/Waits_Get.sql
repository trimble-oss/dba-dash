CREATE PROC dbo.Waits_Get(
	@InstanceID INT,
	@FromDate DATETIME2(2)=NULL, /* UTC */
	@ToDate DATETIME2(2)=NULL, /* UTC */
	@DateGroupingMin INT=NULL,
	@Top INT=10,
	@WaitType NVARCHAR(60)=NULL,
	@CriticalWaitsOnly BIT=0,
	@Use60MIN BIT=NULL,
	@WaitTypeID INT=NULL,
	@UTCOffset INT=0, /* Used for Hours filter */
	@DaysOfWeek IDs READONLY, /* e.g. 1=Monday. exclude weekends:  1,2,3,4,5.  Filter applied in local timezone (@UTCOffset) */
	@Hours IDs READONLY/* e.g. 9 to 5 :  9,10,11,12,13,14,15,16. Filter applied in local timezone (@UTCOffset)*/
)
AS
SET DATEFIRST 1 /* Start week on Monday */
SET NOCOUNT ON
IF @FromDate IS NULL
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
IF @ToDate IS NULL
	SET @ToDate = GETUTCDATE()
DECLARE @SQL NVARCHAR(MAX)
DECLARE @DateGroupingSQL NVARCHAR(MAX)
DECLARE @DateGroupingJoin NVARCHAR(MAX)
DECLARE @SchedulerCount INT 

SELECT @SchedulerCount = I.scheduler_count
FROM dbo.Instances I WHERE I.InstanceID = @InstanceID

SELECT @DateGroupingSQL= CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin=0 THEN 'W.SnapshotDate'
			ELSE 'DG.DateGroup' END,
		 @DateGroupingJoin = CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin=0 THEN ''
			ELSE 'CROSS APPLY dbo.DateGroupingMins(W.SnapshotDate,@DateGroupingMin) DG' END 

IF @Use60MIN IS NULL
BEGIN
	SELECT @Use60MIN = CASE WHEN @DateGroupingMin<60 THEN 0
						WHEN DATEDIFF(hh,@FromDate,@ToDate)>24 THEN 1
						WHEN DATEPART(mi,@FromDate)+DATEPART(s,@FromDate)+DATEPART(ms,@FromDate)=0 
							AND (DATEPART(mi,@ToDate)+DATEPART(s,@ToDate)+DATEPART(ms,@ToDate)=0 
									OR @ToDate>=DATEADD(s,-2,GETUTCDATE())
								)
						THEN 1
						ELSE 0 END
END
IF @WaitType IS NOT NULL AND @WaitType NOT LIKE '%[%]%' AND @WaitTypeID IS NULL /* Filtering for a specific wait type - get the WaitTypeID */
BEGIN
	SELECT @WaitTypeID = WaitTypeID
	FROM dbo.WaitType 
	WHERE WaitType = @WaitType
END
IF @WaitTypeID IS NOT NULL /* Ignore @WaitType if we have @WaitTypeID */
BEGIN
	SET @WaitType = NULL
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

CREATE TABLE #WaitGrp(
	[Time] DATETIME2(2) NOT NULL,
	WaitTypeID SMALLINT NOT NULL,
	TotalWaitMs BIGINT NOT NULL,
	SignalWaitMs BIGINT NOT NULL,
	WaitingTasksCount BIGINT NOT NULL
)
CREATE TABLE #Time(
	[Time] DATETIME2(2) NOT NULL,
	SampleDurationMs BIGINT NULL
)
/* 
	Wait types with zero wait are not recorded so we use the max of the sample_ms_diff for each wait type/time, then sum for the date grouping.
*/
SET @SQL = '
WITH W AS (
	SELECT W.SnapshotDate,
			MAX(W.sample_ms_diff) AS sample_ms_diff
	FROM dbo.Waits' + CASE WHEN @Use60MIN =1 THEN '_60MIN' ELSE '' END + ' W 	
	WHERE W.SnapshotDate>= @FromDate
	AND W.SnapshotDate < @ToDate
	' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, W.SnapshotDate)) IN (' + @DaysOfWeekCsv + ')' END + '
	' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, W.SnapshotDate)) IN(' + @HoursCsv + ')' END + '
	AND W.InstanceID=@InstanceID
	GROUP BY W.SnapshotDate
)
SELECT ' + @DateGroupingSQL + ' AS [Time],
			SUM(W.sample_ms_diff) AS SampleDurationMs
FROM W
' + @DateGroupingJoin + '
GROUP BY ' + @DateGroupingSQL 

INSERT INTO #Time
(
    Time,
    SampleDurationMs
)
EXEC sp_executesql @SQL,
				N'@FromDate DATETIME2(2),
				@ToDate DATETIME2(2),
				@InstanceID INT,
				@Top INT,
				@DateGroupingMin INT,
				@UTCOffset INT',
				@FromDate,
				@ToDate,
				@InstanceID,
				@Top,
				@DateGroupingMin,
				@UTCOffset;


SET @SQL = N'
SELECT ' + @DateGroupingSQL + ' AS [Time],
			W.WaitTypeID,
			SUM(W.wait_time_ms) AS TotalWaitMs,
			SUM(W.signal_wait_time_ms) AS SignalWaitMs,
			SUM(W.waiting_tasks_count) AS WaitingTasksCount
FROM dbo.Waits' + CASE WHEN @Use60MIN =1 THEN '_60MIN' ELSE '' END + ' W 
' + @DateGroupingJoin + '
JOIN dbo.WaitType WT ON WT.WaitTypeID = W.WaitTypeID
WHERE W.SnapshotDate>= @FromDate
AND W.SnapshotDate < @ToDate
AND W.InstanceID=@InstanceID
' + CASE WHEN @CriticalWaitsOnly=1 THEN 'AND WT.IsCriticalWait=1' ELSE '' END + '
' + CASE WHEN @WaitType IS NULL THEN '' ELSE 'AND WT.WaitType LIKE @WaitType' END + '
' + CASE WHEN @WaitTypeID IS NULL THEN '' ELSE 'AND WT.WaitTypeID = @WaitTypeID' END + '
' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, W.SnapshotDate)) IN (' + @DaysOfWeekCsv + ')' END + '
' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, W.SnapshotDate)) IN(' + @HoursCsv + ')' END + '
AND WT.IsExcluded = 0
GROUP BY W.WaitTypeID, ' + @DateGroupingSQL 

INSERT INTO #WaitGrp([Time],WaitTypeID,TotalWaitMs,SignalWaitMs,WaitingTasksCount)
EXEC sp_executesql @SQL,
					N'@FromDate DATETIME2(2),
					@ToDate DATETIME2(2),
					@InstanceID INT,
					@Top INT,
					@DateGroupingMin INT,
					@WaitType NVARCHAR(60),
					@WaitTypeID INT,
					@UTCOffset INT',
					@FromDate,
					@ToDate,
					@InstanceID,
					@Top,
					@DateGroupingMin,
					@WaitType,
					@WaitTypeID,
					@UTCOffset;

IF @WaitTypeID IS NOT NULL -- Filtering for a specific wait type
BEGIN
	/*	If a wait doesn't accumulate for a time period it won't be present.  
		This query will fill in the blanks with zeros when we are filtering for a specific wait type - otherwise the charts can look misleading.
	*/
	SELECT T.Time,
			WT.WaitType,
			ISNULL(W.TotalWaitMs/1000.0,0) AS TotalWaitSec,
			ISNULL(W.SignalWaitMs/1000.0,0) AS SignalWaitSec,
			ISNULL(W.SignalWaitMs*100.0/NULLIF(W.TotalWaitMs,0),0) AS SignalWaitPct,
			ISNULL(W.TotalWaitMs / (T.SampleDurationMs/1000.0),0) AS WaitTimeMsPerSec,
			ISNULL(W.SignalWaitMs / (T.SampleDurationMs/1000.0),0) AS SignalWaitMsPerSec,
			ISNULL(W.TotalWaitMs / (T.SampleDurationMs/1000.0) / @SchedulerCount,0) AS WaitTimeMsPerCorePerSec,
			ISNULL(W.SignalWaitMs / (T.SampleDurationMs/1000.0) / @SchedulerCount,0) AS SignalWaitMsPerCorePerSec,
			T.SampleDurationMs /1000 SampleDurationSec,
			W.TotalWaitMs*1.0 /ISNULL(NULLIF(W.WaitingTasksCount,0),1)  AS AvgWaitTimeMs,
			ISNULL(W.WaitingTasksCount,0) AS WaitingTasksCount
	FROM #Time T
	CROSS JOIN dbo.WaitType WT 
	LEFT JOIN #WaitGrp W ON W.Time = T.Time AND WT.WaitTypeID = W.WaitTypeID
	WHERE WT.WaitTypeID = @WaitTypeID
	ORDER BY WT.WaitType,T.Time
END;
ELSE
BEGIN
	/* 
		Show the top waits for each time period and group other waits as "{Other}"
	*/
	WITH W AS (
		SELECT *,ROW_NUMBER() OVER(PARTITION BY [Time] ORDER BY T1.TotalWaitMs DESC) rnum
		FROM #WaitGrp T1
	)
	SELECT T.[Time],
		CASE WHEN rnum> @Top THEN '{Other}' WHEN WT.IsCriticalWait=1 THEN '!!'  + WT.WaitType ELSE WT.WaitType END as WaitType,
		SUM(W.TotalWaitMs)/1000.0 AS TotalWaitSec,
        SUM(W.SignalWaitMs)/1000.0 AS SignalWaitSec,
		SUM(W.SignalWaitMs)*100.0 / NULLIF(SUM(W.TotalWaitMs),0) AS SignalWaitPct,
		SUM(W.TotalWaitMs) / SUM(T.SampleDurationMs/1000.0) AS WaitTimeMsPerSec,
		SUM(W.SignalWaitMs) / SUM(T.SampleDurationMs/1000.0) AS SignalWaitMsPerSec,
		SUM(W.TotalWaitMs) / SUM(T.SampleDurationMs/1000.0) / @SchedulerCount AS WaitTimeMsPerCorePerSec,
		SUM(W.SignalWaitMs) / SUM(T.SampleDurationMs/1000.0) / @SchedulerCount AS SignalWaitMsPerCorePerSec,
		SUM(T.SampleDurationMs) /1000 SampleDurationSec,
        SUM(W.TotalWaitMs*1.0) /ISNULL(NULLIF(SUM(WaitingTasksCount),0),1)  AS AvgWaitTimeMs,
		SUM(W.WaitingTasksCount) AS WaitingTasksCount
	FROM W 
	JOIN #Time T ON T.Time = W.Time
	JOIN dbo.WaitType WT ON WT.WaitTypeID = W.WaitTypeID
	GROUP BY T.[Time],CASE WHEN rnum> @Top THEN '{Other}' WHEN WT.IsCriticalWait=1 THEN '!!'  + WT.WaitType ELSE WT.WaitType END
	ORDER BY WaitType,T.Time
END

DROP TABLE #WaitGrp
GO