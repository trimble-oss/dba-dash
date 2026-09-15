/*
	Moves stored AI analyses to the signature recomputed from their own graph.

	Each answer was produced from one graph, so it belongs to exactly one pattern: it is moved, not copied,
	and only ever to the pattern that graph belongs to now.  When a version change splits an over-broad
	pattern, the other patterns get no answer rather than the wrong one.

	A NULL signature means the graph didn't parse.  The analysis keeps the signature it has, but takes the new
	version all the same, so AI.DeadlockAnalysisSignatureRecompute_Get doesn't return it in every batch.  The graph
	stored with an analysis is one the viewer had already parsed, so this is a safeguard rather than an expected case.

	Only moves a row forward, so two services recomputing against the same repository at once is harmless.
*/
CREATE PROC AI.DeadlockAnalysisSignatureRecompute_Upd(
	@Signatures AI.DeadlockAnalysisSignatures READONLY
)
AS
SET NOCOUNT ON

UPDATE DA
SET DA.Signature = ISNULL(CONVERT(BINARY(8), S.Signature, 1), DA.Signature),
	DA.SignatureVersion = S.SignatureVersion
FROM AI.DeadlockAnalysis DA
JOIN @Signatures S ON S.DeadlockAnalysisID = DA.DeadlockAnalysisID
WHERE (DA.SignatureVersion < S.SignatureVersion OR DA.SignatureVersion IS NULL)
