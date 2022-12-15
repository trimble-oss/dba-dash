CREATE VIEW dbo.AgentJob7Day
AS
WITH agg AS ( 
	SELECT JS.InstanceID,
		job_id,
		SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) AND JS.step_id = 0 THEN JS.FailedCount ELSE 0 END) AS FailCount7Days,
		SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) AND JS.step_id = 0 THEN JS.SucceededCount ELSE 0 END) AS SucceededCount7Days,
		SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-1,GETUTCDATE()) AND JS.step_id = 0 THEN JS.FailedCount ELSE 0 END) AS FailCount24Hrs,
		SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-1,GETUTCDATE()) AND JS.step_id = 0 THEN JS.SucceededCount ELSE 0 END) AS SucceededCount24Hrs,
		SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) AND JS.step_id > 0 THEN JS.FailedCount ELSE 0 END) AS JobStepFails7Days,
		SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) AND JS.step_id > 0 THEN JS.FailedCount ELSE 0 END) AS JobStepFails24Hrs,
		SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) THEN JS.RetryCount ELSE 0 END) AS RetryCount7Days,
		SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-7,GETUTCDATE()) THEN JS.RetryCount ELSE 0 END) AS RetryCount24Hrs,
		MAX(CASE WHEN JS.step_id=0 THEN JS.MaxRunDurationSec ELSE NULL END) AS MaxDurationSec,
		AVG(CASE WHEN JS.step_id=0 THEN JS.RunDurationSec / NULLIF((JS.FailedCount+JS.SucceededCount),0) ELSE NULL END) AS AvgDurationSec
	FROM dbo.JobStats_60MIN JS 
	WHERE JS.RunDateTime >= CAST(DATEADD(d,-7,GETUTCDATE()) AS DATETIME2(2))
	GROUP BY JS.InstanceID,job_id
)
SELECT I.InstanceID,
	I.Instance,
	I.InstanceDisplayName,
	J.job_id,
	J.name,
	J.description,
	J.LastSucceeded,
	J.LastFailed,
	DATEDIFF(mi,J.LastSucceeded,GETUTCDATE()) AS TimeSinceLastSucceeded,
	DATEDIFF(mi,J.LastFailed,GETUTCDATE()) AS TimeSinceLastFailed,
    ISNULL(agg.FailCount7Days,0) AS FailCount7Days,
    ISNULL(agg.SucceededCount7Days,0) AS SucceededCount7Days,
    ISNULL(agg.FailCount24Hrs,0) AS FailCount24Hrs,
    ISNULL(agg.SucceededCount24Hrs,0) AS SucceededCount24Hrs,
    ISNULL(agg.JobStepFails7Days,0) AS JobStepFails7Days,
    ISNULL(agg.JobStepFails24Hrs,0) AS  JobStepFails24Hrs,
    ISNULL(agg.RetryCount7Days,0) AS RetryCount7Days,
    ISNULL(agg.RetryCount24Hrs,0) AS RetryCount24Hrs,
	CASE WHEN J.LastFailed > ISNULL(J.LastSucceeded,'19000101') THEN 1 ELSE 0 END AS IsLastFail,
	agg.MaxDurationSec,
	agg.AvgDurationSec,
	J.enabled,
	I.ShowInSummary
FROM dbo.Instances I 
JOIN  dbo.Jobs J ON I.InstanceID = J.InstanceID
LEFT JOIN agg ON J.job_id = agg.job_id AND J.InstanceID = agg.InstanceID
WHERE I.IsActive=1
AND J.IsActive=1