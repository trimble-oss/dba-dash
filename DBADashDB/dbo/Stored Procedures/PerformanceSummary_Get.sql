CREATE PROC dbo.PerformanceSummary_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL,
	@TagIDs VARCHAR(MAX)=NULL,
	@Use60MIN BIT=NULL, /* NULL: whole hours from the hourly rollups, part hours from the raw rows (raw rows only for a time of day or weekday filter with a part hour @UTCOffset).  0: raw rows only.  1: hourly rollups only */
	@Debug BIT=0,
	@DaysOfWeek IDs READONLY, /* e.g. exclude weekends:  Monday,Tuesday,Wednesday,Thursday,Friday. Filter applied in local timezone (@UTCOffset) */
	@Hours IDs READONLY, /* e.g. 9 to 5 :  9,10,11,12,13,14,15,16. Filter applied in local timezone (@UTCOffset)  */
	@UTCOffset INT=0, /* Used for filtering on hours & weekday in current timezone */
	@ShowHidden BIT=1
)
AS
SET NOCOUNT ON
SET DATEFIRST 1 /* Start week on Monday */
IF @FromDate IS NULL
	SET @FromDate = DATEADD(mi,-15,GETUTCDATE())
IF @ToDate IS NULL
	SET @ToDate=GETUTCDATE()

/*
	CPU, IO and waits each come from their hourly rollup for the whole hours of the window and from the raw rows
	for the part hours at either end - see dbo.RollupRanges.  An hour in each rollup is the raw rows whose time
	falls in it added together, so the figures are the raw rows' for close to the cost of reading the rollups.
	Choosing one table for the whole window read every raw row of every instance for any window up to a day that
	didn't start on the hour, and left out the part hours of a longer one.

	Each range is a seek per instance on the clustered key, as in dbo.QueryStats_Get.  The waits rollup leaves out
	intervals of a day or more and gives every wait type in an hour the hour's sample time, which the raw rows
	don't; neither changes the figures here in practice.
*/
DECLARE @HasDaysOfWeek BIT = CASE WHEN EXISTS(SELECT 1 FROM @DaysOfWeek) THEN 1 ELSE 0 END
DECLARE @HasHours BIT = CASE WHEN EXISTS(SELECT 1 FROM @Hours) THEN 1 ELSE 0 END

/*
	A rollup row is filtered on the local time of its hour's start.  With an offset that isn't a whole number of
	hours every rollup row straddles two local hours, and the one at local midnight two days, so a time of day or
	weekday filter can only be applied exactly to the raw rows.
*/
IF @Use60MIN IS NULL AND @UTCOffset % 60 <> 0 AND (@HasHours = 1 OR @HasDaysOfWeek = 1)
	SET @Use60MIN = 0

/* CPU times are DATETIME2(3); IO and waits DATETIME2(2), compared at their own type so each range is a seek */
DECLARE @HourFrom DATETIME2(3), @HourTo DATETIME2(3)
DECLARE @FromDate2 DATETIME2(2) = @FromDate, @ToDate2 DATETIME2(2) = @ToDate
DECLARE @HourFrom2 DATETIME2(2), @HourTo2 DATETIME2(2)

SELECT @HourFrom = HourFrom, @HourTo = HourTo FROM dbo.RollupRanges(@FromDate, @ToDate, @Use60MIN)
SELECT @HourFrom2 = HourFrom, @HourTo2 = HourTo FROM dbo.RollupRanges(@FromDate2, @ToDate2, @Use60MIN)

IF @Debug = 1
	PRINT CONCAT('CPU: raw [', @FromDate, ', ', @HourFrom, ') hourly [', @HourFrom, ', ', @HourTo, ') raw [', @HourTo, ', ', @ToDate, ')', CHAR(13) + CHAR(10),
				 'IO, waits: raw [', @FromDate2, ', ', @HourFrom2, ') hourly [', @HourFrom2, ', ', @HourTo2, ') raw [', @HourTo2, ', ', @ToDate2, ')')

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

