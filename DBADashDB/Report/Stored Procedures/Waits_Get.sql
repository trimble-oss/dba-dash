﻿CREATE PROC [Report].[Waits_Get](@InstanceID INT,@Mins INT=60,@TimeGroup TINYINT=1)
AS
SELECT CASE WHEN @TimeGroup=0 THEN NULL	
			WHEN @TimeGroup=1 THEN CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,W.SnapshotDate,120),16),120)
			WHEN @TimeGroup=2 THEN CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,W.SnapshotDate,120),15) + '0',120)
			WHEN @TimeGroup=3 THEN CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,W.SnapshotDate,120),13) + ':00',120)
			ELSE NULL END AS Time,
			WT.IsCriticalWait, 
			WT.WaitType,
			SUM(W.wait_time_ms) WaitMs,
			SUM(W.waiting_tasks_count) WaitCount,
			SUM(W.signal_wait_time_ms) AS SignalWaitMs
FROM dbo.Waits W 
JOIN dbo.WaitType WT ON WT.WaitTypeID = W.WaitTypeID
WHERE W.SnapshotDate>= DATEADD(mi,-@Mins,GETUTCDATE())
AND WT.WaitType <>'REDO_THREAD_PENDING_WORK'
AND W.InstanceID=@InstanceID
GROUP BY WT.WaitType,
		WT.IsCriticalWait,
		CASE WHEN @TimeGroup=0 THEN NULL	
			WHEN @TimeGroup=1 THEN CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,W.SnapshotDate,120),16),120)
			WHEN @TimeGroup=2 THEN CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,W.SnapshotDate,120),15) + '0',120)
			WHEN @TimeGroup=3 THEN CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,W.SnapshotDate,120),13) + ':00',120)
			ELSE NULL END 
HAVING SUM(W.wait_time_ms)>1000