/*
	Current conversation endpoint counts by state.  Replaced for an instance on each collection.
	State '??' is the total for a database with too many endpoints to count by state.
*/
CREATE TABLE dbo.ServiceBrokerConversations(
    InstanceID INT NOT NULL,
    DatabaseID INT NOT NULL,
    state CHAR(2) NOT NULL,
    conversation_count BIGINT NOT NULL,
    SnapshotDate DATETIME2(2) NOT NULL,
    CONSTRAINT PK_ServiceBrokerConversations PRIMARY KEY(InstanceID,DatabaseID,state),
    CONSTRAINT FK_ServiceBrokerConversations_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID),
    CONSTRAINT FK_ServiceBrokerConversations_Databases FOREIGN KEY(DatabaseID) REFERENCES dbo.Databases(DatabaseID)
);
