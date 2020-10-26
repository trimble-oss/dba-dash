CREATE PROC [dbo].[CPU_Get](
	@InstanceID INT,
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL,
	@DateGroupingMin INT=NULL
)
AS
IF @FromDate IS NULL
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
IF @ToDate IS NULL
	SET @ToDate = GETUTCDATE()
DECLARE @SQL NVARCHAR(MAX)
DECLARE @DateGroupingSQL NVARCHAR(MAX)
SELECT @DateGroupingSQL= CASE WHEN @DateGroupingMin = 0 OR @DateGroupingMin IS NULL THEN 'EventTime'
			ELSE 'DateGroup' END

SET @SQL = N'
SELECT ' + @DateGroupingSQL + ' AS EventTime,
       SUM(SumSQLProcessCPU*1.0)/SUM(SampleCount*1.0) as SQLProcessCPU,
	   SUM(SumOtherCPU*1.0)/SUM(SampleCount*1.0) as OtherCPU,
	   MAX(MaxTotalCPU*1.0) as MaxCPU
FROM '+ CASE WHEN @DateGroupingMin >=60 THEN 'dbo.CPU_60MIN' ELSE 'dbo.CPU' END + '
' + CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin= 0 THEN '' ELSE 'CROSS APPLY dbo.DateGroupingMins(EventTime,@DateGroupingMin)' END + '
WHERE InstanceID = @InstanceID
AND EventTime >= @fromDate
AND EventTime < @ToDate
GROUP BY ' + @DateGroupingSQL + '
ORDER BY EventTime'

PRINT @SQL
EXEC sp_executesql @sql,N'@InstanceID INT,@FromDate DATETIME2(3),@ToDate DATETIME2(3),@DateGroupingMin INT',@InstanceID,@FromDate,@ToDate,@DateGroupingMin