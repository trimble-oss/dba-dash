CREATE PROC dbo.JobStats_Get(
	@InstanceID INT,
	@JobID UNIQUEIDENTIFIER,
	@StepID INT,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@DateGroupingMin INT OUT,
	@Debug BIT =0
)
AS

SELECT @DateGroupingMin= CASE WHEN EXISTS(SELECT 1 
											FROM dbo.DataRetention
											WHERE TableName='JobHistory'
											AND @FromDate < DATEADD(d,-RetentionDays,GETUTCDATE())
											AND @DateGroupingMin<60
											) THEN 60
									ELSE @DateGroupingMin END
DECLARE @DateCol NVARCHAR(MAX) = CASE WHEN @DateGroupingMin=0 THEN 'JS.RunDateTime' ELSE 'G.DateGroup' END


DECLARE @SQL NVARCHAR(MAX)

SET @SQL =N'
SELECT ' + @DateCol + ' AS DateGroup,
		SUM(FailedCount) AS FailedCount,
		SUM(SucceededCount) AS SucceededCount,
		SUM(RetryCount) AS RetryCount,
		SUM(RunDurationSec)/NULLIF(SUM(SucceededCount+FailedCount),0) AS AvgDurationSec,
		MAX(JS.MaxRunDurationSec) AS MaxDurationSec,
		MIN(JS.MinRunDurationSec) AS MinDurationSec,
		SUM(RunDurationSec) AS TotalDurationSec
FROM ' + CASE WHEN @DateGroupingMin <60 THEN 'dbo.JobStats_RAW' ELSE 'dbo.JobStats_60MIN' END + ' AS JS
' + CASE WHEN @DateGroupingMin=0 THEN '' ELSE 'CROSS APPLY dbo.DateGroupingMins(JS.RunDateTime,@DateGroupingMin) G' END + '
WHERE JS.InstanceID=@InstanceID
AND JS.job_id=@JobID
AND JS.step_id=@StepID
AND JS.RunDateTime>=@FromDate
AND JS.RunDateTime < @ToDate
GROUP BY ' + @DateCol + '
ORDER BY ' + @DateCol 

IF @Debug=1
BEGIN
	PRINT @SQL
END

EXEC sp_executesql @SQL,N'@InstanceID INT,@JobID UNIQUEIDENTIFIER,@StepID INT,@FromDate DATETIME2(2),@ToDate DATETIME2(2),@DateGroupingMin INT',@InstanceID,@JobID,@StepID,@FromDate,@ToDate,@DateGroupingMin
