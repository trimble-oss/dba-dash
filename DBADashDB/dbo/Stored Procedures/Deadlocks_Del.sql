CREATE PROC dbo.Deadlocks_Del(
	@InstanceID INT,
	@DaysToKeep INT,
	@BatchSize INT=100000
)
AS
/*
	Partition switching is used to efficiently remove data from these tables for all instances.
	This proc provides an alternative way to remove data associated with a specific instance.

	All four deadlock tables are handled together: they are one collection, and dbo.Instance_Del would
	otherwise need to know about each of them.  Children are removed before headers so a partly
	completed run never leaves process or resource rows without the deadlock they belong to.
*/
SET NOCOUNT ON
DECLARE @MaxDate DATETIME2(3)
SELECT @MaxDate = CASE WHEN @DaysToKeep = 0 THEN '30000101' ELSE DATEADD(d,-@DaysToKeep,GETUTCDATE()) END

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize)
	FROM dbo.DeadlockProcesses
	WHERE InstanceID = @InstanceID
	AND EventTime < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize)
	FROM dbo.DeadlockResources
	WHERE InstanceID = @InstanceID
	AND EventTime < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize)
	FROM dbo.DeadlockXml
	WHERE InstanceID = @InstanceID
	AND EventTime < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize)
	FROM dbo.Deadlocks
	WHERE InstanceID = @InstanceID
	AND EventTime < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END
