CREATE PROC dbo.Waits_Get(
	@InstanceID INT,
	@FromDate DATETIME2(2)=NULL, 
	@ToDate DATETIME2(2)=NULL,
	@DateGroupingMin INT=NULL,
	@Top INT=10,
	@WaitType NVARCHAR(60)=NULL,
	@CriticalWaitsOnly BIT=0,
	@Use60MIN BIT=NULL,
	@WaitTypeID INT=NULL
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
IF @WaitType IS NOT NULL AND @WaitType NOT LIKE '%[%]%' AND @WaitTypeID IS NULL /* Filtering for a specific wait type - get the WaitTypeID */
BEGIN
	SELECT @WaitTypeID = WaitTypeID
	FROM dbo.WaitType 
	WHERE WaitType = @WaitType
END
IF @WaitTypeID IS NOT NULL /* Ignore @WaitType if we have @WaitTypeID */
BEGIN
	SET @WaitType = NULL
END

CREATE TABLE #WaitGrp(
	[Time] DATETIME2(2) NOT NULL,
	WaitTypeID SMALLINT NOT NULL,
	WaitTimeMsPerSec DECIMAL(19,5) NULL
)

SET @SQL = N'
SELECT ' + @DateGroupingSQL + ' AS [Time],
			W.WaitTypeID,
			SUM(W.wait_time_ms)*1000.0 / SUM(W.sample_ms_diff) AS WaitTimeMsPerSec
FROM dbo.Waits' + CASE WHEN @Use60MIN =1 THEN '_60MIN' ELSE '' END + ' W 
' + @DateGroupingJoin + '
JOIN dbo.WaitType WT ON WT.WaitTypeID = W.WaitTypeID
WHERE W.SnapshotDate>= @FromDate
AND W.SnapshotDate <= @ToDate
AND W.InstanceID=@InstanceID
' + CASE WHEN @CriticalWaitsOnly=1 THEN 'AND WT.IsCriticalWait=1' ELSE '' END + '
' + CASE WHEN @WaitType IS NULL THEN '' ELSE 'AND WT.WaitType LIKE @WaitType' END + '
AND WT.IsExcluded = 0
GROUP BY W.WaitTypeID, ' + @DateGroupingSQL 

INSERT INTO #WaitGrp([Time],WaitTypeID,WaitTimeMsPerSec)
EXEC sp_executesql @SQL,N'@FromDate DATETIME2(2),@ToDate DATETIME2(2),@InstanceID INT,@Top INT,@DateGroupingMin INT,@WaitType NVARCHAR(60)',@FromDate,@ToDate,@InstanceID,@Top,@DateGroupingMin,@WaitType;

IF @WaitTypeID IS NOT NULL -- Filtering for a specific wait type
BEGIN
	/*	If a wait doesn't accumulate for a time period it won't be present.  
		This query will fill in the blanks with zeros when we are filtering for a specific wait type - otherwise the charts can look misleading.
	*/
	SELECT T.Time,WT.WaitType,ISNULL(W.WaitTimeMsPerSec,0) AS WaitTimeMsPerSec
	FROM (SELECT DISTINCT [Time] FROM #WaitGrp) T 
	CROSS JOIN dbo.WaitType WT 
	LEFT JOIN #WaitGrp W ON W.Time = T.Time AND WT.WaitTypeID = W.WaitTypeID
	WHERE WT.WaitTypeID = @WaitTypeID
	ORDER BY WT.WaitType,T.Time
END;
ELSE
BEGIN
	/* 
		Show the top waits for each time period and group other waits as "{Other}"
	*/
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
	ORDER BY WaitType,T.Time
END

DROP TABLE #WaitGrp