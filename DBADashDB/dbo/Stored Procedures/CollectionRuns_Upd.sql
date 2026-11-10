CREATE PROC dbo.CollectionRuns_Upd(
	@InstanceID INT,
	@SnapshotDate DATETIME2(7),
	@CollectionRuns dbo.CollectionRuns READONLY,
	@StoreDetail BIT = 0, /* Keep each run in dbo.CollectionRuns as well as the hourly rollup.  The service sets this from LogInternalPerformanceCounters */
	@ImportStartDate DATETIME2(7) = NULL, /* UTC, by the importer's clock.  When the import of the file started */
	@SentDate DATETIME2(7) = NULL, /* UTC, by the collector's clock.  When the batch was first handed to a destination, once everything in it was collected */
	@BatchID UNIQUEIDENTIFIER = NULL /* The collector's id for the batch (file).  NULL for durations from an older service - told apart by @SnapshotDate only */
)
AS
/*
	Imports the collection runs from one batch (file).
	- Every run is added to the hourly rollup, dbo.CollectionRuns_60MIN.  With @StoreDetail, each run is kept in dbo.CollectionRuns too.
	- dbo.CollectionRunStatus is moved on to the latest run of each collection, counting failures in a row.  A run older
	  than the latest already recorded (a file imported out of order) counts in the rollup but doesn't move the status back.
	- The file's import lag is recorded against each run in it: from the batch being handed to a destination to its import
	  starting.  Near zero for a SQL destination; the time it waited in S3 or a folder otherwise.  Not from the batch's
	  SnapshotDate - that's when collecting started, so the lag would include collecting everything else in the batch.
	  NULL for a file without @SentDate.
	- The DBADash performance counters for each collection are registered as virtual counters (CounterType 5) read from
	  these tables - see dbo.PerformanceCountersBetweenDates.

	A batch is only imported once (dbo.CollectionRunsBatch, by @SnapshotDate and @BatchID): the rollup is incremented, so
	importing a file twice would otherwise count its runs twice.

	Batches for the same instance are imported one at a time (an application lock per instance).  Two batches can arrive
	together - collections on different schedules, or files imported from a folder in parallel - and both add to the same
	hourly row: without the lock, each could miss the other's new row, so one would either fail on the insert or leave its
	runs out of the rollup while its batch was still recorded as imported.
*/
SET NOCOUNT ON
SET XACT_ABORT ON

DECLARE @BatchDate DATETIME2(2) = @SnapshotDate
SET @BatchID = ISNULL(@BatchID,'00000000-0000-0000-0000-000000000000')
DECLARE @ImportLagMs INT = CASE WHEN @ImportStartDate IS NULL OR @SentDate IS NULL THEN NULL
								WHEN @ImportStartDate < @SentDate THEN 0 /* The clocks disagree - a different service imported it */
								WHEN DATEDIFF_BIG(ms,@SentDate,@ImportStartDate) > 2147483647 THEN 2147483647 /* Over 24 days - a file imported long after */
								ELSE DATEDIFF_BIG(ms,@SentDate,@ImportStartDate) END

IF EXISTS(	SELECT 1
			FROM dbo.CollectionRunsBatch
			WHERE InstanceID = @InstanceID
			AND SnapshotDate = @BatchDate
			AND BatchID = @BatchID
			)
BEGIN
	RETURN
END

/* Register any collections we haven't seen before.  Serialised with an application lock as two imports could otherwise
   race to add the same one (see dbo.PerformanceCounters_Upd) - only taken when there is something new. */
IF EXISTS(	SELECT Reference, Step
			FROM @CollectionRuns
			EXCEPT
			SELECT Reference, Step
			FROM dbo.CollectionRunReferences
			)
