/*
	The DBA Dash service's own resource usage - one row per sample (every minute by default - ServiceStatsFrequencySeconds),
	collected while the service has LogInternalPerformanceCounters on and
	reported by the service itself rather than any instance it collects.  Keyed by the agent (dbo.DBADashAgent), so a remote
	service that writes to S3 or a folder is recorded against itself, not the service that imports its files.

	SnapshotDate is when the sample was taken (UTC).  ProcessStartDate is when the service process started - a new value
	is a restart.  These cover IntervalMs, the time since the previous sample (or since the process started, for its first
	sample), so each sample stands on its own:
		CpuMs				Processor time the process used (user + kernel, all cores).  The share of the host's
							processors is CpuMs / (IntervalMs * ProcessorCount).
		Gen0/1/2Collections	.NET garbage collections.
		GCPauseMs			Time the process was paused for garbage collection.
		MaxBusyWorkers		The most collection workers processing an item at once.  Workers here means none were free.
		QueueItemsStarted	Items taken off the collection work queue by a worker.
		QueueSkipped		Collections not queued because the same one, from its previous schedule, was still queued or
							running - the schedule is more frequent than the service can keep up with.
		QueueWaitTotalMs	Time the started items waited for a worker, added up.  The average is over QueueItemsStarted.
		QueueWaitMaxMs		The longest any started item waited for a worker.
		QueueItemsQueued	Items accepted onto the collection work queue.  With QueueSkipped, every attempt to queue one.
	The rest are the value when the sample was taken:
		WorkingSetKB		Physical memory in use - the Memory column in Task Manager.
		PeakWorkingSetKB	The highest working set since the process started.
		PrivateBytesKB		Memory committed to the process alone (Private Bytes, Commit size).
		ManagedHeapKB		Allocated on the .NET heap.  Growing with the working set points at managed memory; a working
							set growing without it points elsewhere (native memory, the SQL client).
		GCCommittedKB		Memory the .NET GC has committed, as of its last collection.
		ThreadCount			Operating system threads in the process.
		ThreadPoolThreads	.NET thread pool threads.
		ThreadPoolQueueLength	Work items waiting for a thread pool thread.  A queue that stays up is thread pool starvation.
		HandleCount			Operating system handles.  Steady growth is a leak.
		SourceConnections	Sources the service is handling - SQL connections plus folder and S3 imports.  Includes
							discovered sources, so it can be higher than the connections in the config.
		SQLConnections		SQL connections - an instance, Azure DB or read replica each.  The scale to compare memory
							and CPU against.
		DiscoveredConnections	Sources the service added itself: Azure DBs found from a master connection (ScanForAzureDBs)
							and read replicas (MonitorReadReplicas).
		OfflineInstances	Source connections the service is treating as offline.
		QueueLengthHigh/Normal/Low	Collections waiting in each priority of the collection work queue.
		Workers				Collection workers.
		BusyWorkers			Workers processing an item.
	The queue columns are NULL unless queue based scheduling is on.

	No foreign key to dbo.DBADashAgent: PurgeData removes agents no instance refers to, and their rows age out here.
*/
CREATE TABLE dbo.ServiceStats(
	DBADashAgentID INT NOT NULL,
	SnapshotDate DATETIME2(2) NOT NULL,
	ProcessStartDate DATETIME2(2) NULL,
	IntervalMs INT NOT NULL,
	CpuMs INT NULL,
	ProcessorCount SMALLINT NOT NULL,
	WorkingSetKB INT NULL,
	PeakWorkingSetKB INT NULL,
	PrivateBytesKB INT NULL,
	ManagedHeapKB INT NULL,
	GCCommittedKB INT NULL,
	Gen0Collections INT NULL,
	Gen1Collections INT NULL,
	Gen2Collections INT NULL,
	GCPauseMs INT NULL,
	ThreadCount INT NULL,
	ThreadPoolThreads INT NULL,
	ThreadPoolQueueLength INT NULL,
	HandleCount INT NULL,
	SourceConnections INT NULL,
	SQLConnections INT NULL,
	DiscoveredConnections INT NULL,
	OfflineInstances INT NULL,
	QueueLengthHigh INT NULL,
	QueueLengthNormal INT NULL,
	QueueLengthLow INT NULL,
	Workers INT NULL,
	BusyWorkers INT NULL,
	MaxBusyWorkers INT NULL,
	QueueItemsStarted INT NULL,
	QueueSkipped INT NULL,
	QueueWaitTotalMs BIGINT NULL,
	QueueWaitMaxMs INT NULL,
	QueueItemsQueued INT NULL,
	CONSTRAINT PK_ServiceStats PRIMARY KEY CLUSTERED(DBADashAgentID,SnapshotDate) WITH (DATA_COMPRESSION = PAGE) ON PS_ServiceStats(SnapshotDate)
) ON PS_ServiceStats(SnapshotDate);
