/*
	Current state of each Service Broker user queue.  Updated by ServiceBroker_Upd on each collection.
	Queues no longer found are marked IsActive=0 rather than deleted as ServiceBrokerQueueHistory refers to them.
*/
CREATE TABLE dbo.ServiceBrokerQueues(
    QueueID INT IDENTITY(1,1) NOT NULL,
    InstanceID INT NOT NULL,
    DatabaseID INT NOT NULL,
    schema_name NVARCHAR(128) NOT NULL,
    queue_name NVARCHAR(128) NOT NULL,
    object_id INT NOT NULL,
    is_receive_enabled BIT NOT NULL,
    is_enqueue_enabled BIT NOT NULL,
    is_retention_enabled BIT NOT NULL,
    is_activation_enabled BIT NOT NULL,
    is_poison_message_handling_enabled BIT NULL,
    activation_procedure NVARCHAR(776) NULL,
    activation_procedure_exists BIT NULL,
    max_readers SMALLINT NULL,
    message_count BIGINT NULL,
    activated_tasks INT NOT NULL,
    monitor_state NVARCHAR(32) NULL,
    monitor_tasks_waiting INT NULL,
    monitor_last_activated_time DATETIME2(3) NULL,
    monitor_last_empty_rowset_time DATETIME2(3) NULL,
    oldest_message_time DATETIME2(3) NULL,
    /* Snapshot where the queue was first seen enabled/disabled since it changed.  NULL if it hasn't changed since it was first collected. */
    status_changed_date DATETIME2(2) NULL,
    SnapshotDate DATETIME2(2) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_ServiceBrokerQueues_IsActive DEFAULT(1),
    CONSTRAINT PK_ServiceBrokerQueues PRIMARY KEY(QueueID),
    CONSTRAINT FK_ServiceBrokerQueues_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID),
    CONSTRAINT FK_ServiceBrokerQueues_Databases FOREIGN KEY(DatabaseID) REFERENCES dbo.Databases(DatabaseID)
);
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_ServiceBrokerQueues_DatabaseID_schema_name_queue_name ON dbo.ServiceBrokerQueues(DatabaseID,schema_name,queue_name);
GO
/*	One instance's rows, then inactive ones, then the oldest.  ServiceBroker_Upd seeks this to deactivate
	and purge queues.  A scan of the clustered key would take update locks on every instance's rows and
	deadlock imports running in parallel. */
CREATE NONCLUSTERED INDEX IX_ServiceBrokerQueues_InstanceID_IsActive_SnapshotDate ON dbo.ServiceBrokerQueues(InstanceID, IsActive, SnapshotDate);
