/*
	AI analysis of a deadlock, kept against the pattern rather than the occurrence.

	A deadlock that happens two hundred times is one problem with one answer.  The signature
	identifies the pattern (see DeadlockSignature in DBADash.Deadlock), and the viewer shows what was
	found last time whenever that pattern is opened again - so nobody pays for the same answer twice
	without meaning to.

	The model and payload version are part of the key so that the answers accumulate along the axes
	that change what an answer is worth: a different model, or a request that now carries object
	definitions, is a different answer worth keeping beside the old one rather than replacing it.
	Within one of those, the newest answer replaces the previous - asking again is what somebody does
	when they did not trust the first reply, not a request to keep both.
*/
CREATE TABLE AI.DeadlockAnalysis
(
	DeadlockAnalysisID BIGINT IDENTITY(1, 1) NOT NULL,
	Signature BINARY(8) NOT NULL,
	Model NVARCHAR(128) NOT NULL,
	PayloadVersion VARCHAR(30) NOT NULL,
	Analysis NVARCHAR(MAX) NOT NULL,
	/* Where the graph that produced this answer came from.  Informational: the answer belongs to the
	   pattern, and the same pattern can occur on several instances. */
	InstanceID INT NULL,
	GeneratedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_DeadlockAnalysis_GeneratedUtc DEFAULT SYSUTCDATETIME(),
	CONSTRAINT PK_DeadlockAnalysis PRIMARY KEY CLUSTERED (DeadlockAnalysisID)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_DeadlockAnalysis_Signature_Model_PayloadVersion
	ON AI.DeadlockAnalysis (Signature, Model, PayloadVersion);
