/*
	Previous analyses of a deadlock pattern, newest first.

	Matched on the signature alone, deliberately: the question is "has anyone looked at this deadlock,
	and what did they find", not "can this answer be reused".  An answer from an older model, or from
	before object definitions were being sent, is still worth reading - the viewer shows it for what
	it is, and asking again is one button away.
*/
CREATE PROC AI.DeadlockAnalysisHistory_Get
(
	@Signature VARCHAR(18),
	@MaxRows INT = 10
)
AS
SET NOCOUNT ON

/* Callers pass the signature as the familiar "0x..." hex string; compare against the 8-byte binary
   stored form (style 1 parses the leading 0x). */
DECLARE @SignatureBin BINARY(8) = CONVERT(BINARY(8), @Signature, 1);

SELECT TOP (@MaxRows)
	DA.Analysis,
	DA.Model,
	DA.PayloadVersion,
	DA.GeneratedUtc,
	DA.InstanceID,
	I.InstanceDisplayName
FROM AI.DeadlockAnalysis DA
LEFT JOIN dbo.Instances I ON I.InstanceID = DA.InstanceID
WHERE DA.Signature = @SignatureBin
ORDER BY DA.GeneratedUtc DESC
