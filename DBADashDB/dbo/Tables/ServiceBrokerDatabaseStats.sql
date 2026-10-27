/*
	Database level Service Broker activity for each collection.
	Activation columns are the change in the cumulative Broker Activation counters since the previous collection.  NULL where there was no previous collection to compare with.
	The count columns are totals as at the snapshot.  NULL when that database could not be read for the snapshot.
*/
CREATE TABLE dbo.ServiceBrokerDatabaseStats(
    InstanceID INT NOT NULL,
    SnapshotDate DATETIME2(2) NOT NULL,
    DatabaseID INT NOT NULL,
    sample_ms_diff INT NULL,
    procedures_invoked BIGINT NULL,
    task_limit_reached BIGINT NULL,
    tasks_aborted BIGINT NULL,
    transmission_queue_count BIGINT NULL,
    conversation_count BIGINT NULL,
    CONSTRAINT PK_ServiceBrokerDatabaseStats PRIMARY KEY CLUSTERED(InstanceID,SnapshotDate,DatabaseID) WITH (DATA_COMPRESSION = PAGE) ON PS_ServiceBrokerDatabaseStats(SnapshotDate),
    CONSTRAINT FK_ServiceBrokerDatabaseStats_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID),
    CONSTRAINT FK_ServiceBrokerDatabaseStats_Databases FOREIGN KEY(DatabaseID) REFERENCES dbo.Databases(DatabaseID)
) ON PS_ServiceBrokerDatabaseStats(SnapshotDate);
