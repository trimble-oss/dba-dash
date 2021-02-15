CREATE PROC [dbo].[PerformanceCounter_Get](
	@InstanceID INT,
	@CounterID INT,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@DateGroupingMin INT=NULL,
	@Use60Min BIT=NULL
)
AS
DECLARE @DateGroupingSQL NVARCHAR(MAX)
DECLARE @SQL NVARCHAR(MAX)
SELECT @DateGroupingSQL= CASE WHEN @DateGroupingMin = 0 OR @DateGroupingMin IS NULL THEN 'PC.SnapshotDate'
			ELSE 'DG.DateGroup' END

SELECT @Use60Min = CASE WHEN @DateGroupingMin>=60 THEN 1 ELSE 0 END

SET @SQL = N'SELECT PC.CounterID,
       ' + @DateGroupingSQL + ' AS SnapshotDate,
       ' + CASE WHEN @Use60Min=1 THEN 'SUM(PC.Value_Total) AS Value_Total,
										MIN(PC.Value_Min) AS Value_Min,
										MAX(PC.Value_Max) AS Value_Max,
										SUM(PC.Value_Total)/SUM(PC.SampleCount*1.0) AS Value_Avg,
										SUM(PC.SampleCount) AS SampleCount'
		ELSE  'SUM(PC.Value) AS Value_Total,
					MIN(PC.Value) AS Value_Min,
					MAX(PC.Value) AS Value_Max,
					AVG(PC.Value) AS Value_Avg,
					COUNT(*) AS SampleCount'
	END + '
FROM dbo.PerformanceCounters' + CASE WHEN @Use60Min=1 THEN '_60MIN' ELSE '' END + ' PC
' + CASE WHEN @DateGroupingMin = 0 OR @DateGroupingMin IS NULL THEN '' ELSE 'CROSS APPLY dbo.DateGroupingMins(PC.SnapshotDate,@DateGroupingMin) DG' END + '
WHERE PC.InstanceID =@InstanceID
AND PC.CounterID = @CounterID
AND PC.SnapshotDate>=@FromDate
AND PC.SnapshotDate<@ToDate
GROUP BY ' + @DateGroupingSQL + ',PC.CounterID
ORDER BY SnapshotDate'

EXEC sp_executesql @SQL,N'@FromDate DATETIME2(2),@ToDate DATETIME2(2),@InstanceID INT,@CounterID INT,@DateGroupingMin INT',@FromDate,@ToDate,@InstanceID,@CounterID,@DateGroupingMin