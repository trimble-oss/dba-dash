CREATE PROC AgentJobThresholds_Upd(
	   @InstanceId INT,
       @job_id UNIQUEIDENTIFIER,
       @TimeSinceLastFailureWarning INT=NULL,
       @TimeSinceLastFailureCritical INT=NULL,
       @TimeSinceLastSucceededWarning INT=NULL,
       @TimeSinceLastSucceededCritical INT=NULL,
       @FailCount24HrsWarning INT=NULL,
       @FailCount24HrsCritical INT=NULL,
       @FailCount7DaysCritical INT=NULL,
       @FailCount7DaysWarning INT=NULL,
       @JobStepFails24HrsWarning INT=NULL,
       @JobStepFails24HrsCritical INT=NULL,
       @JobStepFails7DaysWarning INT=NULL,
       @JobStepFails7DaysCritical INT=NULL,
       @LastFailIsCritical INT=NULL,
       @LastFailIsWarning INT=NULL,
	   @Inherit BIT
)
AS
SET XACT_ABORT ON
SET NOCOUNT ON

BEGIN TRAN
DELETE dbo.AgentJobThresholds
WHERE InstanceId = @InstanceId
AND job_id = @job_id

IF @Inherit=0
BEGIN

	INSERT INTO dbo.AgentJobThresholds
	(
		InstanceId,
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
		LastFailIsWarning
	)
	VALUES( @InstanceId,
		   @job_id,
		   @TimeSinceLastFailureWarning,
		   @TimeSinceLastFailureCritical,
		   @TimeSinceLastSucceededWarning,
		   @TimeSinceLastSucceededCritical,
		   @FailCount24HrsWarning,
		   @FailCount24HrsCritical,
		   @FailCount7DaysCritical,
		   @FailCount7DaysWarning,
		   @JobStepFails24HrsWarning,
		   @JobStepFails24HrsCritical,
		   @JobStepFails7DaysWarning,
		   @JobStepFails7DaysCritical,
		   @LastFailIsCritical,
		   @LastFailIsWarning)
END

COMMIT