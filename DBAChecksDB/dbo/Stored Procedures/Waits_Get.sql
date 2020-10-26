CREATE PROC [dbo].[Waits_Get](
	@InstanceID INT,
	@FromDate DATETIME2(3)=NULL, 
	@ToDate DATETIME2(3)=NULL,
	@DateGroupingMin INT=NULL,
	@Top INT=10
)
AS
IF @FromDate IS NULL
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
IF @ToDate IS NULL
	SET @ToDate = GETUTCDATE()
DECLARE @SQL NVARCHAR(MAX)
DECLARE @DateGroupingSQL NVARCHAR(MAX)
DECLARE @DateGroupingJoin NVARCHAR(MAX)
DECLARE @Table NVARCHAR(MAX)
SELECT @DateGroupingSQL= CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin=0 THEN 'W.SnapshotDate'
			ELSE 'DG.DateGroup' END,
		 @DateGroupingJoin = CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin=0 THEN ''
			ELSE 'CROSS APPLY dbo.DateGroupingMins(W.SnapshotDate,@DateGroupingMin) DG' END 

SELECT @Table = CASE WHEN @DateGroupingMin>=60 THEN 'dbo.Waits_60MIN' ELSE 'dbo.Waits' END


SET @SQL = N'
WITH T AS (
SELECT ' + @DateGroupingSQL + ' AS [Time],
			CASE WHEN WT.IsCriticalWait=1 THEN ''!!'' ELSE '''' END + WT.WaitType as WaitType,
			SUM(W.wait_time_ms)*1000.0 / SUM(W.sample_ms_diff) WaitTimeMsPerSec,
			ROW_NUMBER() OVER(PARTITION BY ' + @DateGroupingSQL + ' ORDER BY SUM(W.wait_time_ms) DESC) rnum
FROM ' + @Table + ' W 
' + @DateGroupingJoin + '
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

EXEC sp_executesql @SQL,N'@FromDate DATETIME2(3),@ToDate DATETIME2(3),@InstanceID INT,@Top INT,@DateGroupingMin INT',@FromDate,@ToDate,@InstanceID,@Top,@DateGroupingMin