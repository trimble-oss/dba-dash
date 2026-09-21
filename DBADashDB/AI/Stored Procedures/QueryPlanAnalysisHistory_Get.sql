/*
	Previous analyses of a query plan: conversations about this exact plan first, then about the same
	query with a different plan, newest first within each.  The query plan counterpart of
	AI.DeadlockAnalysisHistory_Get, built the same way and for the same reasons.

	Leading with the plan matters more here than it does for a deadlock.  A query that has had two plans
	has usually had a good one and a bad one, and an answer about the good one put in front of somebody
	staring at the bad one is worse than no answer at all - so IsThisPlan says which, and the viewer
	labels it.

	A row is one turn, and what the viewer offers is conversations, so this returns every turn of the
	conversations it found rather than the matching turns alone.

	Model and payload version are deliberately not matched: the question is "has anyone looked at this
	query, and what did they find", not "can this answer be reused".
*/
CREATE PROC AI.QueryPlanAnalysisHistory_Get
(
	@Signature VARCHAR(18),
	/* Turns, not conversations - @MaxConversations is the limit that matters. */
	@MaxRows INT = 300,
	@PlanHash VARCHAR(18) = NULL,
	@MaxConversations INT = 25
)
AS
SET NOCOUNT ON

/* Callers pass the identities as the familiar "0x..." hex strings; compare against the 8-byte binary
   stored form (style 1 parses the leading 0x). */
DECLARE @SignatureBin BINARY(8) = CONVERT(BINARY(8), @Signature, 1);
DECLARE @PlanHashBin BINARY(8) = CONVERT(BINARY(8), NULLIF(@PlanHash, ''), 1);

/* The turns that match, and the conversation each belongs to.  A row with no ConversationID is its
   own, keyed on its ID so it can never collide with a real ConversationID.

   Matched on the query, never on the plan.  QueryPlanHash identifies a shape of plan, not a query:
   two unrelated statements that compile to the same shape share one, so matching on it as well would
   put another query's conversation in this query's list - and, because the plan hash is what marks a
   row as being about the plan on screen, put it there first and labelled as being about this one. */
CREATE TABLE #Matched
(
	ConversationKey VARCHAR(50) NOT NULL,
	ConversationID UNIQUEIDENTIFIER NULL,
	QueryPlanAnalysisID BIGINT NOT NULL PRIMARY KEY,
	GeneratedUtc DATETIME2(3) NOT NULL,
	IsThisPlan BIT NOT NULL
);

INSERT INTO #Matched (ConversationKey, ConversationID, QueryPlanAnalysisID, GeneratedUtc, IsThisPlan)
SELECT	ISNULL(CONVERT(VARCHAR(36), QPA.ConversationID), 'row:' + CONVERT(VARCHAR(20), QPA.QueryPlanAnalysisID)),
		QPA.ConversationID,
		QPA.QueryPlanAnalysisID,
		QPA.GeneratedUtc,
		CASE WHEN QPA.PlanHash = @PlanHashBin THEN 1 ELSE 0 END
FROM AI.QueryPlanAnalysis QPA
WHERE QPA.Signature = @SignatureBin;

/* Newest first, with anything about this exact plan ahead of the rest of the query's. */
CREATE TABLE #Conversations
(
	ConversationKey VARCHAR(50) NOT NULL PRIMARY KEY,
	IsThisPlan BIT NOT NULL,
	LastGeneratedUtc DATETIME2(3) NOT NULL
);

INSERT INTO #Conversations (ConversationKey, IsThisPlan, LastGeneratedUtc)
SELECT TOP (@MaxConversations)
		M.ConversationKey,
		MAX(CONVERT(TINYINT, M.IsThisPlan)),
		MAX(M.GeneratedUtc)
FROM #Matched M
GROUP BY M.ConversationKey
ORDER BY MAX(CONVERT(TINYINT, M.IsThisPlan)) DESC, MAX(M.GeneratedUtc) DESC;

/* Every turn of those conversations: the matched turns, plus any sibling turn the match itself did
   not return. */
WITH Turns AS (
	SELECT C.ConversationKey, C.IsThisPlan, C.LastGeneratedUtc, M.QueryPlanAnalysisID
	FROM #Conversations C
	JOIN #Matched M ON M.ConversationKey = C.ConversationKey
	UNION
	SELECT C.ConversationKey, C.IsThisPlan, C.LastGeneratedUtc, QPA.QueryPlanAnalysisID
	FROM #Conversations C
	JOIN #Matched M ON M.ConversationKey = C.ConversationKey AND M.ConversationID IS NOT NULL
	JOIN AI.QueryPlanAnalysis QPA ON QPA.ConversationID = M.ConversationID
)
SELECT TOP (@MaxRows)
	QPA.QueryPlanAnalysisID,
	QPA.ConversationID,
	T.ConversationKey,
	QPA.Analysis,
	QPA.Question,
	QPA.Model,
	QPA.PayloadVersion,
	QPA.GeneratedUtc,
	QPA.InstanceID,
	I.InstanceDisplayName,
	T.IsThisPlan,
	ISNULL(QPA.TurnNumber, 1) AS TurnNumber
FROM Turns T
JOIN AI.QueryPlanAnalysis QPA ON QPA.QueryPlanAnalysisID = T.QueryPlanAnalysisID
LEFT JOIN dbo.Instances I ON I.InstanceID = QPA.InstanceID
ORDER BY T.IsThisPlan DESC, T.LastGeneratedUtc DESC, T.ConversationKey, TurnNumber, QPA.QueryPlanAnalysisID
