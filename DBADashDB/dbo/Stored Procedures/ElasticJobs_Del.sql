CREATE PROC dbo.ElasticJobs_Del(
	@InstanceID INT,
	@DaysToKeep INT,
	@BatchSize INT=500000
)
AS
/*
	Partition switching is used to efficiently remove data from the elastic job history tables (dbo.ElasticJobExecutions and
	dbo.ElasticJobStats_60MIN) for all instances.  This proc provides an alternative way to remove data associated with a specific
	instance - the job database of an elastic job agent.  Batched, as the executions are a row for each step against each target.
	With @DaysToKeep=0, the jobs, their steps, target group members and alert snapshot for the instance are also removed.
*/
SET NOCOUNT ON
DECLARE @MaxDate DATETIME2(2)
SELECT @MaxDate = CASE WHEN @DaysToKeep = 0 THEN '30000101' ELSE DATEADD(d,-@DaysToKeep,GETUTCDATE()) END

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize)
	FROM dbo.ElasticJobExecutions
	WHERE InstanceID = @InstanceID
	AND create_time < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize)
	FROM dbo.ElasticJobStats_60MIN
	WHERE InstanceID = @InstanceID
	AND RunDateTime < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END

IF @DaysToKeep = 0
BEGIN
	DELETE Alert.ElasticJobSnapshot
	WHERE InstanceID = @InstanceID

	/* Steps reference the jobs */
	DELETE dbo.ElasticJobSteps
	WHERE InstanceID = @InstanceID

	DELETE dbo.ElasticJobTargetGroupMembers
	WHERE InstanceID = @InstanceID

	DELETE dbo.ElasticJobs
	WHERE InstanceID = @InstanceID
END
