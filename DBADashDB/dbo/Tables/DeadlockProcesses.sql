/*
	One row per process taking part in a deadlock - the process-list of the graph.

	Note that a process is not a session: a parallel query contributes several process entries sharing
	a SPID and differing by Ecid, which is why ProcessIndex (the ordinal within the graph) rather than
	SPID completes the key.

	These are the columns the "group by application / database / login / host / procedure" reports
	group on.  Shredding them here is the point of the collection: it makes those reports an indexed
	query rather than XML shredding over thousands of graphs at view time, and it is what removes the
	need to return the whole graph on every row the way sp_BlitzLock does.
*/
CREATE TABLE dbo.DeadlockProcesses(
	InstanceID INT NOT NULL,
	EventTime DATETIME2(3) NOT NULL,
	DeadlockHash BINARY(16) NOT NULL,
	ProcessIndex SMALLINT NOT NULL,
	IsVictim BIT NOT NULL,
	DatabaseID INT NULL,
	SPID INT NULL,
	Ecid INT NULL,
	LoginName sysname NULL,
	HostName sysname NULL,
	ClientApp sysname NULL,
	/* Three part module name from the execution stack, e.g. "Sales.dbo.usp_UpdateOrder".  NULL for ad-hoc SQL. */
	ProcedureName NVARCHAR(776) NULL,
	StatementText NVARCHAR(MAX) NULL,
	IsolationLevel VARCHAR(50) NULL,
	LockMode VARCHAR(20) NULL,
	/* Raw wait_resource string, e.g. "KEY: 5:72057594045595648 (61a06abd401c)".  Left undecoded - decoding needs the source instance. */
	WaitResource NVARCHAR(512) NULL,
	WaitTimeMs BIGINT NULL,
	LogUsed BIGINT NULL,
	TransactionName NVARCHAR(128) NULL,
	Priority SMALLINT NULL,
	LastBatchStarted DATETIME2(3) NULL,
	LastBatchCompleted DATETIME2(3) NULL,
	LastTransactionStarted DATETIME2(3) NULL,
	/* e.g. "suspended", "background".  A process that is not suspended was not waiting on a lock. */
	Status VARCHAR(30) NULL,
	TransactionCount INT NULL,
	HostPid INT NULL,
	/*	The batch as submitted, which is often more use than the statement: it carries the EXEC and its
		parameter values.  Kept alongside StatementText rather than instead of it - StatementText is the
		frame that actually deadlocked, which the buffer alone does not identify. */
	InputBuffer NVARCHAR(MAX) NULL,
	/*	SET options bitmasks from the graph, stored raw and decoded at report time by
		dbo.DecodeClientOptions.  Storing the mask rather than the decoded text keeps the bits SQL Server
		does not name, and lets the naming be corrected without recollecting anything. */
	ClientOption1 INT NULL,
	ClientOption2 INT NULL,
	CONSTRAINT PK_DeadlockProcesses PRIMARY KEY CLUSTERED (InstanceID ASC, EventTime ASC, DeadlockHash ASC, ProcessIndex ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_DeadlockProcesses(EventTime),
	CONSTRAINT FK_DeadlockProcesses_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
);
