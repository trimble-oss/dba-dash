CREATE PROC [dbo].[CPU_Get](
	@InstanceID INT,
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL,
	@DateGrouping VARCHAR(50)='None'
)
AS
IF @FromDate IS NULL
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
IF @ToDate IS NULL
	SET @ToDate = GETUTCDATE()
DECLARE @SQL NVARCHAR(MAX)
DECLARE @DateGroupingSQL NVARCHAR(MAX)
SELECT @DateGroupingSQL= CASE WHEN @DateGrouping = 'None' THEN 'EventTime'
			WHEN @DateGrouping = '1MIN' THEN 'DATEADD(mi, DATEDIFF(mi, 0, DATEADD(s, 30, EventTime)), 0)'
			WHEN @DateGrouping ='10MIN' THEN 'CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,EventTime,120),15) + ''0'',120)'
			WHEN @DateGrouping = '60MIN' THEN 'CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,EventTime,120),13) + '':00'',120)'
			WHEN @DateGrouping = '120MIN' THEN 'DATEADD(hh,DATEPART(hh,EventTime) - DATEPART(hh,EventTime) % 2, CAST(CAST(EventTime AS DATE) AS DATETIME))'
			WHEN @DateGrouping ='DAY' THEN 'CAST(CAST(EventTime as DATE) as DATETIME)'
			ELSE NULL END

SET @SQL = N'
SELECT ' + @DateGroupingSQL + ' AS EventTime,
       SUM(SumSQLProcessCPU*1.0)/SUM(SampleCount*1.0) as SQLProcessCPU,
	   SUM(SumOtherCPU*1.0)/SUM(SampleCount*1.0) as OtherCPU,
	   MAX(MaxTotalCPU*1.0) as MaxCPU
FROM '+ CASE WHEN @DateGrouping IN('DAY','120MIN','60MIN') THEN 'dbo.CPU_60MIN' ELSE 'dbo.CPU' END + '
WHERE InstanceID = @InstanceID
AND EventTime >= @fromDate
AND EventTime < @ToDate
GROUP BY ' + @DateGroupingSQL + '
ORDER BY EventTime'

PRINT @SQL
EXEC sp_executesql @sql,N'@InstanceID INT,@FromDate DATETIME2(3),@ToDate DATETIME2(3)',@InstanceID,@FromDate,@ToDate