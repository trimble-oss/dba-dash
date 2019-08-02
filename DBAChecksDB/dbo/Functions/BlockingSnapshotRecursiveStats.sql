CREATE FUNCTION [dbo].[BlockingSnapshotRecursiveStats](@BlockingSnapshotID INT,@session_id INT)
RETURNS TABLE 
AS
RETURN
WITH R AS (
	SELECT session_id, wait_time,1 AS IsDirect
	FROM dbo.BlockingSnapshot
	WHERE BlockingSnapshotID=@BlockingSnapshotID
	AND blocking_session_id=@session_id
	UNION ALL
	SELECT BSS.session_id, BSS.wait_time,0 AS IsDirect
	FROM R 
	JOIN dbo.BlockingSnapshot BSS ON BSS.blocking_session_id = R.session_id
	WHERE BlockingSnapshotID=@BlockingSnapshotID
	AND BSS.session_id<>@session_id
)
SELECT COUNT(*) BlockCountRecursive, 
	ISNULL(SUM(R.wait_time),0) WaitTimeRecursive,
	ISNULL(SUM(IsDirect),0) AS BlockCount,
	ISNULL(SUM(CASE WHEN R.IsDirect=1 THEN R.wait_time ELSE 0 END),0) AS BlockWaitTime
FROM R