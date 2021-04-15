CREATE VIEW dbo.AgentJob7Day
AS
SELECT I.InstanceID,
	I.Instance,
	J.job_id,
	J.name,
	J.description,
	J.LastSucceeded,
	J.LastFailed,
	DATEDIFF(mi,J.LastSucceeded,GETUTCDATE()) AS TimeSinceLastSucceeded,
	DATEDIFF(mi,J.LastFailed,GETUTCDATE()) AS TimeSinceLastFailed,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) AND JS.step_id = 0 THEN JS.FailedCount ELSE 0 END) AS FailCount7Days,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) AND JS.step_id = 0 THEN JS.SucceededCount ELSE 0 END) AS SucceededCount7Days,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-1,GETUTCDATE()) AND JS.step_id = 0 THEN JS.FailedCount ELSE 0 END) AS FailCount24Hrs,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-1,GETUTCDATE()) AND JS.step_id = 0 THEN JS.SucceededCount ELSE 0 END) AS SucceededCount24Hrs,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) AND JS.step_id > 0 THEN JS.FailedCount ELSE 0 END) AS JobStepFails7Days,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) AND JS.step_id > 0 THEN JS.FailedCount ELSE 0 END) AS JobStepFails24Hrs,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) THEN JS.RetryCount ELSE 0 END) AS RetryCount7Days,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) THEN JS.RetryCount ELSE 0 END) AS RetryCount24Hrs,
	CASE WHEN J.LastFailed > ISNULL(J.LastSucceeded,'19000101') THEN 1 ELSE 0 END AS IsLastFail,
	MAX(CASE WHEN JS.step_id=0 THEN JS.MaxRunDurationSec ELSE NULL END) AS MaxDurationSec,
	AVG(CASE WHEN JS.step_id=0 THEN JS.RunDurationSec / NULLIF((JS.FailedCount+JS.SucceededCount),0) ELSE NULL END) AS AvgDurationSec,
	J.enabled
FROM dbo.Instances I 
JOIN  dbo.Jobs J ON I.InstanceID = J.InstanceID
LEFT JOIN dbo.JobStats_60MIN JS ON J.job_id = JS.job_id AND J.InstanceID = JS.InstanceID
									AND JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE())
WHERE I.IsActive=1
AND J.IsActive=1
GROUP BY I.InstanceID,
	I.Instance,
	J.job_id,
	J.LastSucceeded,
	J.LastFailed,
	J.name,
	J.enabled,
	J.description