BEGIN
	DECLARE @LockResult INT
	BEGIN TRAN
		EXEC @LockResult = sp_getapplock @Resource = 'DBADash_CollectionRunReferences',
										 @LockMode = 'Exclusive',
										 @LockOwner = 'Transaction',
										 @LockTimeout = 30000
		IF @LockResult < 0
		BEGIN
			ROLLBACK;
			THROW 50000, 'Timed out waiting to register collection run references', 1;
		END
		INSERT INTO dbo.CollectionRunReferences(Reference,Step)
		SELECT Reference, Step
		FROM @CollectionRuns
		EXCEPT
		SELECT Reference, Step
		FROM dbo.CollectionRunReferences
	COMMIT
END

DECLARE @Runs TABLE(
	ReferenceID INT NOT NULL,
	SnapshotDate DATETIME2(2) NOT NULL,
	SnapshotDate60 DATETIME2(2) NOT NULL,
	Status TINYINT NULL,
	Attempts TINYINT NULL,
	DurationMs INT NULL,
	CpuMs INT NULL,
	LogicalReads BIGINT NULL,
	ImportMs INT NULL,
	ImportCpuMs INT NULL,
	ImportLogicalReads BIGINT NULL,
	ImportLagMs INT NULL,
	PRIMARY KEY(ReferenceID,SnapshotDate)
)

/* The same collection twice at the same time would be a duplicate - keep one */
INSERT INTO @Runs(ReferenceID,SnapshotDate,SnapshotDate60,Status,Attempts,DurationMs,CpuMs,LogicalReads,ImportMs,ImportCpuMs,ImportLogicalReads,ImportLagMs)
SELECT	ReferenceID,
		SnapshotDate,
		DATEADD(hh,DATEDIFF(hh,0,SnapshotDate),0),
		Status,
		Attempts,
		DurationMs,
		CpuMs,
		LogicalReads,
		ImportMs,
		ImportCpuMs,
		ImportLogicalReads,
		@ImportLagMs
FROM (	SELECT	R.ReferenceID,
				CAST(CR.SnapshotDate AS DATETIME2(2)) AS SnapshotDate,
				CR.Status,
				CR.Attempts,
				CR.DurationMs,
				CR.CpuMs,
				CR.LogicalReads,
				CR.ImportMs,
				CR.ImportCpuMs,
				CR.ImportLogicalReads,
				ROW_NUMBER() OVER(PARTITION BY R.ReferenceID, CAST(CR.SnapshotDate AS DATETIME2(2)) ORDER BY CR.Status DESC) AS rnum
		FROM @CollectionRuns CR
		JOIN dbo.CollectionRunReferences R ON R.Reference = CR.Reference AND R.Step = CR.Step
		) T
WHERE rnum = 1

DECLARE @Hourly TABLE(
	ReferenceID INT NOT NULL,
	SnapshotDate DATETIME2(2) NOT NULL,
	RunCount INT NOT NULL,
	SucceededCount INT NOT NULL,
	WarningCount INT NOT NULL,
	RetriedCount INT NOT NULL,
	PartialCount INT NOT NULL,
	FailedCount INT NOT NULL,
	ImportFailedCount INT NOT NULL,
	DurationCount INT NOT NULL,
	DurationTotalMs BIGINT NULL,
	DurationMinMs INT NULL,
	DurationMaxMs INT NULL,
	CpuCount INT NOT NULL,
	CpuTotalMs BIGINT NULL,
	CpuMinMs INT NULL,
	CpuMaxMs INT NULL,
	ReadsCount INT NOT NULL,
	ReadsTotal BIGINT NULL,
	ReadsMin BIGINT NULL,
	ReadsMax BIGINT NULL,
	ImportCount INT NOT NULL,
	ImportTotalMs BIGINT NULL,
	ImportMinMs INT NULL,
	ImportMaxMs INT NULL,
	ImportCpuCount INT NOT NULL,
	ImportCpuTotalMs BIGINT NULL,
	ImportCpuMinMs INT NULL,
	ImportCpuMaxMs INT NULL,
	ImportReadsCount INT NOT NULL,
	ImportReadsTotal BIGINT NULL,
	ImportReadsMin BIGINT NULL,
	ImportReadsMax BIGINT NULL,
	ImportLagCount INT NOT NULL,
	ImportLagTotalMs BIGINT NULL,
	ImportLagMinMs INT NULL,
	ImportLagMaxMs INT NULL,
	PRIMARY KEY(ReferenceID,SnapshotDate)
)

