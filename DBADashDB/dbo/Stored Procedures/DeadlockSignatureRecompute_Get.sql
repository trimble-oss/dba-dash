/*
	The next batch of deadlocks whose signature was computed by an older signature version, in no particular order.

	The signature is computed in the service from the parsed graph (DBADash.Deadlock.Analysis.DeadlockSignature),
	so only deadlocks that still have their graph can be recomputed.  Those whose graph has aged out of
	dbo.DeadlockXml keep the signature and SignatureVersion they were stored with.

	IX_Deadlocks_SignatureVersion makes this a seek per partition, and every row returned is updated by
	dbo.DeadlockSignatureRecompute_Upd - including one whose graph no longer parses - so it leaves the seek range.
	Each batch therefore only reads what is left to do, with no position to carry between batches and no ordering to
	pay a sort for: TOP stops as soon as it has a batch.  That also makes the check the service runs on every start,
	when there is usually nothing to do, cheap.

	Deadlocks older than the graph retention are left out.  They can't have a graph, and they are the rows that stay
	on the old version for good - without the bound, every batch would look up a graph for each of them first, since
	the partitions are read oldest first.  The bound is a range on the partitioning column, so those partitions are
	eliminated rather than read.  A couple of days' margin covers purge running behind; a deadlock older than that
	which somehow still has its graph just keeps its old signature.
*/
CREATE PROC dbo.DeadlockSignatureRecompute_Get(
	@SignatureVersion TINYINT,
	@BatchSize INT
)
AS
SET NOCOUNT ON

DECLARE @FromEventTime DATETIME2(3) = '00010101'

SELECT @FromEventTime = DATEADD(d, -(DR.RetentionDays + 2), SYSUTCDATETIME())
FROM dbo.DataRetention DR
WHERE DR.SchemaName = 'dbo'
AND DR.TableName = 'DeadlockXml'
AND DR.RetentionDays > 0

SELECT TOP(@BatchSize)
	D.InstanceID,
	D.EventTime,
	D.DeadlockHash,
	CAST(DECOMPRESS(X.DeadlockXmlCompressed) AS NVARCHAR(MAX)) AS DeadlockGraph
FROM dbo.Deadlocks D
JOIN dbo.DeadlockXml X ON X.InstanceID = D.InstanceID
						AND X.EventTime = D.EventTime
						AND X.DeadlockHash = D.DeadlockHash
WHERE (D.SignatureVersion < @SignatureVersion OR D.SignatureVersion IS NULL)
AND D.EventTime >= @FromEventTime
AND X.DeadlockXmlCompressed IS NOT NULL
OPTION(RECOMPILE)
