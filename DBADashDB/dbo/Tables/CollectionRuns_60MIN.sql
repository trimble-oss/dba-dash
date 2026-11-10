/*
	Collection runs rolled up by hour - always kept, whether or not the runs themselves are (see dbo.CollectionRuns).
	RunCount includes runs with no status (older services) - the status counts don't, so they can add up to less.
	The duration, CPU, reads and import columns are over the runs that reported them - the Count columns say how many.
	Import lag (see dbo.CollectionRuns) is per file, so it's recorded against each run in the file.  The Instance collection
	runs once in every file, so its lag figures are the files'.
*/
CREATE TABLE dbo.CollectionRuns_60MIN(
	InstanceID INT NOT NULL,
	ReferenceID INT NOT NULL,
	SnapshotDate DATETIME2(2) NOT NULL,
	RunCount INT NOT NULL,
	SucceededCount INT NOT NULL,
	WarningCount INT NOT NULL,
	RetriedCount INT NOT NULL,
	PartialCount INT NOT NULL,
	FailedCount INT NOT NULL,
	ImportFailedCount INT NOT NULL,
	DurationCount INT NOT NULL,
	DurationTotalMs BIGINT NULL,
	DurationMinMs INT NULL,
	DurationMaxMs INT NULL,
	CpuCount INT NOT NULL,
	CpuTotalMs BIGINT NULL,
	CpuMinMs INT NULL,
	CpuMaxMs INT NULL,
	ReadsCount INT NOT NULL,
	ReadsTotal BIGINT NULL,
	ReadsMin BIGINT NULL,
	ReadsMax BIGINT NULL,
	ImportCount INT NOT NULL,
	ImportTotalMs BIGINT NULL,
	ImportMinMs INT NULL,
	ImportMaxMs INT NULL,
	ImportCpuCount INT NOT NULL,
	ImportCpuTotalMs BIGINT NULL,
	ImportCpuMinMs INT NULL,
	ImportCpuMaxMs INT NULL,
	ImportReadsCount INT NOT NULL,
	ImportReadsTotal BIGINT NULL,
	ImportReadsMin BIGINT NULL,
	ImportReadsMax BIGINT NULL,
	ImportLagCount INT NOT NULL,
	ImportLagTotalMs BIGINT NULL,
	ImportLagMinMs INT NULL,
	ImportLagMaxMs INT NULL,
	CONSTRAINT PK_CollectionRuns_60MIN PRIMARY KEY CLUSTERED(InstanceID,ReferenceID,SnapshotDate) WITH (DATA_COMPRESSION = PAGE) ON PS_CollectionRuns_60MIN(SnapshotDate),
	CONSTRAINT FK_CollectionRuns_60MIN_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID),
	CONSTRAINT FK_CollectionRuns_60MIN_CollectionRunReferences FOREIGN KEY(ReferenceID) REFERENCES dbo.CollectionRunReferences(ReferenceID)
) ON PS_CollectionRuns_60MIN(SnapshotDate);