INSERT INTO @Hourly
SELECT	ReferenceID,
		SnapshotDate60,
		COUNT(*),
		COUNT(CASE WHEN Status = 1 THEN 1 END),
		COUNT(CASE WHEN Status = 2 THEN 1 END),
		COUNT(CASE WHEN Status = 3 THEN 1 END),
		COUNT(CASE WHEN Status = 4 THEN 1 END),
		COUNT(CASE WHEN Status = 5 THEN 1 END),
		COUNT(CASE WHEN Status = 6 THEN 1 END),
		COUNT(DurationMs),
		SUM(CAST(DurationMs AS BIGINT)),
		MIN(DurationMs),
		MAX(DurationMs),
		COUNT(CpuMs),
		SUM(CAST(CpuMs AS BIGINT)),
		MIN(CpuMs),
		MAX(CpuMs),
		COUNT(LogicalReads),
		SUM(LogicalReads),
		MIN(LogicalReads),
		MAX(LogicalReads),
		COUNT(ImportMs),
		SUM(CAST(ImportMs AS BIGINT)),
		MIN(ImportMs),
		MAX(ImportMs),
		COUNT(ImportCpuMs),
		SUM(CAST(ImportCpuMs AS BIGINT)),
		MIN(ImportCpuMs),
		MAX(ImportCpuMs),
		COUNT(ImportLogicalReads),
		SUM(ImportLogicalReads),
		MIN(ImportLogicalReads),
		MAX(ImportLogicalReads),
		COUNT(ImportLagMs),
		SUM(CAST(ImportLagMs AS BIGINT)),
		MIN(ImportLagMs),
		MAX(ImportLagMs)
FROM @Runs
GROUP BY ReferenceID, SnapshotDate60

/* Latest run of each collection in the batch */
DECLARE @Latest TABLE(
	ReferenceID INT NOT NULL PRIMARY KEY,
	SnapshotDate DATETIME2(2) NOT NULL,
	Status TINYINT NOT NULL,
	DurationMs INT NULL
)
INSERT INTO @Latest(ReferenceID,SnapshotDate,Status,DurationMs)
SELECT ReferenceID, SnapshotDate, Status, DurationMs
FROM (	SELECT	ReferenceID,
				SnapshotDate,
				Status,
				DurationMs,
				ROW_NUMBER() OVER(PARTITION BY ReferenceID ORDER BY SnapshotDate DESC) AS rnum
		FROM @Runs
		WHERE Status IS NOT NULL
		) T
WHERE rnum = 1

DECLARE @InstanceLockResult INT
DECLARE @InstanceLock NVARCHAR(255) = CONCAT(N'DBADash_CollectionRuns_',@InstanceID)

