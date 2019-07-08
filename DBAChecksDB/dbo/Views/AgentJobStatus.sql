CREATE VIEW AgentJobStatus
AS
SELECT I.Instance, 
	J.InstanceID,
    J.job_id,
    J.name,
    J.LastFail,
	DATEDIFF(mi,J.LastFail,GETUTCDATE()) AS TimeSinceLastFail,
	CASE WHEN DATEDIFF(mi,J.LastFail,GETUTCDATE())  <= cfg.TimeSinceLastFailureCritical THEN 1 WHEN DATEDIFF(mi,J.LastFail,GETUTCDATE()) <= cfg.TimeSinceLastFailureCritical THEN 2 WHEN cfg.TimeSinceLastFailureWarning IS NULL AND cfg.TimeSinceLastFailureCritical IS NULL THEN 3 ELSE 4 END AS TimeSinceLastFailureStatus,
    J.LastSucceed,
	DATEDIFF(mi,J.LastSucceed,GETUTCDATE()) AS TimeSinceLastSucceeded,
	CASE WHEN DATEDIFF(mi,J.LastSucceed,GETUTCDATE())  > cfg.TimeSinceLastSucceededCritical THEN 1 WHEN DATEDIFF(mi,J.LastSucceed,GETUTCDATE()) >= cfg.TimeSinceLastSucceededWarning THEN 2 WHEN cfg.TimeSinceLastSucceededWarning IS NULL AND cfg.TimeSinceLastSucceededCritical IS NULL THEN 3 ELSE 4 END AS TimeSinceLastSucceededStatus,
	DATEDIFF(mi,J.LastSucceed,GETUTCDATE()) AS TimeSinceLastSucceed,
    J.FailCount24Hrs,
	CASE WHEN J.FailCount24Hrs > cfg.FailCount24HrsCritical THEN 1 WHEN J.FailCount24Hrs > cfg.FailCount24HrsWarning THEN 2 WHEN cfg.FailCount24HrsWarning IS NULL AND cfg.FailCount24HrsCritical IS NULL THEN 3  ELSE 4 END AS FailCount24HrsStatus,
    J.SucceedCount24Hrs,
    J.FailCount7Days,
	CASE WHEN J.FailCount7Days > cfg.FailCount7DaysCritical THEN 1 WHEN J.FailCount24Hrs > cfg.FailCount7DaysWarning THEN 2 WHEN cfg.FailCount7DaysWarning IS NULL AND cfg.FailCount7DaysCritical IS NULL THEN 3  ELSE 4 END AS FailCount7DaysStatus,
    J.SucceedCount7Days,
    J.JobStepFails7Days,
	CASE WHEN J.JobStepFails7Days > cfg.JobStepFails7DaysCritical THEN 1 WHEN J.JobStepFails7Days > cfg.JobStepFails7DaysWarning THEN 2 WHEN cfg.FailCount7DaysWarning IS NULL AND cfg.JobStepFails7DaysCritical IS NULL THEN 3  ELSE 4 END AS JobStepFail7DaysStatus,
    J.JobStepFails24Hrs,
	CASE WHEN J.JobStepFails24Hrs > cfg.JobStepFails24HrsCritical THEN 1 WHEN J.JobStepFails24Hrs > cfg.JobStepFails24HrsWarning THEN 2 WHEN cfg.JobStepFails24HrsWarning IS NULL AND cfg.JobStepFails24HrsCritical IS NULL THEN 3  ELSE 4 END AS JobStepFail24HrsStatus,
    J.enabled,
    J.MaxDurationSec,
    J.AvgDurationSec,
	CASE WHEN J.IsLastFail=1 AND cfg.LastFailIsCritical=1 THEN 1 WHEN J.IsLastFail=1 AND cfg.LastFailIsWarning=1 THEN 2 WHEN cfg.LastFailIsCritical IS NULL AND cfg.LastFailIsWarning IS NULL THEN 3 ELSE 4 END AS LastFailStatus,
    J.IsLastFail,
    cfg.TimeSinceLastFailureWarning,
    cfg.TimeSinceLastFailureCritical,
    cfg.TimeSinceLastSucceededWarning,
    cfg.TimeSinceLastSucceededCritical,
    cfg.FailCount24HrsWarning,
    cfg.FailCount24HrsCritical,
    cfg.FailCount7DaysCritical,
    cfg.FailCount7DaysWarning,
    cfg.JobStepFails24HrsWarning,
    cfg.JobStepFails24HrsCritical,
    cfg.JobStepFails7DaysWarning,
    cfg.JobStepFails7DaysCritical,
    cfg.LastFailIsCritical,
    cfg.LastFailIsWarning,
	CASE WHEN cfg.job_id='00000000-0000-0000-0000-000000000000' AND cfg.InstanceId=-1 THEN 'Root' WHEN cfg.job_id='00000000-0000-0000-0000-000000000000' THEN 'Instance' WHEN cfg.job_id IS NULL THEN 'N/A' ELSE 'Job' END AS ConfiguredLevel
FROM dbo.AgentJobs J
JOIN dbo.Instances I ON J.InstanceID = I.InstanceID
OUTER APPLY(SELECT TOP(1) * 
			FROM dbo.AgentJobThresholds T 
			WHERE (T.InstanceId = J.InstanceID OR T.InstanceId =-1) 
			AND (T.job_id = J.job_id OR T.job_id ='00000000-0000-0000-0000-000000000000')
			ORDER BY T.InstanceId DESC,T.job_id DESC) cfg