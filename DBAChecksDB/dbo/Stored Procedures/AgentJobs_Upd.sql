CREATE PROC [dbo].[AgentJobs_Upd](@AgentJobs AgentJobs READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS

IF NOT EXISTS(SELECT 1 FROM dbo.SnapshotDates SSD WHERE SSD.AgentJobsDate>=@SnapshotDate AND SSD.InstanceID = @InstanceID)
BEGIN
BEGIN TRAN
	DELETE dbo.AgentJobs WHERE InstanceID = @InstanceID
	INSERT INTO [dbo].[AgentJobs]
			   ([InstanceID]
			   ,[job_id]
			   ,[name]
			   ,[LastFail]
			   ,[LastSucceed]
			   ,[FailCount24Hrs]
			   ,[SucceedCount24Hrs]
			   ,[FailCount7Days]
			   ,[SucceedCount7Days]
			   ,[JobStepFails7Days]
			   ,[JobStepFails24Hrs]
			   ,[enabled]
			   ,[MaxDurationSec]
			   ,[AvgDurationSec]
			   ,[IsLastFail])
	SELECT @InstanceID
			   ,[job_id]
			   ,[name]
			   ,[LastFail]
			   ,[LastSucceed]
			   ,[FailCount24Hrs]
			   ,[SucceedCount24Hrs]
			   ,[FailCount7Days]
			   ,[SucceedCount7Days]
			   ,[JobStepFails7Days]
			   ,[JobStepFails24Hrs]
			   ,[enabled]
			   ,[MaxDurationSec]
			   ,[AvgDurationSec]
			   ,[IsLastFail]
	FROM @AgentJobs

	UPDATE dbo.SnapshotDates
	SET AgentJobsDate=@SnapshotDate
	WHERE InstanceID=@InstanceID
	COMMIT
END