CREATE PROC dbo.ServiceStats_Upd(
	@DBADashAgentID INT,
	@ServiceStats dbo.ServiceStats READONLY
)
AS
/*
	Service resource usage samples - see dbo.ServiceStats.  A sample that's already recorded is skipped: the service sends
	samples again after a send that failed, and with several destinations one can fail after another has them.
*/
SET NOCOUNT ON
SET XACT_ABORT ON

INSERT INTO dbo.ServiceStats(
	DBADashAgentID,
	SnapshotDate,
	ProcessStartDate,
	IntervalMs,
	CpuMs,
	ProcessorCount,
	WorkingSetKB,
	PeakWorkingSetKB,
	PrivateBytesKB,
	ManagedHeapKB,
	GCCommittedKB,
	Gen0Collections,
	Gen1Collections,
	Gen2Collections,
	GCPauseMs,
	ThreadCount,
	ThreadPoolThreads,
	ThreadPoolQueueLength,
	HandleCount,
	SourceConnections,
	SQLConnections,
	DiscoveredConnections,
	OfflineInstances,
	QueueLengthHigh,
	QueueLengthNormal,
	QueueLengthLow,
	Workers,
	BusyWorkers,
	MaxBusyWorkers,
	QueueItemsStarted,
	QueueSkipped,
	QueueWaitTotalMs,
	QueueWaitMaxMs,
	QueueItemsQueued
)
SELECT	@DBADashAgentID,
		T.SnapshotDate,
		T.ProcessStartDate,
		T.IntervalMs,
		T.CpuMs,
		T.ProcessorCount,
		T.WorkingSetKB,
		T.PeakWorkingSetKB,
		T.PrivateBytesKB,
		T.ManagedHeapKB,
		T.GCCommittedKB,
		T.Gen0Collections,
		T.Gen1Collections,
		T.Gen2Collections,
		T.GCPauseMs,
		T.ThreadCount,
		T.ThreadPoolThreads,
		T.ThreadPoolQueueLength,
		T.HandleCount,
		T.SourceConnections,
		T.SQLConnections,
		T.DiscoveredConnections,
		T.OfflineInstances,
		T.QueueLengthHigh,
		T.QueueLengthNormal,
		T.QueueLengthLow,
		T.Workers,
		T.BusyWorkers,
		T.MaxBusyWorkers,
		T.QueueItemsStarted,
		T.QueueSkipped,
		T.QueueWaitTotalMs,
		T.QueueWaitMaxMs,
		T.QueueItemsQueued
FROM (	/* Rounded to the stored precision first, so the duplicate check compares what's stored */
		SELECT	CAST(SnapshotDate AS DATETIME2(2)) AS SnapshotDate,
				CAST(ProcessStartDate AS DATETIME2(2)) AS ProcessStartDate,
				IntervalMs,
				CpuMs,
				ProcessorCount,
				WorkingSetKB,
				PeakWorkingSetKB,
				PrivateBytesKB,
				ManagedHeapKB,
				GCCommittedKB,
				Gen0Collections,
				Gen1Collections,
				Gen2Collections,
				GCPauseMs,
				ThreadCount,
				ThreadPoolThreads,
				ThreadPoolQueueLength,
				HandleCount,
				SourceConnections,
				SQLConnections,
				DiscoveredConnections,
				OfflineInstances,
				QueueLengthHigh,
				QueueLengthNormal,
				QueueLengthLow,
				Workers,
				BusyWorkers,
				MaxBusyWorkers,
				QueueItemsStarted,
				QueueSkipped,
				QueueWaitTotalMs,
				QueueWaitMaxMs,
				QueueItemsQueued,
				ROW_NUMBER() OVER(PARTITION BY CAST(SnapshotDate AS DATETIME2(2)) ORDER BY SnapshotDate) AS rnum
		FROM @ServiceStats
		) T
WHERE T.rnum = 1
/*	Locked to the end of the insert, so two deliveries of the same samples at once (the same samples can reach a repository
	two ways - a SQL destination, and a folder another service imports) can't both pass the check and fail the insert on
	the primary key */
AND NOT EXISTS(	SELECT 1
				FROM dbo.ServiceStats SS WITH(UPDLOCK,HOLDLOCK)
				WHERE SS.DBADashAgentID = @DBADashAgentID
				AND SS.SnapshotDate = T.SnapshotDate
				)
