CREATE PROC dbo.PerformanceSummary_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL,
	@TagIDs VARCHAR(MAX)=NULL,
	@Use60MIN BIT=NULL,
	@Debug BIT=0,
	@DaysOfWeek IDs READONLY, /* e.g. exclude weekends:  Monday,Tuesday,Wednesday,Thursday,Friday. Filter applied in local timezone (@UTCOffset) */
	@Hours IDs READONLY, /* e.g. 9 to 5 :  9,10,11,12,13,14,15,16. Filter applied in local timezone (@UTCOffset)  */
	@UTCOffset INT=0 /* Used for filtering on hours & weekday in current timezone */
)
AS
SET DATEFIRST 1 /* Start week on Monday */
IF @FromDate IS NULL
	SET @FromDate = DATEADD(mi,-15,GETUTCDATE())
IF @ToDate IS NULL
	SET @ToDate=GETUTCDATE()

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
CREATE TABLE #Instances(
	InstanceID INT PRIMARY KEY
)

IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO #Instances
	(
		InstanceID
	)
	SELECT InstanceID
	FROM dbo.InstancesMatchingTags(@TagIDs)
END 
ELSE 
BEGIN
	INSERT INTO #Instances
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')

END;

DECLARE @DaysOfWeekCsv NVARCHAR(MAX)
SELECT @DaysOfWeekCsv =  STUFF((SELECT ',' + CAST(ID AS VARCHAR)
FROM @DaysOfWeek
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')

DECLARE @HoursCsv NVARCHAR(MAX)
SELECT @HoursCsv =  STUFF((SELECT ',' + CAST(ID AS VARCHAR)
FROM @Hours
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')

DECLARE @SQL NVARCHAR(MAX) 
SET @SQL = CAST(N'' AS NVARCHAR(MAX)) + N'
WITH cpuAgg AS (
	SELECT InstanceID,
		SUM(SumTotalCPU*1.0)/SUM(SampleCount*1.0) as AvgCPU,
	    SUM(SumOtherCPU*1.0)/SUM(SampleCount*1.0) as OtherCPU,
	    MAX(MaxTotalCPU*1.0) as MaxCPU,
		SUM(CAST(CPU10 as INT)) AS CPU10,
		SUM(CAST(CPU20 as INT)) AS CPU20,
		SUM(CAST(CPU30 as INT)) AS CPU30,
		SUM(CAST(CPU40 as INT)) AS CPU40,
		SUM(CAST(CPU50 as INT)) AS CPU50,
		SUM(CAST(CPU60 as INT)) AS CPU60,
		SUM(CAST(CPU70 as INT)) AS CPU70,
		SUM(CAST(CPU80 as INT)) AS CPU80,
		SUM(CAST(CPU90 as INT)) AS CPU90,
		SUM(CAST(CPU100 as INT)) AS CPU100
	FROM ' + CASE WHEN @Use60MIN=1 THEN 'dbo.CPU_60MIN' ELSE 'dbo.CPU_Histogram' END + ' as CPU
	WHERE EventTime >=@FromDate
	AND EventTime <@ToDate
	' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, EventTime)) IN (' + @DaysOfWeekCsv + ')' END + '
	' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, EventTime)) IN(' + @HoursCsv + ')' END + '
	AND EXISTS(SELECT 1 FROM #Instances t WHERE CPU.InstanceID = t.InstanceID)
	GROUP BY InstanceID
)
, dbio AS (
	SELECT IOS.InstanceID,
			SUM(IOS.num_of_reads)/(SUM(IOS.sample_ms_diff)/1000.0) AS ReadIOPs,
			SUM(IOS.num_of_writes)/(SUM(IOS.sample_ms_diff)/1000.0) AS WriteIOPs,
			SUM(IOS.num_of_reads+IOS.num_of_writes)/(SUM(IOS.sample_ms_diff)/1000.0) AS IOPs,
			SUM(IOS.num_of_bytes_read)/POWER(1024.0,2)/(SUM(IOS.sample_ms_diff)/1000.0) ReadMBsec,
			SUM(IOS.num_of_bytes_written)/POWER(1024.0,2)/(SUM(IOS.sample_ms_diff)/1000.0) WriteMBsec,
			SUM(IOS.num_of_bytes_read+IOS.num_of_bytes_written)/POWER(1024.0,2)/(SUM(IOS.sample_ms_diff)/1000.0) MBsec,
			SUM(IOS.io_stall_read_ms)/(NULLIF(SUM(IOS.num_of_reads),0)*1.0) AS ReadLatency,
			SUM(IOS.io_stall_write_ms)/(NULLIF(SUM(IOS.num_of_writes),0)*1.0) AS WriteLatency,
			SUM(IOS.io_stall_read_ms+IOS.io_stall_write_ms)/(NULLIF(SUM(IOS.num_of_writes+IOS.num_of_reads),0)*1.0) AS Latency,
			MAX(IOS.MaxReadIOPs) AS MaxReadIOPs,
			MAX(IOS.MaxWriteIOPs) AS MaxWriteIOPs,
			MAX(IOS.MaxIOPs) AS MaxIOPs,
			MAX(IOS.MaxReadMBsec) AS MaxReadMBsec,
			MAX(IOS.MaxWriteMBsec) AS MaxWriteMBsec,
			MAX(IOS.MaxMBsec) AS MaxMBsec
	FROM ' + CASE WHEN @Use60MIN = 1 THEN 'dbo.DBIOStats_60MIN' ELSE 'dbo.DBIOStats' END + ' AS IOS
	WHERE IOS.DatabaseID=-1
	AND IOS.Drive=''*''
	AND IOS.FileID=-1
	AND IOS.SnapshotDate>=CAST(@FromDate AS DATETIME2(2))
	AND IOS.SnapshotDate<CAST(@ToDate AS DATETIME2(2))
	' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, IOS.SnapshotDate)) IN (' + @DaysOfWeekCsv + ')' END + '
	' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, IOS.SnapshotDate)) IN(' + @HoursCsv + ')' END + '
	AND EXISTS(SELECT 1 FROM #Instances t WHERE IOS.InstanceID = t.InstanceID)
	GROUP BY IOS.InstanceID
)
, wait1 AS (
	SELECT W.InstanceID,
		W.WaitTypeID,
		SUM(W.wait_time_ms)*1000.0 / MAX(SUM(W.sample_ms_diff*1.0)) OVER(PARTITION BY InstanceID) WaitMsPerSec,
		SUM(W.wait_time_ms) wait_time_ms,
		SUM(W.signal_wait_time_ms) as signal_wait_time_ms
	FROM ' + CASE WHEN @Use60MIN=1 THEN 'dbo.Waits_60MIN' ELSE 'dbo.Waits' END + ' W 
	WHERE W.SnapshotDate>= CAST(@FromDate AS DATETIME2(2))
	AND W.SnapshotDate < CAST(@ToDate AS DATETIME2(2))
	' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, W.SnapshotDate)) IN (' + @DaysOfWeekCsv + ')' END + '
	' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, W.SnapshotDate)) IN(' + @HoursCsv + ')' END + '
	AND EXISTS(SELECT 1 FROM #Instances t WHERE W.InstanceID = t.InstanceID)
	GROUP BY W.InstanceID,W.WaitTypeID

)
, wait AS (
	SELECT W.InstanceID,	
		SUM(CASE WHEN WT.IsCriticalWait =1 THEN W.WaitMsPerSec ELSE 0 END) CriticalWaitMsPerSec,
		SUM(CASE WHEN WT.WaitType LIKE ''LATCH%'' THEN W.WaitMsPerSec  ELSE 0 END) LatchWaitMsPerSec,
		SUM(CASE WHEN WT.WaitType LIKE ''LCK%'' THEN W.WaitMsPerSec  ELSE 0 END) LockWaitMsPerSec,
		SUM(CASE WHEN WT.WaitType LIKE ''PAGEIO%'' OR WT.WaitType LIKE ''WRITE%'' THEN W.WaitMsPerSec  ELSE 0 END) IOWaitMsPerSec,
		SUM(W.WaitMsPerSec) WaitMsPerSec,
		SUM(signal_wait_time_ms)/NULLIF(SUM(wait_time_ms*1.0),0) as SignalWaitPct
	FROM wait1 w
	JOIN dbo.WaitType WT ON WT.WaitTypeID = W.WaitTypeID
	GROUP BY w.InstanceID
)
SELECT I.InstanceID,
	   I.ConnectionID,
       I.Instance,
	   I.InstanceDisplayName,
	   cpuAgg.AvgCPU,
	   CASE WHEN cpuAgg.AvgCPU > thres.CPUCriticalThreshold THEN 1 WHEN cpuAgg.AvgCPU > thres.CPUWarningThreshold THEN 2 WHEN cpuAgg.AvgCPU < thres.CPULowThreshold THEN 4 ELSE 3 END AS AvgCPUStatus,
	   cpuAgg.MaxCPU,
	   CASE WHEN cpuAgg.MaxCPU > thres.CPUCriticalThreshold  THEN 1 WHEN cpuAgg.MaxCPU > thres.CPUWarningThreshold THEN 2 WHEN cpuAgg.MaxCPU < thres.CPULowThreshold THEN 4 ELSE 3 END AS MaxCPUStatus,
       dbio.ReadIOPs,
       dbio.WriteIOPs,
	   dbio.IOPs,
       dbio.ReadMBsec,
       dbio.WriteMBsec,
	   dbio.MBsec,
       dbio.ReadLatency,
	   CASE WHEN dbio.ReadIOPs < thres.MinIOPsThreshold THEN 3 WHEN dbio.ReadLatency > thres.ReadLatencyCriticalThreshold THEN 1 WHEN dbio.ReadLatency > thres.ReadLatencyWarningThreshold THEN 2 WHEN dbio.ReadLatency <= thres.ReadLatencyGoodThreshold THEN 4 ELSE 3 END AS ReadLatencyStatus,
       dbio.WriteLatency,
	   CASE WHEN dbio.WriteIOPs < thres.MinIOPsThreshold THEN 3 WHEN dbio.WriteLatency > thres.ReadLatencyCriticalThreshold THEN 1 WHEN dbio.WriteLatency > thres.ReadLatencyWarningThreshold THEN 2 WHEN dbio.WriteLatency <= thres.ReadLatencyGoodThreshold THEN 4 ELSE 3 END AS WriteLatencyStatus,
       dbio.Latency,
       dbio.MaxReadIOPs,
       dbio.MaxWriteIOPs,
       dbio.MaxIOPs,
       dbio.MaxReadMBsec,
       dbio.MaxWriteMBsec,
       dbio.MaxMBsec,
       wait.CriticalWaitMsPerSec,
	   CASE WHEN wait.CriticalWaitMsPerSec=0 THEN 4 WHEN wait.CriticalWaitMsPerSec> thres.CriticalWaitCriticalThreshold THEN 1 WHEN wait.CriticalWaitMsPerSec> thres.CriticalWaitWarningThreshold THEN 2 ELSE 3 END AS CriticalWaitStatus, 
       wait.LatchWaitMsPerSec,
       wait.LockWaitMsPerSec,
       wait.IOWaitMsPerSec,
       wait.WaitMsPerSec,
	   wait.SignalWaitPct,
	   CPU10,
	   CPU20,
	   CPU30,
	   CPU40,
	   CPU50,
	   CPU60,
	   CPU70,
	   CPU80,
	   CPU90,
	   CPU100
FROM dbo.Instances I 
LEFT JOIN dbio ON I.InstanceID = dbio.InstanceID
LEFT JOIN cpuAgg ON I.InstanceID = cpuAgg.InstanceID
LEFT JOIN wait ON I.InstanceID = wait.InstanceID
CROSS JOIN dbo.PerformanceThresholds thres
WHERE EXISTS(SELECT 1 FROM #Instances t WHERE I.InstanceID = t.InstanceID)
AND I.IsActive=1
ORDER BY CASE WHEN wait.CriticalWaitMsPerSec > thres.CriticalWaitWarningThreshold THEN wait.CriticalWaitMsPerSec ELSE 0 END DESC, cpuAgg.AvgCPU DESC
OPTION(RECOMPILE)'

IF @Debug=1
BEGIN
	EXEC dbo.PrintMax @SQL 
END

EXEC sp_executesql @SQL,
					N'@FromDate DATETIME2(3),
					@ToDate DATETIME2(3),
					@UTCOffset INT',
					@FromDate,
					@ToDate,
					@UTCOffset

