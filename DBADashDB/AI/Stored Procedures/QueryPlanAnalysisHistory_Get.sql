/*
	Previous analyses of a query plan: conversations about this exact plan first, then about the same
	query with a different plan, newest first within each.  The query plan counterpart of
	AI.DeadlockAnalysisHistory_Get, built the same way and for the same reasons.

	Leading with the plan matters more here than it does for a deadlock.  A query that has had two plans
	has usually had a good one and a bad one, and an answer about the good one put in front of somebody
	staring at the bad one is worse than no answer at all - so IsThisPlan says which, and the viewer
	labels it.

	A row is one conversation - its shared opening analysis.  The reader's own follow-ups belong to them
	and are kept on their own machine, stitched back on by the viewer, so this returns one row per
	conversation and nothing private.

	Model and payload version are deliberately not matched: the question is "has anyone looked at this
	query, and what did they find", not "can this answer be reused".
*/
CREATE PROC AI.QueryPlanAnalysisHistory_Get
(
	@Signature VARCHAR(18),
	/* One row per conversation now, so this is a conversation count in all but name.  Kept as a distinct
	   parameter, and defaulting alongside @MaxConversations, so an older caller's arguments still bind. */
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

/* One conversation per row: its opening analysis.  A row from before conversations existed keys on its
   ID so it can never collide with a real ConversationID.  This exact plan leads the rest of the query,
   newest first within each.

   Matched on the query, never on the plan.  QueryPlanHash identifies a shape of plan, not a query: two
   unrelated statements that compile to the same shape share one, so matching on it as well would put
   another query's conversation in this query's list - and, because the plan hash is what marks a row as
   being about the plan on screen, put it there first and labelled as being about this one. */
SELECT TOP (@MaxConversations)
	QPA.QueryPlanAnalysisID,
	QPA.ConversationID,
	ISNULL(CONVERT(VARCHAR(36), QPA.ConversationID), 'row:' + CONVERT(VARCHAR(20), QPA.QueryPlanAnalysisID)) AS ConversationKey,
	QPA.Analysis,
	QPA.Model,
	QPA.PayloadVersion,
	QPA.GeneratedUtc,
	QPA.InstanceID,
	I.InstanceDisplayName,
	CONVERT(BIT, CASE WHEN QPA.PlanHash = @PlanHashBin THEN 1 ELSE 0 END) AS IsThisPlan
FROM AI.QueryPlanAnalysis QPA
LEFT JOIN dbo.Instances I ON I.InstanceID = QPA.InstanceID
WHERE QPA.Signature = @SignatureBin
ORDER BY IsThisPlan DESC, QPA.GeneratedUtc DESC
