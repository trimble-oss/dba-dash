CREATE PROC dbo.JobTimelineSummary_Get(
	@InstanceIDs IDs READONLY,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@category NVARCHAR(128)=NULL,
	@JobName NVARCHAR(128)=NULL,
	@MergeGapSec INT=0,
	@MinDurationSec INT=0,
	@ProblemsOnly BIT=0
)
AS
/*
	The job timeline across many instances: one row per instance, with the instance's job executions merged into
	"busy" spans, so a collapsed instance shows when anything was running on it.  The jobs of an instance are loaded
	with dbo.JobTimeline_Get when it is expanded.

	Result sets:
	1. Instances - every instance in @InstanceIDs that has agent jobs (in @category, with @JobName in their name), including those with no executions
	   in the period, so a missing run shows as a gap rather than a missing row.
	2. Spans - executions merged per instance.  Executions are merged when they overlap or the gap between them is no
	   more than @MergeGapSec.  The caller passes the time the narrowest bar covers (a few pixels), which caps the spans per instance at
	   roughly the chart's width however many jobs there are.  run_status is the worst status in the span
	   (Failed, Retry, Cancelled, In Progress, then Succeeded) and Executions the number merged.
	3. Status counts - executions per instance and run_status, for the legend.

	@MinDurationSec and @ProblemsOnly filter the executions as in JobTimeline_Get.  Problems only also leaves out the
	instances with none.

	Job outcomes only (step_id 0), selected as in JobTimeline_Get: executions that started before @ToDate and finished
	after @FromDate, with a 1 day lookback, plus executions still running from dbo.RunningJobs.
*/
SET NOCOUNT ON
DECLARE @AsOf DATETIME2(2) = CAST(GETUTCDATE() AS DATETIME2(2))
SET @MergeGapSec = ISNULL(@MergeGapSec, 0)

CREATE TABLE #Instances(InstanceID INT NOT NULL PRIMARY KEY)

INSERT INTO #Instances(InstanceID)
SELECT ID
FROM @InstanceIDs

CREATE TABLE #Runs(
	InstanceID INT NOT NULL,
	RunDateTime DATETIME2(2) NOT NULL,
	FinishDateTime DATETIME2(2) NOT NULL,
	run_status INT NOT NULL
)

INSERT INTO #Runs(InstanceID, RunDateTime, FinishDateTime, run_status)
SELECT	JH.InstanceID,
		JH.RunDateTime,
		CASE WHEN JH.FinishDateTime > JH.RunDateTime THEN JH.FinishDateTime ELSE DATEADD(s, 1, JH.RunDateTime) END,
		ISNULL(JH.run_status, 0)
FROM #Instances I
JOIN dbo.Jobs J ON J.InstanceID = I.InstanceID
JOIN dbo.JobHistory JH ON JH.InstanceID = J.InstanceID AND JH.job_id = J.job_id
WHERE JH.step_id = 0
AND JH.FinishDateTime >= @FromDate
AND JH.RunDateTime < @ToDate
AND JH.RunDateTime > DATEADD(d, -1, @FromDate)
AND JH.RunDurationSec >= ISNULL(@MinDurationSec, 0)
AND (@ProblemsOnly = 0 OR JH.run_status IN (0,2,3)) /* failed, retry, cancelled */
AND (@category IS NULL OR J.category = @category)
AND (@JobName IS NULL OR CHARINDEX(@JobName, J.name) > 0)
UNION ALL
/* Still running: JobHistory has no outcome until the job finishes */
SELECT	RJ.InstanceID,
		CAST(RJ.start_execution_date_utc AS DATETIME2(2)),
		CASE WHEN CAST(RJ.start_execution_date_utc AS DATETIME2(2)) >= OE.ObservedEnd THEN DATEADD(s, 1, CAST(RJ.start_execution_date_utc AS DATETIME2(2))) ELSE OE.ObservedEnd END,
		4
