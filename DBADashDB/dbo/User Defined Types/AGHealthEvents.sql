CREATE TYPE dbo.AGHealthEvents AS TABLE(
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
       Component NVARCHAR(60) NULL,
       database_id INT NULL,
       group_database_id UNIQUEIDENTIFIER NULL,
       PRIMARY KEY(EventTime,EventHash)
)
