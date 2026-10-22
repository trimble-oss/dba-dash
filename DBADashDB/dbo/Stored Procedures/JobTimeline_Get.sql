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

	JobHistory has no step_id 0 outcome row until the job finishes, so a job that is still
	executing would otherwise be missing. Those executions are added from dbo.RunningJobs.
	The bar ends at the collection snapshot (or at UTC now when the snapshot is ahead of now).
	It is not extended past the last observation. The same applies to the step that is currently
	executing when steps are included. Running jobs are not subject to the 1-day JobHistory
	lookback; that filter is only there to limit partition scans and would hide long-running work.
	RunningJobs has at most one row per job.
*/
SET @DateGroupingMin = ISNULL(@DateGroupingMin,0)
DECLARE @AsOf DATETIME2(2) = CAST(GETUTCDATE() AS DATETIME2(2))
DECLARE @SQL NVARCHAR(MAX)
DECLARE @RunningSQL NVARCHAR(MAX) = N''
DECLARE @CategoryFilter NVARCHAR(MAX) = CASE WHEN @category IS NULL THEN N'' ELSE N'
AND J.category = @category' END
DECLARE @JobFilter NVARCHAR(MAX) = CASE WHEN @job_id IS NULL THEN N'' ELSE N'
AND RJ.job_id = @job_id' END

/* Outcome row for a job that is still executing. run_status 4 = In Progress. */
IF @IncludeOutcome = 1
BEGIN
	SET @RunningSQL = @RunningSQL + N'
UNION ALL
SELECT	J.name,
		RJ.job_id,
		CAST(NULL AS INT) AS instance_id,
		''Outcome'' AS step_name,
		0 AS step_id,
		CAST(RJ.start_execution_date_utc AS DATETIME2(2)) AS RunDateTime,
		CASE WHEN RJ.start_execution_date_utc >= OE.ObservedEnd THEN 0 ELSE DATEDIFF(s, RJ.start_execution_date_utc, OE.ObservedEnd) END AS RunDurationSec,
		CASE WHEN CAST(RJ.start_execution_date_utc AS DATETIME2(2)) >= OE.ObservedEnd THEN DATEADD(s, 1, CAST(RJ.start_execution_date_utc AS DATETIME2(2))) ELSE OE.ObservedEnd END AS FinishDateTime,
		4 AS run_status,
		1 AS Executions
FROM dbo.RunningJobs RJ
JOIN dbo.Jobs J ON RJ.job_id = J.job_id AND J.InstanceID = RJ.InstanceID
CROSS APPLY (
		SELECT CASE WHEN RJ.SnapshotDate >= @AsOf THEN @AsOf ELSE CAST(RJ.SnapshotDate AS DATETIME2(2)) END AS ObservedEnd
	) OE
WHERE RJ.InstanceID = @InstanceID
AND RJ.start_execution_date_utc IS NOT NULL
AND RJ.start_execution_date_utc < CAST(@ToDate AS DATETIME) /* started before the end of the selected period */
AND RJ.SnapshotDate >= CAST(@FromDate AS DATETIME) /* still observed running at or after the start of the selected period */
AND (RJ.current_execution_status IS NULL OR RJ.current_execution_status <> 4) /* not idle */
' + @CategoryFilter + @JobFilter + N'
AND NOT EXISTS ( /* outcome already captured in history for this execution */
		SELECT 1
		FROM dbo.JobHistory JH2
		WHERE JH2.InstanceID = RJ.InstanceID
		AND JH2.job_id = RJ.job_id
		AND JH2.step_id = 0
		AND JH2.RunDateTime >= CAST(RJ.start_execution_date_utc AS DATETIME2(2))
		AND JH2.RunDateTime < @AsOf
	)'
END

/* The step that is executing now is not in JobHistory until it completes. */
IF @IncludeSteps = 1
BEGIN
	SET @RunningSQL = @RunningSQL + N'
UNION ALL
SELECT	J.name,
		RJ.job_id,
		CAST(NULL AS INT) AS instance_id,
		ISNULL(JS.step_name, ISNULL(RJ.current_execution_step_name, ''Step:'' + CAST(RJ.current_execution_step_id AS NVARCHAR(128)))) AS step_name,
		RJ.current_execution_step_id AS step_id,
		SS.StepStart AS RunDateTime,
		CASE WHEN SS.StepStart >= OE.ObservedEnd THEN 0 ELSE DATEDIFF(s, SS.StepStart, OE.ObservedEnd) END AS RunDurationSec,
		CASE WHEN SS.StepStart >= OE.ObservedEnd THEN DATEADD(s, 1, SS.StepStart) ELSE OE.ObservedEnd END AS FinishDateTime,
		4 AS run_status,
		1 AS Executions
