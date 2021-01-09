CREATE PROC [dbo].[AzureDBElasticPoolResourceStats_Get](
	@InstanceID INT,
	@elastic_pool_name SYSNAME,
	@FromDate DATETIME2(3),
	@ToDate DATETIME2(3),
	@DateGroupingMin INT=NULL, 
	@UTCOffset INT=0,
	@Use60MIN BIT=NULL
) 
AS
SELECT @InstanceID =MasterInstanceID 
FROM dbo.AzureDBMasterInstance(@InstanceID)
SELECT @FromDate= DATEADD(mi, -@UTCOffset, @FromDate),
	@ToDate = DATEADD(mi, -@UTCOffset, @ToDate) 

IF @Use60MIN IS NULL
BEGIN
	SELECT @Use60MIN = CASE WHEN @DateGroupingMin>=60 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
END


DECLARE @SQL NVARCHAR(MAX)
DECLARE @DateGroupingSQL NVARCHAR(MAX)
DECLARE @DateGroupingJoin NVARCHAR(MAX)
SELECT @DateGroupingSQL= CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin=0 THEN 'DATEADD(mi, @UTCOffset, RS.end_time)'
			ELSE 'DG.DateGroup' END,
		@DateGroupingJoin = CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin=0 THEN '' ELSE 'CROSS APPLY dbo.DateGroupingMins(DATEADD(mi, @UTCOffset, RS.end_time),@DateGroupingMin) DG' END

SET @SQL = N'
SELECT ' + @DateGroupingSQL + ' as end_time,
       AVG(RS.avg_cpu_percent) as avg_cpu_percent,
       AVG(RS.avg_data_io_percent) as avg_data_io_percent,
       AVG(RS.avg_log_write_percent) as avg_log_write_percent,
       MAX(RS.max_worker_percent) as max_worker_percent,
       MAX(RS.max_session_percent) as max_session_percent,
       MAX(RS.elastic_pool_dtu_limit) as dtu_limit,
	   AVG(RS.avg_dtu_percent) as AvgDTUPercent,
	   AVG(RS.avg_dtu) as AvgDTUsUsed,
	   MAX(RS.max_cpu_percent) as max_cpu_percent,
       MAX(RS.max_data_io_percent) as max_data_io_percent,
       MAX(RS.max_log_write_percent) as max_log_write_percent,
	   MAX(RS.max_dtu_percent) as MaxDTUPercent,
	   MAX(RS.max_dtu) as MaxDTUsUsed
FROM ' + CASE WHEN @Use60MIN=1 THEN 'dbo.AzureDBElasticPoolResourceStats_60MIN' ELSE 'dbo.AzureDBElasticPoolResourceStats_Raw' END + ' RS
JOIN dbo.AzureDBElasticPool EP ON RS.PoolID = EP.PoolID
' + @DateGroupingJoin + '
WHERE EP.InstanceID = @InstanceID
AND RS.end_time>=@FromDate 
AND RS.end_time<@ToDate
AND EP.elastic_pool_name=@elastic_pool_name
GROUP BY ' + @DateGroupingSQL +'
ORDER BY end_time'

PRINT @SQL
EXEC sp_executesql @SQL, N'@InstanceID INT,@FromDate DATETIME2(3),@ToDate DATETIME2(3),@UTCOffset INT,@elastic_pool_name SYSNAME,@DateGroupingMin INT',@InstanceID,@FromDate,@ToDate,@UTCOffset,@elastic_pool_name,@DateGroupingMin