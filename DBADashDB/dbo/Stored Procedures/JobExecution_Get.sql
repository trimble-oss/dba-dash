CREATE PROC dbo.JobExecution_Get(
	@InstanceID INT,
	@job_id UNIQUEIDENTIFIER,
	@instance_id INT=NULL
)
AS
/*
	Every history row of one execution of a job: the step rows and the job outcome (step_id 0).

	@instance_id is the msdb instance_id of any history row belonging to the execution - the outcome or one of its steps.
	Agent writes step rows as each step finishes and the outcome row last, so the execution is the rows after the
	previous outcome up to and including the first outcome at or after @instance_id.

	When no outcome follows (the job is still running), or @instance_id is NULL (the latest execution), the steps
	completed so far are returned along with an In Progress outcome row and an In Progress row for the current step,
	taken from dbo.RunningJobs.  As in JobTimeline_Get, a running row ends at the collection snapshot (capped at now).
*/
DECLARE @OutcomeID INT
DECLARE @FromID INT
DECLARE @AsOf DATETIME2(2) = CAST(GETUTCDATE() AS DATETIME2(2))

IF @instance_id IS NOT NULL
BEGIN
	SELECT TOP(1) @OutcomeID = instance_id
	FROM dbo.JobHistory
	WHERE InstanceID = @InstanceID
	AND job_id = @job_id
	AND step_id = 0
	AND instance_id >= @instance_id
	ORDER BY instance_id
END

SELECT @FromID = ISNULL(MAX(instance_id),0)
FROM dbo.JobHistory
WHERE InstanceID = @InstanceID
AND job_id = @job_id
AND step_id = 0
AND instance_id < COALESCE(@OutcomeID, @instance_id, 2147483647)

DECLARE @Execution TABLE(
	instance_id INT NULL,
	step_id INT NOT NULL,
	step_name NVARCHAR(128) NULL,
	RunDateTime DATETIME2(2) NOT NULL,
	RunDurationSec INT NULL,
	FinishDateTime DATETIME2(2) NULL,
	run_status INT NULL,
	sql_message_id INT NULL,
	sql_severity INT NULL,
	message NVARCHAR(4000) NULL,
	retries_attempted INT NULL
)

INSERT INTO @Execution
SELECT	JH.instance_id,
		JH.step_id,
		CASE WHEN JH.step_id = 0 THEN N'Outcome' ELSE ISNULL(JS.step_name, ISNULL(JH.step_name, N'Step:' + CAST(JH.step_id AS NVARCHAR(128)))) END,
		JH.RunDateTime,
		JH.RunDurationSec,
		JH.FinishDateTime,
		JH.run_status,
		JH.sql_message_id,
		JH.sql_severity,
		JH.message,
		JH.retries_attempted
FROM dbo.JobHistory JH
LEFT JOIN dbo.JobSteps JS ON JS.InstanceID = JH.InstanceID AND JS.job_id = JH.job_id AND JS.step_id = JH.step_id
WHERE JH.InstanceID = @InstanceID
AND JH.job_id = @job_id
AND JH.instance_id > @FromID
AND (@OutcomeID IS NULL OR JH.instance_id <= @OutcomeID)

/* No outcome yet - add the running outcome and current step if this is the execution that is running now */
IF @OutcomeID IS NULL
BEGIN
	DECLARE @LastStepFinish DATETIME2(2) = (SELECT MAX(FinishDateTime) FROM @Execution)

	INSERT INTO @Execution(instance_id,step_id,step_name,RunDateTime,RunDurationSec,FinishDateTime,run_status,message,retries_attempted)
	SELECT	NULL,
			S.step_id,
			S.step_name,
			S.StartTime,
			CASE WHEN S.StartTime >= OE.ObservedEnd THEN 0 ELSE DATEDIFF(s, S.StartTime, OE.ObservedEnd) END,
			CASE WHEN S.StartTime >= OE.ObservedEnd THEN S.StartTime ELSE OE.ObservedEnd END,
			4,
			S.message,
			RJ.current_retry_attempt
	FROM dbo.RunningJobs RJ
	LEFT JOIN dbo.JobSteps JS ON JS.InstanceID = RJ.InstanceID AND JS.job_id = RJ.job_id AND JS.step_id = RJ.current_execution_step_id
	CROSS APPLY (
			SELECT CASE WHEN RJ.SnapshotDate >= @AsOf THEN @AsOf ELSE CAST(RJ.SnapshotDate AS DATETIME2(2)) END AS ObservedEnd
		) OE
	CROSS APPLY (
			SELECT	0 AS step_id,
					N'Outcome' AS step_name,
					CAST(RJ.start_execution_date_utc AS DATETIME2(2)) AS StartTime,
					CONCAT(N'Job is currently executing.', CASE WHEN RJ.current_execution_step_id IS NOT NULL THEN CONCAT(N' Current step: (', RJ.current_execution_step_id, N') ', RJ.current_execution_step_name) ELSE N'' END) AS message
			UNION ALL
			SELECT	RJ.current_execution_step_id,
					ISNULL(JS.step_name, ISNULL(RJ.current_execution_step_name, N'Step:' + CAST(RJ.current_execution_step_id AS NVARCHAR(128)))),
					CASE WHEN @LastStepFinish > CAST(RJ.start_execution_date_utc AS DATETIME2(2)) THEN @LastStepFinish ELSE CAST(RJ.start_execution_date_utc AS DATETIME2(2)) END,
					N'Step is currently executing.'
			WHERE RJ.current_execution_step_id > 0
		) S
	WHERE RJ.InstanceID = @InstanceID
	AND RJ.job_id = @job_id
	AND RJ.start_execution_date_utc IS NOT NULL
	AND (RJ.current_execution_status IS NULL OR RJ.current_execution_status <> 4) /* not idle */
	AND NOT EXISTS ( /* the running execution has already finished */
			SELECT 1
			FROM dbo.JobHistory JH2
			WHERE JH2.InstanceID = RJ.InstanceID
			AND JH2.job_id = RJ.job_id
			AND JH2.step_id = 0
			AND JH2.RunDateTime >= CAST(RJ.start_execution_date_utc AS DATETIME2(2))
		)
	/* the completed steps found belong to the running execution (none found also counts) */
	AND NOT EXISTS (
			SELECT 1
			FROM @Execution E
			WHERE E.RunDateTime < CAST(RJ.start_execution_date_utc AS DATETIME2(2))
		)
END

SELECT	E.instance_id,
		J.name,
		J.job_id,
		E.step_id,
		E.step_name,
		E.RunDateTime,
		ISNULL(E.RunDurationSec,0) AS RunDurationSec,
		ISNULL(E.FinishDateTime, E.RunDateTime) AS FinishDateTime,
		ISNULL(E.run_status,0) AS run_status,
		1 AS Executions,
		E.sql_message_id,
		E.sql_severity,
		E.message,
		E.retries_attempted
FROM @Execution E
JOIN dbo.Jobs J ON J.InstanceID = @InstanceID AND J.job_id = @job_id
ORDER BY CASE WHEN E.step_id = 0 THEN 1 ELSE 0 END,
		ISNULL(E.instance_id, 2147483647),
		E.RunDateTime
