/*	Availability group events read from the AlwaysOn_health extended events session on each instance:
	replica state changes, AG DDL (manual/forced failovers), lease expiry, automatic failover validation, HADR errors,
	sp_server_diagnostics health check errors, HADR manager state, replica start/stop and database sync state changes.
	Role changes are derived from the state change events into dbo.AvailabilityGroupRoleChanges (Source='XE').
	Partitioned monthly on EventTime - old events are removed by data retention (dbo.DataRetention, 365 days by default).
*/
CREATE TABLE dbo.AvailabilityGroupHealthEvents(
       InstanceID INT NOT NULL,
       EventTime DATETIME2(3) NOT NULL,
       EventName NVARCHAR(60) NOT NULL,
       EventHash BINARY(32) NOT NULL,
       group_id UNIQUEIDENTIFIER NULL,
       AvailabilityGroupName NVARCHAR(128) NULL,
       replica_id UNIQUEIDENTIFIER NULL,
       ReplicaName NVARCHAR(256) NULL,
       PreviousState NVARCHAR(60) NULL,
       CurrentState NVARCHAR(60) NULL,
       DDLAction NVARCHAR(60) NULL,
       DDLPhase NVARCHAR(60) NULL,
       ErrorNumber INT NULL,
       Details NVARCHAR(4000) NULL,
       Component NVARCHAR(60) NULL, /* sp_server_diagnostics component */
       database_id INT NULL,
       group_database_id UNIQUEIDENTIFIER NULL,
       DatabaseName NVARCHAR(128) NULL, /* Resolved on import - from group_database_id, or database_id when there is none */
       CONSTRAINT PK_AvailabilityGroupHealthEvents PRIMARY KEY CLUSTERED(InstanceID,EventTime,EventHash) WITH (DATA_COMPRESSION = PAGE) ON PS_AvailabilityGroupHealthEvents(EventTime),
       CONSTRAINT FK_AvailabilityGroupHealthEvents_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID)
)
GO
CREATE NONCLUSTERED INDEX IX_AvailabilityGroupHealthEvents_AvailabilityGroupName_EventTime ON dbo.AvailabilityGroupHealthEvents(AvailabilityGroupName,EventTime) INCLUDE(EventName,DDLPhase,group_id) WITH (DATA_COMPRESSION = PAGE) ON PS_AvailabilityGroupHealthEvents(EventTime)
