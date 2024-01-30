CREATE PROC dbo.SlowQueriesSummary_Get(
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
	@GroupBy VARCHAR(50)='InstanceDisplayName',
	@Text NVARCHAR(MAX)=NULL,
	@DatabaseName SYSNAME=NULL,
	@UserName SYSNAME=NULL,
	@Result SYSNAME=NULL,
	@Top INT=20,
	@SessionID VARCHAR(MAX)=NULL, /* Comma-separated list supported */
	@InstanceDisplayName NVARCHAR(128)=NULL,
	@ExcludeClientAppName SYSNAME=NULL,
	@ExcludeClientHostName SYSNAME=NULL,
	@ExcludeDatabaseName SYSNAME=NULL,
	@ExcludeInstanceDisplayName NVARCHAR(128)=NULL,
	@ExcludeObjectName SYSNAME=NULL,
	@ExcludeResult SYSNAME=NULL,
	@ExcludeSessionID VARCHAR(MAX)=NULL, /* Comma-separated list supported */
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
	@Debug BIT=0,
	@ShowHidden BIT=1,
	@Metric VARCHAR(50)='duration', /* duration or cpu_time */
	@ContextInfo VARBINARY(128)=NULL,
	@ExcludeContextInfo VARBINARY(128)=NULL
)
AS
DECLARE @DurationFromUS BIGINT 
DECLARE @DurationToUS BIGINT
SELECT @DurationFromUS = (SELECT MAX(value) FROM (VALUES(@DurationFromSec*1000000),(@DurationFromMs*1000)) AS T(value)), 
	   @DurationToUS= (SELECT MIN(value) FROM (VALUES(@DurationToSec*1000000),(@DurationToMs*1000)) AS T(value))

IF @Metric NOT IN('duration','cpu_time')
BEGIN
	RAISERROR('Invalid @Metric value.  Valid options: duration, cpu_time',11,1);
	RETURN;
END

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
SET @ExcludeSessionID = REPLACE(@ExcludeSessionID,' ','')
SET @SessionID = REPLACE(@SessionID,' ','')
SET @Text = REPLACE(REPLACE(REPLACE(@Text,'[','[[]'),'_','[_]'),'%','[%]') -- Like encode input

DECLARE @TimestampMins INT
DECLARE @TimestampOffset INT
IF @GroupBy LIKE 'timestamp%'
BEGIN
	SELECT	@TimestampMins = PARSENAME(@GroupBy,2), 
			@TimestampOffset = PARSENAME(@GroupBy,1),
			@GroupBy = 'timestamp'
END


DECLARE @GroupSQL NVARCHAR(MAX) = CASE @GroupBy WHEN 'ConnectionID' THEN 'I.ConnectionID' 
												WHEN 'client_hostname' THEN 'SQ.client_hostname'  
												WHEN 'username' THEN 'SQ.username'
												WHEN 'object_name' THEN 'SQ.object_name'
												WHEN 'client_app_name' THEN 'SQ.client_app_name'
												WHEN 'DatabaseName' THEN 'D.name'
												WHEN 'Result' THEN 'SQ.Result'
												WHEN 'text' THEN 'LEFT(SQ.text,1000)'
												WHEN 'session_id' THEN 'SQ.session_id'
												WHEN 'InstanceDisplayName' THEN 'I.InstanceDisplayName' 
												WHEN 'EventType' THEN 'SQ.event_type'
												WHEN 'timestamp' THEN 'DATEADD(MINUTE, (DATEDIFF(MINUTE, 0, DATEADD(mi,@TimestampOffset,SQ.timestamp)) / @TimeStampMins) * @TimeStampMins, 0)'
												WHEN 'context_info' THEN 'CONVERT(VARCHAR(258),SQ.context_info,1)'
												ELSE 'InstanceDisplayName' END
IF @Top<=0
BEGIN
	SET @Top=NULL;
