/*
	Previous analyses of a deadlock: those of this exact deadlock first, then those of its pattern, newest
	first within each.

	A row is one conversation - its shared opening analysis.  The reader's own follow-ups belong to them
	and are kept on their own machine, stitched back on by the viewer, so this returns one row per
	conversation and nothing private.  Rows stored before conversations existed have no ConversationID and
	are each a conversation in their own right, which is exactly what they are.

	Model and payload version are deliberately not matched: the question is "has anyone looked at this
	deadlock, and what did they find", not "can this answer be reused".  An answer from an older model, or
	from before object definitions were being sent, is still worth reading - the viewer shows it for what
	it is, and asking again is one button away.

	IsThisDeadlock says whether a conversation is about this deadlock or another occurrence of its pattern.
	A conversation about this deadlock stays one even when a signature version change has since moved the
	pattern it was stored under.
*/
CREATE PROC AI.DeadlockAnalysisHistory_Get
(
	@Signature VARCHAR(18),
	/* One row per conversation now, so this is a conversation count in all but name.  Kept as a distinct
	   parameter, and defaulting alongside @MaxConversations, so an older caller's arguments still bind. */
	@MaxRows INT = 300,
	@DeadlockHash BINARY(16) = NULL,
	@MaxConversations INT = 25
)
AS
SET NOCOUNT ON

/* Callers pass the signature as the familiar "0x..." hex string; compare against the 8-byte binary
   stored form (style 1 parses the leading 0x). */
DECLARE @SignatureBin BINARY(8) = CONVERT(BINARY(8), @Signature, 1);

/* One conversation per row: its opening analysis.  A row from before conversations existed keys on its
   ID so it can never collide with a real ConversationID.  This exact deadlock leads the rest of the
   pattern, newest first within each. */
SELECT TOP (@MaxConversations)
	DA.DeadlockAnalysisID,
	DA.ConversationID,
	ISNULL(CONVERT(VARCHAR(36), DA.ConversationID), 'row:' + CONVERT(VARCHAR(20), DA.DeadlockAnalysisID)) AS ConversationKey,
	DA.Analysis,
	DA.Model,
	DA.PayloadVersion,
	DA.GeneratedUtc,
	DA.InstanceID,
	I.InstanceDisplayName,
	CONVERT(BIT, CASE WHEN DA.DeadlockHash = @DeadlockHash THEN 1 ELSE 0 END) AS IsThisDeadlock
FROM AI.DeadlockAnalysis DA
LEFT JOIN dbo.Instances I ON I.InstanceID = DA.InstanceID
WHERE DA.Signature = @SignatureBin
OR DA.DeadlockHash = @DeadlockHash
ORDER BY IsThisDeadlock DESC, DA.GeneratedUtc DESC