WITH cpuAgg AS (
	SELECT CPU.InstanceID,
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
	FROM (/* The part hour at the start of the window, from the raw rows */
		  SELECT R.*
		  FROM #Instances T
		  CROSS APPLY (SELECT C.InstanceID, C.EventTime, C.SampleCount, C.SumTotalCPU, C.SumOtherCPU, C.MaxTotalCPU,
							  H.CPU10, H.CPU20, H.CPU30, H.CPU40, H.CPU50, H.CPU60, H.CPU70, H.CPU80, H.CPU90, H.CPU100
					   FROM dbo.CPU C WITH (FORCESEEK)
					   CROSS APPLY dbo.Histogram_CPU(C.TotalCPU) H
					   WHERE C.InstanceID = T.InstanceID
					   AND C.EventTime >= @FromDate
					   AND C.EventTime < @HourFrom) R
		  UNION ALL
		  /* The whole hours, from the rollup */
		  SELECT H.*
		  FROM #Instances T
		  CROSS APPLY (SELECT C.InstanceID, C.EventTime, C.SampleCount, C.SumTotalCPU, C.SumOtherCPU, C.MaxTotalCPU,
							  C.CPU10, C.CPU20, C.CPU30, C.CPU40, C.CPU50, C.CPU60, C.CPU70, C.CPU80, C.CPU90, C.CPU100
					   FROM dbo.CPU_60MIN C WITH (FORCESEEK)
					   WHERE C.InstanceID = T.InstanceID
					   AND C.EventTime >= @HourFrom
					   AND C.EventTime < @HourTo) H
		  UNION ALL
		  /* The part hour at the end */
		  SELECT R.*
		  FROM #Instances T
		  CROSS APPLY (SELECT C.InstanceID, C.EventTime, C.SampleCount, C.SumTotalCPU, C.SumOtherCPU, C.MaxTotalCPU,
							  H.CPU10, H.CPU20, H.CPU30, H.CPU40, H.CPU50, H.CPU60, H.CPU70, H.CPU80, H.CPU90, H.CPU100
					   FROM dbo.CPU C WITH (FORCESEEK)
					   CROSS APPLY dbo.Histogram_CPU(C.TotalCPU) H
					   WHERE C.InstanceID = T.InstanceID
					   AND C.EventTime >= @HourTo
					   AND C.EventTime < @ToDate) R) CPU
	WHERE (@HasDaysOfWeek = 0 OR EXISTS(SELECT 1 FROM @DaysOfWeek D WHERE D.ID = DATEPART(dw, DATEADD(mi, @UTCOffset, CPU.EventTime))))
	AND (@HasHours = 0 OR EXISTS(SELECT 1 FROM @Hours H WHERE H.ID = DATEPART(hh, DATEADD(mi, @UTCOffset, CPU.EventTime))))
	GROUP BY CPU.InstanceID
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
	FROM (/* The part hour at the start of the window, from the raw rows.  The instance totals are the rows for database, file and drive -1, -1, '*' */
		  SELECT R.*
		  FROM #Instances T
		  CROSS APPLY (SELECT S.InstanceID, S.SnapshotDate, S.num_of_reads, S.num_of_writes, S.num_of_bytes_read, S.num_of_bytes_written,
							  S.io_stall_read_ms, S.io_stall_write_ms, S.sample_ms_diff, S.MaxReadIOPs, S.MaxWriteIOPs, S.MaxIOPs,
							  S.MaxReadMBsec, S.MaxWriteMBsec, S.MaxMBsec
					   FROM dbo.DBIOStats S WITH (FORCESEEK)
					   WHERE S.InstanceID = T.InstanceID
					   AND S.DatabaseID = -1
					   AND S.FileID = -1
					   AND S.Drive = '*'
					   AND S.SnapshotDate >= @FromDate2
					   AND S.SnapshotDate < @HourFrom2) R
		  UNION ALL
		  /* The whole hours, from the rollup */
		  SELECT H.*
		  FROM #Instances T
		  CROSS APPLY (SELECT S.InstanceID, S.SnapshotDate, S.num_of_reads, S.num_of_writes, S.num_of_bytes_read, S.num_of_bytes_written,
							  S.io_stall_read_ms, S.io_stall_write_ms, S.sample_ms_diff, S.MaxReadIOPs, S.MaxWriteIOPs, S.MaxIOPs,
							  S.MaxReadMBsec, S.MaxWriteMBsec, S.MaxMBsec
					   FROM dbo.DBIOStats_60MIN S WITH (FORCESEEK)
					   WHERE S.InstanceID = T.InstanceID
					   AND S.DatabaseID = -1
					   AND S.FileID = -1
					   AND S.Drive = '*'
					   AND S.SnapshotDate >= @HourFrom2
					   AND S.SnapshotDate < @HourTo2) H
		  UNION ALL
		  /* The part hour at the end */
		  SELECT R.*
		  FROM #Instances T
		  CROSS APPLY (SELECT S.InstanceID, S.SnapshotDate, S.num_of_reads, S.num_of_writes, S.num_of_bytes_read, S.num_of_bytes_written,
							  S.io_stall_read_ms, S.io_stall_write_ms, S.sample_ms_diff, S.MaxReadIOPs, S.MaxWriteIOPs, S.MaxIOPs,
							  S.MaxReadMBsec, S.MaxWriteMBsec, S.MaxMBsec
					   FROM dbo.DBIOStats S WITH (FORCESEEK)
					   WHERE S.InstanceID = T.InstanceID
					   AND S.DatabaseID = -1
					   AND S.FileID = -1
					   AND S.Drive = '*'
					   AND S.SnapshotDate >= @HourTo2
					   AND S.SnapshotDate < @ToDate2) R) IOS
	WHERE (@HasDaysOfWeek = 0 OR EXISTS(SELECT 1 FROM @DaysOfWeek D WHERE D.ID = DATEPART(dw, DATEADD(mi, @UTCOffset, IOS.SnapshotDate))))
	AND (@HasHours = 0 OR EXISTS(SELECT 1 FROM @Hours H WHERE H.ID = DATEPART(hh, DATEADD(mi, @UTCOffset, IOS.SnapshotDate))))
	GROUP BY IOS.InstanceID
)
, wait1 AS (
	SELECT W.InstanceID,
		W.WaitTypeID,
		SUM(W.wait_time_ms)*1000.0 / MAX(SUM(W.sample_ms_diff*1.0)) OVER(PARTITION BY W.InstanceID) WaitMsPerSec,
		SUM(W.wait_time_ms) wait_time_ms,
		SUM(W.signal_wait_time_ms) as signal_wait_time_ms
	FROM (/* The part hour at the start of the window, from the raw rows */
		  SELECT R.*
		  FROM #Instances T
		  CROSS APPLY (SELECT S.InstanceID, S.SnapshotDate, S.WaitTypeID, S.wait_time_ms, S.signal_wait_time_ms, S.sample_ms_diff
					   FROM dbo.Waits S WITH (FORCESEEK)
					   WHERE S.InstanceID = T.InstanceID
					   AND S.SnapshotDate >= @FromDate2
					   AND S.SnapshotDate < @HourFrom2) R
		  UNION ALL
		  /* The whole hours, from the rollup */
		  SELECT H.*
		  FROM #Instances T
		  CROSS APPLY (SELECT S.InstanceID, S.SnapshotDate, S.WaitTypeID, S.wait_time_ms, S.signal_wait_time_ms, S.sample_ms_diff
					   FROM dbo.Waits_60MIN S WITH (FORCESEEK)
					   WHERE S.InstanceID = T.InstanceID
					   AND S.SnapshotDate >= @HourFrom2
					   AND S.SnapshotDate < @HourTo2) H
		  UNION ALL
		  /* The part hour at the end */
		  SELECT R.*
		  FROM #Instances T
		  CROSS APPLY (SELECT S.InstanceID, S.SnapshotDate, S.WaitTypeID, S.wait_time_ms, S.signal_wait_time_ms, S.sample_ms_diff
					   FROM dbo.Waits S WITH (FORCESEEK)
					   WHERE S.InstanceID = T.InstanceID
					   AND S.SnapshotDate >= @HourTo2
					   AND S.SnapshotDate < @ToDate2) R) W
	WHERE (@HasDaysOfWeek = 0 OR EXISTS(SELECT 1 FROM @DaysOfWeek D WHERE D.ID = DATEPART(dw, DATEADD(mi, @UTCOffset, W.SnapshotDate))))
	AND (@HasHours = 0 OR EXISTS(SELECT 1 FROM @Hours H WHERE H.ID = DATEPART(hh, DATEADD(mi, @UTCOffset, W.SnapshotDate))))
	GROUP BY W.InstanceID,W.WaitTypeID

)
, wait AS (
	SELECT W.InstanceID,
		SUM(CASE WHEN WT.IsCriticalWait =1 THEN W.WaitMsPerSec ELSE 0 END) CriticalWaitMsPerSec,
		SUM(CASE WHEN WT.WaitType LIKE 'LATCH%' THEN W.WaitMsPerSec  ELSE 0 END) LatchWaitMsPerSec,
		SUM(CASE WHEN WT.WaitType LIKE 'LCK%' THEN W.WaitMsPerSec  ELSE 0 END) LockWaitMsPerSec,
		SUM(CASE WHEN WT.WaitType LIKE 'PAGEIO%' OR WT.WaitType LIKE 'WRITE%' THEN W.WaitMsPerSec  ELSE 0 END) IOWaitMsPerSec,
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
AND (@ShowHidden = 1 OR I.ShowInSummary = 1)
ORDER BY CASE WHEN wait.CriticalWaitMsPerSec > thres.CriticalWaitWarningThreshold THEN wait.CriticalWaitMsPerSec ELSE 0 END DESC, cpuAgg.AvgCPU DESC
OPTION(RECOMPILE)
