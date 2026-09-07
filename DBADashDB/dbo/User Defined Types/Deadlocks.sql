/*
	Deadlock headers sent by the collector, one row per graph.

	The graph XML travels here rather than in a type of its own even though it lands in a separate
	table - it is 1:1 with the header, and carrying it alongside guarantees that stays true.

	DeadlockHash is the identity of the deadlock and is what links the process and resource rows in
	this batch to their header - the same key they are stored under, so nothing has to be mapped on
	import.

	Signature arrives as the familiar "0x..." hex string and is stored as BINARY(8), matching the
	convention AI.DeadlockAnalysis_Upd already uses.
*/
CREATE TYPE dbo.Deadlocks AS TABLE (
	EventTime DATETIME2(3) NOT NULL,
	DeadlockHash BINARY(16) NOT NULL,
	Signature VARCHAR(18) NULL,
	SignatureVersion TINYINT NULL,
	ProcessCount SMALLINT NULL,
	VictimCount SMALLINT NULL,
	ResourceCount SMALLINT NULL,
	IsParallel BIT NULL,
	DeadlockXmlCompressed VARBINARY(MAX) NULL,
	PRIMARY KEY (EventTime, DeadlockHash)
);
