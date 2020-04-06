CREATE PROC [Report].[PerformanceDashboard](@InstanceIDs VARCHAR(MAX)=NULL,@Mins INT=60)
AS
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT Item
	FROM dbo.SplitStrings(@InstanceIDs,',')
END;

WITH instanceAgg AS (
	SELECT I.InstanceID,I.Instance,I.ConnectionID,IOS.SnapshotDate,
			SUM(IOS.num_of_reads) AS num_of_reads,
			SUM(IOS.num_of_writes) AS num_of_writes,
			SUM(IOS.num_of_bytes_read) num_of_bytes_read,
			SUM(IOS.num_of_bytes_written) num_of_bytes_written,
			SUM(IOS.io_stall_read_ms) io_stall_read_ms,
			SUM(IOS.io_stall_write_ms) io_stall_write_ms,
			MAX(IOS.sample_ms_diff) sample_ms_diff
	FROM dbo.Instances I
	LEFT JOIN dbo.IOStats IOS ON IOS.InstanceID = I.InstanceID
						AND IOS.SnapshotDate>=DATEADD(mi,-@Mins,GETUTCDATE())
	WHERE I.IsActive=1
	AND EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
	GROUP BY I.InstanceID,I.Instance,I.ConnectionID,IOS.SnapshotDate
)
, cpuAgg AS (
	SELECT InstanceID,AVG(100-SystemIdleCPU) AvgCPU
	FROM dbo.CPU
	WHERE EventTime >=DATEADD(mi,-@Mins,GETUTCDATE())
	GROUP BY InstanceID
)
, i AS (
SELECT a.InstanceID,
		a.ConnectionID,
		a.Instance,
		SUM(a.num_of_reads)/(SUM(a.sample_ms_diff)/1000.0) AS ReadIOPs,
		SUM(a.num_of_writes)/(SUM(a.sample_ms_diff)/1000.0) AS WriteIOPs,
		SUM(a.num_of_bytes_read)/POWER(1024.0,2)/(SUM(a.sample_ms_diff)/1000.0) ReadMBsec,
		SUM(a.num_of_bytes_written)/POWER(1024.0,2)/(SUM(a.sample_ms_diff)/1000.0) WriteMBsec,
		SUM(a.io_stall_read_ms)/(NULLIF(SUM(a.num_of_reads),0)*1.0) AS ReadLatency,
		SUM(a.io_stall_write_ms)/(NULLIF(SUM(a.num_of_writes),0)*1.0) AS WriteLatency,
		SUM(a.io_stall_read_ms+a.io_stall_write_ms)/(NULLIF(SUM(a.num_of_writes+a.num_of_reads),0)*1.0) AS Latency,
		MAX(cpuAgg.AvgCPU) AS AvgCPU,
		MAX(a.num_of_reads/(a.sample_ms_diff/1000.0)) AS MaxReadIOPs,
		MAX(a.num_of_writes/(a.sample_ms_diff/1000.0)) AS MaxWriteIOPs,
		MAX((a.num_of_writes+a.num_of_reads)/(a.sample_ms_diff/1000.0)) AS MaxIOPs,
		MAX(a.num_of_bytes_read/(a.sample_ms_diff/1000.0))/POWER(1024.0,2) AS MaxReadMBsec,
		MAX(a.num_of_bytes_written/(a.sample_ms_diff/1000.0))/POWER(1024.0,2) AS MaxWriteMBsec,
		MAX((a.num_of_bytes_written+a.num_of_bytes_read)/(a.sample_ms_diff/1000.0))/POWER(1024.0,2) AS MaxMBsec
FROM instanceAgg a
LEFT JOIN cpuAgg ON a.InstanceID = cpuAgg.InstanceID
GROUP BY a.InstanceID,a.Instance,a.ConnectionID
)
, wait AS (
	SELECT W.InstanceID,
		WT.IsCriticalWait, 
		WT.WaitType,
		SUM(W.wait_time_ms) TotalWaitMs,
		SUM(W.wait_time_ms)*1.0/ SUM(SUM(W.wait_time_ms)) OVER(PARTITION BY W.InstanceID) Pct
	FROM dbo.Waits W 
	JOIN dbo.WaitType WT ON WT.WaitTypeID = W.WaitTypeID
	WHERE W.SnapshotDate>= DATEADD(mi,-@Mins,GETUTCDATE())
	AND WT.WaitType <>'REDO_THREAD_PENDING_WORK'
	GROUP BY WT.WaitType,W.InstanceID,WT.IsCriticalWait
)
SELECT i.InstanceID,
		i.ConnectionID,
       i.Instance,
       i.ReadIOPs,
       i.WriteIOPs,
       i.ReadMBsec,
       i.WriteMBsec,
       i.ReadLatency,
       i.WriteLatency,
       i.Latency,
       i.AvgCPU,
       i.MaxReadIOPs,
       i.MaxWriteIOPs,
       i.MaxIOPs,
       i.MaxReadMBsec,
       i.MaxWriteMBsec,
       i.MaxMBsec,
       ISNULL(w.WaitType,'') WaitType,
       ISNULL(w.TotalWaitMs,0) TotalWaitMs,
       ISNULL(w.Pct,0) Pct,
	   ISNULL(W.IsCriticalWait,CAST(0 AS BIT)) IsCriticalWait,
	   CASE WHEN w.WaitType LIKE 'LCK%' THEN w.TotalWaitMs ELSE 0 END AS LockWaitMs,
	   CASE WHEN w.WaitType LIKE 'LCK%' THEN w.Pct ELSE 0 END AS LockWaitPct,
	   CASE WHEN w.WaitType LIKE 'PAGEIO%' OR w.WaitType LIKE 'WRITELOG%' THEN w.TotalWaitMs ELSE 0 END AS IOWaitMs,
	   CASE WHEN w.WaitType LIKE 'PAGEIO%' OR w.WaitType LIKE 'WRITELOG%' THEN w.Pct ELSE 0 END AS IOWaitPct
FROM i 
LEFT JOIN wait w ON i.InstanceID = w.InstanceID AND (w.Pct>=0.01 OR w.IsCriticalWait=1 OR w.WaitType LIKE 'PAGEIO%' OR w.WaitType LIKE 'WRITELOG%')