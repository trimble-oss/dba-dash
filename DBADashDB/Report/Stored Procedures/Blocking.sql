CREATE PROC [Report].[Blocking](@BlockingSnapshotID INT,@blocking_session_id INT)
AS
SELECT 
       BSS.session_id,
       BSS.blocking_session_id,
       ISNULL(BSS.Txt,'') Txt,
       BSS.start_time_utc,
       BSS.command,
       BSS.database_id,
       BSS.database_name,
       BSS.host_name,
       BSS.program_name,
       BSS.wait_time,
       BSS.login_name,
       BSS.wait_resource,
       BSS.Status,
	   stat.BlockCountRecursive,
       stat.WaitTimeRecursive,
       stat.BlockCount,
       stat.BlockWaitTime
FROM dbo.BlockingSnapshot BSS
OUTER APPLY dbo.BlockingSnapshotRecursiveStats(BSS.BlockingSnapshotID,BSS.session_id) stat
WHERE BSS.BlockingSnapshotID = @BlockingSnapshotID
AND (BSS.blocking_session_id=@blocking_session_id
	OR (@blocking_session_id=0
			AND NOT EXISTS(SELECT 1 
						FROM dbo.BlockingSnapshot BSS2 
						WHERE BSS2.BlockingSnapshotID=BSS.BlockingSnapshotID
						AND blocking_session_id=0
						AND @blocking_session_id=0)
			)
	)