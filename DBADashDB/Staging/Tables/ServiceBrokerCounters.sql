/* Previous snapshot of the cumulative Service Broker counters, used to calculate the change since the last collection */
CREATE TABLE Staging.ServiceBrokerCounters(
    InstanceID INT NOT NULL,
    instance_name NVARCHAR(128) NOT NULL,
    counter_name NVARCHAR(128) NOT NULL,
    cntr_value BIGINT NOT NULL,
    SnapshotDate DATETIME2(2) NOT NULL,
    CONSTRAINT PK_Staging_ServiceBrokerCounters PRIMARY KEY(InstanceID,instance_name,counter_name)
);
