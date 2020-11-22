CREATE PROC [dbo].[BlockingSnapshot_Upd](@BlockingSnapshot dbo.BlockingSnapshot READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
DECLARE @BlockingSnapshotDeDupe TABLE(
	[session_id] [smallint] NOT NULL,
	[blocking_session_id] [smallint] NOT NULL,
	[Txt] [nvarchar](max) NULL,
	[start_time_utc] [datetime2](3) NULL,
	[command] [nvarchar](32) NULL,
	[database_id] [smallint] NULL,
	[database_name] [nvarchar](128) NULL,
	[host_name] [nvarchar](128) NULL,
	[program_name] [nvarchar](128) NULL,
	[wait_time] [int] NULL,
	[login_name] [nvarchar](128) NULL,
	[wait_resource] [nvarchar](256) NULL,
	[Status] [nvarchar](30) NULL,
	[wait_type] [nvarchar](60) NULL,
	UTCOffset [int] NOT NULL
);
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
			   UTCOffset,
			   ROW_NUMBER() OVER(PARTITION BY t.session_id ORDER BY t.wait_time DESC) rnum
		FROM @BlockingSnapshot t
)
INSERT INTO @BlockingSnapshotDeDupe(
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
		wait_type,
		UTCOffset)
SELECT 	session_id,
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
		UTCOffset
FROM T
WHERE rnum=1

DECLARE @SnapshotID INT
INSERT INTO dbo.BlockingSnapshotSummary(InstanceID,SnapshotDateUTC,BlockedSessionCount,BlockedWaitTime,UTCOffset)
SELECT @InstanceID,@SnapshotDate, COUNT(*),SUM(wait_time),MAX(T.UTCOffset)
FROM @BlockingSnapshotDeDupe T
WHERE blocking_session_id>0
AND NOT EXISTS(SELECT 1 FROM dbo.BlockingSnapshotSummary SS WHERE SS.InstanceID=@InstanceID AND SS.SnapshotDateUTC = @SnapshotDate)
HAVING(COUNT(*)>0)

SET @SnapshotID = SCOPE_IDENTITY();

IF @SnapshotID IS NOT NULL
BEGIN;
	INSERT INTO dbo.BlockingSnapshot(BlockingSnapshotID,
		   SnapshotDateUTC,
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
		   @SnapshotDate,
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
	FROM @BlockingSnapshotDeDupe
END


EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
                             @Reference = 'BlockingSnapshot', 
                             @SnapshotDate = @SnapshotDate