CREATE VIEW dbo.AgentJobStatus
AS
SELECT	J.Instance, 
		J.InstanceDisplayName,
		J.InstanceID,
		J.job_id,
		J.name,
		J.category,
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
		MaxD.HumanDuration AS MaxDuration,
		J.AvgDurationSec,
		AvgD.HumanDuration AS AvgDuration,
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
		CASE WHEN exc.IsExcluded=1 AND exc.ExcludedAtInstanceID=-1 THEN 'Excluded (Root)'
			WHEN exc.IsExcluded=1 THEN 'Excluded (Instance)'
			WHEN cfg.job_id='00000000-0000-0000-0000-000000000000' AND cfg.InstanceID =-1 THEN 'Root'
			WHEN cfg.job_id='00000000-0000-0000-0000-000000000000' THEN 'Instance'
			WHEN cfg.job_id IS NULL THEN 'N/A'
			ELSE 'Job' END AS ConfiguredLevel,
		exc.IsExcluded,
		CASE	WHEN exc.IsExcluded=1 THEN 3 /* N/A.  Job excluded by a name/category exclusion rule */
				WHEN J.enabled=0 THEN 3 /* N/A.  Not enabled */
				WHEN	(
						st.TimeSinceLastFailureStatus=1 
						OR st.TimeSinceLastSucceededStatus=1 
						OR st.FailCount24HrsStatus=1 
						OR st.FailCount7DaysStatus=1 
						OR st.JobStepFail7DaysStatus=1 
						OR st.JobStepFail24HrsStatus=1 
						OR st.LastFailStatus=1
						)			
					THEN 1 /* Critical if any status is critical */
				WHEN	(
						st.TimeSinceLastFailureStatus=2 
						OR st.TimeSinceLastSucceededStatus=2 
						OR st.FailCount24HrsStatus=2 
						OR st.FailCount7DaysStatus=2
						OR st.JobStepFail7DaysStatus=2 
						OR st.JobStepFail24HrsStatus=2 
						OR st.LastFailStatus=2 
						)
					THEN 2 /* Warning */
				WHEN	(
						st.TimeSinceLastFailureStatus=5 
						OR st.TimeSinceLastSucceededStatus=5
						OR st.FailCount24HrsStatus=5 
						OR st.FailCount7DaysStatus=5
						OR st.JobStepFail7DaysStatus=5 
						OR st.JobStepFail24HrsStatus=5 
						OR st.LastFailStatus=5 
						)
					THEN 5 /* Acknowledged */
				WHEN (st.TimeSinceLastFailureStatus=4 
					OR st.TimeSinceLastSucceededStatus=4 
					OR st.FailCount24HrsStatus=4
					OR st.FailCount7DaysStatus=4 
					OR st.JobStepFail7DaysStatus=4 
					OR st.JobStepFail24HrsStatus=4 
					OR st.LastFailStatus=4
					)
					THEN 4 /* Good */
				ELSE 3 /* N/A */
				END AS JobStatus, /* Overall status. */
	J.ShowInSummary,
	J.AckDate,
	CASE WHEN J.AckDate > J.LastFailed AND J.AckDate > J.LastSucceeded THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS AckStatus,
	J.StepLastFailed
FROM dbo.AgentJob7Day J
CROSS APPLY dbo.SecondsToHumanDuration(J.MaxDurationSec) AS MaxD
CROSS APPLY dbo.SecondsToHumanDuration(J.AvgDurationSec) AS AvgD
OUTER APPLY(SELECT TOP(1) * 
			FROM dbo.AgentJobThresholds T 
			WHERE (T.InstanceID = J.InstanceID OR T.InstanceID =-1) 
			AND (T.job_id = J.job_id OR T.job_id ='00000000-0000-0000-0000-000000000000')
			ORDER BY T.InstanceID DESC,T.job_id DESC) cfg
/* Job is excluded from monitoring if it matches a name/category/description exclusion defined at root (InstanceID=-1) or instance level.
   Filters use LIKE-or-equals matching (mirrors Alert.AgentJobAlert_Upd).  A NULL filter matches any value. */
