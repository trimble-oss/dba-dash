/*
	Stores the opening analysis of a query plan.  Always adds: every answer is kept (see
	AI.QueryPlanAnalysis).

	Only the opening analysis.  It is shared - a plan and some generated notes submitted for analysis,
	with nothing in it belonging to whoever pressed the button - and the follow-up conversation that
	comes after it is not, so it never reaches this database.  See the guard below.
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

/*
	Follow-up turns are not stored here, whoever asks for them to be.

	A follow-up belongs to the person who asked it, and it is kept on their own machine - encrypted to
	their Windows account, where nobody administering this database can read it.  The viewer says so on
	the request (clientStoresTurn) and the service does not call this for those turns.

	This is the same rule stated a second time, in the one place no client can talk its way past.  The
	flag defaults to false so that a viewer too old to know about any of this keeps working, which also
	means an old viewer still asks for its follow-ups to be stored - and they would land here, in the
	clear, in a table the whole team reads.  Refusing them here makes the guarantee a property of the
	repository rather than of whichever version happens to be installed.  An old viewer keeps its
	opening analyses, and simply stops keeping its follow-ups, which is what upgrading would do anyway.

	Written as a guard rather than by removing the parameters: an old viewer passes them on every call,
	including the opening analysis, and a procedure that would not accept them would stop storing that
	too.  The values themselves are no longer stored - the columns that held them are gone - so @Question
	is accepted and ignored, and @TurnNumber only decides whether the row is stored at all.

	NULL is an opening turn - answers predate the column, and callers that know nothing of conversations
	do not pass it.  NULL > 1 is unknown, so those fall through and are stored, which is correct.
*/
IF @TurnNumber > 1
	RETURN;

INSERT INTO AI.QueryPlanAnalysis
(
	Signature,
	PlanHash,
	Model,
	PayloadVersion,
	Analysis,
	InstanceID,
	StatementText,
	ConversationID
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
		@ConversationID
