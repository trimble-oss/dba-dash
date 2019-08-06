CREATE PROC [dbo].[BlockingSnapshot_Upd](@BlockingSnapshot dbo.BlockingSnapshot READONLY,@InstanceID INT,@SnapshotDate DATETIME)
AS
DECLARE @SnapshotID INT
INSERT INTO dbo.BlockingSnapshotSummary(InstanceID,SnapshotDateUTC,BlockedSessionCount,BlockedWaitTime,UTCOffset)
SELECT @InstanceID,MAX(SnapshotDateUTC), COUNT(*),SUM(wait_time),MAX(T.UTCOffset)
FROM @BlockingSnapshot T
WHERE blocking_session_id>0
AND NOT EXISTS(SELECT 1 FROM dbo.BlockingSnapshotSummary SS WHERE SS.InstanceID=@InstanceID AND SS.SnapshotDateUTC = T.SnapshotDateUTC)
HAVING(COUNT(*)>0)

SET @SnapshotID = SCOPE_IDENTITY();

IF @SnapshotID IS NOT NULL
BEGIN;
	WITH T AS (
			SELECT session_id,
			   blocking_session_id,
			   Txt,
			   start_time_utc,
			   command,
			   database_id,
			   database_name,
			   host_name,
			   program_name,
			   wait_time,
			   login_name,
			   wait_resource,
			   Status,
			   wait_type,
			   ROW_NUMBER() OVER(PARTITION BY t.session_id ORDER BY t.wait_time DESC) rnum
		FROM @BlockingSnapshot t
	)
	INSERT INTO dbo.BlockingSnapshot(BlockingSnapshotID,
		   session_id,
		   blocking_session_id,
		   Txt,
		   start_time_utc,
		   command,
		   database_id,
		   database_name,
		   host_name,
		   program_name,
		   wait_time,
		   login_name,
		   wait_resource,
		   Status,
		   wait_type)
	SELECT @SnapshotID,
		   session_id,
		   blocking_session_id,
		   Txt,
		   start_time_utc,
		   command,
		   database_id,
		   database_name,
		   host_name,
		   program_name,
		   wait_time,
		   login_name,
		   wait_resource,
		   Status,
		   wait_type
	FROM T
	WHERE rnum=1
END


EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
                             @Reference = 'BlockingSnapshot', 
                             @SnapshotDate = @SnapshotDate