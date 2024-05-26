CREATE FUNCTION dbo.RunningQueriesBlockingRecursiveStats(
	@InstanceID INT,
	@SnapshotDateUTC DATETIME2,
	@session_id SMALLINT
)
RETURNS TABLE 
AS
RETURN
WITH R AS (
	SELECT Q.session_id, Q.wait_time,1 AS IsDirect
	FROM dbo.RunningQueries Q
	WHERE Q.InstanceID = @InstanceID
	AND Q.blocking_session_id=@session_id 
	AND Q.SnapshotDateUTC = @SnapshotDateUTC
	UNION ALL
	SELECT RQ.session_id, RQ.wait_time,0 AS IsDirect
	FROM R 
	JOIN dbo.RunningQueries RQ ON RQ.blocking_session_id = R.session_id
	WHERE RQ.session_id<>@session_id
	AND RQ.InstanceID = @InstanceID
	AND RQ.SnapshotDateUTC = @SnapshotDateUTC
)
SELECT COUNT(*) BlockCountRecursive, 
	ISNULL(SUM(CAST(R.wait_time AS BIGINT)),0) BlockWaitTimeRecursiveMs,
	ISNULL(SUM(R.IsDirect),0) AS BlockCount,
	ISNULL(SUM(CASE WHEN R.IsDirect=1 THEN CAST(R.wait_time AS BIGINT) ELSE 0 END),0) AS BlockWaitTimeMs
FROM R
