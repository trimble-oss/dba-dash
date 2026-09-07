/*
	Deadlock participants sent by the collector, linked to their header by (EventTime, DeadlockHash).

	database_id is the source instance's id, resolved to DBA Dash's DatabaseID by dbo.Deadlocks_Upd -
	the same treatment dbo.SlowQueries gets.
*/
CREATE TYPE dbo.DeadlockProcesses AS TABLE (
	EventTime DATETIME2(3) NOT NULL,
	DeadlockHash BINARY(16) NOT NULL,
	ProcessIndex SMALLINT NOT NULL,
	IsVictim BIT NOT NULL,
	database_id INT NULL,
	SPID INT NULL,
	Ecid INT NULL,
	LoginName sysname NULL,
	HostName sysname NULL,
	ClientApp sysname NULL,
	ProcedureName NVARCHAR(776) NULL,
	StatementText NVARCHAR(MAX) NULL,
	IsolationLevel VARCHAR(50) NULL,
	LockMode VARCHAR(20) NULL,
	WaitResource NVARCHAR(512) NULL,
	WaitTimeMs BIGINT NULL,
	LogUsed BIGINT NULL,
	TransactionName NVARCHAR(128) NULL,
	Priority SMALLINT NULL,
	LastBatchStarted DATETIME2(3) NULL,
	LastBatchCompleted DATETIME2(3) NULL,
	LastTransactionStarted DATETIME2(3) NULL,
	Status VARCHAR(30) NULL,
	TransactionCount INT NULL,
	HostPid INT NULL,
	InputBuffer NVARCHAR(MAX) NULL,
	ClientOption1 INT NULL,
	ClientOption2 INT NULL,
	PRIMARY KEY (EventTime, DeadlockHash, ProcessIndex)
);
