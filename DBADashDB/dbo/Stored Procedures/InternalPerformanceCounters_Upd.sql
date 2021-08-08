CREATE PROC dbo.InternalPerformanceCounters_Upd(
	@InstanceID INT,
	@SnapshotDate DATETIME2(7),
	@InternalPerformanceCounters dbo.PerformanceCounters READONLY
)
AS
EXEC dbo.PerformanceCounters_Upd @PerformanceCounters=@InternalPerformanceCounters,
								@InstanceID = @InstanceID,
								@SnapshotDate=@SnapshotDate,
								@Internal=1