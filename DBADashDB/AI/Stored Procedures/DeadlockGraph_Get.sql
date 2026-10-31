/*
	One deadlock graph, decompressed, with the instance it happened on - the AI counterpart of
	dbo.DeadlockGraph_Get, for MCP clients.  The key is as AI.DeadlockList_Get returns it, the hash in its
	"0x..." form.  No rows when the deadlock is unknown or its graph has passed its retention.
*/
CREATE PROC AI.DeadlockGraph_Get(
	@InstanceID INT,
	@EventTime DATETIME2(3),
	@DeadlockHash VARCHAR(34)
)
AS
SET NOCOUNT ON
SELECT	I.InstanceDisplayName,
		CAST(DECOMPRESS(X.DeadlockXmlCompressed) AS NVARCHAR(MAX)) AS DeadlockGraph
FROM dbo.DeadlockXml X
JOIN dbo.Instances I ON I.InstanceID = X.InstanceID
WHERE X.InstanceID = @InstanceID
AND X.EventTime = @EventTime
AND X.DeadlockHash = CONVERT(BINARY(16), @DeadlockHash, 1)
AND X.DeadlockXmlCompressed IS NOT NULL
