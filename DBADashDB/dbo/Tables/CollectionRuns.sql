/*
	One row per collection run.  Only kept when the service has LogInternalPerformanceCounters on - otherwise runs are only
	added to the hourly rollup, dbo.CollectionRuns_60MIN.
	SnapshotDate is when the run started (UTC).
	Status:	1 Succeeded, 2 Succeeded with a warning, 3 Succeeded after a retry, 4 Partial (succeeded but logged errors),
			5 Failed, 6 Collected but the import failed.  NULL for runs reported by older services that only logged a duration.
	DurationMs is the last attempt, as the Collection Duration (ms) counter always was.  CpuMs and LogicalReads are the
	server-side work of the run's own sessions, all attempts.  NULL where the collection wasn't measured (it doesn't run
	through a SQL connection the collector measures - WMI, SMO scripting).
	ImportMs is the time spent importing the run's tables into the repository, all attempts, and ImportCpuMs and
	ImportLogicalReads the work that caused in the repository (measured with LogInternalPerformanceCounters on, as for the
	collection).  NULL for a run with nothing of its own to import.
	ImportLagMs is the time from the file the run came in being handed to a destination - once everything in it was
	collected - to its import starting.  The same for every run in the file.  Near zero for a SQL destination; a backlog at
	the destination (S3, SQS, folder) shows here.  The importer's clock against the collector's, so negative lag (clocks
	that disagree) is counted as zero.
*/
CREATE TABLE dbo.CollectionRuns(
	InstanceID INT NOT NULL,
	ReferenceID INT NOT NULL,
	SnapshotDate DATETIME2(2) NOT NULL,
	Status TINYINT NULL,
	Attempts TINYINT NULL,
	DurationMs INT NULL,
	CpuMs INT NULL,
	LogicalReads BIGINT NULL,
	ImportMs INT NULL,
	ImportCpuMs INT NULL,
	ImportLogicalReads BIGINT NULL,
	ImportLagMs INT NULL,
	CONSTRAINT PK_CollectionRuns PRIMARY KEY CLUSTERED(InstanceID,ReferenceID,SnapshotDate) WITH (DATA_COMPRESSION = PAGE) ON PS_CollectionRuns(SnapshotDate),
	CONSTRAINT FK_CollectionRuns_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID),
	CONSTRAINT FK_CollectionRuns_CollectionRunReferences FOREIGN KEY(ReferenceID) REFERENCES dbo.CollectionRunReferences(ReferenceID)
) ON PS_CollectionRuns(SnapshotDate);
