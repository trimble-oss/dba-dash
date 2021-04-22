CREATE PROC dbo.AvailabilityGroups_Upd(
		@AvailabilityGroups dbo.AvailabilityGroups READONLY,
		@InstanceID INT,
		@SnapshotDate DATETIME2(2)
)
AS
DECLARE @Ref VARCHAR(30)='AvailabilityGroups'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
BEGIN TRAN;
	DELETE dbo.AvailabilityGroups WHERE InstanceID = @InstanceID 

	INSERT INTO dbo.AvailabilityGroups(
		   InstanceID,
		   group_id,
           name,
           resource_id,
           resource_group_id,
           failure_condition_level,
           health_check_timeout,
           automated_backup_preference,
           version,
           basic_features,
           dtc_support,
           db_failover,
           is_distributed,
           cluster_type,
           required_synchronized_secondaries_to_commit,
           sequence_number,
           is_contained)
    SELECT @InstanceID,
		   group_id,
           name,
           resource_id,
           resource_group_id,
           failure_condition_level,
           health_check_timeout,
           automated_backup_preference,
           version,
           basic_features,
           dtc_support,
           db_failover,
           is_distributed,
           cluster_type,
           required_synchronized_secondaries_to_commit,
           sequence_number,
           is_contained
    FROM @AvailabilityGroups

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate;
	COMMIT;
END;