/*
	Previous analyses of a deadlock: those of this exact deadlock first, then those of its pattern, newest
	first within each.

	A row is one turn, and what the viewer offers is conversations, so this returns every turn of the
	conversations it found rather than the matching turns alone.  A follow-up read without the analysis it
	followed is an answer to a question nobody can see; and picking a conversation out of the drop-down has
	to put the whole exchange back on screen, because that is what the reader can then carry on adding to.
	Rows stored before conversations existed have no ConversationID and are each a conversation of one
	turn, which is exactly what they are.

	Model and payload version are deliberately not matched: the question is "has anyone looked at this
	deadlock, and what did they find", not "can this answer be reused".  An answer from an older model, or
	from before object definitions were being sent, is still worth reading - the viewer shows it for what
	it is, and asking again is one button away.

	IsThisDeadlock says whether a conversation is about this deadlock or another occurrence of its pattern.
	It is taken over the conversation rather than the turn, so a whole exchange is described the one way.
	A conversation about this deadlock stays one even when a signature version change has since moved the
	pattern it was stored under.
*/
CREATE PROC AI.DeadlockAnalysisHistory_Get
(
	@Signature VARCHAR(18),
	/* Turns, not conversations - @MaxConversations is the limit that matters.  High enough that it only
	   ever trims the tail of the oldest conversation returned. */
	@MaxRows INT = 300,
	@DeadlockHash BINARY(16) = NULL,
	@MaxConversations INT = 25
)
AS
SET NOCOUNT ON

/* Callers pass the signature as the familiar "0x..." hex string; compare against the 8-byte binary
   stored form (style 1 parses the leading 0x). */
DECLARE @SignatureBin BINARY(8) = CONVERT(BINARY(8), @Signature, 1);

/* The turns that match, and the conversation each belongs to.  A row from before conversations existed
   is its own, keyed on its ID so it can never collide with a real ConversationID. */
CREATE TABLE #Matched
(
	ConversationKey VARCHAR(50) NOT NULL,
	ConversationID UNIQUEIDENTIFIER NULL,
	DeadlockAnalysisID BIGINT NOT NULL PRIMARY KEY,
	GeneratedUtc DATETIME2(3) NOT NULL,
	IsThisDeadlock BIT NOT NULL
);

INSERT INTO #Matched (ConversationKey, ConversationID, DeadlockAnalysisID, GeneratedUtc, IsThisDeadlock)
SELECT	ISNULL(CONVERT(VARCHAR(36), DA.ConversationID), 'row:' + CONVERT(VARCHAR(20), DA.DeadlockAnalysisID)),
		DA.ConversationID,
		DA.DeadlockAnalysisID,
		DA.GeneratedUtc,
		CASE WHEN DA.DeadlockHash = @DeadlockHash THEN 1 ELSE 0 END
FROM AI.DeadlockAnalysis DA
WHERE DA.Signature = @SignatureBin
OR DA.DeadlockHash = @DeadlockHash;

/* Newest first, with anything about this exact deadlock ahead of the rest of the pattern's. */
CREATE TABLE #Conversations
(
	ConversationKey VARCHAR(50) NOT NULL PRIMARY KEY,
	IsThisDeadlock BIT NOT NULL,
	LastGeneratedUtc DATETIME2(3) NOT NULL
);

INSERT INTO #Conversations (ConversationKey, IsThisDeadlock, LastGeneratedUtc)
SELECT TOP (@MaxConversations)
		M.ConversationKey,
		MAX(CONVERT(TINYINT, M.IsThisDeadlock)),
		MAX(M.GeneratedUtc)
FROM #Matched M
GROUP BY M.ConversationKey
ORDER BY MAX(CONVERT(TINYINT, M.IsThisDeadlock)) DESC, MAX(M.GeneratedUtc) DESC;

/* Every turn of those conversations.  The matched turns, plus any sibling turn the match itself did not
   return - all turns of a conversation carry the same signature, so that is belt to the braces of a
   signature recompute that somehow moved only part of one. */
WITH Turns AS (
	SELECT C.ConversationKey, C.IsThisDeadlock, C.LastGeneratedUtc, M.DeadlockAnalysisID
	FROM #Conversations C
	JOIN #Matched M ON M.ConversationKey = C.ConversationKey
	UNION
	SELECT C.ConversationKey, C.IsThisDeadlock, C.LastGeneratedUtc, DA.DeadlockAnalysisID
	FROM #Conversations C
	JOIN #Matched M ON M.ConversationKey = C.ConversationKey AND M.ConversationID IS NOT NULL
	JOIN AI.DeadlockAnalysis DA ON DA.ConversationID = M.ConversationID
)
SELECT TOP (@MaxRows)
	DA.DeadlockAnalysisID,
	DA.ConversationID,
	T.ConversationKey,
	DA.Analysis,
	DA.Question,
	DA.Model,
	DA.PayloadVersion,
	DA.GeneratedUtc,
	DA.InstanceID,
	I.InstanceDisplayName,
	T.IsThisDeadlock,
	ISNULL(DA.TurnNumber, 1) AS TurnNumber
FROM Turns T
JOIN AI.DeadlockAnalysis DA ON DA.DeadlockAnalysisID = T.DeadlockAnalysisID
LEFT JOIN dbo.Instances I ON I.InstanceID = DA.InstanceID
ORDER BY T.IsThisDeadlock DESC, T.LastGeneratedUtc DESC, T.ConversationKey, TurnNumber, DA.DeadlockAnalysisID
