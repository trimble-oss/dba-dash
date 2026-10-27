/*
	Service Broker thresholds.  Configured at root (InstanceID=-1, DatabaseID=-1, schema_name='', queue_name=''), instance, database or queue level.
	The most specific row applies.  Queue names are only unique within a schema, so a queue level row has both.
	Queue depth and age thresholds apply at any level.  Transmission queue and conversation thresholds are for a database so queue level rows don't set them.
*/
CREATE TABLE dbo.ServiceBrokerThresholds(
    InstanceID INT NOT NULL,
    DatabaseID INT NOT NULL,
    schema_name NVARCHAR(128) NOT NULL CONSTRAINT DF_ServiceBrokerThresholds_schema_name DEFAULT(''),
    queue_name NVARCHAR(128) NOT NULL,
    QueueDepthWarningThreshold BIGINT NULL,
    QueueDepthCriticalThreshold BIGINT NULL,
    QueueAgeWarningThreshold INT NULL, /* Mins - age of the oldest message waiting in the queue */
    QueueAgeCriticalThreshold INT NULL, /* Mins */
    TransmissionQueueAgeWarningThreshold INT NULL, /* Mins */
    TransmissionQueueAgeCriticalThreshold INT NULL, /* Mins */
    ConversationCountWarningThreshold BIGINT NULL,
    ConversationCountCriticalThreshold BIGINT NULL,
    CONSTRAINT PK_ServiceBrokerThresholds PRIMARY KEY(InstanceID,DatabaseID,schema_name,queue_name)
);
