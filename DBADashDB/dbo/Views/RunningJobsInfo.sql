CREATE VIEW dbo.RunningJobsInfo
AS
SELECT RJ.InstanceID,
		I.InstanceDisplayName,
		RJ.job_id,
		J.name AS job_name,
		RJ.current_execution_status,
		CASE	WHEN RJ.current_execution_status = 1 THEN '1. Executing'
				WHEN RJ.current_execution_status = 2 THEN '2. Waiting For Thread'
				WHEN RJ.current_execution_status = 3 THEN '3. Between Retries'
				WHEN RJ.current_execution_status = 4 THEN '4. Idle'
				WHEN RJ.current_execution_status = 5 THEN '5. Suspended'
				WHEN RJ.current_execution_status = 6 THEN '6. Obsolete'
				WHEN RJ.current_execution_status = 7 THEN '7. Performing Completion Actions'
				ELSE CONCAT(RJ.current_execution_status, '. Unknown') END AS current_execution_status_description,

		HD.HumanDuration AS RunningTime, /* How long has the job been running */
		HDAvg.HumanDuration AS AvgDuration, /* How long does the job usually take to run (Avg over 30 days) */
		JStats.AvgRunDurationSec, /* How long does the job usually take to run (Avg over 30 days) */
		HDMax.HumanDuration AS MaxDuration,	/* What's the max time the job took to run (over 30 days) */
		JStats.MaxRunDurationSec, /* What's the max time the job took to run (over 30 days) */
		DATEADD(s,JStats.AvgRunDurationSec,RJ.start_execution_date_utc) AS EstimatedCompletionTime, /* Estimated completion date based on average duration for this job */
		RJ.start_execution_date_utc, /* Job started */
		DATEDIFF(s,RJ.start_execution_date_utc,RJ.SnapshotDate) AS RunningTimeSec, /* How long job was running at time of data collection */
		
		/* Current Step */

		CASE WHEN RJ.current_execution_step_id IS NULL THEN '' ELSE CONCAT('(',RJ.current_execution_step_id,')  ',RJ.current_execution_step_name) END AS current_execution_step,
		RJ.current_execution_step_id,
		RJ.current_execution_step_name,
		RJ.current_retry_attempt,
		HDCurrentStep.HumanDuration AS CurrentStepDuration,
		H.CurrentStepRunDurationSec,

		/* Previous Step */
		CASE WHEN RJ.last_executed_step_id IS NULL THEN '' ELSE CONCAT('(',RJ.last_executed_step_id,')  ',JS.step_name) END AS last_executed_step,
		RJ.last_executed_step_id,
		JS.step_name AS last_executed_step_name,
		RJ.last_executed_step_date_utc AS LastStepStartDateUtc,
		H.LastStepFinishDateUtc,
		HDLastStep.HumanDuration AS LastStepDuration,
		H.LastStepRunDurationSec,
		
		/* Running Jobs collection info */
		HDSnapshot.HumanDuration AS TimeSinceSnapshot, 
		RJ.SnapshotDate,
		CDS.Status AS SnapshotAgeStatus

FROM dbo.RunningJobs RJ
JOIN dbo.Instances I ON RJ.InstanceID = I.InstanceID
LEFT JOIN dbo.Jobs J ON RJ.InstanceID = J.InstanceID AND J.job_id = RJ.job_id
LEFT JOIN dbo.JobSteps JS ON JS.InstanceID = RJ.InstanceID AND RJ.job_id = JS.job_id AND RJ.last_executed_step_id = JS.step_id
/* Get Avg and Max run times for this job over last 30 days */
OUTER APPLY(SELECT	SUM(JS.RunDurationSec)/NULLIF(SUM(JS.SucceededCount+JS.FailedCount),0) AvgRunDurationSec,
					MAX(JS.MaxRunDurationSec) MaxRunDurationSec
			FROM dbo.JobStats_60MIN JS
			WHERE JS.RunDateTime >= DATEADD(d,-30,GETUTCDATE())
			AND JS.InstanceID = RJ.InstanceID
			AND JS.job_id = RJ.job_id
			AND JS.step_id =0 /* result */
			) JStats

LEFT JOIN dbo.CollectionDatesStatus CDS ON CDS.InstanceID = RJ.InstanceID AND CDS.Reference = 'RunningJobs'
/* Find the last step in JobHistory table so we know it's finish time and we can calculate how long the current step has been running */
OUTER APPLY(SELECT TOP(1)	JH.FinishDateTime AS LastStepFinishDateUtc, 
							JH.RunDurationSec AS LastStepRunDurationSec,
							DATEDIFF_BIG(s,CASE WHEN RJ.last_executed_step_id IS NULL THEN RJ.start_execution_date_utc ELSE JH.FinishDateTime END,RJ.SnapshotDate) AS CurrentStepRunDurationSec
			FROM dbo.JobHistory JH 
			WHERE JH.InstanceID = RJ.InstanceID
			AND JH.job_id = RJ.job_id
			AND JH.FinishDateTime >= CAST(RJ.start_execution_date_utc AS DATETIME2(2))
			AND JH.FinishDateTime <= CAST(GETUTCDATE() AS DATETIME2(2))
			AND JH.RunDateTime >= CAST(RJ.start_execution_date_utc AS DATETIME2(2))
			AND JH.RunDateTime < CAST(GETUTCDATE() AS DATETIME2(2))
			AND JH.step_id = RJ.last_executed_step_id
			ORDER BY JH.instance_id
			) AS H
CROSS APPLY dbo.SecondsToHumanDuration(H.CurrentStepRunDurationSec) HDCurrentStep
CROSS APPLY dbo.SecondsToHumanDuration(H.LastStepRunDurationSec) HDLastStep
CROSS APPLY dbo.SecondsToHumanDuration(JStats.AvgRunDurationSec) HDAvg
CROSS APPLY dbo.SecondsToHumanDuration(JStats.MaxRunDurationSec) HDMax
CROSS APPLY dbo.MillisecondsToHumanDuration(DATEDIFF_BIG(ms,RJ.start_execution_date_utc,RJ.SnapshotDate)) HD
CROSS APPLY dbo.MillisecondsToHumanDuration(DATEDIFF_BIG(ms,RJ.SnapshotDate,GETUTCDATE())) HDSnapshot
