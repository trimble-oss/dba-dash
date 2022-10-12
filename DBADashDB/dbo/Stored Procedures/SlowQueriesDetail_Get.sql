CREATE PROC dbo.SlowQueriesDetail_Get(
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL,
	@ObjectName SYSNAME=NULL,
	@ClientHostName NVARCHAR(128)=NULL,
	@ConnectionID SYSNAME=NULL,
	@ClientAppName SYSNAME=NULL,
	@InstanceIDs VARCHAR(MAX)=NULL,
	@DurationFromSec BIGINT=NULL,
	@DurationToSec BIGINT=NULL,
	@DurationFromMs BIGINT=NULL,
	@DurationToMs BIGINT=NULL,
	@Text NVARCHAR(MAX)=NULL,
	@DatabaseName SYSNAME=NULL,
	@UserName SYSNAME=NULL,
	@Result SYSNAME=NULL,
	@Top INT = 30,
	@SessionID VARCHAR(MAX) =NULL,
	@Sort VARCHAR(50)='Duration',
	@SortDesc BIT = 1,
	@InstanceDisplayName NVARCHAR(128)=NULL,
	@ExcludeClientAppName SYSNAME=NULL,
	@ExcludeClientHostName SYSNAME=NULL,
	@ExcludeDatabaseName SYSNAME=NULL,
	@ExcludeInstanceDisplayName SYSNAME=NULL,
	@ExcludeObjectName SYSNAME=NULL,
	@ExcludeResult SYSNAME=NULL,
	@ExcludeSessionID VARCHAR(MAX)=NULL,
	@ExcludeText NVARCHAR(MAX)=NULL,
	@ExcludeUserName SYSNAME=NULL,
	@CPUFromMs BIGINT=NULL,
	@CPUToMs BIGINT=NULL,
	@PhysicalReadsFrom BIGINT=NULL,
	@PhysicalReadsTo BIGINT=NULL,
	@LogicalReadsFrom BIGINT=NULL,
	@LogicalReadsTo BIGINT=NULL,
	@WritesFrom BIGINT=NULL,
	@WritesTo BIGINT=NULL,
	@EventType SYSNAME=NULL,
	@ResultFailed BIT=NULL
)
AS
DECLARE @DurationFromUS BIGINT 
DECLARE @DurationToUS BIGINT
SELECT @DurationFromUS = (SELECT MAX(value) FROM (VALUES(@DurationFromSec*1000000),(@DurationFromMs*1000)) AS T(value)), 
	   @DurationToUS= (SELECT MIN(value) FROM (VALUES(@DurationToSec*1000000),(@DurationToMs*1000)) AS T(value))


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
' + CASE WHEN @ObjectName IS NULL THEN '' WHEN @ObjectName LIKE '%[%]%' THEN 'AND SQ.object_name LIKE @ObjectName' ELSE 'AND SQ.object_name = @ObjectName' END +'
' + CASE WHEN @ClientHostName IS NULL THEN '' WHEN @ClientHostName LIKE '%[%]%' THEN 'AND SQ.client_hostname LIKE @ClientHostName' ELSE 'AND SQ.client_hostname = @ClientHostName' END +'
' + CASE WHEN @ConnectionID IS NULL THEN '' ELSE 'AND I.ConnectionID = @ConnectionID' END + '
' + CASE WHEN @InstanceDisplayName IS NULL THEN '' WHEN @InstanceDisplayName LIKE '%[%]%'  THEN 'AND I.InstanceDisplayName LIKE @InstanceDisplayName'  ELSE 'AND I.InstanceDisplayName = @InstanceDisplayName' END + '
' + CASE WHEN @ClientAppName IS NULL THEN '' WHEN @ClientAppName LIKE '%[%]%' THEN 'AND SQ.client_app_name LIKE @ClientAppName' ELSE 'AND SQ.client_app_name = @ClientAppName' END + '
' + CASE WHEN @DurationFromUS IS NULL THEN '' ELSE 'AND SQ.Duration >= @DurationFrom' END + '
' + CASE WHEN @DurationToUS IS NULL THEN '' ELSE 'AND SQ.Duration < @DurationTo' END + '
' + CASE WHEN @Text IS NULL THEN '' ELSE 'AND SQ.Text LIKE ''%'' + @Text + ''%''' END + '
' + CASE WHEN @DatabaseName IS NULL THEN '' WHEN @DatabaseName LIKE '%[%]%' THEN 'AND D.name LIKE @DatabaseName' ELSE 'AND D.name = @DatabaseName' END + '
' + CASE WHEN @UserName IS NULL THEN '' WHEN @UserName LIKE '%[%]%'  THEN 'AND SQ.username LIKE @UserName' ELSE 'AND SQ.username = @UserName' END + '
' + CASE WHEN @Result IS NULL THEN '' WHEN @Result LIKE '%[%]%' THEN 'AND SQ.Result LIKE @Result' ELSE 'AND SQ.Result = @Result' END + '
' + CASE WHEN @SessionID IS NULL THEN '' WHEN @SessionID LIKE '%,%' THEN 'AND EXISTS(SELECT 1 FROM STRING_SPLIT(@SessionID,'','') SS WHERE CAST(SS.value AS INT) = SQ.session_id)' ELSE 'AND SQ.session_id = CAST(@SessionID AS INT)' END + '
' + CASE WHEN @ExcludeClientAppName IS NULL THEN '' WHEN @ExcludeClientAppName LIKE '%[%]%' THEN 'AND SQ.client_app_name NOT LIKE @ExcludeClientAppName' ELSE 'AND SQ.client_app_name <> @ExcludeClientAppName' END + '
' + CASE WHEN @ExcludeClientHostName IS NULL THEN '' WHEN @ExcludeClientHostName LIKE '%[%]%' THEN 'AND SQ.client_hostname NOT LIKE @ExcludeClientHostName'  ELSE 'AND SQ.client_hostname <> @ExcludeClientHostName' END + '
' + CASE WHEN @ExcludeDatabaseName IS NULL THEN '' WHEN @ExcludeDatabaseName LIKE '%[%]%' THEN 'AND D.name NOT LIKE @ExcludeDatabaseName' ELSE 'AND D.name <> @ExcludeDatabaseName' END + '
' + CASE WHEN @ExcludeInstanceDisplayName IS NULL THEN '' WHEN @ExcludeInstanceDisplayName LIKE '%[%]%' THEN 'AND I.InstanceDisplayName NOT LIKE @ExcludeInstanceDisplayName' ELSE 'AND I.InstanceDisplayName <> @ExcludeInstanceDisplayName' END + '
' + CASE WHEN @ExcludeObjectName IS NULL THEN '' WHEN @ExcludeObjectName LIKE '%[%]%' THEN 'AND SQ.object_name NOT LIKE @ExcludeObjectName' ELSE 'AND SQ.object_name <> @ExcludeObjectName' END + '
' + CASE WHEN @ExcludeResult IS NULL THEN '' WHEN @ExcludeResult LIKE '%[%]%' THEN 'AND SQ.Result NOT LIKE @ExcludeResult' ELSE 'AND SQ.Result <> @ExcludeResult' END + '
' + CASE WHEN @ExcludeSessionID IS NULL THEN '' WHEN @ExcludeSessionID LIKE '%,%' THEN 'AND NOT EXISTS(SELECT 1 FROM STRING_SPLIT(@ExcludeSessionID,'','') SS WHERE CAST(SS.value AS INT) = SQ.session_id)' ELSE 'AND SQ.session_id <> CAST(@ExcludeSessionID AS INT)' END + '
' + CASE WHEN @ExcludeText IS NULL THEN '' ELSE 'AND SQ.Text NOT LIKE ''%'' + @ExcludeText + ''%''' END + '
' + CASE WHEN @ExcludeUserName IS NULL THEN '' WHEN @ExcludeUserName LIKE '%[%]%' THEN 'AND SQ.username NOT LIKE @ExcludeUserName' ELSE 'AND SQ.username <> @ExcludeUserName' END + '
' + CASE WHEN @CPUFromMs IS NULL THEN '' ELSE 'AND SQ.cpu_time >= (@CPUFromMs*1000)' END + '
' + CASE WHEN @CPUToMs IS NULL THEN '' ELSE 'AND SQ.cpu_time < (@CPUToMs*1000)' END + '
' + CASE WHEN @PhysicalReadsFrom IS NULL THEN '' ELSE 'AND SQ.physical_reads >= @PhysicalReadsFrom' END + '
' + CASE WHEN @PhysicalReadsTo IS NULL THEN '' ELSE 'AND SQ.physical_reads < @PhysicalReadsTo' END + '
' + CASE WHEN @LogicalReadsFrom IS NULL THEN '' ELSE 'AND SQ.logical_reads >= @LogicalReadsFrom' END + '
' + CASE WHEN @LogicalReadsTo IS NULL THEN '' ELSE 'AND SQ.logical_reads < @LogicalReadsTo' END + '
' + CASE WHEN @WritesFrom IS NULL THEN '' ELSE 'AND SQ.writes >= @WritesFrom' END + '
' + CASE WHEN @WritesTo IS NULL THEN '' ELSE 'AND SQ.writes < @WritesTo' END + '
' + CASE WHEN @EventType IS NULL THEN '' ELSE 'AND SQ.event_type = @EventType' END + '
' + CASE WHEN @ResultFailed IS NULL THEN '' WHEN @ResultFailed = 1 THEN 'AND SQ.result <> ''0 - OK'''  ELSE 'AND SQ.result = ''0 - OK''' END + '
' + @SortSQL

