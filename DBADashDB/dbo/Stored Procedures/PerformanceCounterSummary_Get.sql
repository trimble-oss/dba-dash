CREATE PROC [dbo].[PerformanceCounterSummary_Get](
	@InstanceID INT,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@Search NVARCHAR(128)=NULL,
	@Use60Min BIT=NULL,
	@Debug BIT=0
)
AS
IF @Use60MIN IS NULL
BEGIN
	SELECT @Use60MIN = CASE WHEN DATEDIFF(hh,@FromDate,@ToDate)>24 THEN 1
						WHEN DATEPART(mi,@FromDate)+DATEPART(s,@FromDate)+DATEPART(ms,@FromDate)=0 
							AND (DATEPART(mi,@ToDate)+DATEPART(s,@ToDate)+DATEPART(ms,@ToDate)=0 
									OR @ToDate>=DATEADD(s,-2,GETUTCDATE())
								)
						THEN 1
						ELSE 0 END
END
DECLARE @SQL NVARCHAR(MAX) =N'
SELECT C.CounterID,
       C.object_name,
       C.counter_name,
       C.instance_name,
	   ' + CASE WHEN @Use60Min=1 THEN 'MAX(PC.Value_Max) AS MaxValue,
	   MIN(PC.Value_Min) AS MinValue,
	   SUM(PC.Value_Total)/SUM(PC.SampleCount*1.0) AS AvgValue,
	   SUM(PC.Value_Total) AS TotalValue,' 
	   ELSE '
       MAX(PC.Value) AS MaxValue,
	   MIN(PC.Value) AS MinValue,
	   AVG(PC.Value) AS AvgValue,
	   SUM(PC.Value) as TotalValue,' END + '
	   (SELECT TOP(1) Value FROM dbo.PerformanceCounters LV WHERE LV.InstanceID = @InstanceID AND LV.CounterID = C.CounterID ORDER BY LV.SnapshotDate DESC) AS CurrentValue 
FROM dbo.InstanceCounters IC
JOIN dbo.Counters C ON C.CounterID = IC.CounterID
JOIN dbo.PerformanceCounters' + CASE WHEN @Use60Min=1 THEN '_60MIN' ELSE '' END + ' PC ON PC.InstanceID = IC.InstanceID AND PC.CounterID = IC.CounterID
WHERE IC.InstanceID = @InstanceID
AND PC.SnapshotDate>=@FromDate
AND PC.SnapshotDate<@ToDate
' + CASE WHEN @Search IS NULL THEN '' ELSE 'AND (C.object_name LIKE @Search
	OR C.instance_name LIKE @Search
	OR C.counter_name LIKE @Search
	)' END + '
GROUP BY  C.CounterID,
       C.object_name,
       C.counter_name,
       C.instance_name
ORDER BY C.object_name,C.counter_name,C.instance_name;'

IF @Debug=1
BEGIN
	PRINT @SQL
END

EXEC sp_executesql @SQL,N'@InstanceID INT,@FromDate DATETIME2(2),@ToDate DATETIME2(2),@Search NVARCHAR(128)=NULL',@InstanceID, @FromDate, @ToDate, @Search