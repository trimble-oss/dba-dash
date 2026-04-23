CREATE PROC DBADash.AI_AgentJobAlerts_Get(
	@MaxRows INT = 200,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = 168
)
AS
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