BEGIN TRAN

	EXEC @InstanceLockResult = sp_getapplock @Resource = @InstanceLock,
											 @LockMode = 'Exclusive',
											 @LockOwner = 'Transaction',
											 @LockTimeout = 30000
	IF @InstanceLockResult < 0
	BEGIN
		ROLLBACK;
		THROW 50000, 'Timed out waiting to import collection runs for the instance', 1;
	END

	/* Checked again now we hold the lock - the same file imported twice at once */
	IF EXISTS(	SELECT 1
				FROM dbo.CollectionRunsBatch
				WHERE InstanceID = @InstanceID
				AND SnapshotDate = @BatchDate
				AND BatchID = @BatchID
				)
	BEGIN
		COMMIT;
		RETURN;
	END

	INSERT INTO dbo.CollectionRunsBatch(InstanceID,SnapshotDate,BatchID)
	VALUES(@InstanceID,@BatchDate,@BatchID)

	IF @StoreDetail = 1
	BEGIN
		INSERT INTO dbo.CollectionRuns(InstanceID,ReferenceID,SnapshotDate,Status,Attempts,DurationMs,CpuMs,LogicalReads,ImportMs,ImportCpuMs,ImportLogicalReads,ImportLagMs)
		SELECT	@InstanceID,
				R.ReferenceID,
				R.SnapshotDate,
				R.Status,
				R.Attempts,
				R.DurationMs,
				R.CpuMs,
				R.LogicalReads,
				R.ImportMs,
				R.ImportCpuMs,
				R.ImportLogicalReads,
				R.ImportLagMs
		FROM @Runs R
		WHERE NOT EXISTS(	SELECT 1
							FROM dbo.CollectionRuns CR
							WHERE CR.InstanceID = @InstanceID
							AND CR.ReferenceID = R.ReferenceID
							AND CR.SnapshotDate = R.SnapshotDate
							)
	END

	UPDATE H60
		SET H60.RunCount += H.RunCount,
			H60.SucceededCount += H.SucceededCount,
			H60.WarningCount += H.WarningCount,
			H60.RetriedCount += H.RetriedCount,
			H60.PartialCount += H.PartialCount,
			H60.FailedCount += H.FailedCount,
			H60.ImportFailedCount += H.ImportFailedCount,
			H60.DurationCount += H.DurationCount,
			H60.DurationTotalMs = CASE WHEN H.DurationCount = 0 THEN H60.DurationTotalMs ELSE ISNULL(H60.DurationTotalMs,0) + H.DurationTotalMs END,
			H60.DurationMinMs = CASE WHEN H60.DurationMinMs IS NULL OR H.DurationMinMs < H60.DurationMinMs THEN H.DurationMinMs ELSE H60.DurationMinMs END,
			H60.DurationMaxMs = CASE WHEN H60.DurationMaxMs IS NULL OR H.DurationMaxMs > H60.DurationMaxMs THEN H.DurationMaxMs ELSE H60.DurationMaxMs END,
			H60.CpuCount += H.CpuCount,
			H60.CpuTotalMs = CASE WHEN H.CpuCount = 0 THEN H60.CpuTotalMs ELSE ISNULL(H60.CpuTotalMs,0) + H.CpuTotalMs END,
			H60.CpuMinMs = CASE WHEN H60.CpuMinMs IS NULL OR H.CpuMinMs < H60.CpuMinMs THEN H.CpuMinMs ELSE H60.CpuMinMs END,
			H60.CpuMaxMs = CASE WHEN H60.CpuMaxMs IS NULL OR H.CpuMaxMs > H60.CpuMaxMs THEN H.CpuMaxMs ELSE H60.CpuMaxMs END,
			H60.ReadsCount += H.ReadsCount,
			H60.ReadsTotal = CASE WHEN H.ReadsCount = 0 THEN H60.ReadsTotal ELSE ISNULL(H60.ReadsTotal,0) + H.ReadsTotal END,
			H60.ReadsMin = CASE WHEN H60.ReadsMin IS NULL OR H.ReadsMin < H60.ReadsMin THEN H.ReadsMin ELSE H60.ReadsMin END,
			H60.ReadsMax = CASE WHEN H60.ReadsMax IS NULL OR H.ReadsMax > H60.ReadsMax THEN H.ReadsMax ELSE H60.ReadsMax END,
			H60.ImportCount += H.ImportCount,
			H60.ImportTotalMs = CASE WHEN H.ImportCount = 0 THEN H60.ImportTotalMs ELSE ISNULL(H60.ImportTotalMs,0) + H.ImportTotalMs END,
			H60.ImportMinMs = CASE WHEN H60.ImportMinMs IS NULL OR H.ImportMinMs < H60.ImportMinMs THEN H.ImportMinMs ELSE H60.ImportMinMs END,
			H60.ImportMaxMs = CASE WHEN H60.ImportMaxMs IS NULL OR H.ImportMaxMs > H60.ImportMaxMs THEN H.ImportMaxMs ELSE H60.ImportMaxMs END,
			H60.ImportCpuCount += H.ImportCpuCount,
			H60.ImportCpuTotalMs = CASE WHEN H.ImportCpuCount = 0 THEN H60.ImportCpuTotalMs ELSE ISNULL(H60.ImportCpuTotalMs,0) + H.ImportCpuTotalMs END,
			H60.ImportCpuMinMs = CASE WHEN H60.ImportCpuMinMs IS NULL OR H.ImportCpuMinMs < H60.ImportCpuMinMs THEN H.ImportCpuMinMs ELSE H60.ImportCpuMinMs END,
			H60.ImportCpuMaxMs = CASE WHEN H60.ImportCpuMaxMs IS NULL OR H.ImportCpuMaxMs > H60.ImportCpuMaxMs THEN H.ImportCpuMaxMs ELSE H60.ImportCpuMaxMs END,
			H60.ImportReadsCount += H.ImportReadsCount,
			H60.ImportReadsTotal = CASE WHEN H.ImportReadsCount = 0 THEN H60.ImportReadsTotal ELSE ISNULL(H60.ImportReadsTotal,0) + H.ImportReadsTotal END,
			H60.ImportReadsMin = CASE WHEN H60.ImportReadsMin IS NULL OR H.ImportReadsMin < H60.ImportReadsMin THEN H.ImportReadsMin ELSE H60.ImportReadsMin END,
			H60.ImportReadsMax = CASE WHEN H60.ImportReadsMax IS NULL OR H.ImportReadsMax > H60.ImportReadsMax THEN H.ImportReadsMax ELSE H60.ImportReadsMax END,
			H60.ImportLagCount += H.ImportLagCount,
			H60.ImportLagTotalMs = CASE WHEN H.ImportLagCount = 0 THEN H60.ImportLagTotalMs ELSE ISNULL(H60.ImportLagTotalMs,0) + H.ImportLagTotalMs END,
			H60.ImportLagMinMs = CASE WHEN H60.ImportLagMinMs IS NULL OR H.ImportLagMinMs < H60.ImportLagMinMs THEN H.ImportLagMinMs ELSE H60.ImportLagMinMs END,
			H60.ImportLagMaxMs = CASE WHEN H60.ImportLagMaxMs IS NULL OR H.ImportLagMaxMs > H60.ImportLagMaxMs THEN H.ImportLagMaxMs ELSE H60.ImportLagMaxMs END
	FROM dbo.CollectionRuns_60MIN H60
	JOIN @Hourly H ON H60.InstanceID = @InstanceID
					AND H60.ReferenceID = H.ReferenceID
					AND H60.SnapshotDate = H.SnapshotDate

	INSERT INTO dbo.CollectionRuns_60MIN(
		InstanceID,ReferenceID,SnapshotDate,RunCount,SucceededCount,WarningCount,RetriedCount,PartialCount,FailedCount,ImportFailedCount,
		DurationCount,DurationTotalMs,DurationMinMs,DurationMaxMs,
		CpuCount,CpuTotalMs,CpuMinMs,CpuMaxMs,
		ReadsCount,ReadsTotal,ReadsMin,ReadsMax,
		ImportCount,ImportTotalMs,ImportMinMs,ImportMaxMs,
		ImportCpuCount,ImportCpuTotalMs,ImportCpuMinMs,ImportCpuMaxMs,
		ImportReadsCount,ImportReadsTotal,ImportReadsMin,ImportReadsMax,
		ImportLagCount,ImportLagTotalMs,ImportLagMinMs,ImportLagMaxMs
	)
	SELECT	@InstanceID,H.ReferenceID,H.SnapshotDate,H.RunCount,H.SucceededCount,H.WarningCount,H.RetriedCount,H.PartialCount,H.FailedCount,H.ImportFailedCount,
			H.DurationCount,H.DurationTotalMs,H.DurationMinMs,H.DurationMaxMs,
			H.CpuCount,H.CpuTotalMs,H.CpuMinMs,H.CpuMaxMs,
			H.ReadsCount,H.ReadsTotal,H.ReadsMin,H.ReadsMax,
			H.ImportCount,H.ImportTotalMs,H.ImportMinMs,H.ImportMaxMs,
			H.ImportCpuCount,H.ImportCpuTotalMs,H.ImportCpuMinMs,H.ImportCpuMaxMs,
			H.ImportReadsCount,H.ImportReadsTotal,H.ImportReadsMin,H.ImportReadsMax,
			H.ImportLagCount,H.ImportLagTotalMs,H.ImportLagMinMs,H.ImportLagMaxMs
	FROM @Hourly H
	WHERE NOT EXISTS(	SELECT 1
						FROM dbo.CollectionRuns_60MIN H60
						WHERE H60.InstanceID = @InstanceID
						AND H60.ReferenceID = H.ReferenceID
						AND H60.SnapshotDate = H.SnapshotDate
						)

	/* Status 5 (failed) and 6 (import failed) are failures.  Everything else got the data in. */
	UPDATE S
		SET S.LastRunDate = L.SnapshotDate,
			S.LastStatus = L.Status,
			S.ConsecutiveFailures = CASE WHEN L.Status IN(5,6) THEN S.ConsecutiveFailures + 1 ELSE 0 END,
			S.LastSuccessDate = CASE WHEN L.Status IN(5,6) THEN S.LastSuccessDate ELSE L.SnapshotDate END,
			S.LastFailureDate = CASE WHEN L.Status IN(5,6) THEN L.SnapshotDate ELSE S.LastFailureDate END,
			S.LastPartialDate = CASE WHEN L.Status = 4 THEN L.SnapshotDate ELSE S.LastPartialDate END,
			S.LastDurationMs = L.DurationMs
	FROM dbo.CollectionRunStatus S
	JOIN @Latest L ON S.ReferenceID = L.ReferenceID
	WHERE S.InstanceID = @InstanceID
	AND L.SnapshotDate > S.LastRunDate

	INSERT INTO dbo.CollectionRunStatus(InstanceID,ReferenceID,LastRunDate,LastStatus,ConsecutiveFailures,LastSuccessDate,LastFailureDate,LastPartialDate,LastDurationMs)
	SELECT	@InstanceID,
			L.ReferenceID,
			L.SnapshotDate,
			L.Status,
			CASE WHEN L.Status IN(5,6) THEN 1 ELSE 0 END,
			CASE WHEN L.Status IN(5,6) THEN NULL ELSE L.SnapshotDate END,
			CASE WHEN L.Status IN(5,6) THEN L.SnapshotDate ELSE NULL END,
			CASE WHEN L.Status = 4 THEN L.SnapshotDate ELSE NULL END,
			L.DurationMs
	FROM @Latest L
	WHERE NOT EXISTS(	SELECT 1
						FROM dbo.CollectionRunStatus S
						WHERE S.InstanceID = @InstanceID
						AND S.ReferenceID = L.ReferenceID
						)

