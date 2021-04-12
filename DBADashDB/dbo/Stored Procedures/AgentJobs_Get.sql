CREATE PROC [dbo].[AgentJobs_Get](
	@InstanceIDs VARCHAR(MAX) = NULL,
	@enabled TINYINT=1,
	@IncludeCritical BIT=1,
	@IncludeWarning BIT=1,
	@IncludeNA BIT=0,
	@IncludeOK BIT=0,
	@JobName SYSNAME=NULL,
    @JobID UNIQUEIDENTIFIER=NULL
)
AS
DECLARE @Instances IDs
IF @InstanceIDs IS NOT NULL
BEGIN
	INSERT INTO @Instances
	(
		ID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;

DECLARE @SQL NVARCHAR(MAX)
DECLARE @StatusesString NVARCHAR(MAX) = '0' + CASE WHEN @IncludeCritical=1 THEN ',1' ELSE '' END + CASE WHEN @IncludeWarning=1 THEN ',2' ELSE '' END + CASE WHEN @IncludeNA=1 THEN ',3' ELSE '' END + CASE WHEN @IncludeOK=1 THEN ',4' ELSE '' END

SET @SQL = N'
SELECT J.Instance,
       J.InstanceID,
       J.job_id,
       J.name,
	   J.description,
       J.LastFailed,
       J.TimeSinceLastFailed,
       J.TimeSinceLastFailureStatus,
       J.LastSucceeded,
       J.TimeSinceLastSucceeded,
       J.TimeSinceLastSucceededStatus,
       J.FailCount24Hrs,
       J.FailCount24HrsStatus,
       J.SucceededCount24Hrs,
       J.FailCount7Days,
       J.FailCount7DaysStatus,
       J.SucceededCount7Days,
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
WHERE J.JobStatus IN(' + @StatusesString + ')
AND J.enabled=@enabled
' + CASE WHEN @InstanceIDs IS NULL THEN '' ELSE 'AND EXISTS(SELECT 1 FROM @Instances I WHERE I.ID = J.InstanceID)' END + '
' + CASE WHEN @JobName IS NULL THEN '' ELSE 'AND J.name LIKE @JobName' END + '
' + CASE WHEN @JobID IS NULL THEN '' ELSE 'AND J.job_id = @JobID' END + ' 
ORDER BY J.IsLastFail DESC,J.LastFailed DESC'

EXEC sp_executesql @SQL,N'@Instances IDs READONLY,@JobName SYSNAME,@JobID UNIQUEIDENTIFIER,@enabled BIT',@Instances,@JobName,@JobID,@enabled