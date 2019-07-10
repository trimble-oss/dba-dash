CREATE PROC [dbo].[AgentJobs_Upd](@Jobs AgentJobs READONLY,@InstanceID INT,@SnapshotDate DATETIME)
AS
BEGIN TRAN
DELETE AgentJobs WHERE InstanceID = @InstanceID
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
FROM @Jobs

UPDATE dbo.SnapshotDates
SET AgentJobsDate=@SnapshotDate
WHERE InstanceID=@InstanceID
COMMIT