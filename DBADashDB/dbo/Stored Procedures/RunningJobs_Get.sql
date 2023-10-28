CREATE PROC RunningJobs_Get(
	@InstanceIDs IDs READONLY,
	@MinimumDurationSec INT = 0 
)
AS
SELECT RJ.InstanceID,
       RJ.InstanceDisplayName,
       RJ.job_id,
       RJ.job_name,
       RJ.current_execution_status,
       RJ.current_execution_status_description,
       RJ.RunningTime,
       RJ.AvgDuration,
       RJ.AvgRunDurationSec,
       RJ.MaxDuration,
       RJ.MaxRunDurationSec,
       RJ.EstimatedCompletionTime,
       RJ.start_execution_date_utc,
       RJ.RunningTimeSec,
       RJ.current_execution_step,
       RJ.current_execution_step_id,
       RJ.current_execution_step_name,
       RJ.current_retry_attempt,
       RJ.CurrentStepDuration,
       RJ.CurrentStepRunDurationSec,
       RJ.last_executed_step,
       RJ.last_executed_step_id,
       RJ.last_executed_step_name,
       RJ.LastStepStartDateUtc,
       RJ.LastStepFinishDateUtc,
       RJ.LastStepDuration,
       RJ.LastStepRunDurationSec,
       RJ.TimeSinceSnapshot,
       RJ.SnapshotDate,
       RJ.SnapshotAgeStatus
FROM dbo.RunningJobsInfo RJ
WHERE EXISTS(	
				SELECT 1 
				FROM @InstanceIDs I 
				WHERE I.ID = RJ.InstanceID
			)
AND RJ.RunningTimeSec >= @MinimumDurationSec
ORDER BY RJ.start_execution_date_utc