OUTER APPLY(SELECT CAST(CASE WHEN COUNT(*)>0 THEN 1 ELSE 0 END AS BIT) AS IsExcluded,
					/* MAX prefers an instance-level match (InstanceID>=0) over a root match (-1) when both apply */
					MAX(ex.InstanceID) AS ExcludedAtInstanceID
				FROM dbo.AgentJobExclusions ex
				WHERE (ex.InstanceID = J.InstanceID OR ex.InstanceID = -1)
				AND (ex.JobNameFilter IS NULL OR J.name LIKE ex.JobNameFilter OR J.name = ex.JobNameFilter)
				AND (ex.CategoryFilter IS NULL OR J.category LIKE ex.CategoryFilter OR J.category = ex.CategoryFilter)
				AND (ex.DescriptionFilter IS NULL OR J.description LIKE ex.DescriptionFilter OR J.description = ex.DescriptionFilter)
				) exc
OUTER APPLY(SELECT	CASE WHEN exc.IsExcluded=1 THEN 3 /* N/A. Job excluded by a name/category/description exclusion rule */
						WHEN J.enabled=0 THEN 3 /* N/A. Job not enabled */
						WHEN (J.TimeSinceLastFailedMin <= cfg.TimeSinceLastFailureCritical OR J.TimeSinceLastFailedMin <= cfg.TimeSinceLastFailureWarning) 
							AND J.AckDate > ISNULL(J.LastFailed,'19000101') THEN 5 /* Acknowledged.  Warning or Critical threshold met but acknowledged date greater than last failed */
						WHEN J.TimeSinceLastFailedMin <= cfg.TimeSinceLastFailureCritical THEN 1 /* Critical. */
						WHEN J.TimeSinceLastFailedMin <= cfg.TimeSinceLastFailureWarning THEN 2 /* Warning. */
						WHEN cfg.TimeSinceLastFailureWarning IS NULL AND cfg.TimeSinceLastFailureCritical IS NULL THEN 3 /* N/A.  Threshold not set */
						ELSE 4 END /* Good. */
						AS TimeSinceLastFailureStatus, 

					CASE WHEN exc.IsExcluded=1 THEN 3 /* N/A. Job excluded by a name/category/description exclusion rule */
						WHEN J.enabled=0 THEN 3 /* N/A. Job not enabled */
						WHEN (J.TimeSinceLastSucceededMin  >= cfg.TimeSinceLastSucceededCritical OR  J.TimeSinceLastSucceededMin >= cfg.TimeSinceLastSucceededWarning)
							AND J.AckDate > ISNULL(J.LastFailed,'19000101') 
							AND J.AckDate > ISNULL(J.LastSucceeded,'19000101') 
							AND J.AckDate > DATEADD(d,-1,GETUTCDATE()) 
							THEN 5 /* Acknowledged.  Warning or Critical threshold met.  Acknowledged within the last 24hrs and no success/failure since Acknowledged  */
						WHEN J.TimeSinceLastSucceededMin >= cfg.TimeSinceLastSucceededCritical THEN 1 /* Critical. */
						WHEN J.TimeSinceLastSucceededMin >= cfg.TimeSinceLastSucceededWarning THEN 2 /* Warning. */
						WHEN cfg.TimeSinceLastSucceededWarning IS NULL AND cfg.TimeSinceLastSucceededCritical IS NULL THEN 3 /* N/A.  Threshold not set */
						ELSE 4 END /* Good. */
						AS TimeSinceLastSucceededStatus, 

					CASE WHEN exc.IsExcluded=1 THEN 3 /* N/A. Job excluded by a name/category/description exclusion rule */
						WHEN J.enabled=0 THEN 3 /* N/A. Job not enabled */
						WHEN (J.FailCount24Hrs >= cfg.FailCount24HrsCritical OR  J.FailCount24Hrs >= cfg.FailCount24HrsWarning) 
							AND J.AckDate > ISNULL(J.LastFailed,'19000101') 
							THEN 5  /* Acknowledged.  Warning or Critical threshold met but acknowledged date greater than last failed */
						WHEN J.FailCount24Hrs >= cfg.FailCount24HrsCritical THEN 1 /* Critical. */
						WHEN J.FailCount24Hrs >= cfg.FailCount24HrsWarning THEN 2 /* Warning. */
						WHEN cfg.FailCount24HrsWarning IS NULL AND cfg.FailCount24HrsCritical IS NULL THEN 3  /* N/A.  Threshold not set */
						ELSE 4 END /* Good. */
						AS FailCount24HrsStatus, 

					CASE WHEN exc.IsExcluded=1 THEN 3 /* N/A. Job excluded by a name/category/description exclusion rule */
						WHEN J.enabled=0 THEN 3 /* N/A. Job not enabled */
						WHEN (J.FailCount7Days >= cfg.FailCount7DaysCritical OR J.FailCount7Days >= cfg.FailCount7DaysWarning) 
							AND J.AckDate > ISNULL(J.LastFailed,'19000101') THEN 5  /* Acknowledged.  Warning or Critical threshold met but acknowledged date greater than last failed */
						WHEN J.FailCount7Days >= cfg.FailCount7DaysCritical THEN 1 /* Critical. */
						WHEN J.FailCount7Days >= cfg.FailCount7DaysWarning THEN 2 /* Warning. */
						WHEN cfg.FailCount7DaysWarning IS NULL AND cfg.FailCount7DaysCritical IS NULL THEN 3  /* N/A.  Threshold not set */
						ELSE 4 END /* Good. */
						AS FailCount7DaysStatus, 
						
					CASE WHEN exc.IsExcluded=1 THEN 3 /* N/A. Job excluded by a name/category/description exclusion rule */
						WHEN J.enabled=0 THEN 3 /* N/A. Job not enabled */
						WHEN (J.JobStepFails7Days >= cfg.JobStepFails7DaysCritical OR J.JobStepFails7Days >= cfg.JobStepFails7DaysWarning) 
							AND  J.AckDate > ISNULL(J.StepLastFailed,'19000101') 
							THEN 5  /* Acknowledged.  Warning or Critical threshold met but acknowledged date greater than last failed */
						WHEN J.JobStepFails7Days >= cfg.JobStepFails7DaysCritical THEN 1 /* Critical. */
						WHEN J.JobStepFails7Days >= cfg.JobStepFails7DaysWarning THEN 2 /* Warning. */
						WHEN cfg.JobStepFails7DaysWarning IS NULL AND cfg.JobStepFails7DaysCritical IS NULL THEN 3  /* N/A.  Threshold not set */
						ELSE 4 END /* Good. */
						AS JobStepFail7DaysStatus,

					CASE WHEN exc.IsExcluded=1 THEN 3 /* N/A. Job excluded by a name/category/description exclusion rule */
						WHEN J.enabled=0 THEN 3 /* N/A. Job not enabled */
						WHEN (J.JobStepFails24Hrs >= cfg.JobStepFails24HrsCritical OR J.JobStepFails24Hrs >= cfg.JobStepFails24HrsWarning) 
							AND J.AckDate > ISNULL(J.StepLastFailed,'19000101') 
							THEN 5  /* Acknowledged.  Warning or Critical threshold met but acknowledged date greater than last failed */
						WHEN J.JobStepFails24Hrs >= cfg.JobStepFails24HrsCritical THEN 1 /* Critical. */
						WHEN J.JobStepFails24Hrs >= cfg.JobStepFails24HrsWarning THEN 2 /* Warning. */
						WHEN cfg.JobStepFails24HrsWarning IS NULL AND cfg.JobStepFails24HrsCritical IS NULL THEN 3 /* N/A.  Threshold not set */
						ELSE 4 END /* Good. */
						AS JobStepFail24HrsStatus,

					CASE WHEN exc.IsExcluded=1 THEN 3 /* N/A. Job excluded by a name/category/description exclusion rule */
						WHEN J.enabled=0 THEN 3 /* N/A. Job not enabled */
						WHEN J.AckDate > ISNULL(J.LastFailed,'19000101') 
							AND J.IsLastFail=1 
							AND (cfg.LastFailIsCritical=1 OR cfg.LastFailIsWarning=1) 
							THEN 5 /* Acknowledged.  Warning or Critical threshold met but acknowledged date greater than last failed */
						WHEN J.IsLastFail=1 AND cfg.LastFailIsCritical=1 THEN 1 /* Critical. */
						WHEN J.IsLastFail=1 AND cfg.LastFailIsWarning=1 THEN 2 /* Warning. */
						WHEN cfg.LastFailIsCritical =0 AND cfg.LastFailIsWarning =0 THEN 3 /* N/A.  Threshold not set */
						ELSE 4 END /* Good. */
						AS LastFailStatus 
				) St