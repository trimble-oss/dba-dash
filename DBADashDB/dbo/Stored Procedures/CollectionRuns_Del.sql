CREATE PROC dbo.CollectionRuns_Del(
	@InstanceID INT,
	@DaysToKeep INT,
	@BatchSize INT=500000
)
AS
/*
	Partition switching removes old collection runs for all instances.  This removes them for one instance.
	With @DaysToKeep=0 the current status for the instance is cleared as well.
*/
SET NOCOUNT ON
DECLARE @MaxDate DATETIME2(2)
SELECT @MaxDate = CASE WHEN @DaysToKeep = 0 THEN '30000101' ELSE DATEADD(d,-@DaysToKeep,GETUTCDATE()) END

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize)
	FROM dbo.CollectionRuns
	WHERE InstanceID = @InstanceID
	AND SnapshotDate < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize)
	FROM dbo.CollectionRuns_60MIN
	WHERE InstanceID = @InstanceID
	AND SnapshotDate < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize)
	FROM dbo.CollectionRunsBatch
	WHERE InstanceID = @InstanceID
	AND SnapshotDate < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END

IF @DaysToKeep = 0
BEGIN
	DELETE dbo.CollectionRunStatus
	WHERE InstanceID = @InstanceID
END
