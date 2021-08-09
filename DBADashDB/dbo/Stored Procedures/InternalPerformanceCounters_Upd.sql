CREATE PROC dbo.InternalPerformanceCounters_Upd(
	@InstanceID INT,
	@SnapshotDate DATETIME2(7),
	@InternalPerformanceCounters dbo.PerformanceCounters READONLY
)
AS
SET XACT_ABORT ON
DECLARE @Ref NVARCHAR(128)='InternalPerformanceCounters'

EXEC dbo.PerformanceCounters_Upd @PerformanceCounters=@InternalPerformanceCounters,
								@InstanceID = @InstanceID,
								@SnapshotDate=@SnapshotDate,
								@Internal=1

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
								@Reference = @Ref,
								@SnapshotDate = @SnapshotDate