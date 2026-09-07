/*
	Contended resources sent by the collector, linked to their header by (EventTime, DeadlockHash).
*/
CREATE TYPE dbo.DeadlockResources AS TABLE (
	EventTime DATETIME2(3) NOT NULL,
	DeadlockHash BINARY(16) NOT NULL,
	ResourceIndex SMALLINT NOT NULL,
	ResourceType VARCHAR(50) NOT NULL,
	database_id INT NULL,
	ObjectName NVARCHAR(776) NULL,
	IndexName sysname NULL,
	LockMode VARCHAR(20) NULL,
	OwnerModes VARCHAR(200) NULL,
	WaiterModes VARCHAR(200) NULL,
	OwnerCount SMALLINT NULL,
	WaiterCount SMALLINT NULL,
	IsParallelismResource BIT NULL,
	PRIMARY KEY (EventTime, DeadlockHash, ResourceIndex)
);
