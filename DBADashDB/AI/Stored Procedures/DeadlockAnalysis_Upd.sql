/*
	Stores an analysis of a deadlock, or a follow-up answer in the conversation one started.  Always
	adds: every answer is kept (see AI.DeadlockAnalysis).

	The graph is stored with the answer, compressed, so that the signature can be recomputed from it when
	the signature version changes, and its hash so the viewer can prefer answers about this exact deadlock.
*/
CREATE PROC AI.DeadlockAnalysis_Upd
(
	@Signature VARCHAR(18),
	@Model NVARCHAR(128),
	@PayloadVersion VARCHAR(30),
	@Analysis NVARCHAR(MAX),
	@InstanceID INT = NULL,
	@SignatureVersion TINYINT = NULL,
	@GraphXml NVARCHAR(MAX) = NULL,
	@DeadlockHash BINARY(16) = NULL,
	@ConversationID UNIQUEIDENTIFIER = NULL,
	@TurnNumber SMALLINT = NULL,
	@Question NVARCHAR(MAX) = NULL
)
AS
SET NOCOUNT ON

INSERT INTO AI.DeadlockAnalysis
(
	Signature,
	Model,
	PayloadVersion,
	Analysis,
	InstanceID,
	SignatureVersion,
	DeadlockXmlCompressed,
	DeadlockHash,
	ConversationID,
	TurnNumber,
	Question
)
/* Callers pass the signature as the familiar "0x..." hex string; store it in its 8-byte binary form
   (style 1 parses the leading 0x).  COMPRESS of an NVARCHAR is gzipped UTF-16 - the same form
   dbo.DeadlockXml stores. */
SELECT	CONVERT(BINARY(8), @Signature, 1),
		@Model,
		@PayloadVersion,
		@Analysis,
		@InstanceID,
		@SignatureVersion,
		COMPRESS(NULLIF(@GraphXml, N'')),
		@DeadlockHash,
		@ConversationID,
		@TurnNumber,
		NULLIF(@Question, N'')
