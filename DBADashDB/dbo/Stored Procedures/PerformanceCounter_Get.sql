CREATE PROC dbo.PerformanceCounter_Get(
	@InstanceID INT,
	@CounterID INT,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@DateGroupingMin INT=NULL,
	@Use60Min BIT=NULL,
	@DaysOfWeek IDs READONLY, /* e.g. exclude weekends:  Monday,Tuesday,Wednesday,Thursday,Friday. Filter applied in local timezone (@UTCOffset) */
	@Hours IDs READONLY, /* e.g. 9 to 5 :  9,10,11,12,13,14,15,16. Filter applied in local timezone (@UTCOffset)  */
	@UTCOffset INT=0 /* Used for filtering on hours & weekday in current timezone */
)
AS
SET DATEFIRST 1 /* Start week on Monday */
DECLARE @DateGroupingSQL NVARCHAR(MAX)
DECLARE @SQL NVARCHAR(MAX)
SELECT @DateGroupingSQL= CASE WHEN @DateGroupingMin = 0 OR @DateGroupingMin IS NULL THEN 'PC.SnapshotDate'
			ELSE 'DG.DateGroup' END

SELECT @Use60Min = CASE WHEN @DateGroupingMin>=60 THEN 1 ELSE 0 END

DECLARE @DaysOfWeekCsv NVARCHAR(MAX)
SELECT @DaysOfWeekCsv =  STUFF((SELECT ',' + CAST(ID AS VARCHAR)
FROM @DaysOfWeek
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')
DECLARE @HoursCsv NVARCHAR(MAX)
SELECT @HoursCsv =  STUFF((SELECT ',' + CAST(ID AS VARCHAR)
FROM @Hours
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')

DECLARE @CounterType TINYINT
SELECT @CounterType = CounterType
FROM dbo.Counters
WHERE CounterID = @CounterID

SET @SQL = N'SELECT PC.CounterID,
       ' + @DateGroupingSQL + ' AS SnapshotDate,
       ' + CASE WHEN @Use60Min=1 THEN 'SUM(PC.Value_Total) AS Value_Total,
										MIN(PC.Value_Min) AS Value_Min,
										MAX(PC.Value_Max) AS Value_Max,
										SUM(PC.Value_Total)/SUM(PC.SampleCount*1.0) AS Value_Avg,
										SUM(PC.SampleCount) AS Value_SampleCount'
		ELSE  'SUM(PC.Value) AS Value_Total,
					MIN(PC.Value) AS Value_Min,
					MAX(PC.Value) AS Value_Max,
					AVG(PC.Value) AS Value_Avg,
					COUNT(*) AS Value_SampleCount'
	END + '
FROM dbo.PerformanceCountersBetweenDates' + CASE WHEN @Use60Min=1 THEN '_60MIN' ELSE '' END + '(@FromDate,@ToDate,@InstanceID,@CounterID,@CounterType) PC
' + CASE WHEN @DateGroupingMin = 0 OR @DateGroupingMin IS NULL THEN '' ELSE 'CROSS APPLY dbo.DateGroupingMins(PC.SnapshotDate,@DateGroupingMin) DG' END + '
WHERE 1=1
	' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, PC.SnapshotDate)) IN (' + @DaysOfWeekCsv + ')' END + '
	' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, PC.SnapshotDate)) IN(' + @HoursCsv + ')' END + '
GROUP BY ' + @DateGroupingSQL + ',PC.CounterID
ORDER BY SnapshotDate'

EXEC sp_executesql @SQL,
					N'@FromDate DATETIME2(2),
					@ToDate DATETIME2(2),
					@InstanceID INT,
					@CounterID INT,
					@DateGroupingMin INT,
					@UTCOffset INT,
					@CounterType TINYINT',
					@FromDate,
					@ToDate,
					@InstanceID,
					@CounterID,
					@DateGroupingMin,
					@UTCOffset,
					@CounterType