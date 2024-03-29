﻿CREATE PROC [Report].[PerformanceDashboard](@InstanceIDs VARCHAR(MAX)=NULL,@Mins INT=60)
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

WITH cpuAgg AS (
	SELECT InstanceID,AVG(100-SystemIdleCPU) AvgCPU
	FROM dbo.CPU
	WHERE EventTime >=CAST(DATEADD(mi,-@Mins,GETUTCDATE()) AS DATETIME2(3))
	AND EventTime < CAST(DATEADD(mi,1,GETUTCDATE()) AS DATETIME2(3))
	GROUP BY InstanceID
),
i AS (
	SELECT I.InstanceID,
		I.Instance,
		I.ConnectionID,
		SUM(IOS.num_of_reads)/(SUM(IOS.sample_ms_diff)/1000.0) AS ReadIOPs,
		SUM(IOS.num_of_writes)/(SUM(IOS.sample_ms_diff)/1000.0) AS WriteIOPs,
		SUM(IOS.num_of_bytes_read)/POWER(1024.0,2)/(SUM(IOS.sample_ms_diff)/1000.0) ReadMBsec,
		SUM(IOS.num_of_bytes_written)/POWER(1024.0,2)/(SUM(IOS.sample_ms_diff)/1000.0) WriteMBsec,
		SUM(IOS.io_stall_read_ms)/(NULLIF(SUM(IOS.num_of_reads),0)*1.0) AS ReadLatency,
		SUM(IOS.io_stall_write_ms)/(NULLIF(SUM(IOS.num_of_writes),0)*1.0) AS WriteLatency,
		SUM(IOS.io_stall_read_ms+IOS.io_stall_write_ms)/(NULLIF(SUM(IOS.num_of_writes+IOS.num_of_reads),0)*1.0) AS Latency,
		MAX(IOS.MaxReadIOPs) AS MaxReadIOPs,
		MAX(IOS.MaxWriteIOPs) AS MaxWriteIOPs,
		MAX(IOS.MaxIOPs) AS MaxIOPs,
		MAX(IOS.MaxReadMBsec) AS MaxReadMBsec,
		MAX(IOS.MaxWriteMBsec) AS MaxWriteMBsec,
		MAX(MaxMBsec) AS MaxMBsec
	FROM dbo.Instances I
	LEFT JOIN dbo.DBIOStats IOS ON IOS.InstanceID = I.InstanceID
						AND IOS.SnapshotDate>=CAST(DATEADD(mi,-@Mins,GETUTCDATE()) AS DATETIME2(2))
						AND IOS.SnapshotDate< CAST(DATEADD(mi,1,GETUTCDATE()) AS DATETIME2(2))
						AND IOS.FileID=-1
						AND IOS.DatabaseID=-1
						AND IOS.Drive='*'
	WHERE I.IsActive=1
	AND EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
	GROUP BY I.InstanceID,I.Instance,I.ConnectionID
)
, wait AS (
	SELECT W.InstanceID,
		WT.IsCriticalWait, 
		WT.WaitType,
		SUM(W.wait_time_ms) TotalWaitMs,
		SUM(W.wait_time_ms)*1.0/ SUM(SUM(W.wait_time_ms)) OVER(PARTITION BY W.InstanceID) Pct
	FROM dbo.Waits W 
	JOIN dbo.WaitType WT ON WT.WaitTypeID = W.WaitTypeID
	WHERE W.SnapshotDate>= CAST(DATEADD(mi,-@Mins,GETUTCDATE()) AS DATETIME2(2))
	AND W.SnapshotDate< CAST(DATEADD(mi,1,GETUTCDATE()) AS DATETIME2(2))
	AND WT.WaitType <>'REDO_THREAD_PENDING_WORK'
	AND EXISTS(SELECT 1 FROM @Instances t WHERE W.InstanceID = t.InstanceID)
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
       cpuAgg.AvgCPU,
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
LEFT JOIN cpuAgg ON i.InstanceID = cpuAgg.InstanceID
LEFT JOIN wait w ON i.InstanceID = w.InstanceID AND (w.Pct>=0.01 OR w.IsCriticalWait=1 OR w.WaitType LIKE 'PAGEIO%' OR w.WaitType LIKE 'WRITELOG%')