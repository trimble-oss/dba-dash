CREATE PROC dbo.CPU_Get(
	@InstanceID INT,
	@FromDate DATETIME2(3)=NULL, /* UTC */
	@ToDate DATETIME2(3)=NULL, /* UTC */
	@DateGroupingMin INT=NULL, /* How many minutes to group by.  */
	@UTCOffset INT=0, /* Used for Hours filter */
	@DaysOfWeek IDs READONLY, /* e.g. 1=Monday. exclude weekends:  1,2,3,4,5.  Filter applied in local timezone (@UTCOffset) */
	@Hours IDs READONLY/* e.g. 9 to 5 :  9,10,11,12,13,14,15,16. Filter applied in local timezone (@UTCOffset)*/
)
AS
SET DATEFIRST 1 /* Start week on Monday */
IF @FromDate IS NULL
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
IF @ToDate IS NULL
	SET @ToDate = GETUTCDATE()
DECLARE @SQL NVARCHAR(MAX)
DECLARE @DateGroupingSQL NVARCHAR(MAX)
SELECT @DateGroupingSQL= CASE WHEN @DateGroupingMin = 0 OR @DateGroupingMin IS NULL THEN 'EventTime'
			ELSE 'DateGroup' END


/* Generate CSV list from list of integer values (safe from SQL injection compared to passing in a CSV string) */
DECLARE @DaysOfWeekCsv NVARCHAR(MAX)
SELECT @DaysOfWeekCsv =  STUFF((SELECT ',' + CAST(ID AS VARCHAR)
FROM @DaysOfWeek
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')

/* Generate CSV list from list of integer values (safe from SQL injection compared to passing in a CSV string) */
DECLARE @HoursCsv NVARCHAR(MAX)
SELECT @HoursCsv =  STUFF((SELECT ',' + CAST(ID AS VARCHAR)
FROM @Hours
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')

SET @SQL = N'
SELECT ' + @DateGroupingSQL + ' AS EventTime,
       SUM(SumSQLProcessCPU*1.0)/SUM(SampleCount*1.0) as SQLProcessCPU,
	   SUM(SumOtherCPU*1.0)/SUM(SampleCount*1.0) as OtherCPU,
	   MAX(MaxTotalCPU*1.0) as MaxCPU
FROM '+ CASE WHEN @DateGroupingMin >=60 THEN 'dbo.CPU_60MIN' ELSE 'dbo.CPU' END + '
' + CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin= 0 THEN '' ELSE 'CROSS APPLY dbo.DateGroupingMins(EventTime,@DateGroupingMin)' END + '
WHERE InstanceID = @InstanceID
AND EventTime >= @FromDate
AND EventTime < @ToDate
' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, EventTime)) IN (' + @DaysOfWeekCsv + ')' END + '
' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, EventTime)) IN(' + @HoursCsv + ')' END + '
GROUP BY ' + @DateGroupingSQL + '
ORDER BY EventTime'

PRINT @SQL
EXEC sp_executesql @SQL,
				N'@InstanceID INT,
				@FromDate DATETIME2(3),
				@ToDate DATETIME2(3),
				@DateGroupingMin INT,
				@UTCOffset INT',
				@InstanceID,
				@FromDate,
				@ToDate,
				@DateGroupingMin,
				@UTCOffset