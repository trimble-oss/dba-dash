/*
	The batches of collection runs imported for each instance.  Runs are added to the hourly rollup by incrementing it, so a
	file imported twice would count its runs twice - this is how dbo.CollectionRuns_Upd knows a batch is already in.

	A batch is its SnapshotDate and BatchID - an id the collector gives each file.  SnapshotDate alone isn't enough: two
	schedules for the same instance can start within the same 10ms and send different batches with the same SnapshotDate.
	Files without an id (durations from an older service, imported as runs) have the empty id, so they're told apart by
	SnapshotDate as before.  Short retention: only needs to outlive a file being re-imported.
*/
CREATE TABLE dbo.CollectionRunsBatch(
	InstanceID INT NOT NULL,
	SnapshotDate DATETIME2(2) NOT NULL,
	BatchID UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_CollectionRunsBatch_BatchID DEFAULT('00000000-0000-0000-0000-000000000000'),
	CONSTRAINT PK_CollectionRunsBatch PRIMARY KEY CLUSTERED(InstanceID,SnapshotDate,BatchID) ON PS_CollectionRunsBatch(SnapshotDate)
) ON PS_CollectionRunsBatch(SnapshotDate);
