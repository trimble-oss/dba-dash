/*
	Instance level Service Broker activity for each collection.
	Activity columns are the change in the cumulative Broker Statistics counters since the previous collection.  NULL where there was no previous collection to compare with.
*/
CREATE TABLE dbo.ServiceBrokerStats(
    InstanceID INT NOT NULL,
    SnapshotDate DATETIME2(2) NOT NULL,
    sample_ms_diff INT NULL,
    enqueued_messages BIGINT NULL,
    sends BIGINT NULL,
    receives BIGINT NULL,
    dequeued_transmission_queue_messages BIGINT NULL,
    activation_errors BIGINT NULL,
    corrupted_messages BIGINT NULL,
    dropped_messages BIGINT NULL,
    transaction_rollbacks BIGINT NULL,
    CONSTRAINT PK_ServiceBrokerStats PRIMARY KEY CLUSTERED(InstanceID,SnapshotDate) WITH (DATA_COMPRESSION = PAGE) ON PS_ServiceBrokerStats(SnapshotDate),
    CONSTRAINT FK_ServiceBrokerStats_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID)
) ON PS_ServiceBrokerStats(SnapshotDate);
