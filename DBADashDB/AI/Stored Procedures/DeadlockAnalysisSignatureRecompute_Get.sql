/*
	The next batch of AI analyses stored under an older signature version, with the graph each one was
	produced from, in no particular order.  Analyses stored without a graph can't be recomputed and are not
	returned.

	The same pattern as dbo.DeadlockSignatureRecompute_Get: IX_DeadlockAnalysis_SignatureVersion makes this a seek,
	and every row returned is updated by AI.DeadlockAnalysisSignatureRecompute_Upd - including one whose graph no
	longer parses - so it leaves the seek range and no position has to be carried between batches.
*/
CREATE PROC AI.DeadlockAnalysisSignatureRecompute_Get(
	@SignatureVersion TINYINT,
	@BatchSize INT
)
AS
SET NOCOUNT ON

SELECT TOP(@BatchSize)
	DA.DeadlockAnalysisID,
	CAST(DECOMPRESS(DA.DeadlockXmlCompressed) AS NVARCHAR(MAX)) AS DeadlockGraph
FROM AI.DeadlockAnalysis DA
WHERE (DA.SignatureVersion < @SignatureVersion OR DA.SignatureVersion IS NULL)
AND DA.DeadlockXmlCompressed IS NOT NULL
OPTION(RECOMPILE)
