CREATE PROC AgentJobThresholds_Get(
    @InstanceID INT,
    @JobID UNIQUEIDENTIFIER
)
AS
SELECT InstanceID,
       job_id,
       TimeSinceLastFailureWarning,
       TimeSinceLastFailureCritical,
       TimeSinceLastSucceededWarning,
       TimeSinceLastSucceededCritical,
       FailCount24HrsWarning,
       FailCount24HrsCritical,
       FailCount7DaysCritical,
       FailCount7DaysWarning,
       JobStepFails24HrsWarning,
       JobStepFails24HrsCritical,
       JobStepFails7DaysWarning,
       JobStepFails7DaysCritical,
       LastFailIsCritical,
       LastFailIsWarning,
       AgentIsRunningCheck
FROM dbo.AgentJobThresholds
WHERE InstanceID=@InstanceID
AND job_id= @JobID