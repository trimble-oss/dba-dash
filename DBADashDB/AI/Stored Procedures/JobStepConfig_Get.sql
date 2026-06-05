CREATE PROC AI.JobStepConfig_Get(
    @MaxRows INT = 300,
    @InstanceFilter NVARCHAR(256) = NULL,
    @HoursBack INT = 168
)
AS
SET NOCOUNT ON
/* Job step configuration for jobs currently in warning/critical/acknowledged status or with recent step failures.
   Surfaces on_fail_action and retry settings so the model can explain why a failing step may not fail the job
   (silent step failures) and recommend configuration fixes. */
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    j.name AS JobName,
    js.step_id,
    js.step_name,
    js.subsystem,
    js.database_name AS DatabaseName,
    js.on_success_action,
    CASE js.on_success_action
        WHEN 1 THEN 'Quit with success'
        WHEN 2 THEN 'Quit with failure'
        WHEN 3 THEN 'Go to next step'
        WHEN 4 THEN 'Go to step'
        ELSE CAST(js.on_success_action AS VARCHAR(10)) END AS OnSuccessActionDescription,
    js.on_fail_action,
    CASE js.on_fail_action
        WHEN 1 THEN 'Quit with success'
        WHEN 2 THEN 'Quit with failure'
        WHEN 3 THEN 'Go to next step'
        WHEN 4 THEN 'Go to step'
        ELSE CAST(js.on_fail_action AS VARCHAR(10)) END AS OnFailActionDescription,
    js.retry_attempts,
    js.retry_interval,
    js.output_file_name AS OutputFileName,
    js.proxy_name AS ProxyName,
    ajs.JobStatus,
    ajs.IsLastFail,
    ajs.StepLastFailed
FROM dbo.JobSteps js
INNER JOIN dbo.Jobs j ON j.InstanceID = js.InstanceID AND j.job_id = js.job_id
INNER JOIN dbo.Instances i ON i.InstanceID = js.InstanceID
INNER JOIN dbo.AgentJobStatus ajs ON ajs.InstanceID = js.InstanceID AND ajs.job_id = js.job_id
WHERE j.IsActive = 1
  AND ajs.enabled = 1
  AND (ajs.JobStatus IN (1, 2, 5) OR ajs.JobStepFails7Days > 0)
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
  AND (j.LastFailed >= DATEADD(HOUR, -@HoursBack, GETUTCDATE()) OR j.StepLastFailed >= DATEADD(HOUR, -@HoursBack, GETUTCDATE()))
ORDER BY ajs.JobStatus ASC, i.InstanceDisplayName, j.name, js.step_id
