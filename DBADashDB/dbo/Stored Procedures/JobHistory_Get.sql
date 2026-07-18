CREATE PROC dbo.JobHistory_Get(
	@InstanceID INT,
	@JobID UNIQUEIDENTIFIER,
	@StepID INT=0,
	@instance_id INT=NULL,
	@Top INT=500,
	@FailedOnly BIT=0
)
AS
DECLARE @SQL NVARCHAR(MAX)
DECLARE @instance_id_from INT
IF @instance_id IS NOT NULL
BEGIN
	SELECT TOP(1) @instance_id_from = instance_id
	FROM dbo.JobHistory
	WHERE InstanceID = @InstanceID
	AND job_id = @JobID
	AND step_id=0
	AND instance_id<@instance_id
	ORDER BY instance_id DESC

	SET @instance_id_from = ISNULL(@instance_id_from,0)
END

SET @SQL = N'
SELECT TOP(@Top) *
FROM (
	SELECT
		JH.InstanceID,
		J.name,
		JH.job_id,
		JH.RunDateTime,
		JH.instance_id,
		JH.step_id,
		JH.step_name,
		JH.sql_message_id,
		JH.sql_severity,
		JH.message,
		JH.run_status,
		CASE WHEN JH.run_status=0 THEN ''Failed'' WHEN JH.run_status=1 THEN ''Succeeded'' WHEN JH.run_status=2 THEN ''Retry'' WHEN JH.run_status=3 THEN ''Cancelled'' WHEN JH.run_status=4 THEN ''In Progress'' ELSE FORMAT(JH.run_status,''N0'') END as run_status_description,
		JH.RunDurationSec,
		HD.HumanDuration as RunDuration,
		JH.retries_attempted,
		DATEADD(ss,JH.RunDurationSec,JH.RunDateTime) as RunEndDateTime
	FROM dbo.JobHistory JH
	JOIN dbo.Jobs J ON JH.job_id = J.job_id AND J.InstanceID = JH.InstanceID
	CROSS APPLY dbo.SecondsToHumanDuration(JH.RunDurationSec) AS HD
	WHERE JH.InstanceID = @InstanceID
	AND JH.job_id = @JobID
	' + CASE WHEN @StepID IS NULL THEN '' ELSE 'AND JH.step_id = @StepID' END + '
	' + CASE WHEN @instance_id IS NULL THEN '' ELSE 'AND JH.instance_id>@instance_id_from AND JH.instance_id <= @instance_id' END + '
	' + CASE WHEN @FailedOnly=1 THEN 'AND JH.run_status<> 1' ELSE '' END + '
	' +
	/* On the default outcome view, synthesize a placeholder outcome row for a job that is currently executing.
	   SQL Server Agent does not write the step_id=0 outcome record until the whole job finishes, so a running
	   execution would otherwise be invisible in this view.  The synthetic row uses instance_id = 2147483647 so it
	   sorts to the top and so the View Steps drill-down captures the already completed steps of the running run. */
	CASE WHEN @StepID = 0 AND @instance_id IS NULL AND @FailedOnly = 0 THEN N'
	UNION ALL
	SELECT
		RJ.InstanceID,
		J.name,
		RJ.job_id,
		CAST(RJ.start_execution_date_utc AS DATETIME2(2)),
		2147483647,
		0,
		N''(Job in progress)'',
		CAST(NULL AS INT),
		CAST(NULL AS INT),
		CONCAT(N''Job is currently executing.'', CASE WHEN RJ.current_execution_step_id IS NOT NULL THEN CONCAT(N'' Current step: ('', RJ.current_execution_step_id, N'') '', RJ.current_execution_step_name) ELSE N'''' END),
		4,
		N''In Progress'',
		DATEDIFF(s, RJ.start_execution_date_utc, GETUTCDATE()),
		HDR.HumanDuration,
		RJ.current_retry_attempt,
		CAST(NULL AS DATETIME2(2))
	FROM dbo.RunningJobs RJ
	JOIN dbo.Jobs J ON RJ.job_id = J.job_id AND J.InstanceID = RJ.InstanceID
	CROSS APPLY dbo.SecondsToHumanDuration(DATEDIFF(s, RJ.start_execution_date_utc, GETUTCDATE())) AS HDR
	WHERE RJ.InstanceID = @InstanceID
	AND RJ.job_id = @JobID
	AND RJ.start_execution_date_utc IS NOT NULL /* guard: RunDateTime/duration derive from this */
	AND ISNULL(RJ.current_execution_status,1) <> 4 /* not idle */
	AND NOT EXISTS (
		SELECT 1
		FROM dbo.JobHistory JH2
		WHERE JH2.InstanceID = RJ.InstanceID
		AND JH2.job_id = RJ.job_id
		AND JH2.step_id = 0
		AND JH2.RunDateTime >= CAST(RJ.start_execution_date_utc AS DATETIME2(2))
	)' ELSE N'' END + '
) q
ORDER BY q.instance_id DESC'

EXEC sp_executesql @SQL,N'@InstanceID INT,@JobID UNIQUEIDENTIFIER,@StepID INT,@instance_id INT,@instance_id_from INT,@Top INT',@InstanceID,@JobID,@StepID,@instance_id,@instance_id_from,@Top
