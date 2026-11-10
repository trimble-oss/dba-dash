/*
	A collection the service has reported a run for - the collection type (or UserData.<name> for a custom collection) and,
	for the steps that follow some collections (fetching text and plans for QueryStats, say), the step.
	Name is how the collection appears as a DBADash performance counter instance - "QueryStats Plans" for a step, matching
	the Collection Duration (ms) counters older versions logged, so alert rules and saved metrics still find them.
*/
CREATE TABLE dbo.CollectionRunReferences(
	ReferenceID INT IDENTITY(1,1) NOT NULL,
	Reference VARCHAR(100) NOT NULL,
	Step VARCHAR(27) NOT NULL,
	/* NVARCHAR to match dbo.Counters.instance_name, so a join from a counter can seek on it */
	Name AS CAST(Reference + CASE WHEN Step = '' THEN '' ELSE ' ' + Step END AS NVARCHAR(128)) PERSISTED,
	CONSTRAINT PK_CollectionRunReferences PRIMARY KEY CLUSTERED(ReferenceID),
	CONSTRAINT UQ_CollectionRunReferences_Reference_Step UNIQUE(Reference,Step)
);
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_CollectionRunReferences_Name ON dbo.CollectionRunReferences(Name) INCLUDE(Reference,Step);
