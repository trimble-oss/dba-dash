CREATE PROC dbo.SessionWaits_Upd(
	@SessionWaits dbo.SessionWaits READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(7)
)
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='SessionWaits'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=CAST(@SnapshotDate AS DATETIME2(2)) AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
   IF EXISTS(  SELECT wait_type
			   FROM @SessionWaits
			   EXCEPT
			   SELECT WaitType 
			   FROM dbo.WaitType
			   )
   BEGIN
	   INSERT INTO dbo.WaitType
	   (
		   WaitType
	   )
	   SELECT wait_type
	   FROM @SessionWaits
	   EXCEPT
	   SELECT WaitType 
	   FROM dbo.WaitType
   END

   INSERT INTO dbo.SessionWaits
   (
       SnapshotDateUTC,
       InstanceID,
       session_id,
       WaitTypeID,
       waiting_tasks_count,
       wait_time_ms,
       max_wait_time_ms,
       signal_wait_time_ms,
       login_time_utc
   )
   SELECT SW.SnapshotDateUTC,
		  @InstanceID,
          SW.session_id,
          WT.WaitTypeID,
          SW.waiting_tasks_count,
          SW.wait_time_ms,
          SW.max_wait_time_ms,
          SW.signal_wait_time_ms,
          SW.login_time_utc 
	FROM @SessionWaits SW
	JOIN dbo.WaitType WT ON SW.wait_type = WT.WaitType


	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate;

END

