CREATE PROC dbo.ServiceBroker_Del(
	@InstanceID INT,
	@DaysToKeep INT,
	@BatchSize INT=500000
)
AS
/*
	Partitioning switching is used to efficiently remove data from the Service Broker history tables for all instances.
	This proc provides an alternative way to remove data associated with a specific instance.
	With @DaysToKeep=0, the current state tables for the instance are also cleared.
*/
SET NOCOUNT ON
DECLARE @MaxDate DATETIME2(2)
SELECT @MaxDate = CASE WHEN @DaysToKeep = 0 THEN '30000101' ELSE DATEADD(d,-@DaysToKeep,GETUTCDATE()) END

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize)
	FROM dbo.ServiceBrokerQueueHistory
	WHERE InstanceID = @InstanceID
	AND SnapshotDate < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize)
	FROM dbo.ServiceBrokerStats
	WHERE InstanceID = @InstanceID
	AND SnapshotDate < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize)
	FROM dbo.ServiceBrokerDatabaseStats
	WHERE InstanceID = @InstanceID
	AND SnapshotDate < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END

IF @DaysToKeep = 0
BEGIN
	DELETE dbo.ServiceBrokerQueues WHERE InstanceID = @InstanceID
	DELETE dbo.ServiceBrokerTransmissionQueue WHERE InstanceID = @InstanceID
	DELETE dbo.ServiceBrokerConversations WHERE InstanceID = @InstanceID
	DELETE dbo.ServiceBrokerEndpoint WHERE InstanceID = @InstanceID
	DELETE dbo.ServiceBrokerThresholds WHERE InstanceID = @InstanceID
	DELETE Staging.ServiceBrokerCounters WHERE InstanceID = @InstanceID
END
