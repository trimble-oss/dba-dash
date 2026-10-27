/* The Service Broker endpoint for the instance, if there is one.  Used for messaging between instances. */
CREATE TABLE dbo.ServiceBrokerEndpoint(
    InstanceID INT NOT NULL,
    endpoint_name NVARCHAR(128) NOT NULL,
    state_desc NVARCHAR(60) NULL,
    port INT NULL,
    connection_count INT NULL,
    connected_count INT NULL,
    login_failed_count INT NULL,
    last_activity_time DATETIME2(3) NULL,
    SnapshotDate DATETIME2(2) NOT NULL,
    CONSTRAINT PK_ServiceBrokerEndpoint PRIMARY KEY(InstanceID),
    CONSTRAINT FK_ServiceBrokerEndpoint_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID)
);
