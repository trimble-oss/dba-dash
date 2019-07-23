CREATE PROC [dbo].[AgentJobs_Upd](@AgentJobs AgentJobs READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
DECLARE @Ref VARCHAR(30)='AgentJobs'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
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

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate
	COMMIT
END