FROM dbo.RunningJobs RJ
JOIN dbo.Jobs J ON RJ.job_id = J.job_id AND J.InstanceID = RJ.InstanceID
LEFT JOIN dbo.JobSteps JS ON JS.InstanceID = RJ.InstanceID AND JS.job_id = RJ.job_id AND JS.step_id = RJ.current_execution_step_id
CROSS APPLY (
		SELECT CASE WHEN RJ.SnapshotDate >= @AsOf THEN @AsOf ELSE CAST(RJ.SnapshotDate AS DATETIME2(2)) END AS ObservedEnd
	) OE
CROSS APPLY (
		SELECT COALESCE(Prev.FinishDateTime, CASE WHEN RJ.last_executed_step_id IS NULL THEN CAST(RJ.start_execution_date_utc AS DATETIME2(2)) END) AS StepStart,
				Prev.instance_id AS PrevInstanceID
		FROM (VALUES (1)) AS D(n)
		OUTER APPLY (
				SELECT TOP (1) JH.FinishDateTime, JH.instance_id
				FROM dbo.JobHistory JH
				WHERE RJ.last_executed_step_id IS NOT NULL
				AND JH.InstanceID = RJ.InstanceID
				AND JH.job_id = RJ.job_id
				AND JH.step_id = RJ.last_executed_step_id
				AND JH.RunDateTime >= CAST(RJ.start_execution_date_utc AS DATETIME2(2))
				AND JH.RunDateTime < @AsOf
				ORDER BY JH.instance_id DESC
			) Prev
	) SS
WHERE RJ.InstanceID = @InstanceID
AND RJ.start_execution_date_utc IS NOT NULL
AND RJ.current_execution_step_id > 0
AND SS.StepStart IS NOT NULL
AND SS.StepStart < @ToDate
AND RJ.SnapshotDate >= CAST(@FromDate AS DATETIME)
AND (RJ.current_execution_status IS NULL OR RJ.current_execution_status <> 4) /* not idle */
' + @CategoryFilter + @JobFilter + N'
AND NOT EXISTS ( /* a later history row is this attempt, not the previous zero-duration execution of the same step */
		SELECT 1
		FROM dbo.JobHistory JH2
		WHERE JH2.InstanceID = RJ.InstanceID
		AND JH2.job_id = RJ.job_id
		AND JH2.step_id = RJ.current_execution_step_id
		AND JH2.RunDateTime >= CAST(RJ.start_execution_date_utc AS DATETIME2(2))
		AND JH2.RunDateTime < @AsOf
		AND JH2.instance_id > ISNULL(SS.PrevInstanceID, 0)
	)'
END

SET @SQL = N'
SELECT	name,
		job_id,
		instance_id,
		step_name,
		step_id,
		RunDateTime,
		RunDurationSec,
		FinishDateTime,
		run_status,
		Executions
FROM (
SELECT J.name,
	J.job_id,
	' + CASE WHEN @DateGroupingMin= 0 THEN 'JH.instance_id,' ELSE 'CASE WHEN COUNT(*) = 1 THEN MAX(JH.instance_id) END AS instance_id,' END + '
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
			ELSE 'GROUP BY J.name, J.job_id, DG.DateGroup,JH.run_status,JH.step_id' +
					CASE WHEN @IncludeSteps=1 THEN ',JS.step_name' ELSE '' END
			END + '
' + @RunningSQL + N'
) Timeline
ORDER BY name,
		step_id,
		RunDateTime'

IF @Debug=1
	PRINT @SQL

EXEC sp_executesql @SQL,N'@InstanceID INT,@FromDate DATETIME2(2),@ToDate DATETIME2(2),@category NVARCHAR(128),@job_id UNIQUEIDENTIFIER,@DateGroupingMin INT,@AsOf DATETIME2(2)',
						@InstanceID,@FromDate,@ToDate,@category,@job_id,@DateGroupingMin,@AsOf
