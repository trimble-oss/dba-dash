/*
	Stores recomputed deadlock signatures.

	Only moves a row forward: a row already at (or above) the version supplied is left alone.  So two services
	recomputing against the same repository at once is harmless, and an older service still sharing the
	repository can never write an older signature over a newer one.

	A NULL signature means the graph didn't parse.  The row keeps the signature it has, but takes the new version
	all the same: it has been looked at, can never be recomputed, and would otherwise be returned by
	dbo.DeadlockSignatureRecompute_Get in every batch.  The collector only stores graphs it has already parsed, so
	this is a safeguard rather than an expected case.

	AI analyses are not carried across here.  Which new pattern an answer belongs to can't be told from the
	deadlocks - a version change that splits an old pattern would hand one pattern's answer to the others - so
	each answer is recomputed from the graph it was produced from instead (see
	AI.DeadlockAnalysisSignatureRecompute_Upd).
*/
CREATE PROC dbo.DeadlockSignatureRecompute_Upd(
	@Signatures dbo.DeadlockSignatures READONLY
)
AS
SET NOCOUNT ON

UPDATE D
SET D.Signature = ISNULL(CONVERT(BINARY(8), S.Signature, 1), D.Signature),
	D.SignatureVersion = S.SignatureVersion
FROM dbo.Deadlocks D
JOIN @Signatures S ON S.InstanceID = D.InstanceID
					AND S.EventTime = D.EventTime
					AND S.DeadlockHash = D.DeadlockHash
WHERE (D.SignatureVersion < S.SignatureVersion OR D.SignatureVersion IS NULL)
