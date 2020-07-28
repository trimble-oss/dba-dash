CREATE PROC [dbo].[Waits_Get](
	@InstanceID INT,
	@FromDate DATETIME2(3)=NULL, 
	@ToDate DATETIME2(3)=NULL,
	@DateGrouping VARCHAR(50)='None'
)
AS
IF @FromDate IS NULL
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
IF @ToDate IS NULL
	SET @ToDate = GETUTCDATE()
DECLARE @SQL NVARCHAR(MAX)
DECLARE @DateGroupingSQL NVARCHAR(MAX)
SELECT @DateGroupingSQL= CASE WHEN @DateGrouping = 'None' THEN 'W.SnapshotDate'
			WHEN @DateGrouping ='10MIN' THEN 'CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,W.SnapshotDate,120),15) + ''0'',120)'
			WHEN @DateGrouping = '60MIN' THEN 'CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,W.SnapshotDate,120),13) + '':00'',120)'
			WHEN @DateGrouping ='DAY' THEN 'CAST(CAST(W.SnapshotDate as DATE) as DATETIME)'
			ELSE NULL END


SET @SQL = N'
SELECT ' + @DateGroupingSQL + ' AS [Time],
			WT.WaitType,
			SUM(W.wait_time_ms)*1000 / SUM(W.sample_ms_diff) WaitTimeMsPerSec
FROM dbo.Waits W 
JOIN dbo.WaitType WT ON WT.WaitTypeID = W.WaitTypeID
WHERE W.SnapshotDate>= @FromDate
AND W.SnapshotDate <= @ToDate
AND WT.WaitType <>''REDO_THREAD_PENDING_WORK''
AND W.InstanceID=@InstanceID
GROUP BY WT.WaitType, ' + @DateGroupingSQL + ' 
HAVING SUM(W.wait_time_ms)*1000 / SUM(W.sample_ms_diff) > 1
ORDER BY WT.WaitType'

EXEC sp_executesql @SQL,N'@FromDate DATETIME2(3),@ToDate DATETIME2(3),@InstanceID INT',@FromDate,@ToDate,@InstanceID