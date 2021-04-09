CREATE VIEW dbo.AgentJob7Day
AS
SELECT I.InstanceID,
	I.Instance,
	JS.job_id,
	J.name,
	J.LastSucceeded,
	J.LastFailed,
	DATEDIFF(mi,J.LastSucceeded,GETUTCDATE()) as TimeSinceLastSucceeded,
	DATEDIFF(mi,J.LastFailed,GETUTCDATE()) as TimeSinceLastFailed,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) AND JS.step_id = 0 THEN JS.FailedCount ELSE 0 END) as FailCount7Days,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) AND JS.step_id = 0 THEN JS.SucceededCount ELSE 0 END) as SucceededCount7Days,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-1,GETUTCDATE()) AND JS.step_id = 0 THEN JS.FailedCount ELSE 0 END) as FailCount24Hrs,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-1,GETUTCDATE()) AND JS.step_id = 0 THEN JS.SucceededCount ELSE 0 END) as SucceededCount24Hrs,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) AND JS.step_id > 0 THEN JS.FailedCount ELSE 0 END) as JobStepFails7Days,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) AND JS.step_id > 0 THEN JS.FailedCount ELSE 0 END) as JobStepFails24Hrs,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) THEN JS.RetryCount ELSE 0 END) as RetryCount7Days,
	SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) THEN JS.RetryCount ELSE 0 END) as RetryCount24Hrs,
	CASE WHEN J.LastFailed > ISNULL(J.LastSucceeded,'19000101') THEN 1 ELSE 0 END as IsLastFail,
	MAX(CASE WHEN JS.step_id=0 THEN JS.MaxRunDurationSec ELSE NULL END) AS MaxDurationSec,
	AVG(CASE WHEN JS.step_id=0 THEN JS.RunDurationSec / (FailedCount+SucceededCount) ELSE NULL END) as AvgDurationSec,
	J.enabled
FROM dbo.Instances I 
JOIN  dbo.Jobs J ON I.InstanceID = J.InstanceID
LEFT JOIN dbo.JobStats_60MIN JS ON J.job_id = JS.job_id AND J.InstanceID = JS.InstanceID
									AND JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE())
GROUP BY I.InstanceID,
	I.Instance,
	JS.job_id,
	J.LastSucceeded,
	J.LastFailed,
	J.name,
	J.enabled