CREATE PROC dbo.CPU_Get(
	@InstanceID INT,
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL,
	@DateGrouping VARCHAR(50)='None',
	@Agg VARCHAR(50)='MAX'
)
AS
IF @FromDate IS NULL
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
IF @ToDate IS NULL
	SET @ToDate = GETUTCDATE()
DECLARE @SQL NVARCHAR(MAX)
DECLARE @DateGroupingSQL NVARCHAR(MAX)
DECLARE @AggSQL NVARCHAR(MAX) 
SELECT @AggSQL = CASE WHEN @Agg = 'MAX' THEN 'MAX' WHEN @Agg = 'AVG' THEN 'AVG'  ELSE NULL END
SELECT @DateGroupingSQL= CASE WHEN @DateGrouping = 'None' THEN 'EventTime'
			WHEN @DateGrouping ='10MIN' THEN 'CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,EventTime,120),15) + ''0'',120)'
			WHEN @DateGrouping = '60MIN' THEN 'CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,EventTime,120),13) + '':00'',120)'
			WHEN @DateGrouping ='DAY' THEN 'CAST(CAST(EventTime as DATE) as DATETIME)'
			ELSE NULL END

SET @SQL = N'
SELECT ' + @DateGroupingSQL + ' AS EventTime,
       ' + @AggSQL + '(SQLProcessCPU) SQLProcessCPU,
       ' + @AggSQL + '(SystemIdleCPU) SystemIdleCPU,
	    100-' + @AggSQL + '(SQLProcessCPU+SystemIdleCPU) as OtherCPU
FROM dbo.CPU
WHERE InstanceID = @InstanceID
AND EventTime>=@fromDate
AND EventTime < @ToDate
GROUP BY ' + @DateGroupingSQL + '
ORDER BY EventTime'

PRINT @SQL
EXEC sp_executesql @sql,N'@InstanceID INT,@FromDate DATETIME2(3),@ToDate DATETIME2(3)',@InstanceID,@FromDate,@ToDate