EXEC sp_executesql @SQL,N'@Instances IDs READONLY,
							@ObjectName SYSNAME,
							@ClientHostName SYSNAME,
							@ConnectionID SYSNAME,
							@ClientAppName SYSNAME,
							@DurationFrom BIGINT,
							@DurationTo BIGINT,
							@Top INT,
							@Text NVARCHAR(MAX),
							@DatabaseName SYSNAME,
							@FromDate DATETIME2(3),
							@ToDate DATETIME2(3),
							@UserName SYSNAME,
							@Result SYSNAME,
							@SessionID VARCHAR(MAX),
							@InstanceDisplayName NVARCHAR(128),
							@ExcludeClientAppName SYSNAME,
							@ExcludeClientHostName SYSNAME,
							@ExcludeDatabaseName SYSNAME,
							@ExcludeInstanceDisplayName NVARCHAR(128), 
							@ExcludeObjectName SYSNAME,
							@ExcludeResult SYSNAME,
							@ExcludeSessionID VARCHAR(MAX),
							@ExcludeText NVARCHAR(MAX),
							@ExcludeUserName SYSNAME,
							@CPUFromMs BIGINT,
							@CPUToMs BIGINT,
							@PhysicalReadsFrom BIGINT,
							@PhysicalReadsTo BIGINT,
							@LogicalReadsFrom BIGINT,
							@LogicalReadsTo BIGINT,
							@WritesFrom BIGINT,
							@WritesTo BIGINT,
							@EventType SYSNAME',
							@Instances,
							@ObjectName,
							@ClientHostName,
							@ConnectionID,
							@ClientAppName,
							@DurationFromUS,
							@DurationToUS,
							@Top,@Text,
							@DatabaseName,
							@FromDate,
							@ToDate,
							@UserName,
							@Result,
							@SessionID,
							@InstanceDisplayName,
							@ExcludeClientAppName,
							@ExcludeClientHostName,
							@ExcludeDatabaseName,
							@ExcludeInstanceDisplayName,
							@ExcludeObjectName,
							@ExcludeResult,
							@ExcludeSessionID,
							@ExcludeText,
							@ExcludeUserName,
							@CPUFromMs,
							@CPUToMs,
							@PhysicalReadsFrom,
							@PhysicalReadsTo,
							@LogicalReadsFrom,
							@LogicalReadsTo,
							@WritesFrom,
							@WritesTo,
							@EventType