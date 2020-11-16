CREATE PROC [dbo].[AzureDBElasticPoolResourceStats_Get](
	@InstanceID INT,
	@elastic_pool_name SYSNAME,
	@FromDate DATETIME2(3),
	@ToDate DATETIME2(3),
	@DateGroupingMin INT=NULL, 
	@UTCOffset INT=0
) 
AS
SELECT @InstanceID =MasterInstanceID 
FROM dbo.AzureDBMasterInstance(@InstanceID)
SELECT @FromDate= DATEADD(mi, -@UTCOffset, @FromDate),
	@ToDate = DATEADD(mi, -@UTCOffset, @ToDate) 


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
	   AVG(DTU.AvgDTUPercent) as AvgDTUPercent,
	   AVG(DTU.AvgDTUsUsed) as AvgDTUsUsed,
	   MAX(RS.avg_cpu_percent) as max_cpu_percent,
       MAX(RS.avg_data_io_percent) as max_data_io_percent,
       MAX(RS.avg_log_write_percent) as max_log_write_percent,
	   MAX(DTU.AvgDTUPercent) as MaxDTUPercent,
	   MAX(DTU.AvgDTUsUsed) as MaxDTUsUsed
FROM dbo.AzureDBElasticPoolResourceStats RS
JOIN dbo.AzureDBElasticPool EP ON RS.PoolID = EP.PoolID
' + @DateGroupingJoin + '
OUTER APPLY(SELECT 	CASE WHEN RS.elastic_pool_dtu_limit>0 THEN (SELECT Max(v) FROM (VALUES (RS.avg_cpu_percent), (RS.avg_data_io_percent), (RS.avg_log_write_percent)) AS value(v)) ELSE NULL END AS [AvgDTUPercent],
					CASE WHEN RS.elastic_pool_dtu_limit>0 THEN ((RS.elastic_pool_dtu_limit)*((SELECT Max(v) FROM (VALUES (RS.avg_cpu_percent), (RS.avg_data_io_percent), (RS.avg_log_write_percent)) AS value(v))/100.00)) ELSE NULL END AS [AvgDTUsUsed]
			) AS DTU
WHERE EP.InstanceID = @InstanceID
AND RS.end_time>=@FromDate 
AND RS.end_time<@ToDate
AND EP.elastic_pool_name=@elastic_pool_name
GROUP BY ' + @DateGroupingSQL +'
ORDER BY end_time'

PRINT @SQL
EXEC sp_executesql @sql, N'@InstanceID INT,@FromDate DATETIME2(3),@ToDate DATETIME2(3),@UTCOffset INT,@elastic_pool_name SYSNAME,@DateGroupingMin INT',@InstanceID,@FromDate,@ToDate,@UTCOffset,@elastic_pool_name,@DateGroupingMin