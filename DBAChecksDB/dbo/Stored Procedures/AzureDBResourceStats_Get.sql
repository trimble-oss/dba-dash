CREATE PROC [dbo].[AzureDBResourceStats_Get](
	@InstanceID INT,
	@FromDate DATETIME2(3),
	@ToDate DATETIME2(3),
	@DateGroupingMin INT=NULL, 
	@UTCOffset INT=0
)
AS
SELECT @FromDate= DATEADD(mi, -@UTCOffset, @FromDate),
	@ToDate = DATEADD(mi, -@UTCOffset, @ToDate) 

DECLARE @DateTimeCol VARCHAR(MAX) ='DATEADD(mi, @UTCOffset, RS.end_time)' 
DECLARE @SQL NVARCHAR(MAX)
DECLARE @DateGroupingSQL NVARCHAR(MAX)
DECLARE @DateGroupingJoin NVARCHAR(MAX)
SELECT @DateGroupingSQL= CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin=0 THEN 'DATEADD(mi, @UTCOffset, RS.end_time)'
			ELSE 'DG.DateGroup' END,
			@DateGroupingJoin= CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin=0 THEN ''
			ELSE 'CROSS APPLY dbo.DateGroupingMins(DATEADD(mi, @UTCOffset, RS.end_time),@DateGroupingMin) DG' END
SELECT @DateGroupingSQL = REPLACE(@DateGroupingSQL,'@DateTimeCol',@DateTimeCol)

SET @SQL = N'
SELECT ' + @DateGroupingSQL + ' as end_time,
       AVG(RS.avg_cpu_percent) as avg_cpu_percent,
       AVG(RS.avg_data_io_percent) as avg_data_io_percent,
       AVG(RS.avg_log_write_percent) as avg_log_write_percent,
       AVG(RS.avg_memory_usage_percent) as avg_memory_usage_percent,
       MAX(RS.xtp_storage_percent) as xtp_storage_percent,
       MAX(RS.max_worker_percent) as max_worker_percent,
       MAX(RS.max_session_percent) as max_session_percent,
       MAX(RS.dtu_limit) as dtu_limit,
       AVG(RS.avg_instance_cpu_percent) avg_instance_cpu_percent,
       AVG(RS.avg_instance_memory_percent) avg_instance_memory_percent,
       MAX(RS.cpu_limit) as cpu_limit,
	   AVG(DTU.AvgDTUPercent) as AvgDTUPercent,
	   AVG(DTU.AvgDTUsUsed) as AvgDTUsUsed,
	   MAX(RS.avg_cpu_percent) as max_cpu_percent,
       MAX(RS.avg_data_io_percent) as max_data_io_percent,
       MAX(RS.avg_log_write_percent) as max_log_write_percent,
       MAX(RS.avg_memory_usage_percent) as max_memory_usage_percent,
	   MAX(DTU.AvgDTUPercent) as MaxDTUPercent,
	   MAX(DTU.AvgDTUsUsed) as MaxDTUsUsed,
	   MAX(RS.avg_instance_cpu_percent) max_instance_cpu_percent,
       MAX(RS.avg_instance_memory_percent) max_instance_memory_percent
FROM dbo.AzureDBResourceStats RS
' + @DateGroupingJoin + '
OUTER APPLY(SELECT 	CASE WHEN dtu_limit>0 THEN (SELECT Max(v) FROM (VALUES (avg_cpu_percent), (avg_data_io_percent), (avg_log_write_percent)) AS value(v)) ELSE NULL END AS [AvgDTUPercent],
					CASE WHEN dtu_limit>0 THEN ((dtu_limit)*((SELECT Max(v) FROM (VALUES (avg_cpu_percent), (avg_data_io_percent), (avg_log_write_percent)) AS value(v))/100.00)) ELSE NULL END AS [AvgDTUsUsed]
			) AS DTU
WHERE RS.InstanceID = @InstanceID
AND RS.end_time>=@FromDate 
AND RS.end_time<@ToDate
GROUP BY ' + @DateGroupingSQL +'
ORDER BY end_time'

PRINT @SQL
EXEC sp_executesql @sql, N'@InstanceID INT,@FromDate DATETIME2(3),@ToDate DATETIME2(3),@UTCOffset INT,@DateGroupingMin INT',@InstanceID,@FromDate,@ToDate,@UTCOffset,@DateGroupingMin