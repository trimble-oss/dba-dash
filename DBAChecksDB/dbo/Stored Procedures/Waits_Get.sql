CREATE PROC [dbo].[Waits_Get](
	@InstanceID INT,
	@FromDate DATETIME2(3)=NULL, 
	@ToDate DATETIME2(3)=NULL,
	@DateGrouping VARCHAR(50)='None',
	@Top INT=10
)
AS
IF @FromDate IS NULL
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
IF @ToDate IS NULL
	SET @ToDate = GETUTCDATE()
DECLARE @SQL NVARCHAR(MAX)
DECLARE @DateGroupingSQL NVARCHAR(MAX)
DECLARE @Table NVARCHAR(MAX)
SELECT @DateGroupingSQL= CASE WHEN @DateGrouping = 'None' THEN 'W.SnapshotDate'
			WHEN @DateGrouping = '1MIN' THEN 'DATEADD(mi, DATEDIFF(mi, 0, DATEADD(s, 30, W.SnapshotDate)), 0)'
			WHEN @DateGrouping = '10MIN' THEN 'CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,W.SnapshotDate,120),15) + ''0'',120)'
			WHEN @DateGrouping = '60MIN' THEN 'CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,W.SnapshotDate,120),13) + '':00'',120)'
			WHEN @DateGrouping = '120MIN' THEN 'DATEADD(hh,DATEPART(hh,W.SnapshotDate) - DATEPART(hh,W.SnapshotDate) % 2, CAST(CAST(W.SnapshotDate AS DATE) AS DATETIME))'
			WHEN @DateGrouping ='DAY' THEN 'CAST(CAST(W.SnapshotDate as DATE) as DATETIME)'
			ELSE NULL END


SELECT @Table = CASE WHEN @DateGrouping IN('60MIN','DAY') THEN 'dbo.Waits_60MIN' ELSE 'dbo.Waits' END


SET @SQL = N'
WITH T AS (
SELECT ' + @DateGroupingSQL + ' AS [Time],
			CASE WHEN WT.IsCriticalWait=1 THEN ''!!'' ELSE '''' END + WT.WaitType as WaitType,
			SUM(W.wait_time_ms)*1000.0 / SUM(W.sample_ms_diff) WaitTimeMsPerSec,
			ROW_NUMBER() OVER(PARTITION BY ' + @DateGroupingSQL + ' ORDER BY SUM(W.wait_time_ms) DESC) rnum
FROM ' + @Table + ' W 
JOIN dbo.WaitType WT ON WT.WaitTypeID = W.WaitTypeID
WHERE W.SnapshotDate>= @FromDate
AND W.SnapshotDate <= @ToDate
AND WT.WaitType NOT IN(N''PVS_PREALLOCATE'',N''REDO_THREAD_PENDING_WORK'')
AND W.InstanceID=@InstanceID
GROUP BY WT.WaitType,WT.IsCriticalWait, ' + @DateGroupingSQL + ' 
HAVING SUM(W.wait_time_ms)*1000.0 / SUM(W.sample_ms_diff) > 0
)
SELECT [Time],
	CASE WHEN rnum<=@Top THEN WaitType ELSE ''{Other}'' END as WaitType,
	SUM(WaitTimeMsPerSec) as WaitTimeMsPerSec
FROM T 
GROUP BY [Time],CASE WHEN rnum<=@Top THEN WaitType ELSE ''{Other}'' END
ORDER BY WaitType'

EXEC sp_executesql @SQL,N'@FromDate DATETIME2(3),@ToDate DATETIME2(3),@InstanceID INT,@Top INT',@FromDate,@ToDate,@InstanceID,@Top