﻿CREATE PROC dbo.SlowQueriesDetail_Get(
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL,
	@ObjectName SYSNAME=NULL,
	@ClientHostName NVARCHAR(128)=NULL,
	@ConnectionID SYSNAME=NULL,
	@ClientAppName SYSNAME=NULL,
	@InstanceIDs VARCHAR(MAX)=NULL,
	@DurationFromSec BIGINT=NULL,
	@DurationToSec BIGINT=NULL,
	@Text NVARCHAR(MAX)=NULL,
	@DatabaseName SYSNAME=NULL,
	@UserName SYSNAME=NULL,
	@Result SYSNAME=NULL,
	@Top INT = 30,
	@SessionID INT =NULL,
	@Sort VARCHAR(50)='Duration',
	@SortDesc BIT = 1,
	@InstanceDisplayName NVARCHAR(128)=NULL
)
AS
DECLARE @DurationFromUS BIGINT 
DECLARE @DurationToUS BIGINT
SELECT @DurationFromUS = @DurationFromSec*1000000, @DurationToUS=@DurationToSec*1000000
IF @FromDate IS NULL	
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
IF @ToDate IS NULL	
	SET @ToDate = GETUTCDATE()
DECLARE @Instances IDs
IF @InstanceIDs IS NOT NULL
BEGIN
	INSERT INTO @Instances
	(
		ID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;
SET @Text = REPLACE(REPLACE(REPLACE(@Text,'[','[[]'),'_','[_]'),'%','[%]') -- Like encode input

DECLARE @SortSQL NVARCHAR(MAX)
SET @SortSQL = 'ORDER BY ' + CASE WHEN @Sort = 'Duration' THEN 'SQ.Duration' WHEN @Sort='timestamp' THEN 'SQ.timestamp' ELSE NULL END
			+ CASE WHEN @SortDesc=1 THEN ' DESC' ELSE ' ASC' END

IF @SortSQL IS NULL
BEGIN;
	THROW 50000,'Invalid sort specified',1;
END

DECLARE @SQL NVARCHAR(MAX)
SET @SQL = 
N'SELECT TOP(@Top) SQ.InstanceID,
       SQ.DatabaseID,
	   I.Instance,
	   I.InstanceDisplayName,
	   D.name as DatabaseName,
       SQ.event_type,
       SQ.object_name,
       SQ.timestamp,
       SQ.duration/1000000.0 as DurationSec,
       SQ.cpu_time/1000000.0 as CPUTimeSec,
       SQ.logical_reads,
       SQ.physical_reads,
       SQ.writes,
       SQ.username,
       SQ.text,
       SQ.client_hostname,
       SQ.client_app_name,
       SQ.result,
       SQ.Uniqueifier,
	   SQ.session_id,
	   DATEADD(ms,-duration/1000,timestamp) AS start_time,
	   HD.HumanDuration AS Duration
FROM dbo.SlowQueries SQ
JOIN dbo.Instances I ON I.InstanceID = SQ.InstanceID
CROSS APPLY dbo.MillisecondsToHumanDuration(SQ.Duration/1000) HD
LEFT JOIN dbo.Databases D ON D.DatabaseID = SQ.DatabaseID
WHERE timestamp>= @FromDate
AND timestamp< @ToDate
' + CASE WHEN @InstanceIDs IS NULL THEN '' ELSE 'AND EXISTS(SELECT 1 FROM @Instances IDs WHERE IDs.ID = SQ.InstanceID)' END + '
' + CASE WHEN @ObjectName IS NULL THEN '' ELSE 'AND SQ.object_name = @ObjectName' END +'
' + CASE WHEN @ClientHostName IS NULL THEN '' ELSE 'AND SQ.client_hostname = @ClientHostName' END +'
' + CASE WHEN @ConnectionID IS NULL THEN '' ELSE 'AND I.ConnectionID = @ConnectionID' END + '
' + CASE WHEN @InstanceDisplayName IS NULL THEN '' ELSE 'AND I.InstanceDisplayName = @InstanceDisplayName' END + '
' + CASE WHEN @ClientAppName IS NULL THEN '' ELSE 'AND SQ.client_app_name = @ClientAppName' END + '
' + CASE WHEN @DurationFromUS IS NULL THEN '' ELSE 'AND SQ.Duration >= @DurationFrom' END + '
' + CASE WHEN @DurationToUS IS NULL THEN '' ELSE 'AND SQ.Duration < @DurationTo' END + '
' + CASE WHEN @Text IS NULL THEN '' ELSE 'AND SQ.Text LIKE ''%'' + @Text + ''%''' END + '
' + CASE WHEN @DatabaseName IS NULL THEN '' ELSE 'AND D.name = @DatabaseName' END + '
' + CASE WHEN @UserName IS NULL THEN '' ELSE 'AND SQ.username = @UserName' END + '
' + CASE WHEN @Result IS NULL THEN '' ELSE 'AND SQ.Result = @Result' END + '
' + CASE WHEN @SessionID IS NULL THEN '' ELSE 'AND SQ.session_id = @SessionID' END + '
' + @SortSQL

EXEC sp_executesql @SQL,N'@Instances IDs READONLY,@ObjectName SYSNAME,@ClientHostName SYSNAME,
							@ConnectionID SYSNAME,@ClientAppName SYSNAME,@DurationFrom BIGINT,
							@DurationTo BIGINT,@Top INT,@Text NVARCHAR(MAX),@DatabaseName SYSNAME,
							@FromDate DATETIME2(3),@ToDate DATETIME2(3),@UserName SYSNAME,@Result SYSNAME,
							@SessionID INT,@InstanceDisplayName NVARCHAR(128)',
							@Instances,@ObjectName,@ClientHostName,@ConnectionID,@ClientAppName,
							@DurationFromUS,@DurationToUS,@Top,@Text,@DatabaseName,@FromDate,
							@ToDate,@UserName,@Result,@SessionID,@InstanceDisplayName