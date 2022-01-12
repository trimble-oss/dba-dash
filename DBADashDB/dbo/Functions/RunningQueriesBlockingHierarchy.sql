CREATE FUNCTION dbo.RunningQueriesBlockingHierarchy(
	@InstanceID INT,
	@SnapshotDateUTC DATETIME2(7),
	@BlockingSessionID SMALLINT
)
RETURNS TABLE
AS
RETURN
/* 
	Get the blocking hierarchy for a blocked session starting with the current blocking session  
	For example session 123 might be blocked by session 64.  Session 64 is blocked by session 72.  Session 72 is blocked by session 96 (root blocker)
	We pass in 64 as the current blocking session and get: 96 \ 72 \ 64.  
*/
WITH  H AS(
	SELECT @BlockingSessionID as blocking_session_id,
			0 as Sort
	WHERE @BlockingSessionID<>0
	UNION ALL
	SELECT Q.blocking_session_id,H.Sort+1
	FROM H 
	JOIN dbo.RunningQueries Q ON H.blocking_session_id = Q.session_id
	WHERE Q.InstanceID = @InstanceID
	AND Q.SnapshotDateUTC = @SnapshotDateUTC
	AND Q.blocking_session_id <> 0
	AND Q.blocking_session_id <> @BlockingSessionID -- Avoid an infinite loop in a deadlock scenario
	)
SELECT ISNULL(STUFF((SELECT ' \ '  + CAST(blocking_session_id AS VARCHAR(MAX))
				FROM H
				ORDER BY Sort DESC
				FOR XML PATH(''),TYPE).value('.','VARCHAR(MAX)'),1,3,''),'')  as BlockingHierarchy