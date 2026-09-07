/*
	The deadlock graph for a single deadlock, decompressed.

	Exists so that the deadlocks report does not have to carry the graphs.  A graph is the largest thing
	the collection stores by a wide margin, and returning one on every row of a grid means holding all of
	them in the client's memory for the sake of the one the reader eventually clicks - a report over a
	few thousand deadlocks is tens of megabytes of XML that is almost entirely never looked at.  The grid
	carries the key instead and this fetches the one graph on demand.

	Keyed on the primary key of dbo.DeadlockXml, so it is a single row seek.

	Returns no rows when the deadlock is unknown, or when the graph was not stored - the collection can
	be configured to shred without keeping the XML.  The caller reports that as "no graph available"
	rather than as an error, because it is an ordinary state.
*/
CREATE PROC dbo.DeadlockGraph_Get(
	@InstanceID INT,
	@EventTime DATETIME2(3),
	@DeadlockHash BINARY(16)
)
AS
SET NOCOUNT ON

SELECT CAST(DECOMPRESS(X.DeadlockXmlCompressed) AS NVARCHAR(MAX)) AS DeadlockGraph
FROM dbo.DeadlockXml X
WHERE X.InstanceID = @InstanceID
AND X.EventTime = @EventTime
AND X.DeadlockHash = @DeadlockHash
AND X.DeadlockXmlCompressed IS NOT NULL
