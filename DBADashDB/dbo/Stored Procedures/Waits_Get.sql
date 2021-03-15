CREATE   PROC [dbo].[Waits_Get](
	@InstanceID INT,
	@FromDate DATETIME2(2)=NULL, 
	@ToDate DATETIME2(2)=NULL,
	@DateGroupingMin INT=NULL,
	@Top INT=10,
	@WaitType NVARCHAR(60)=NULL,
	@CriticalWaitsOnly BIT=0,
	@Use60MIN BIT=NULL
)
AS
IF @FromDate IS NULL
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
IF @ToDate IS NULL
	SET @ToDate = GETUTCDATE()
DECLARE @SQL NVARCHAR(MAX)
DECLARE @DateGroupingSQL NVARCHAR(MAX)
DECLARE @DateGroupingJoin NVARCHAR(MAX)

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

CREATE TABLE #WaitGrp(
	[Time] DATETIME2(2) NOT NULL,
	WaitTypeID SMALLINT NOT NULL,
	WaitTimeMsPerSec DECIMAL(19,5) NULL
)

SET @SQL = N'
SELECT ' + @DateGroupingSQL + ' AS [Time],
			W.WaitTypeID,
			SUM(W.wait_time_ms)*1000.0 / SUM(W.sample_ms_diff) WaitTimeMsPerSec
FROM dbo.Waits' + CASE WHEN @Use60MIN =1 THEN '_60MIN' ELSE '' END + ' W 
' + @DateGroupingJoin + '
JOIN dbo.WaitType WT ON WT.WaitTypeID = W.WaitTypeID
WHERE W.SnapshotDate>= @FromDate
AND W.SnapshotDate <= @ToDate
AND WT.WaitType NOT IN(N''PVS_PREALLOCATE'',N''REDO_THREAD_PENDING_WORK'')
AND W.InstanceID=@InstanceID
' + CASE WHEN @CriticalWaitsOnly=1 THEN 'AND WT.IsCriticalWait=1' ELSE '' END + '
' + CASE WHEN @WaitType IS NULL THEN '' ELSE 'AND WT.WaitType LIKE @WaitType' END + '
GROUP BY W.WaitTypeID, ' + @DateGroupingSQL + ' 
HAVING SUM(W.wait_time_ms)*1000.0 / SUM(W.sample_ms_diff) > 0;'

INSERT INTO #WaitGrp([Time],WaitTypeID,WaitTimeMsPerSec)
EXEC sp_executesql @SQL,N'@FromDate DATETIME2(2),@ToDate DATETIME2(2),@InstanceID INT,@Top INT,@DateGroupingMin INT,@WaitType NVARCHAR(60)',@FromDate,@ToDate,@InstanceID,@Top,@DateGroupingMin,@WaitType;

WITH T AS (
	SELECT *,ROW_NUMBER() OVER(PARTITION BY [Time] ORDER BY WaitTimeMsPerSec DESC) rnum
	FROM #WaitGrp T1
)
SELECT [Time],
	CASE WHEN rnum> @Top THEN '{Other}' WHEN WT.IsCriticalWait=1 THEN '!!'  + WT.WaitType ELSE WT.WaitType END as WaitType,
	SUM(WaitTimeMsPerSec) as WaitTimeMsPerSec
FROM T 
JOIN dbo.WaitType WT ON WT.WaitTypeID = T.WaitTypeID
GROUP BY [Time],CASE WHEN rnum> @Top THEN '{Other}' WHEN WT.IsCriticalWait=1 THEN '!!'  + WT.WaitType ELSE WT.WaitType END
ORDER BY WaitType