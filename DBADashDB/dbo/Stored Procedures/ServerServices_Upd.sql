CREATE PROCEDURE dbo.ServerServices_Upd(
	@InstanceID INT,
	@SnapshotDate DATETIME2,
	@ServerServices ServerServices READONLY
)
AS
SET XACT_ABORT ON
SET NOCOUNT ON
BEGIN TRAN

DELETE dbo.ServerServices
WHERE InstanceID = @InstanceID

INSERT INTO dbo.ServerServices(
	InstanceID,
	SnapshotDate,
	servicename,
	startup_type,
	startup_type_desc,
	status,
	status_desc,
	process_id,
	last_startup_time,
	service_account,
	filename,
	is_clustered,
	cluster_nodename,
	instant_file_initialization_enabled
)
SELECT
	@InstanceID AS InstanceID,
	@SnapshotDate AS SnapshotDate,
	servicename,
	startup_type,
	startup_type_desc,
	status,
	status_desc,
	process_id,
	last_startup_time,
	service_account,
	filename,
	is_clustered,
	cluster_nodename,
	instant_file_initialization_enabled
FROM @ServerServices

COMMIT TRAN

/* Log the data collection */
EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
		@Reference = 'ServerServices',
		@SnapshotDate = @SnapshotDate
GO