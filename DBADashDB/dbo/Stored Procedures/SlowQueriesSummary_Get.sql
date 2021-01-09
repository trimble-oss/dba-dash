CREATE PROC [dbo].[SlowQueriesSummary_Get](
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL,
	@ObjectName SYSNAME=NULL,
	@ClientHostName NVARCHAR(128)=NULL,
	@ConnectionID SYSNAME=NULL,
	@ClientAppName SYSNAME=NULL,
	@InstanceIDs VARCHAR(MAX)=NULL,
	@DurationFromSec BIGINT=NULL,
	@DurationToSec BIGINT=NULL,
	@GroupBy VARCHAR(50)='ConnectionID',
	@Text NVARCHAR(MAX)=NULL,
	@DatabaseName SYSNAME=NULL,
	@UserName SYSNAME=NULL,
	@Result SYSNAME=NULL,
	@Top INT=20
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
DECLARE @GroupSQL NVARCHAR(MAX) = CASE @GroupBy WHEN 'ConnectionID' THEN 'I.ConnectionID' 
												WHEN 'client_hostname' THEN 'SQ.client_hostname'  
												WHEN 'username' THEN 'SQ.username'
												WHEN 'object_name' THEN 'SQ.object_name'
												WHEN 'client_app_name' THEN 'SQ.client_app_name'
												WHEN 'DatabaseName' THEN 'D.name'
												WHEN 'Result' THEN 'SQ.Result'
												ELSE 'ConnectionID' END

DECLARE @SQL NVARCHAR(MAX)
SET @SQL = 
N'SELECT TOP(@Top) ' + @GroupSQL + ' as Grp,
		SUM(CASE WHEN Duration<5000000 THEN 1 ELSE 0 END) AS [1-5 seconds], 
		SUM(CASE WHEN Duration>=5000000 AND Duration < 10000000 THEN 1 ELSE 0 END) AS [5-10 seconds], 
		SUM(CASE WHEN Duration>=10000000 AND Duration < 20000000 THEN 1 ELSE 0 END) AS [10-20 seconds], 
		SUM(CASE WHEN Duration>=20000000 AND Duration < 30000000 THEN 1 ELSE 0 END) AS [20-30 seconds], 
		SUM(CASE WHEN Duration>=30000000 AND Duration < 60000000 THEN 1 ELSE 0 END) AS [30-60 seconds], 
		SUM(CASE WHEN Duration>=60000000 AND Duration < 300000000 THEN 1 ELSE 0 END) AS [1-5 minutes], 
		SUM(CASE WHEN Duration>=300000000 AND Duration < 600000000 THEN 1 ELSE 0 END) AS [5-10 minutes], 
		SUM(CASE WHEN Duration>=600000000 AND Duration < 1800000000 THEN 1 ELSE 0 END) AS [10-30 minutes], 
		SUM(CASE WHEN Duration>=1800000000 AND Duration < 3600000000 THEN 1 ELSE 0 END) AS [30-60 minutes], 
		SUM(CASE WHEN Duration>=3600000000 THEN 1 ELSE 0 END) AS [1hr+], 
		COUNT(*) Total,
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
' + CASE WHEN @ObjectName IS NULL THEN '' ELSE 'AND SQ.object_name = @ObjectName' END +'
' + CASE WHEN @ClientHostName IS NULL THEN '' ELSE 'AND SQ.client_hostname = @ClientHostName' END +'
' + CASE WHEN @ConnectionID IS NULL THEN '' ELSE 'AND I.ConnectionID = @ConnectionID' END + '
' + CASE WHEN @ClientAppName IS NULL THEN '' ELSE 'AND SQ.client_app_name = @ClientAppName' END + '
' + CASE WHEN @DurationFromUS IS NULL THEN '' ELSE 'AND SQ.Duration >= @DurationFrom' END + '
' + CASE WHEN @DurationToUS IS NULL THEN '' ELSE 'AND SQ.Duration < @DurationTo' END + '
' + CASE WHEN @Text IS NULL THEN '' ELSE 'AND SQ.Text LIKE ''%'' + @Text + ''%''' END + '
' + CASE WHEN @DatabaseName IS NULL THEN '' ELSE 'AND D.name = @DatabaseName' END + '
' + CASE WHEN @UserName IS NULL THEN '' ELSE 'AND SQ.username = @UserName' END + '
' + CASE WHEN @Result IS NULL THEN '' ELSE 'AND SQ.Result = @Result' END + '
GROUP BY ' + @GroupSQL +'
ORDER BY SUM(Duration) DESC'

EXEC sp_executesql @SQL,N'@Instances IDs READONLY,@ObjectName SYSNAME,@ClientHostName SYSNAME,
						@ConnectionID SYSNAME,@ClientAppName SYSNAME,@DurationFrom BIGINT,
						@DurationTo BIGINT,@Text NVARCHAR(MAX),@DatabaseName SYSNAME,
						@FromDate DATETIME2(3),@ToDate DATETIME2(3),@UserName SYSNAME,@Result SYSNAME,@Top INT',
						@Instances,@ObjectName,@ClientHostName,@ConnectionID,@ClientAppName,
						@DurationFromUS,@DurationToUS,@Text,@DatabaseName,@FromDate,@ToDate,@UserName,@Result,@Top