COMMIT

/* Virtual performance counters for each collection - only the ones this batch has values for, so a collection that is
   never measured for CPU doesn't list an empty CPU counter */
DECLARE @Counters TABLE(
	counter_name NVARCHAR(128) NOT NULL,
	instance_name NVARCHAR(128) NOT NULL,
	PRIMARY KEY(counter_name,instance_name)
)
INSERT INTO @Counters(counter_name,instance_name)
SELECT DISTINCT M.counter_name, CR.Name
FROM @Runs R
JOIN dbo.CollectionRunReferences CR ON CR.ReferenceID = R.ReferenceID
CROSS APPLY(VALUES	(N'Collection Duration (ms)',R.DurationMs),
					(N'Collection CPU (ms)',R.CpuMs),
					(N'Collection Logical Reads',R.LogicalReads),
					(N'Collection Failed',R.Status),
					(N'Collection Import Duration (ms)',R.ImportMs),
					(N'Collection Import CPU (ms)',R.ImportCpuMs),
					(N'Collection Import Logical Reads',R.ImportLogicalReads),
					(N'Collection Import Lag (ms)',R.ImportLagMs)
			) M(counter_name,Value)
WHERE M.Value IS NOT NULL

IF EXISTS(	SELECT 1
			FROM @Counters T
			WHERE NOT EXISTS(	SELECT 1
								FROM dbo.Counters C
								WHERE C.object_name = N'DBADash'
								AND C.counter_name = T.counter_name
								AND C.instance_name = T.instance_name
								AND C.CounterType = 5
								)
			)
	OR EXISTS(	SELECT 1
				FROM @Counters T
				JOIN dbo.Counters C ON C.object_name = N'DBADash' AND C.counter_name = T.counter_name AND C.instance_name = T.instance_name
				WHERE NOT EXISTS(	SELECT 1
									FROM dbo.InstanceCounters IC
									WHERE IC.InstanceID = @InstanceID
									AND IC.CounterID = C.CounterID
									)
			)
