CREATE TABLE dbo.RunningQueriesSummary(
	InstanceID INT NOT NULL,
	SnapshotDateUTC DATETIME2(7) NOT NULL,
	RunningQueries SMALLINT NOT NULL,
	BlockedQueries SMALLINT NOT NULL,
	BlockedQueriesWaitMs BIGINT NOT NULL,
	MaxMemoryGrant INT NOT NULL,
	LongestRunningQueryMs BIGINT NOT NULL,
	CriticalWaitCount SMALLINT NOT NULL,
	CriticalWaitTime BIGINT NOT NULL,
	CONSTRAINT PK_RunningQueriesSummary PRIMARY KEY(InstanceID,SnapshotDateUTC)
)
