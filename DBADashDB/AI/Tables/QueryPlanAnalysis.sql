/*
	AI analysis of a query plan.  Every answer is kept.  The query plan counterpart of
	AI.DeadlockAnalysis, and deliberately the same shape.

	Two identities, because a query and a plan for it are different things to ask about:
	  Signature is the query - SQL Server's QueryHash where the plan carried one - and groups the
	    statement across every plan it has ever had.  It is what an answer is found by.
	  PlanHash is this shape of plan for it - QueryPlanHash - and is what the viewer prefers when it
	    has an answer about the plan actually on screen.  A query that got a different plan is a
	    different problem, and often precisely the problem.
	See PlanIdentity in DBADash.QueryPlan, which also computes both for a plan that carried neither.

	A row is one answer, and a conversation is a run of them: ConversationID groups them and TurnNumber
	orders them, with the reader's question on every turn after the first - on the first the question is
	the plan, which the viewer has in front of it.

	The plan itself is not stored.  Unlike a deadlock graph, whose signature is computed by us and so has
	to be recomputable, a plan's identities come from SQL Server or from a stable hash of the plan's own
	text - so there is nothing to recompute, and a plan is far too large to keep a copy of per answer.
	The statement text is kept, because a drop-down of hashes describes nothing.

	Analyses are only ever made by someone pressing a button, so the table stays small.
*/
CREATE TABLE AI.QueryPlanAnalysis
(
	QueryPlanAnalysisID BIGINT IDENTITY(1, 1) NOT NULL,
	/* The query's identity - QueryHash, or a hash of the statement text where the plan had none. */
	Signature BINARY(8) NOT NULL,
	/* This plan's identity - QueryPlanHash, or a hash of the plan XML.  NULL where neither was available. */
	PlanHash BINARY(8) NULL,
	Model NVARCHAR(128) NOT NULL,
	PayloadVersion VARCHAR(30) NOT NULL,
	Analysis NVARCHAR(MAX) NOT NULL,
	/* Where the plan that produced this answer came from.  Informational: the answer belongs to the
	   query, and the same query runs on more than one instance. */
	InstanceID INT NULL,
	/* What the query was, so a stored answer can be described rather than listed as a hash. */
	StatementText NVARCHAR(MAX) NULL,
	GeneratedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_QueryPlanAnalysis_GeneratedUtc DEFAULT SYSUTCDATETIME(),
	/* The exchange this answer belongs to, and where in it. */
	ConversationID UNIQUEIDENTIFIER NULL,
	TurnNumber SMALLINT NULL,
	/* The follow-up that was asked.  NULL on the opening turn of a conversation. */
	Question NVARCHAR(MAX) NULL,
	CONSTRAINT PK_QueryPlanAnalysis PRIMARY KEY CLUSTERED (QueryPlanAnalysisID)
);
GO

CREATE NONCLUSTERED INDEX IX_QueryPlanAnalysis_Signature
	ON AI.QueryPlanAnalysis (Signature, GeneratedUtc);
GO

CREATE NONCLUSTERED INDEX IX_QueryPlanAnalysis_PlanHash
	ON AI.QueryPlanAnalysis (PlanHash)
	WHERE PlanHash IS NOT NULL;
GO

/* Reads a conversation back in order once one of its turns has been found by query or plan. */
CREATE NONCLUSTERED INDEX IX_QueryPlanAnalysis_Conversation
	ON AI.QueryPlanAnalysis (ConversationID, TurnNumber)
	WHERE ConversationID IS NOT NULL;