BEGIN
	DECLARE @CounterLockResult INT
	BEGIN TRAN
		/* Same lock dbo.PerformanceCounters_Upd takes to register counters */
		EXEC @CounterLockResult = sp_getapplock @Resource = 'DBADash_CounterRegistration',
												@LockMode = 'Exclusive',
												@LockOwner = 'Transaction',
												@LockTimeout = 30000
		IF @CounterLockResult >= 0 /* On timeout skip registration this round - it self-heals next import */
		BEGIN
			/* Counters an older version logged as ordinary performance counters (CounterType 1) keep their CounterID - alert rules and saved metrics refer to it */
			UPDATE C
				SET C.CounterType = 5
			FROM dbo.Counters C
			JOIN @Counters T ON C.object_name = N'DBADash' AND C.counter_name = T.counter_name AND C.instance_name = T.instance_name
			WHERE C.CounterType <> 5

			INSERT INTO dbo.Counters(object_name,counter_name,instance_name,CounterType)
			SELECT N'DBADash', T.counter_name, T.instance_name, 5
			FROM @Counters T
			WHERE NOT EXISTS(	SELECT 1
								FROM dbo.Counters C
								WHERE C.object_name = N'DBADash'
								AND C.counter_name = T.counter_name
								AND C.instance_name = T.instance_name
								)

			INSERT INTO dbo.InstanceCounters(InstanceID,CounterID)
			SELECT @InstanceID, C.CounterID
			FROM @Counters T
			JOIN dbo.Counters C ON C.object_name = N'DBADash' AND C.counter_name = T.counter_name AND C.instance_name = T.instance_name
			EXCEPT
			SELECT InstanceID, CounterID
			FROM dbo.InstanceCounters
			WHERE InstanceID = @InstanceID
		END
	COMMIT
END

/* dbo.Counters_Get lists counters updated in the last 2 days.  Once an hour is plenty to keep them listed. */
UPDATE IC
	SET IC.UpdatedDate = SYSUTCDATETIME()
FROM dbo.InstanceCounters IC
JOIN dbo.Counters C ON C.CounterID = IC.CounterID
JOIN @Counters T ON C.object_name = N'DBADash' AND C.counter_name = T.counter_name AND C.instance_name = T.instance_name
WHERE IC.InstanceID = @InstanceID
AND IC.UpdatedDate < DATEADD(hh,-1,SYSUTCDATETIME())
