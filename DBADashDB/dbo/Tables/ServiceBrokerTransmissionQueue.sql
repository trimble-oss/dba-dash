/* Current state of sys.transmission_queue, grouped by target service and status.  Replaced for an instance on each collection. */
CREATE TABLE dbo.ServiceBrokerTransmissionQueue(
    InstanceID INT NOT NULL,
    DatabaseID INT NOT NULL,
    to_service_name NVARCHAR(256) NULL,
    transmission_status NVARCHAR(4000) NULL,
    message_count BIGINT NOT NULL,
    oldest_enqueue_time DATETIME2(3) NULL,
    SnapshotDate DATETIME2(2) NOT NULL,
    CONSTRAINT FK_ServiceBrokerTransmissionQueue_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID),
    CONSTRAINT FK_ServiceBrokerTransmissionQueue_Databases FOREIGN KEY(DatabaseID) REFERENCES dbo.Databases(DatabaseID)
);
GO
/* No natural key as the status is free text */
CREATE CLUSTERED INDEX IX_ServiceBrokerTransmissionQueue_InstanceID_DatabaseID ON dbo.ServiceBrokerTransmissionQueue(InstanceID,DatabaseID);
