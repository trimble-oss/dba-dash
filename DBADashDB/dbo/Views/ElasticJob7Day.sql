/*
	Elastic jobs with their executions over the last 7 days and 24 hours, from dbo.ElasticJobStats_60MIN.  See dbo.ElasticJobStatus.
	The same shape as dbo.AgentJob7Day.  Job step fails are failed executions of the job's steps against their targets.
*/
CREATE VIEW dbo.ElasticJob7Day
AS
WITH agg AS (
	SELECT	JS.InstanceID,
			JS.job_id,
			SUM(CASE WHEN JS.step_id = 0 THEN JS.FailedCount ELSE 0 END) AS FailCount7Days,
			SUM(CASE WHEN JS.step_id = 0 THEN JS.SucceededCount ELSE 0 END) AS SucceededCount7Days,
			SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-1,GETUTCDATE()) AND JS.step_id = 0 THEN JS.FailedCount ELSE 0 END) AS FailCount24Hrs,
			SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-1,GETUTCDATE()) AND JS.step_id = 0 THEN JS.SucceededCount ELSE 0 END) AS SucceededCount24Hrs,
			SUM(CASE WHEN JS.step_id > 0 THEN JS.FailedCount ELSE 0 END) AS JobStepFails7Days,
			SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-1,GETUTCDATE()) AND JS.step_id > 0 THEN JS.FailedCount ELSE 0 END) AS JobStepFails24Hrs,
			SUM(CASE WHEN JS.step_id > 0 THEN JS.SkippedCount ELSE 0 END) AS SkippedCount7Days,
			SUM(CASE WHEN JS.step_id > 0 THEN JS.RetryCount ELSE 0 END) AS RetryCount7Days,
			SUM(CASE WHEN JS.RunDateTime >= DATEADD(d,-1,GETUTCDATE()) AND JS.step_id > 0 THEN JS.RetryCount ELSE 0 END) AS RetryCount24Hrs,
			MAX(CASE WHEN JS.step_id = 0 THEN JS.MaxRunDurationSec ELSE NULL END) AS MaxDurationSec,
			SUM(CASE WHEN JS.step_id = 0 THEN JS.RunDurationSec ELSE 0 END) / NULLIF(SUM(CASE WHEN JS.step_id = 0 THEN JS.TimedCount ELSE 0 END),0) AS AvgDurationSec
	FROM dbo.ElasticJobStats_60MIN JS
	WHERE JS.RunDateTime >= CAST(DATEADD(d,-7,GETUTCDATE()) AS DATETIME2(2))
	GROUP BY JS.InstanceID,
			JS.job_id
)
SELECT	I.InstanceID,
		I.Instance,
		I.InstanceDisplayName,
		J.job_id,
		J.job_name AS name,
		J.description,
		J.schedule_interval_type,
		J.schedule_interval_count,
		J.schedule_start_time,
		J.schedule_end_time,
		J.LastSucceeded,
		J.LastFailed,
		DATEDIFF(mi,J.LastSucceeded,GETUTCDATE()) AS TimeSinceLastSucceeded,
		DATEDIFF(mi,J.LastFailed,GETUTCDATE()) AS TimeSinceLastFailed,
		ISNULL(agg.FailCount7Days,0) AS FailCount7Days,
		ISNULL(agg.SucceededCount7Days,0) AS SucceededCount7Days,
		ISNULL(agg.FailCount24Hrs,0) AS FailCount24Hrs,
		ISNULL(agg.SucceededCount24Hrs,0) AS SucceededCount24Hrs,
		ISNULL(agg.JobStepFails7Days,0) AS JobStepFails7Days,
		ISNULL(agg.JobStepFails24Hrs,0) AS JobStepFails24Hrs,
		ISNULL(agg.SkippedCount7Days,0) AS SkippedCount7Days,
		ISNULL(agg.RetryCount7Days,0) AS RetryCount7Days,
		ISNULL(agg.RetryCount24Hrs,0) AS RetryCount24Hrs,
		CASE WHEN J.LastFailed > ISNULL(J.LastSucceeded,'19000101') THEN 1 ELSE 0 END AS IsLastFail,
		agg.MaxDurationSec,
		agg.AvgDurationSec,
		J.enabled,
		I.ShowInSummary,
		J.AckDate,
		J.StepLastFailed,
		DATEDIFF(mi,J.LastFailed,GETUTCDATE()) AS TimeSinceLastFailedMin,
		DATEDIFF(mi,J.LastSucceeded,GETUTCDATE()) AS TimeSinceLastSucceededMin
FROM dbo.Instances I
JOIN dbo.ElasticJobs J ON I.InstanceID = J.InstanceID
LEFT JOIN agg ON J.job_id = agg.job_id AND J.InstanceID = agg.InstanceID
WHERE I.IsActive = 1
AND J.IsActive = 1
