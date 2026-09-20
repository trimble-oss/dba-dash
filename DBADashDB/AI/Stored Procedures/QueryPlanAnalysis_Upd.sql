/*
	Stores an analysis of a query plan, or a follow-up answer in the conversation one started.  Always
	adds: every answer is kept (see AI.QueryPlanAnalysis).
*/
CREATE PROC AI.QueryPlanAnalysis_Upd
(
	@Signature VARCHAR(18),
	@PlanHash VARCHAR(18) = NULL,
	@Model NVARCHAR(128),
	@PayloadVersion VARCHAR(30),
	@Analysis NVARCHAR(MAX),
	@InstanceID INT = NULL,
	@StatementText NVARCHAR(MAX) = NULL,
	@ConversationID UNIQUEIDENTIFIER = NULL,
	@TurnNumber SMALLINT = NULL,
	@Question NVARCHAR(MAX) = NULL
)
AS
SET NOCOUNT ON

INSERT INTO AI.QueryPlanAnalysis
(
	Signature,
	PlanHash,
	Model,
	PayloadVersion,
	Analysis,
	InstanceID,
	StatementText,
	ConversationID,
	TurnNumber,
	Question
)
/* Callers pass the identities as the familiar "0x..." hex strings; store them in their 8-byte binary
   form (style 1 parses the leading 0x). */
SELECT	CONVERT(BINARY(8), @Signature, 1),
		CONVERT(BINARY(8), NULLIF(@PlanHash, ''), 1),
		@Model,
		@PayloadVersion,
		@Analysis,
		@InstanceID,
		NULLIF(@StatementText, N''),
		@ConversationID,
		@TurnNumber,
		NULLIF(@Question, N'')
