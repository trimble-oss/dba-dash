CREATE PROC [dbo].[PerformanceSummary_Get](
	@InstanceIDs VARCHAR(MAX)=NULL,
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL,
	@TagIDs VARCHAR(MAX)=NULL
)
AS
IF @FromDate IS NULL
	SET @FromDate = DATEADD(mi,-15,GETUTCDATE())
IF @ToDate IS NULL
	SET @ToDate=GETUTCDATE()

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
	FROM dbo.InstancesMatchingTags(@TagIDs)
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')

END;

WITH io1 AS (
	SELECT IOS.InstanceID,
			IOS.SnapshotDate,
			SUM(IOS.num_of_reads) AS num_of_reads,
			SUM(IOS.num_of_writes) AS num_of_writes,
			SUM(IOS.num_of_bytes_read) num_of_bytes_read,
			SUM(IOS.num_of_bytes_written) num_of_bytes_written,
			SUM(IOS.io_stall_read_ms) io_stall_read_ms,
			SUM(IOS.io_stall_write_ms) io_stall_write_ms,
			MAX(IOS.sample_ms_diff) sample_ms_diff
	FROM dbo.IOStats IOS 
	WHERE IOS.SnapshotDate>=@FromDate
	AND IOS.SnapshotDate<@ToDate
	AND EXISTS(SELECT 1 FROM @Instances t WHERE IOS.InstanceID = t.InstanceID)
	GROUP BY IOS.InstanceID,IOS.SnapshotDate
)
, cpuAgg AS (
	SELECT InstanceID,
		AVG(100.0-SystemIdleCPU) AvgCPU,
		MAX(100-SystemIdleCPU) as MaxCPU
	FROM dbo.CPU
	WHERE EventTime >=@FromDate
	AND EventTime <@ToDate
	GROUP BY InstanceID
)
, io2 AS (
	SELECT a.InstanceID,
			SUM(a.num_of_reads)/(SUM(a.sample_ms_diff)/1000.0) AS ReadIOPs,
			SUM(a.num_of_writes)/(SUM(a.sample_ms_diff)/1000.0) AS WriteIOPs,
			SUM(a.num_of_reads+a.num_of_writes)/(SUM(a.sample_ms_diff)/1000.0) AS IOPs,
			SUM(a.num_of_bytes_read)/POWER(1024.0,2)/(SUM(a.sample_ms_diff)/1000.0) ReadMBsec,
			SUM(a.num_of_bytes_written)/POWER(1024.0,2)/(SUM(a.sample_ms_diff)/1000.0) WriteMBsec,
			SUM(a.num_of_bytes_read+a.num_of_bytes_written)/POWER(1024.0,2)/(SUM(a.sample_ms_diff)/1000.0) MBsec,
			SUM(a.io_stall_read_ms)/(NULLIF(SUM(a.num_of_reads),0)*1.0) AS ReadLatency,
			SUM(a.io_stall_write_ms)/(NULLIF(SUM(a.num_of_writes),0)*1.0) AS WriteLatency,
			SUM(a.io_stall_read_ms+a.io_stall_write_ms)/(NULLIF(SUM(a.num_of_writes+a.num_of_reads),0)*1.0) AS Latency,
			MAX(a.num_of_reads/(a.sample_ms_diff/1000.0)) AS MaxReadIOPs,
			MAX(a.num_of_writes/(a.sample_ms_diff/1000.0)) AS MaxWriteIOPs,
			MAX((a.num_of_writes+a.num_of_reads)/(a.sample_ms_diff/1000.0)) AS MaxIOPs,
			MAX(a.num_of_bytes_read/(a.sample_ms_diff/1000.0))/POWER(1024.0,2) AS MaxReadMBsec,
			MAX(a.num_of_bytes_written/(a.sample_ms_diff/1000.0))/POWER(1024.0,2) AS MaxWriteMBsec,
			MAX((a.num_of_bytes_written+a.num_of_bytes_read)/(a.sample_ms_diff/1000.0))/POWER(1024.0,2) AS MaxMBsec
	FROM io1 a
	GROUP BY a.InstanceID
)
, wait1 AS (
	SELECT W.InstanceID,
		W.WaitTypeID,
		SUM(W.wait_time_ms)*1000.0 / MAX(SUM(W.sample_ms_diff)) OVER(PARTITION BY InstanceID) WaitMsPerSec
	FROM dbo.Waits W 
	WHERE W.SnapshotDate>= @FromDate
	AND W.SnapshotDate < @ToDate
	GROUP BY W.InstanceID,W.WaitTypeID

)
, wait AS (
	SELECT W.InstanceID,	
		SUM(CASE WHEN WT.IsCriticalWait =1 THEN W.WaitMsPerSec ELSE 0 END) CriticalWaitMsPerSec,
		SUM(CASE WHEN WT.WaitType LIKE 'LATCH%' THEN W.WaitMsPerSec  ELSE 0 END) LatchWaitMsPerSec,
		SUM(CASE WHEN WT.WaitType LIKE 'LCK%' THEN W.WaitMsPerSec  ELSE 0 END) LockWaitMsPerSec,
		SUM(CASE WHEN WT.WaitType LIKE 'PAGEIO%' OR WT.WaitType LIKE 'WRITE%' THEN W.WaitMsPerSec  ELSE 0 END) IOWaitMsPerSec,
		SUM(W.WaitMsPerSec) WaitMsPerSec
	FROM wait1 w
	JOIN dbo.WaitType WT ON WT.WaitTypeID = W.WaitTypeID
	GROUP BY w.InstanceID
)
SELECT i.InstanceID,
		i.ConnectionID,
       i.Instance,
	   cpuAgg.AvgCPU,
	   CASE WHEN cpuAgg.AvgCPU>90 THEN 1 WHEN cpuAgg.AvgCPU >75 THEN 2 WHEN cpuAgg.AvgCPU<50 THEN 4 ELSE 3 END AS AvgCPUStatus,
	   cpuAgg.MaxCPU,
       io2.ReadIOPs,
       io2.WriteIOPs,
	   io2.IOPs,
       io2.ReadMBsec,
       io2.WriteMBsec,
	   io2.MBsec,
       io2.ReadLatency,
	   CASE WHEN io2.ReadLatency>50 THEN 1 WHEN io2.ReadLatency>10 THEN 2 WHEN io2.ReadLatency<=10 THEN 4 ELSE 3 END AS ReadLatencyStatus,
       io2.WriteLatency,
	   CASE WHEN io2.WriteLatency>50 THEN 1 WHEN io2.WriteLatency>10 THEN 2 WHEN io2.WriteLatency<=10 THEN 4 ELSE 3 END AS WriteLatencyStatus,
       io2.Latency,
       io2.MaxReadIOPs,
       io2.MaxWriteIOPs,
       io2.MaxIOPs,
       io2.MaxReadMBsec,
       io2.MaxWriteMBsec,
       io2.MaxMBsec,
       wait.CriticalWaitMsPerSec,
	   CASE WHEN wait.CriticalWaitMsPerSec=0 THEN 4 WHEN wait.CriticalWaitMsPerSec>1000 THEN 1 WHEN wait.CriticalWaitMsPerSec>1 THEN 2 ELSE 3 END AS CriticalWaitStatus, 
       wait.LatchWaitMsPerSec,
       wait.LockWaitMsPerSec,
       wait.IOWaitMsPerSec,
       wait.WaitMsPerSec	
FROM dbo.Instances I 
LEFT JOIN io2 ON I.InstanceID = io2.InstanceID
LEFT JOIN cpuAgg ON I.InstanceID = cpuAgg.InstanceID
LEFT JOIN wait ON I.InstanceID = wait.InstanceID
WHERE EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
AND I.IsActive=1
ORDER BY CASE WHEN wait.CriticalWaitMsPerSec> 1 THEN CriticalWaitMsPerSec ELSE 0 END DESC, cpuAgg.AvgCPU DESC