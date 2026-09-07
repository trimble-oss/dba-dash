/*
	Imports a batch of deadlocks.

	Takes all three table types in one call rather than the usual one-proc-per-table so that the
	header, the graph, the participants and the resources are written under a single transaction.  The
	children can only be inserted for headers that survived dedup, so splitting this across procs would
	need either an ordering guarantee the importer does not make or a second pass to clean up orphans.
	DBImporter calls this directly (see UpdateDeadlocksAsync) for the same reason.

	Dedup is on (InstanceID, EventTime, DeadlockHash), which is the primary key of dbo.Deadlocks - so
	the constraint that stops a duplicate is the same one the row is stored under, and nothing has to
	be allocated or reconciled.  The collector re-reads overlapping data routinely (a bounded full read
	after a service restart, or after the event file the resume cursor pointed at has rolled away), so
	an already-stored deadlock arriving again is expected rather than an error.

	OUTPUT captures exactly which headers were inserted, so the children can be filtered to those
	without a second pass over dbo.Deadlocks.
*/
CREATE PROC dbo.Deadlocks_Upd(
	@Deadlocks dbo.Deadlocks READONLY,
	@DeadlockProcesses dbo.DeadlockProcesses READONLY,
	@DeadlockResources dbo.DeadlockResources READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(3)
)
AS
SET NOCOUNT ON
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='Deadlocks'

IF EXISTS(SELECT 1 FROM @Deadlocks)
BEGIN
	DECLARE @AzureDatabaseID INT

	/* For AzureDB there is a 1:1 mapping between dbo.Instances and dbo.Databases.  Get the associated DatabaseID */
	SELECT @AzureDatabaseID = D.DatabaseID
	FROM dbo.Instances I
	JOIN dbo.Databases D ON I.InstanceID = D.InstanceID
	WHERE I.EngineEdition=5
	AND I.InstanceID = @InstanceID
	AND I.IsActive=1
	AND D.IsActive=1

	CREATE TABLE #New(
		EventTime DATETIME2(3) NOT NULL,
		DeadlockHash BINARY(16) NOT NULL,
		PRIMARY KEY(EventTime,DeadlockHash)
	)

	BEGIN TRAN

	INSERT INTO dbo.Deadlocks
	(
		InstanceID,
		EventTime,
		DeadlockHash,
		Signature,
		SignatureVersion,
		ProcessCount,
		VictimCount,
		ResourceCount,
		IsParallel
	)
	OUTPUT inserted.EventTime, inserted.DeadlockHash INTO #New(EventTime,DeadlockHash)
	SELECT	@InstanceID,
			D.EventTime,
			D.DeadlockHash,
			/* Callers pass the signature as the familiar "0x..." hex string; store it in its 8-byte
			   binary form (style 1 parses the leading 0x), matching AI.DeadlockAnalysis.Signature so
			   the two join directly. */
			CONVERT(BINARY(8), D.Signature, 1),
			D.SignatureVersion,
			D.ProcessCount,
			D.VictimCount,
			D.ResourceCount,
			D.IsParallel
	FROM @Deadlocks D
	WHERE NOT EXISTS(SELECT 1
					FROM dbo.Deadlocks X
					WHERE X.InstanceID = @InstanceID
					AND X.EventTime = D.EventTime
					AND X.DeadlockHash = D.DeadlockHash
					)

	/*	Only store a row where we actually have a graph.  A NULL here would be indistinguishable from
		one aged out by the shorter retention on this table. */
	INSERT INTO dbo.DeadlockXml
	(
		InstanceID,
		EventTime,
		DeadlockHash,
		DeadlockXmlCompressed
	)
	SELECT	@InstanceID,
			D.EventTime,
			D.DeadlockHash,
			D.DeadlockXmlCompressed
	FROM @Deadlocks D
	JOIN #New N ON N.EventTime = D.EventTime AND N.DeadlockHash = D.DeadlockHash
	WHERE D.DeadlockXmlCompressed IS NOT NULL

	INSERT INTO dbo.DeadlockProcesses
	(
		InstanceID,
		EventTime,
		DeadlockHash,
		ProcessIndex,
		IsVictim,
		DatabaseID,
		SPID,
		Ecid,
		LoginName,
		HostName,
		ClientApp,
		ProcedureName,
		StatementText,
		IsolationLevel,
		LockMode,
		WaitResource,
		WaitTimeMs,
		LogUsed,
		TransactionName,
		Priority,
		LastBatchStarted,
		LastBatchCompleted,
		LastTransactionStarted,
		Status,
		TransactionCount,
		HostPid,
		InputBuffer,
		ClientOption1,
		ClientOption2
	)
	SELECT	@InstanceID,
			P.EventTime,
			P.DeadlockHash,
			P.ProcessIndex,
			P.IsVictim,
			ISNULL(DB.DatabaseID,@AzureDatabaseID), /* For AzureDB, fall back to @AzureDatabaseID if the graph's database_id doesn't match (see SlowQueries_Upd / issue #481) */
			P.SPID,
			P.Ecid,
			P.LoginName,
			P.HostName,
			P.ClientApp,
			P.ProcedureName,
			P.StatementText,
			P.IsolationLevel,
			P.LockMode,
			P.WaitResource,
			P.WaitTimeMs,
			P.LogUsed,
			P.TransactionName,
			P.Priority,
			P.LastBatchStarted,
			P.LastBatchCompleted,
			P.LastTransactionStarted,
			P.Status,
			P.TransactionCount,
			P.HostPid,
			P.InputBuffer,
			P.ClientOption1,
			P.ClientOption2
	FROM @DeadlockProcesses P
	JOIN #New N ON N.EventTime = P.EventTime AND N.DeadlockHash = P.DeadlockHash
	LEFT JOIN dbo.Databases DB ON DB.database_id = P.database_id AND DB.InstanceID = @InstanceID AND DB.IsActive=1

	INSERT INTO dbo.DeadlockResources
	(
		InstanceID,
		EventTime,
		DeadlockHash,
		ResourceIndex,
		ResourceType,
		DatabaseID,
		ObjectName,
		IndexName,
		LockMode,
		OwnerModes,
		WaiterModes,
		OwnerCount,
		WaiterCount,
		IsParallelismResource
	)
	SELECT	@InstanceID,
			R.EventTime,
			R.DeadlockHash,
			R.ResourceIndex,
			R.ResourceType,
			ISNULL(DB.DatabaseID,@AzureDatabaseID),
			R.ObjectName,
			R.IndexName,
			R.LockMode,
			R.OwnerModes,
			R.WaiterModes,
			R.OwnerCount,
			R.WaiterCount,
			R.IsParallelismResource
	FROM @DeadlockResources R
	JOIN #New N ON N.EventTime = R.EventTime AND N.DeadlockHash = R.DeadlockHash
	LEFT JOIN dbo.Databases DB ON DB.database_id = R.database_id AND DB.InstanceID = @InstanceID AND DB.IsActive=1

	COMMIT

	DROP TABLE #New
END

/*	Runs whether or not anything was imported: no deadlocks is the normal case and still means the
	collection ran, so the collection date must advance. */
EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
										@Reference = @Ref,
										@SnapshotDate = @SnapshotDate
