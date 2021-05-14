CREATE PROC [dbo].[AzureDBResourceStats_Get](
	@InstanceID INT,
	@FromDate DATETIME2(3),
	@ToDate DATETIME2(3),
	@DateGroupingMin INT=NULL, 
	@UTCOffset INT=0
)
AS
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
       MAX(RS.max_xtp_storage_percent) as xtp_storage_percent,
       MAX(RS.max_worker_percent) as max_worker_percent,
       MAX(RS.max_session_percent) as max_session_percent,
       MAX(RS.dtu_limit) as dtu_limit,
       AVG(RS.avg_instance_cpu_percent) avg_instance_cpu_percent,
       AVG(RS.avg_instance_memory_percent) avg_instance_memory_percent,
       MAX(RS.cpu_limit) as cpu_limit,
	   AVG(avg_dtu_percent) as AvgDTUPercent,
	   AVG(avg_dtu) as AvgDTUsUsed,
	   MAX(RS.max_cpu_percent) as max_cpu_percent,
       MAX(RS.max_data_io_percent) as max_data_io_percent,
       MAX(RS.max_log_write_percent) as max_log_write_percent,
       MAX(RS.max_memory_usage_percent) as max_memory_usage_percent,
	   MAX(RS.max_dtu_percent) as MaxDTUPercent,
	   MAX(RS.max_dtu) as MaxDTUsUsed,
	   MAX(RS.max_instance_cpu_percent) max_instance_cpu_percent,
       MAX(RS.max_instance_memory_percent) max_instance_memory_percent
FROM ' + CASE WHEN @DateGroupingMin>=60 THEN 'dbo.AzureDBResourceStats_60MIN' ELSE ' dbo.AzureDBResourceStats_Raw' END + ' RS
' + @DateGroupingJoin + '
WHERE RS.InstanceID = @InstanceID
AND RS.end_time>=@FromDate 
AND RS.end_time<@ToDate
GROUP BY ' + @DateGroupingSQL +'
ORDER BY end_time'

PRINT @SQL
EXEC sp_executesql @SQL, N'@InstanceID INT,@FromDate DATETIME2(3),@ToDate DATETIME2(3),@UTCOffset INT,@DateGroupingMin INT',@InstanceID,@FromDate,@ToDate,@UTCOffset,@DateGroupingMin