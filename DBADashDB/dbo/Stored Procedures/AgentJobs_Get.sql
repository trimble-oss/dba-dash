CREATE PROC [dbo].[AgentJobs_Get](
	@InstanceIDs VARCHAR(MAX) = NULL,
	@enabled TINYINT=1,
	@IncludeCritical BIT=1,
	@IncludeWarning BIT=1,
	@IncludeNA BIT=0,
	@IncludeOK BIT=0,
	@JobName SYSNAME=NULL
)
AS
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;


WITH Statuses AS(
	SELECT 1 AS Status
	WHERE @IncludeCritical=1
	UNION ALL
	SELECT 2
	WHERE @IncludeWarning=1
	UNION ALL
	SELECT 3
	WHERE @IncludeNA=1
	UNION ALL
	SELECT 4 
	WHERE @IncludeOK=1
)
SELECT J.Instance,
       J.InstanceID,
       J.job_id,
       J.name,
       J.LastFail,
	   J.LastFailUTC,
       J.TimeSinceLastFail,
       J.TimeSinceLastFailureStatus,
       J.LastSucceed,
	   J.LastSucceedUTC,
       J.TimeSinceLastSucceeded,
       J.TimeSinceLastSucceededStatus,
       J.TimeSinceLastSucceed,
       J.FailCount24Hrs,
       J.FailCount24HrsStatus,
       J.SucceedCount24Hrs,
       J.FailCount7Days,
       J.FailCount7DaysStatus,
       J.SucceedCount7Days,
       J.JobStepFails7Days,
       J.JobStepFail7DaysStatus,
       J.JobStepFails24Hrs,
       J.JobStepFail24HrsStatus,
       J.enabled,
       J.MaxDurationSec,
       J.AvgDurationSec,
       J.LastFailStatus,
       J.IsLastFail,
       J.TimeSinceLastFailureWarning,
       J.TimeSinceLastFailureCritical,
       J.TimeSinceLastSucceededWarning,
       J.TimeSinceLastSucceededCritical,
       J.FailCount24HrsWarning,
       J.FailCount24HrsCritical,
       J.FailCount7DaysCritical,
       J.FailCount7DaysWarning,
       J.JobStepFails24HrsWarning,
       J.JobStepFails24HrsCritical,
       J.JobStepFails7DaysWarning,
       J.JobStepFails7DaysCritical,
       J.LastFailIsCritical,
       J.LastFailIsWarning,
       J.ConfiguredLevel,
	   J.JobStatus
FROM dbo.AgentJobStatus J
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = J.InstanceID)
AND J.enabled=@enabled
AND (J.name LIKE @JobName OR @JobName IS NULL)
AND EXISTS(SELECT 1 FROM Statuses s WHERE S.Status=J.JobStatus)
ORDER BY J.IsLastFail DESC,J.LastFail DESC