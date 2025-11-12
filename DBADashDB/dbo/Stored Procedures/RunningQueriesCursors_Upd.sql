CREATE PROC dbo.RunningQueriesCursors_Upd(
	@RunningQueriesCursors dbo.RunningQueriesCursors READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(7)
)
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='RunningQueriesCursors'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=CAST(@SnapshotDate AS DATETIME2(2)) AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	BEGIN TRANSACTION;

    INSERT INTO dbo.RunningQueriesCursors(
			InstanceID,
			SnapshotDateUTC,
			session_id,
			name,
			properties,
			sql_handle,
			statement_start_offset,
			statement_end_offset,
			plan_generation_num,
			creation_time_utc,
			is_open,
			is_async_population,
			is_close_on_commit,
			fetch_status,
			fetch_buffer_size,
			fetch_buffer_start,
			ansi_position,
			worker_time,
			reads,
			writes,
			dormant_duration,
			uniqueifier
		)
	SELECT 	@InstanceID,
			SnapshotDateUTC,
			session_id,
			name,
			properties,
			sql_handle,
			statement_start_offset,
			statement_end_offset,
			plan_generation_num,
			creation_time_utc,
			is_open,
			is_async_population,
			is_close_on_commit,
			fetch_status,
			fetch_buffer_size,
			fetch_buffer_start,
			ansi_position,
			worker_time,
			reads,
			writes,
			dormant_duration,
			ROW_NUMBER() OVER (PARTITION BY RQC.session_id ORDER BY creation_time_utc) AS uniqueifier
	FROM @RunningQueriesCursors RQC

	EXEC dbo.CollectionDates_Upd	@InstanceID = @InstanceID,  
									@Reference = @Ref,
									@SnapshotDate = @SnapshotDate;
	COMMIT TRANSACTION;

END