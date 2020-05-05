CREATE PROC report.SlowQueriesDetail(
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
	@Top INT = 30
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
	SELECT Item
	FROM dbo.SplitStrings(@InstanceIDs,',')
END;


DECLARE @SQL NVARCHAR(MAX)
SET @SQL = 
N'SELECT TOP(@Top) SQ.InstanceID,
       SQ.DatabaseID,
	   I.Instance,
	   D.name as DatabaseName,
       SQ.event_type,
       SQ.object_name,
       SQ.timestamp,
       SQ.duration,
       SQ.cpu_time,
       SQ.logical_reads,
       SQ.physical_reads,
       SQ.writes,
       SQ.username,
       SQ.text,
       SQ.client_hostname,
       SQ.client_app_name,
       SQ.result,
       SQ.Uniqueifier
FROM dbo.SlowQueries SQ
JOIN dbo.Instances I ON I.InstanceID = SQ.InstanceID
LEFT JOIN dbo.Databases D ON D.DatabaseID = SQ.DatabaseID
WHERE timestamp>= @FromDate
AND timestamp< @ToDate
' + CASE WHEN @InstanceIDs IS NULL THEN '' ELSE 'AND EXISTS(SELECT 1 FROM @Instances IDs WHERE IDs.ID = SQ.InstanceID)' END + '
' + CASE WHEN @ObjectName IS NULL THEN '' ELSE 'AND SQ.object_name = @ObjectName' END +'
' + CASE WHEN @ClientHostName IS NULL THEN '' ELSE 'AND SQ.client_hostname = @ClientHostName' END +'
' + CASE WHEN @ConnectionID IS NULL THEN '' ELSE 'AND I.ConnectionID = @ConnectionID' END + '
' + CASE WHEN @ClientAppName IS NULL THEN '' ELSE 'AND SQ.client_app_name = @ClientAppName' END + '
' + CASE WHEN @DurationFromUS IS NULL THEN '' ELSE 'AND SQ.Duration >= @DurationFrom' END + '
' + CASE WHEN @DurationToUS IS NULL THEN '' ELSE 'AND SQ.Duration < @DurationTo' END + '
' + CASE WHEN @Text IS NULL THEN '' ELSE 'AND SQ.Text LIKE ''%'' + @Text + ''%''' END + '
' + CASE WHEN @DatabaseName IS NULL THEN '' ELSE 'AND D.name = @DatabaseName' END + '
ORDER BY SQ.Duration DESC'

EXEC sp_executesql @sql,N'@Instances IDs READONLY,@ObjectName SYSNAME,@ClientHostName SYSNAME,
							@ConnectionID SYSNAME,@ClientAppName SYSNAME,@DurationFrom BIGINT,
							@DurationTo BIGINT,@Top INT,@Text NVARCHAR(MAX),@DatabaseName SYSNAME,
							@FromDate DATETIME2(3),@ToDate DATETIME2(3)',
							@Instances,@ObjectName,@ClientHostName,@ConnectionID,@ClientAppName,
							@DurationFromUS,@DurationToUS,@Top,@Text,@DatabaseName,@FromDate,@ToDate