FROM #Instances I
JOIN dbo.RunningJobs RJ ON RJ.InstanceID = I.InstanceID
JOIN dbo.Jobs J ON J.InstanceID = RJ.InstanceID AND J.job_id = RJ.job_id
CROSS APPLY (
		SELECT CASE WHEN RJ.SnapshotDate >= @AsOf THEN @AsOf ELSE CAST(RJ.SnapshotDate AS DATETIME2(2)) END AS ObservedEnd
	) OE
WHERE RJ.start_execution_date_utc IS NOT NULL
AND RJ.start_execution_date_utc < CAST(@ToDate AS DATETIME)
AND RJ.SnapshotDate >= CAST(@FromDate AS DATETIME)
AND (RJ.current_execution_status IS NULL OR RJ.current_execution_status <> 4) /* not idle */
AND @ProblemsOnly = 0 /* running isn't a problem */
AND DATEDIFF(s, RJ.start_execution_date_utc, OE.ObservedEnd) >= ISNULL(@MinDurationSec, 0) /* elapsed so far */
AND (@category IS NULL OR J.category = @category)
AND (@JobName IS NULL OR CHARINDEX(@JobName, J.name) > 0)
AND NOT EXISTS (
		SELECT 1
		FROM dbo.JobHistory JH2
		WHERE JH2.InstanceID = RJ.InstanceID
		AND JH2.job_id = RJ.job_id
		AND JH2.step_id = 0
		AND JH2.RunDateTime >= CAST(RJ.start_execution_date_utc AS DATETIME2(2))
		AND JH2.RunDateTime < @AsOf
	)

/* 1. Instances */
SELECT	Inst.InstanceID,
		Inst.InstanceDisplayName
FROM #Instances I
JOIN dbo.Instances Inst ON Inst.InstanceID = I.InstanceID
WHERE EXISTS (
		SELECT 1
		FROM dbo.Jobs J
		WHERE J.InstanceID = I.InstanceID
		AND (@category IS NULL OR J.category = @category)
		AND (@JobName IS NULL OR CHARINDEX(@JobName, J.name) > 0)
	)
/* Problems only lists just the instances that had some */
AND (@ProblemsOnly = 0 OR EXISTS (SELECT 1 FROM #Runs R WHERE R.InstanceID = I.InstanceID))
ORDER BY Inst.InstanceDisplayName

/*
	2. Spans - gaps and islands.  A run starts a new span when it starts later than every earlier run (by start time)
	on the instance had finished, plus the merge gap.
*/
;WITH Ordered AS (
	SELECT	InstanceID,
			RunDateTime,
			FinishDateTime,
			run_status,
			MAX(FinishDateTime) OVER (PARTITION BY InstanceID ORDER BY RunDateTime, FinishDateTime ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING) AS PrevFinish
	FROM #Runs
),
Islands AS (
	SELECT	*,
			SUM(CASE WHEN PrevFinish IS NOT NULL AND DATEADD(s, @MergeGapSec, PrevFinish) >= RunDateTime THEN 0 ELSE 1 END)
				OVER (PARTITION BY InstanceID ORDER BY RunDateTime, FinishDateTime ROWS UNBOUNDED PRECEDING) AS Island
	FROM Ordered
),
Spans AS (
	SELECT	InstanceID,
			MIN(RunDateTime) AS RunDateTime,
			MAX(FinishDateTime) AS FinishDateTime,
			MAX(CASE run_status WHEN 0 THEN 5 WHEN 2 THEN 4 WHEN 3 THEN 3 WHEN 4 THEN 2 ELSE 1 END) AS Severity,
			COUNT(*) AS Executions
	FROM Islands
	GROUP BY InstanceID, Island
)
SELECT	InstanceID,
		RunDateTime,
		FinishDateTime,
		DATEDIFF(s, RunDateTime, FinishDateTime) AS RunDurationSec,
		CASE Severity WHEN 5 THEN 0 WHEN 4 THEN 2 WHEN 3 THEN 3 WHEN 2 THEN 4 ELSE 1 END AS run_status,
		Executions
FROM Spans
ORDER BY InstanceID, RunDateTime

/* 3. Status counts */
SELECT	InstanceID,
		run_status,
		COUNT(*) AS Executions
FROM #Runs
GROUP BY InstanceID, run_status
