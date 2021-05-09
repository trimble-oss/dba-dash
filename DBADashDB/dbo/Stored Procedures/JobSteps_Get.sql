CREATE PROC dbo.JobSteps_Get(@InstanceID INT,@JobID UNIQUEIDENTIFIER)
AS
SELECT step_id,
	step_name,
	subsystem
FROM dbo.JobSteps
WHERE InstanceID = @InstanceID
AND job_id = @JobID
ORDER by step_id

