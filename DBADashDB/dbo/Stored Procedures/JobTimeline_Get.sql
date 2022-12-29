CREATE PROC dbo.JobTimeline_Get(
	@InstanceID INT,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@category NVARCHAR(128)=NULL,
	@IncludeSteps BIT=0,
	@IncludeOutcome BIT=1,
	@job_id UNIQUEIDENTIFIER=NULL,
	@Debug BIT=0,
	@DateGroupingMin INT =0
)
AS
/*	
	We want jobs that were executing between the selected @FromDate and @ToDate.
	The job should have started before the end of our selected time period and finished after the start of our time period.
	
	Illustration:

		start							end
		[..................................] our selected time period. 
			[.......] Job included
[......................] Job included
				[..............................] Job included
[..............................................] Job included
[..] not included
												[...] Not included
	
*/
SET @DateGroupingMin = ISNULL(@DateGroupingMin,0)
DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT J.name,
	' + CASE WHEN @IncludeSteps=1 THEN 'CASE WHEN JH.step_id = 0 THEN ''Outcome'' ELSE ISNULL(JS.step_name,''Step:'' + CAST(JH.step_id AS NVARCHAR(128))) END AS step_name,' ELSE '''Outcome'' AS step_name,' END + '
	JH.step_id,
	' + CASE WHEN @DateGroupingMin= 0 THEN 'JH.RunDateTime,' ELSE 'MIN(JH.RunDateTime) as RunDateTime,' END + '
	' + CASE WHEN @DateGroupingMin= 0 THEN 'JH.RunDurationSec,' ELSE 'AVG(JH.RunDurationSec) AS RunDurationSec,' END + '
	' + CASE WHEN @DateGroupingMin= 0 THEN 'JH.FinishDateTime,' ELSE 'MAX(JH.FinishDateTime) AS FinishDateTime,' END + '
	JH.run_status,
	' + CASE WHEN @DateGroupingMin= 0 THEN '1 AS Executions' ELSE 'COUNT(*) Executions' END + '
FROM dbo.JobHistory JH
JOIN dbo.Jobs J ON JH.job_id = J.job_id AND J.InstanceID = JH.InstanceID
' + CASE WHEN @IncludeSteps=1 THEN 'LEFT JOIN dbo.JobSteps JS ON JH.job_id = JS.job_id AND JH.InstanceID = JS.InstanceID AND JH.step_id = JS.step_id' ELSE '' END + '
' + CASE WHEN @DateGroupingMin= 0 THEN '' ELSE 'CROSS APPLY dbo.DateGroupingMins(RunDateTime,@DateGroupingMin) DG' END + '
WHERE JH.InstanceID = @InstanceID
AND JH.FinishDateTime >= @FromDate -- Finished after start of selected time period
AND JH.RunDateTime < @ToDate -- Sarted before end of selected time period
AND JH.RunDateTime > DATEADD(d,-1,@FromDate) -- started after 1 day before start of selected time period. (For performance - will exclude very long running jobs)
' + CASE WHEN @IncludeSteps = 0 THEN 'AND JH.step_id=0 /* Job outcome step */' ELSE '' END + '
' + CASE WHEN @IncludeOutcome = 0 THEN 'AND JH.step_id > 0 /* Exclude outcome */' ELSE '' END + '
' + CASE WHEN @category IS NULL THEN '' ELSE 'AND J.category = @category' END + '
' + CASE WHEN @job_id IS NULL THEN '' ELSE 'AND JH.job_id = @job_id' END + '
' + CASE WHEN @DateGroupingMin= 0 
			THEN '' 
			ELSE 'GROUP BY J.name, DG.DateGroup,JH.run_status,JH.step_id' + 
					CASE WHEN @IncludeSteps=1 THEN ',JS.step_name' ELSE '' END 
			END + '
ORDER BY J.name,
		JH.step_id,
		RunDateTime'

IF @Debug=1
	PRINT @SQL

EXEC sp_executesql @SQL,N'@InstanceID INT,@FromDate DATETIME2(2),@ToDate DATETIME2(2),@category NVARCHAR(128),@job_id UNIQUEIDENTIFIER,@DateGroupingMin INT',
						@InstanceID,@FromDate,@ToDate,@category,@job_id,@DateGroupingMin