/*
	AI analysis of a deadlock.  Every answer is kept.

	A deadlock that happens two hundred times is one problem, so an answer is looked up by the pattern as
	well as the occurrence: the viewer shows an answer about this exact deadlock first (DeadlockHash), and
	otherwise the newest answer about its pattern (Signature, see DeadlockSignature in DBADash.Deadlock) - so
	nobody pays for the same answer twice without meaning to.  The rest are a drop-down away.

	A row is one answer, and it is the opening one.  What follows it belongs to the person who asked and
	is kept on their own machine instead, encrypted to their Windows account (see
	AiLocalConversationStore in the GUI).  Nothing anyone can read here is anybody's private
	conversation, and that is the point.

	ConversationID is what joins the two halves back together: the viewer reads this row, finds its own
	follow-ups under the same id locally, and shows one exchange.  It is load-bearing for that.

	A row is exactly one opening analysis.  Follow-ups were once stored here too, as further rows of the
	same conversation, but they belonged to the person who asked and are now kept only on that person's
	own machine.  Any that had reached this shared table were deleted, and the TurnNumber and Question
	columns that carried them removed, when this arrangement came in (see Script.PreDeployment1.sql) - so
	nothing anyone can read here is anybody's private conversation, and that is the point.

	History rather than replacement: every answer has been paid for, two runs of one model over one graph
	rarely say the same thing, and asking again is often a search for a better answer rather than a
	correction of a wrong one.  Analyses are only ever made by someone pressing a button, so the table
	stays small.

	The graph that was analysed is kept with the answer, along with the version of the signature it was
	stored under.  When the signature version changes, the service recomputes the answer's signature from
	its own graph (see AI.DeadlockAnalysisSignatureRecompute_Upd), so the answer follows the deadlock it
	was actually about - a version change that splits an over-broad pattern must not hand one pattern's
	answer to the others.  It is kept on every turn of a conversation and not just the first, so a version
	change moves the whole conversation together rather than stranding its follow-ups under the old
	signature.  Answers stored without a graph can't be recomputed and keep their signature.
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
	SignatureVersion TINYINT NULL,
	/* UTF-16 gzipped, as dbo.DeadlockXml: CAST(DECOMPRESS(DeadlockXmlCompressed) AS NVARCHAR(MAX)) */
	DeadlockXmlCompressed VARBINARY(MAX) NULL,
	/* The graph's occurrence identity, as dbo.Deadlocks.DeadlockHash - computed the same way (DBADash.Deadlock.Analysis.DeadlockHash),
	   so a graph opened from a file matches too.  NULL for answers stored before it was recorded. */
	DeadlockHash BINARY(16) NULL,
	/* The exchange this answer opens.  The viewer finds the reader's own follow-ups under this id on
	   their machine, so this is how the two halves of a conversation find each other.  NULL on answers
	   stored before conversations existed, which are conversations of one turn read back as themselves. */
	ConversationID UNIQUEIDENTIFIER NULL,
	CONSTRAINT PK_DeadlockAnalysis PRIMARY KEY CLUSTERED (DeadlockAnalysisID)
);
GO

CREATE NONCLUSTERED INDEX IX_DeadlockAnalysis_Signature
	ON AI.DeadlockAnalysis (Signature, GeneratedUtc);
GO

CREATE NONCLUSTERED INDEX IX_DeadlockAnalysis_DeadlockHash
	ON AI.DeadlockAnalysis (DeadlockHash);
GO

/* Drives the signature recompute (AI.DeadlockAnalysisSignatureRecompute_Get), which checks for work on every service
   start - as IX_Deadlocks_SignatureVersion does for dbo.Deadlocks. */
CREATE NONCLUSTERED INDEX IX_DeadlockAnalysis_SignatureVersion
	ON AI.DeadlockAnalysis (SignatureVersion);
GO

/* Finds a conversation once one has been matched by signature or hash. */
CREATE NONCLUSTERED INDEX IX_DeadlockAnalysis_Conversation
	ON AI.DeadlockAnalysis (ConversationID)
	WHERE ConversationID IS NOT NULL;
