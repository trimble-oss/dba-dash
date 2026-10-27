/* Queue depth for each queue on each collection */
CREATE TABLE dbo.ServiceBrokerQueueHistory(
    InstanceID INT NOT NULL,
    SnapshotDate DATETIME2(2) NOT NULL,
    QueueID INT NOT NULL,
    message_count BIGINT NULL,
    CONSTRAINT PK_ServiceBrokerQueueHistory PRIMARY KEY CLUSTERED(InstanceID,SnapshotDate,QueueID) WITH (DATA_COMPRESSION = PAGE) ON PS_ServiceBrokerQueueHistory(SnapshotDate),
    CONSTRAINT FK_ServiceBrokerQueueHistory_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID),
    CONSTRAINT FK_ServiceBrokerQueueHistory_ServiceBrokerQueues FOREIGN KEY(QueueID) REFERENCES dbo.ServiceBrokerQueues(QueueID)
) ON PS_ServiceBrokerQueueHistory(SnapshotDate);
