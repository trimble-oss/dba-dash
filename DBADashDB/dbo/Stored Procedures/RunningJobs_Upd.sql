CREATE PROC dbo.RunningJobs_Upd(
	@RunningJobs dbo.RunningJobs READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(7)
)
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='RunningJobs'

IF NOT EXISTS(	SELECT 1 
				FROM dbo.CollectionDates 
				WHERE SnapshotDate>=@SnapshotDate 
				AND InstanceID = @InstanceID 
				AND Reference=@Ref
				)
BEGIN
	DELETE dbo.RunningJobs
	WHERE InstanceID = @InstanceID

	INSERT INTO dbo.RunningJobs(InstanceID,
			job_id,
			run_requested_date_utc,
			run_requested_source,
			queued_date_utc,
			start_execution_date_utc,
			last_executed_step_id,
			last_executed_step_date_utc,
			SnapshotDate,
			current_execution_step_id,
			current_execution_step_name,
			current_retry_attempt,
			current_execution_status
	)
	SELECT @InstanceID,
			job_id,
			run_requested_date_utc,
			run_requested_source,
			queued_date_utc,
			start_execution_date_utc,
			last_executed_step_id,
			last_executed_step_date_utc,
			@SnapshotDate,
			current_execution_step_id,
			current_execution_step_name,
			current_retry_attempt,
			current_execution_status
	FROM @RunningJobs

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate

END