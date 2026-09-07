/*
	One row per resource contended in a deadlock - the resource-list of the graph.  Answers "which
	table or index is behind our deadlocks" without opening a single graph.

	Owner and waiter modes are stored as the distinct sorted set the graph carried (e.g. "U,X") rather
	than a row per participant: the participants themselves are already in dbo.DeadlockProcesses, and
	the set of modes is what the reports and the signature actually key on.

	Hobt ids and lock ids are deliberately not stored.  They identify this occurrence rather than the
	problem and change when the table is rebuilt, so they group badly and mislead in a report; the
	graph in dbo.DeadlockXml still has them for anyone who needs them.
*/
CREATE TABLE dbo.DeadlockResources(
	InstanceID INT NOT NULL,
	EventTime DATETIME2(3) NOT NULL,
	DeadlockHash BINARY(16) NOT NULL,
	ResourceIndex SMALLINT NOT NULL,
	/* Raw element name from the graph, e.g. "keylock", "pagelock", "objectlock", "exchangeEvent". */
	ResourceType VARCHAR(50) NOT NULL,
	DatabaseID INT NULL,
	/* Three part object name where the graph supplies one, e.g. "Sales.dbo.Orders". */
	ObjectName NVARCHAR(776) NULL,
	IndexName sysname NULL,
	/* The mode the resource is held in, e.g. "X", "U", "RangeS-U". */
	LockMode VARCHAR(20) NULL,
	OwnerModes VARCHAR(200) NULL,
	WaiterModes VARCHAR(200) NULL,
	OwnerCount SMALLINT NULL,
	WaiterCount SMALLINT NULL,
	/* True for resource types that only arise within a single parallel query (exchangeEvent, threadpool, syncPoint). */
	IsParallelismResource BIT NULL,
	CONSTRAINT PK_DeadlockResources PRIMARY KEY CLUSTERED (InstanceID ASC, EventTime ASC, DeadlockHash ASC, ResourceIndex ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_DeadlockResources(EventTime),
	CONSTRAINT FK_DeadlockResources_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
);
