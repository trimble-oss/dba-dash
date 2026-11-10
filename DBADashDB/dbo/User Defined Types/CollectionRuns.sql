/* Collection runs as the service reports them - see dbo.CollectionRuns.  Matched by column position from the DataTable.
   The Import columns are added by the importer (DBImporter), which may be a different service from the collector. */
CREATE TYPE dbo.CollectionRuns AS TABLE(
	Reference VARCHAR(100) NOT NULL,
	Step VARCHAR(27) NOT NULL,
	SnapshotDate DATETIME2(7) NOT NULL,
	Status TINYINT NULL,
	Attempts TINYINT NULL,
	DurationMs INT NULL,
	CpuMs INT NULL,
	LogicalReads BIGINT NULL,
	ImportMs INT NULL,
	ImportCpuMs INT NULL,
	ImportLogicalReads BIGINT NULL
);
