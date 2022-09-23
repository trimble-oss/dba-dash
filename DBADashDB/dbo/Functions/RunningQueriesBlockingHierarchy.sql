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
	SELECT	@BlockingSessionID as blocking_session_id,
			0 as Sort,
			' \ ' + CAST(@BlockingSessionID AS VARCHAR(MAX)) AS BlockingHierarchy,
			0 AS IsDeadlock
	WHERE @BlockingSessionID<>0
	UNION ALL
	SELECT	Q.blocking_session_id,
			H.Sort+1,
			CASE WHEN calc.IsDeadlock =1 THEN ' \ {!Deadlock!}' ELSE '' END + ' \ ' + CAST(Q.blocking_session_id AS VARCHAR(MAX)) + H.BlockingHierarchy,
			calc.IsDeadlock
	FROM H 
	JOIN dbo.RunningQueries Q ON H.blocking_session_id = Q.session_id
	-- Check for a loop (deadlock).  Break execution on the next iteration when a deadlock is detected
	OUTER APPLY(SELECT CASE WHEN Q.blocking_session_id = @BlockingSessionID OR H.BlockingHierarchy LIKE '%\ ' + CAST(Q.blocking_session_id AS VARCHAR(MAX)) + ' \%' THEN 1 ELSE 0 END AS IsDeadlock) calc
	WHERE Q.InstanceID = @InstanceID
	AND Q.SnapshotDateUTC = @SnapshotDateUTC
	AND Q.blocking_session_id <> 0
	AND H.IsDeadlock=0 -- Stop if a deadlock was detected on the previous loop
	AND H.Sort < 100 -- Stop before we break max recursion
	)
SELECT TOP(1) ISNULL(STUFF(BlockingHierarchy,1,3,''),'')  as BlockingHierarchy
FROM H
ORDER BY Sort DESC