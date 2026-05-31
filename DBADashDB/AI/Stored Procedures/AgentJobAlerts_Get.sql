CREATE PROC AI.AgentJobAlerts_Get(
	@MaxRows INT = 200,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = 168
)
AS
SET NOCOUNT ON
/* Result set 1: Job alerts */
SELECT TOP (@MaxRows)
	aa.AlertID,
	aa.AlertType,
	aa.AlertKey,
	aa.Priority,
	aa.IsResolved,
	aa.ResolvedDate,
	aa.UpdatedDate,
	aa.LastMessage,
	i.InstanceDisplayName,
	i.ConnectionID
FROM Alert.ActiveAlerts aa
INNER JOIN dbo.Instances i ON i.InstanceID = aa.InstanceID
WHERE (aa.AlertType LIKE '%AgentJob%' OR aa.AlertKey LIKE '%AgentJob%' OR aa.AlertKey LIKE '%Job%')
  AND aa.UpdatedDate >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY aa.IsResolved ASC, aa.Priority ASC, aa.UpdatedDate DESC

/* Result set 2: Job status summary for jobs with warning/critical status or recent failures */
SELECT TOP (@MaxRows)
	js.InstanceDisplayName,
	js.name AS JobName,
	js.description AS JobDescription,
	js.enabled AS IsEnabled,
	js.JobStatus,
	js.IsLastFail,
	js.LastFailed,
	js.LastSucceeded,
	js.FailCount24Hrs,
	js.SucceededCount24Hrs,
	js.FailCount7Days,
	js.SucceededCount7Days,
	js.JobStepFails24Hrs,
	js.JobStepFails7Days,
	js.MaxDuration,
	js.AvgDuration,
	js.MaxDurationSec,
	js.AvgDurationSec,
	js.StepLastFailed
FROM dbo.AgentJobStatus js
WHERE js.JobStatus IN (1, 2, 5)
  AND js.enabled = 1
  AND (@InstanceFilter IS NULL OR js.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY js.JobStatus ASC, js.FailCount24Hrs DESC, js.FailCount7Days DESC

/* Result set 3: Recent failure messages (error text) for jobs in warning/critical/acknowledged status.
   Includes step-level messages so the actual error (e.g. missing stored procedure) is available. */
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	j.name AS JobName,
	jh.step_id,
	jh.step_name,
	jh.run_status,
	CASE WHEN jh.run_status=0 THEN 'Failed' WHEN jh.run_status=1 THEN 'Succeeded' WHEN jh.run_status=2 THEN 'Retry' WHEN jh.run_status=3 THEN 'Cancelled' WHEN jh.run_status=4 THEN 'In Progress' ELSE FORMAT(jh.run_status,'N0') END AS run_status_description,
	jh.sql_message_id,
	jh.sql_severity,
	jh.RunDateTime,
	jh.message
FROM dbo.JobHistory jh
INNER JOIN dbo.Jobs j ON j.InstanceID = jh.InstanceID AND j.job_id = jh.job_id
INNER JOIN dbo.Instances i ON i.InstanceID = jh.InstanceID
WHERE jh.run_status <> 1 /* not succeeded */
  AND jh.RunDateTime >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
  AND j.IsActive = 1
  AND EXISTS (
		SELECT 1
		FROM dbo.AgentJobStatus js2
		WHERE js2.InstanceID = jh.InstanceID
		  AND js2.job_id = jh.job_id
		  AND js2.JobStatus IN (1, 2, 5)
		  AND js2.enabled = 1
	  )
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY jh.RunDateTime DESC, jh.step_id ASC
