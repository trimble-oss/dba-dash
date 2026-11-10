/*
	The latest run of each collection for each instance and how many have failed in a row.  Kept separately from
	dbo.CollectionDates, which only has a row once a collection has succeeded - a collection that has never worked
	belongs here most of all.
*/
CREATE TABLE dbo.CollectionRunStatus(
	InstanceID INT NOT NULL,
	ReferenceID INT NOT NULL,
	LastRunDate DATETIME2(2) NOT NULL,
	LastStatus TINYINT NOT NULL,
	ConsecutiveFailures INT NOT NULL,
	LastSuccessDate DATETIME2(2) NULL,
	LastFailureDate DATETIME2(2) NULL,
	LastPartialDate DATETIME2(2) NULL, /* Partly failed, some data collected.  With LastFailureDate, lets the summary skip the failure rate for collections with no problems lately */
	LastDurationMs INT NULL,
	CONSTRAINT PK_CollectionRunStatus PRIMARY KEY CLUSTERED(InstanceID,ReferenceID),
	CONSTRAINT FK_CollectionRunStatus_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID),
	CONSTRAINT FK_CollectionRunStatus_CollectionRunReferences FOREIGN KEY(ReferenceID) REFERENCES dbo.CollectionRunReferences(ReferenceID)
);
