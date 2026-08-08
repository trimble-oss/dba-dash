CREATE PROC dbo.RunningQueriesSessionLatestSnapshot_Get(
	@InstanceID INT,
	@session_id INT
)
AS
/* Used by the session detail viewer's "Latest Snapshot" option.
   SessionLatestSnapshotUTC  = the most recent snapshot the session appears in (NULL if it appears in none).
   InstanceLatestSnapshotUTC = the most recent snapshot collected for the instance.
   When SessionLatestSnapshotUTC < InstanceLatestSnapshotUTC the session is no longer active (it has dropped out
   of newer snapshots), so the caller can highlight that the latest available snapshot is stale. */
SELECT
	(SELECT MAX(SnapshotDateUTC) FROM dbo.RunningQueries WHERE InstanceID = @InstanceID AND session_id = @session_id) AS SessionLatestSnapshotUTC,
	(SELECT MAX(SnapshotDateUTC) FROM dbo.RunningQueriesSummary WHERE InstanceID = @InstanceID) AS InstanceLatestSnapshotUTC
