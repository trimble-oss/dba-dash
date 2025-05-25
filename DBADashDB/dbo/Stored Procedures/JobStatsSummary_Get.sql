CREATE PROC dbo.JobStatsSummary_Get(
	@InstanceID INT,
	@JobID UNIQUEIDENTIFIER=NULL,
	@StepID INT=NULL,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@Debug BIT =0,
	@Use60MIN BIT=NULL
)
AS
DECLARE @SQL NVARCHAR(MAX)
IF @Use60MIN IS NULL
BEGIN
	SELECT @Use60MIN = CASE WHEN DATEDIFF(hh,@FromDate,@ToDate)>24 THEN 1
						WHEN DATEPART(mi,@FromDate)+DATEPART(s,@FromDate)+DATEPART(ms,@FromDate)=0 
							AND (DATEPART(mi,@ToDate)+DATEPART(s,@ToDate)+DATEPART(ms,@ToDate)=0 
									OR @ToDate>=DATEADD(s,-2,GETUTCDATE())
								)
						THEN 1
						ELSE 0 END
END

SET @SQL = N'
WITH T AS (
	SELECT JS.job_id,
			JS.step_id,
			ISNULL(J.name,CAST(JS.job_id AS NVARCHAR(128))) JobName,
			CASE WHEN JS.step_id=0 THEN ''{outcome}'' ELSE step.step_name END as JobStep,
			SUM(JS.FailedCount) AS FailedCount,
			SUM(JS.SucceededCount) AS SucceededCount,
			SUM(JS.RetryCount) AS RetryCount,
			SUM(JS.RunDurationSec)/NULLIF(SUM(JS.SucceededCount+JS.FailedCount),0) AS AvgDurationSec,
			MAX(JS.MaxRunDurationSec) AS MaxDurationSec,
			MIN(JS.MinRunDurationSec) AS MinDurationSec,
			SUM(JS.RunDurationSec) AS TotalDurationSec
	FROM ' + CASE WHEN @Use60MIN=0 THEN 'dbo.JobStats_RAW' ELSE 'dbo.JobStats_60MIN' END + ' AS JS
	LEFT JOIN dbo.Jobs J ON J.job_id = JS.job_id AND J.InstanceID = JS.InstanceID
	LEFT JOIN dbo.JobSteps step ON JS.job_id = step.job_id AND JS.InstanceID = step.InstanceID AND JS.step_id = step.step_id
	WHERE JS.InstanceID=@InstanceID
	' + CASE WHEN @JobID IS NULL OR @JobID = '00000000-0000-0000-0000-000000000000' THEN '' ELSE 'AND JS.job_id=@JobID' END + '
	' + CASE WHEN @StepID IS NULL THEN '' ELSE 'AND JS.step_id=@StepID' END + '
	AND JS.RunDateTime>=@FromDate
	AND JS.RunDateTime < @ToDate
	GROUP BY JS.job_id, J.name, JS.step_id,step.step_name
)
SELECT  T.job_id,
		T.step_id,
		T.JobName,
		T.JobStep,
		T.FailedCount,
		T.SucceededCount,
		T.RetryCount,
		T.AvgDurationSec,		
		T.MaxDurationSec,		
		T.MinDurationSec,		
		T.TotalDurationSec,
		HDAvg.HumanDuration AS AvgDuration,
		HDMax.HumanDuration AS MaxDuration,
		HDMin.HumanDuration AS MinDuration,
		HDTotal.HumanDuration AS TotalDuration,
		@InstanceID InstanceID
FROM T
CROSS APPLY dbo.SecondsToHumanDuration(T.AvgDurationSec) HDAvg
CROSS APPLY dbo.SecondsToHumanDuration(T.MaxDurationSec) HDMax
CROSS APPLY dbo.SecondsToHumanDuration(T.MinDurationSec) HDMin
CROSS APPLY dbo.SecondsToHumanDuration(T.TotalDurationSec) HDTotal'

IF @Debug=1
BEGIN
	PRINT @SQL
END

EXEC sp_executesql @SQL,N'@InstanceID INT,@JobID UNIQUEIDENTIFIER,@StepID INT,@FromDate DATETIME2(2),@ToDate DATETIME2(2)',@InstanceID,@JobID,@StepID,@FromDate,@ToDate