END
DECLARE @SQL NVARCHAR(MAX)
SET @SQL = 
N'SELECT ' + CASE WHEN @Top IS NOT NULL THEN 'TOP(@Top) ' ELSE '' END + @GroupSQL + ' as Grp,
		SUM(CASE WHEN {Metric}<5000000 THEN 1 ELSE 0 END) AS [<5 seconds], 
		SUM(CASE WHEN {Metric}>=5000000 AND {Metric} < 10000000 THEN 1 ELSE 0 END) AS [5-10 seconds], 
		SUM(CASE WHEN {Metric}>=10000000 AND {Metric} < 20000000 THEN 1 ELSE 0 END) AS [10-20 seconds], 
		SUM(CASE WHEN {Metric}>=20000000 AND {Metric} < 30000000 THEN 1 ELSE 0 END) AS [20-30 seconds], 
		SUM(CASE WHEN {Metric}>=30000000 AND {Metric}< 60000000 THEN 1 ELSE 0 END) AS [30-60 seconds], 
		SUM(CASE WHEN {Metric}>=60000000 AND {Metric} < 300000000 THEN 1 ELSE 0 END) AS [1-5 minutes], 
		SUM(CASE WHEN {Metric}>=300000000 AND {Metric} < 600000000 THEN 1 ELSE 0 END) AS [5-10 minutes], 
		SUM(CASE WHEN {Metric}>=600000000 AND {Metric} < 1800000000 THEN 1 ELSE 0 END) AS [10-30 minutes], 
		SUM(CASE WHEN {Metric}>=1800000000 AND {Metric} < 3600000000 THEN 1 ELSE 0 END) AS [30-60 minutes], 
		SUM(CASE WHEN {Metric}>=3600000000 THEN 1 ELSE 0 END) AS [1hr+], 
		COUNT(*) Total,
		SUM(CASE WHEN result <> ''0 - OK'' THEN 1 ELSE 0 END) AS FailedCount,
		SUM(Duration) as TotalDuration,
		SUM(cpu_time) as TotalCPU,
		SUM(logical_reads+Writes) as TotalIO,
		SUM(physical_reads+writes) as TotalPhysicalIO
FROM dbo.SlowQueries SQ
JOIN dbo.Instances I ON I.InstanceID = SQ.InstanceID
LEFT JOIN dbo.Databases D ON D.DatabaseID = SQ.DatabaseID
WHERE timestamp>= @FromDate
AND timestamp< @ToDate
' + CASE WHEN @InstanceIDs IS NULL THEN '' ELSE 'AND EXISTS(SELECT 1 FROM @Instances IDs WHERE IDs.ID = SQ.InstanceID)' END + '
' + CASE WHEN @ObjectName IS NULL THEN '' WHEN @ObjectName LIKE '%[%]%' THEN 'AND SQ.object_name LIKE @ObjectName' ELSE 'AND SQ.object_name = @ObjectName' END +'
' + CASE WHEN @ClientHostName IS NULL THEN '' WHEN @ClientHostName LIKE '%[%]%' THEN 'AND SQ.client_hostname LIKE @ClientHostName' ELSE 'AND SQ.client_hostname = @ClientHostName' END +'
' + CASE WHEN @ConnectionID IS NULL THEN '' ELSE 'AND I.ConnectionID = @ConnectionID' END + '
' + CASE WHEN @InstanceDisplayName IS NULL THEN '' WHEN @InstanceDisplayName LIKE '%[%]%' THEN 'AND I.InstanceDisplayName LIKE @InstanceDisplayName'  ELSE 'AND I.InstanceDisplayName = @InstanceDisplayName' END + '
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
' + CASE WHEN @ShowHidden=1 THEN '' ELSE 'AND I.ShowInSummary=1' END + '
' + CASE WHEN @ContextInfo IS NULL THEN '' ELSE 'AND SQ.context_info = @ContextInfo' END + '
' + CASE WHEN @ExcludeContextInfo IS NULL THEN '' ELSE 'AND (SQ.context_info <> @ExcludeContextInfo OR SQ.context_info IS NULL)' END + '
GROUP BY ' + @GroupSQL +'
ORDER BY ' + CASE WHEN @GroupBy ='timestamp' THEN 'Grp DESC' ELSE 'SUM(Duration) DESC' END

SET @SQL = REPLACE(@SQL,'{Metric}',CASE WHEN @Metric = 'cpu_time' THEN 'cpu_time' ELSE 'Duration' END)

IF @Debug=1
	PRINT @SQL

EXEC sp_executesql @SQL,N'@Instances IDs READONLY,
						@ObjectName SYSNAME,
						@ClientHostName SYSNAME,
						@ConnectionID SYSNAME,
						@ClientAppName SYSNAME,
						@DurationFrom BIGINT,
						@DurationTo BIGINT,
						@Text NVARCHAR(MAX),
						@DatabaseName SYSNAME,
						@FromDate DATETIME2(3),
						@ToDate DATETIME2(3),
						@UserName SYSNAME,
						@Result SYSNAME,@Top INT,
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
						@EventType SYSNAME,
						@TimestampMins INT,
						@TimestampOffset INT,
						@ContextInfo VARBINARY(128),
						@ExcludeContextInfo VARBINARY(128)',
						@Instances,
						@ObjectName,
						@ClientHostName,
						@ConnectionID,
						@ClientAppName,
						@DurationFromUS,
						@DurationToUS,
						@Text,
						@DatabaseName,
						@FromDate,
						@ToDate,
						@UserName,
						@Result,
						@Top,
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
						@EventType,
						@TimestampMins,
						@TimestampOffset,
						@ContextInfo,
						@ExcludeContextInfo