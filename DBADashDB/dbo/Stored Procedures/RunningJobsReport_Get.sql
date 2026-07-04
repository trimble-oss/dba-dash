CREATE PROC dbo.RunningJobsReport_Get(
	@InstanceIDs IDs READONLY,
	@MinimumDurationSec INT = 60
)
AS
SELECT RJ.InstanceID,
       RJ.InstanceDisplayName,
       RJ.job_id,
       RJ.job_name,
       RJ.current_execution_status,
       RJ.current_execution_status_description,
       CASE WHEN RJ.current_execution_status IS NULL THEN 3
            WHEN RJ.current_execution_status = 1 THEN 4
            ELSE 2 END AS ExecutionStatus,
       RJ.RunningTime,
       RJ.RunningTimeSec,
       RJ.AvgDuration,
       RJ.AvgRunDurationSec,
       RJ.MaxDuration,
       RJ.MaxRunDurationSec,
       CASE WHEN RJ.RunningTimeSec IS NULL THEN 3
            WHEN RJ.AvgRunDurationSec IS NULL OR RJ.MaxRunDurationSec IS NULL THEN 3
            WHEN RJ.RunningTimeSec < RJ.AvgRunDurationSec * 1.10 THEN 4
            WHEN RJ.RunningTimeSec < RJ.MaxRunDurationSec * 1.10 THEN 2
            ELSE 1 END AS RunningTimeStatus,
       RJ.EstimatedCompletionTime,
       RJ.start_execution_date_utc,
       RJ.current_execution_step,
       RJ.current_execution_step_id,
       RJ.current_execution_step_name,
       RJ.current_retry_attempt,
       CASE WHEN RJ.current_retry_attempt IS NULL THEN 3
            WHEN RJ.current_retry_attempt > 0 THEN 2
            ELSE 4 END AS RetryStatus,
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
       ISNULL(RJ.SnapshotAgeStatus,1) AS SnapshotAgeStatus
FROM dbo.RunningJobsInfo RJ
WHERE EXISTS(
		SELECT 1
		FROM @InstanceIDs I
		WHERE I.ID = RJ.InstanceID
		)
AND (RJ.RunningTimeSec >= @MinimumDurationSec OR RJ.RunningTimeSec IS NULL)
ORDER BY RJ.start_execution_date_utc
GO