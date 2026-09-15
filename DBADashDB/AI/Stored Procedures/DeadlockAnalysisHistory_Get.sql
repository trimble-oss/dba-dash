/*
	Previous analyses of a deadlock: those of this exact deadlock first, then those of its pattern, newest
	first within each.

	Model and payload version are deliberately not matched: the question is "has anyone looked at this
	deadlock, and what did they find", not "can this answer be reused".  An answer from an older model, or
	from before object definitions were being sent, is still worth reading - the viewer shows it for what
	it is, and asking again is one button away.

	IsThisDeadlock says which of the two an answer is.  An answer about this deadlock stays one even when a
	signature version change has since moved the pattern it was stored under.
*/
CREATE PROC AI.DeadlockAnalysisHistory_Get
(
	@Signature VARCHAR(18),
	@DeadlockHash BINARY(16) = NULL,
	@MaxRows INT = 50
)
AS
SET NOCOUNT ON

/* Callers pass the signature as the familiar "0x..." hex string; compare against the 8-byte binary
   stored form (style 1 parses the leading 0x). */
DECLARE @SignatureBin BINARY(8) = CONVERT(BINARY(8), @Signature, 1);

WITH A AS (
	SELECT DA.DeadlockAnalysisID
	FROM AI.DeadlockAnalysis DA
	WHERE DA.Signature = @SignatureBin
	UNION
	SELECT DA.DeadlockAnalysisID
	FROM AI.DeadlockAnalysis DA
	WHERE DA.DeadlockHash = @DeadlockHash
)
SELECT TOP (@MaxRows)
	DA.DeadlockAnalysisID,
	DA.Analysis,
	DA.Model,
	DA.PayloadVersion,
	DA.GeneratedUtc,
	DA.InstanceID,
	I.InstanceDisplayName,
	CAST(CASE WHEN DA.DeadlockHash = @DeadlockHash THEN 1 ELSE 0 END AS BIT) AS IsThisDeadlock
FROM A
JOIN AI.DeadlockAnalysis DA ON DA.DeadlockAnalysisID = A.DeadlockAnalysisID
LEFT JOIN dbo.Instances I ON I.InstanceID = DA.InstanceID
ORDER BY IsThisDeadlock DESC, DA.GeneratedUtc DESC
