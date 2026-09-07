/*
	The deadlock graph, gzip compressed.

	Split out from dbo.Deadlocks so it can carry its own (shorter) retention: a graph runs to several
	KB and a parallel deadlock over a large table can run to hundreds, while the shredded rows are
	small.  Keeping trend and grouping for a year costs little; keeping every graph for a year does
	not.

	Compressed for the same reason the query plan cache is (see dbo.QueryPlans.query_plan_compresed):
	graph XML is tag dense and compresses hard, and the collection payload travels to the destination
	as uncompressed XML, so a raw string column would additionally be XML escaped on the wire.

	The bytes are UTF-16 (Encoding.Unicode) gzipped by the collector, matching SMOBaseClass.Zip, so
	SQL Server's own DECOMPRESS reads them directly:

		SELECT CAST(DECOMPRESS(DeadlockXmlCompressed) AS NVARCHAR(MAX)) FROM dbo.DeadlockXml

	No PAGE compression: the payload is already gzipped and the rest of the row is three small
	columns, so there is nothing left for it to find.
*/
CREATE TABLE dbo.DeadlockXml(
	InstanceID INT NOT NULL,
	EventTime DATETIME2(3) NOT NULL,
	DeadlockHash BINARY(16) NOT NULL,
	DeadlockXmlCompressed VARBINARY(MAX) NULL,
	CONSTRAINT PK_DeadlockXml PRIMARY KEY CLUSTERED (InstanceID ASC, EventTime ASC, DeadlockHash ASC) ON PS_DeadlockXml(EventTime),
	CONSTRAINT FK_DeadlockXml_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID)
);
