
CREATE PROC [dbo].[AgentJobs_Upd](@AgentJobs AgentJobs READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
DECLARE @Ref VARCHAR(30)='AgentJobs'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
BEGIN TRAN;
	DELETE dbo.AgentJobs WHERE InstanceID = @InstanceID;
	INSERT INTO [dbo].[AgentJobs]
	(
		[InstanceID],
		[job_id],
		[name],
		[LastFail],
		[LastSucceed],
		[FailCount24Hrs],
		[SucceedCount24Hrs],
		[FailCount7Days],
		[SucceedCount7Days],
		[JobStepFails7Days],
		[JobStepFails24Hrs],
		[enabled],
		[MaxDurationSec],
		[AvgDurationSec],
		[IsLastFail],
		[start_step_id],
		[category_id],
		owner_sid,
		notify_email_operator_id,
		notify_netsend_operator_id,
		notify_page_operator_id,
		notify_level_eventlog,
		notify_level_email,
		notify_level_netsend,
		notify_level_page,
		date_created,
		date_modified,
		Description,
		delete_level,
		version_number
	)
	SELECT @InstanceID,
		   [job_id],
		   [name],
		   [LastFail],
		   [LastSucceed],
		   [FailCount24Hrs],
		   [SucceedCount24Hrs],
		   [FailCount7Days],
		   [SucceedCount7Days],
		   [JobStepFails7Days],
		   [JobStepFails24Hrs],
		   [enabled],
		   [MaxDurationSec],
		   [AvgDurationSec],
		   [IsLastFail],
		   start_step_id,
		   category_id,
		   CONVERT(VARBINARY(85), owner_sid_string, 2),
		   notify_email_operator_id,
		   notify_netsend_operator_id,
		   notify_page_operator_id,
		   notify_level_eventlog,
		   notify_level_email,
		   notify_level_netsend,
		   notify_level_page,
		   date_created,
		   date_modified,
		   Description,
		   delete_level,
		   version_number
	FROM @AgentJobs;

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate;
	COMMIT;
END;