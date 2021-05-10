CREATE VIEW [dbo].[AgentJobStatus]
AS
SELECT J.Instance, 
	J.InstanceID,
    J.job_id,
    J.name,
	J.description,
    J.LastFailed, 
	J.TimeSinceLastFailed,
	st.TimeSinceLastFailureStatus,
    J.LastSucceeded,
	J.TimeSinceLastSucceeded,
	st.TimeSinceLastSucceededStatus,
    J.FailCount24Hrs,
	st.FailCount24HrsStatus,
    J.SucceededCount24Hrs,
    J.FailCount7Days,
	st.FailCount7DaysStatus,
    J.SucceededCount7Days,
    J.JobStepFails7Days,
	st.JobStepFail7DaysStatus,
    J.JobStepFails24Hrs,
	st.JobStepFail24HrsStatus,
    J.enabled,
    J.MaxDurationSec,
    J.AvgDurationSec,
	st.LastFailStatus,
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
	CASE WHEN cfg.job_id='00000000-0000-0000-0000-000000000000' AND cfg.InstanceId=-1 THEN 'Root' 
		WHEN cfg.job_id='00000000-0000-0000-0000-000000000000' THEN 'Instance' 
		WHEN cfg.job_id IS NULL THEN 'N/A' 
		ELSE 'Job' END AS ConfiguredLevel,
	CASE WHEN enabled=0 THEN 3
		WHEN st.TimeSinceLastFailureStatus=1 OR st.TimeSinceLastSucceededStatus=1 OR st.FailCount24HrsStatus=1 
				OR st.FailCount7DaysStatus=1 OR st.JobStepFail7DaysStatus=1 OR st.JobStepFail24HrsStatus=1 OR st.LastFailStatus=1 THEN 1
		WHEN st.TimeSinceLastFailureStatus=2 OR st.TimeSinceLastSucceededStatus=2 OR st.FailCount24HrsStatus=2 
				OR st.FailCount7DaysStatus=2 OR st.JobStepFail7DaysStatus=2 OR st.JobStepFail24HrsStatus=2 OR st.LastFailStatus=2 THEN 2
		WHEN st.TimeSinceLastFailureStatus=4 OR st.TimeSinceLastSucceededStatus=4 OR st.FailCount24HrsStatus=4
				OR st.FailCount7DaysStatus=4 OR st.JobStepFail7DaysStatus=4 OR st.JobStepFail24HrsStatus=4 OR st.LastFailStatus=4 THEN 4
		ELSE 3 END AS JobStatus 
FROM dbo.AgentJob7Day J
OUTER APPLY(SELECT TOP(1) * 
			FROM dbo.AgentJobThresholds T 
			WHERE (T.InstanceId = J.InstanceID OR T.InstanceId =-1) 
			AND (T.job_id = J.job_id OR T.job_id ='00000000-0000-0000-0000-000000000000')
			ORDER BY T.InstanceId DESC,T.job_id DESC) cfg
OUTER APPLY(SELECT CASE WHEN J.enabled=0 THEN 3
						WHEN DATEDIFF(mi,J.LastFailed,GETUTCDATE())  <= cfg.TimeSinceLastFailureCritical THEN 1 
						WHEN DATEDIFF(mi,J.LastFailed,GETUTCDATE()) <= cfg.TimeSinceLastFailureCritical THEN 2 
						WHEN cfg.TimeSinceLastFailureWarning IS NULL AND cfg.TimeSinceLastFailureCritical IS NULL THEN 3 
						ELSE 4 END AS TimeSinceLastFailureStatus,
				CASE WHEN J.enabled=0 THEN 3
					WHEN DATEDIFF(mi,J.LastSucceeded,GETUTCDATE())  >= cfg.TimeSinceLastSucceededCritical THEN 1 
					WHEN DATEDIFF(mi,J.LastSucceeded,GETUTCDATE()) >= cfg.TimeSinceLastSucceededWarning THEN 2 
					WHEN cfg.TimeSinceLastSucceededWarning IS NULL AND cfg.TimeSinceLastSucceededCritical IS NULL THEN 3 
					ELSE 4 END AS TimeSinceLastSucceededStatus,
				CASE WHEN J.enabled=0 THEN 3
					WHEN J.FailCount24Hrs >= cfg.FailCount24HrsCritical THEN 1 
					WHEN J.FailCount24Hrs >= cfg.FailCount24HrsWarning THEN 2 
					WHEN cfg.FailCount24HrsWarning IS NULL AND cfg.FailCount24HrsCritical IS NULL THEN 3  
					ELSE 4 END AS FailCount24HrsStatus,
				CASE WHEN J.enabled=0 THEN 3
					WHEN J.FailCount7Days >= cfg.FailCount7DaysCritical THEN 1 
					WHEN J.FailCount7Days >= cfg.FailCount7DaysWarning THEN 2 
					WHEN cfg.FailCount7DaysWarning IS NULL AND cfg.FailCount7DaysCritical IS NULL THEN 3  
					ELSE 4 END AS FailCount7DaysStatus,
				CASE WHEN J.enabled=0 THEN 3
						WHEN J.JobStepFails7Days >= cfg.JobStepFails7DaysCritical THEN 1 
						WHEN J.JobStepFails7Days >= cfg.JobStepFails7DaysWarning THEN 2 
						WHEN cfg.JobStepFails7DaysWarning IS NULL AND cfg.JobStepFails7DaysCritical IS NULL THEN 3  
						ELSE 4 END AS JobStepFail7DaysStatus,
				CASE WHEN J.enabled=0 THEN 3
						WHEN J.JobStepFails24Hrs >= cfg.JobStepFails24HrsCritical THEN 1 
						WHEN J.JobStepFails24Hrs >= cfg.JobStepFails24HrsWarning THEN 2 
						WHEN cfg.JobStepFails24HrsWarning IS NULL AND cfg.JobStepFails24HrsCritical IS NULL THEN 3 
						ELSE 4 END AS JobStepFail24HrsStatus,
				CASE WHEN J.enabled=0 THEN 3
						WHEN J.IsLastFail=1 AND cfg.LastFailIsCritical=1 THEN 1 
						WHEN J.IsLastFail=1 AND cfg.LastFailIsWarning=1 THEN 2 
						WHEN cfg.LastFailIsCritical =0 AND cfg.LastFailIsWarning =0 THEN 3 
						ELSE 4 END AS LastFailStatus
				) St