CREATE PROC dbo.PerformanceCounterSummary_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@TagIDs VARCHAR(MAX)=NULL,
	@Counters VARCHAR(MAX)=NULL,
	@InstanceID INT=NULL,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@Search NVARCHAR(128)=NULL,
	@Use60Min BIT=NULL,
	@Debug BIT=0,
	@DaysOfWeek IDs READONLY, /* e.g. exclude weekends:  Monday,Tuesday,Wednesday,Thursday,Friday. Filter applied in local timezone (@UTCOffset) */
	@Hours IDs READONLY, /* e.g. 9 to 5 :  9,10,11,12,13,14,15,16. Filter applied in local timezone (@UTCOffset)  */
	@UTCOffset INT=0, /* Used for filtering on hours & weekday in current timezone */
	@ShowHidden BIT=1
)
AS
SET DATEFIRST 1 /* Start week on Monday */
IF @Use60Min IS NULL
BEGIN
	SELECT @Use60Min = CASE WHEN DATEDIFF(hh,@FromDate,@ToDate)>24 THEN 1
						WHEN DATEPART(mi,@FromDate)+DATEPART(s,@FromDate)+DATEPART(ms,@FromDate)=0 
							AND (DATEPART(mi,@ToDate)+DATEPART(s,@ToDate)+DATEPART(ms,@ToDate)=0 
									OR @ToDate>=DATEADD(s,-2,GETUTCDATE())
								)
						THEN 1
						ELSE 0 END
END

DECLARE @DaysOfWeekCsv NVARCHAR(MAX)
SELECT @DaysOfWeekCsv =  STUFF((SELECT ',' + CAST(ID AS VARCHAR)
FROM @DaysOfWeek
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')

DECLARE @HoursCsv NVARCHAR(MAX)
SELECT @HoursCsv =  STUFF((SELECT ',' + CAST(ID AS VARCHAR)
FROM @Hours
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')

DECLARE @SQL NVARCHAR(MAX) =N'
WITH T AS (
	SELECT IC.InstanceID,
			C.CounterID,
			C.object_name,
			C.counter_name,
			C.instance_name,
			' + CASE WHEN @Use60Min=1 THEN 'MAX(PC.Value_Max) AS MaxValue,
			MIN(PC.Value_Min) AS MinValue,
			SUM(PC.Value_Total)/SUM(PC.SampleCount*1.0) AS AvgValue,
			SUM(PC.Value_Total) AS TotalValue,
			SUM(SampleCount) as SampleCount,' 
			ELSE '
			MAX(PC.Value) AS MaxValue,
			MIN(PC.Value) AS MinValue,
			AVG(PC.Value) AS AvgValue,
			SUM(PC.Value) as TotalValue,
			COUNT(*) as SampleCount,' END + '
			(SELECT TOP(1) Value FROM dbo.PerformanceCountersBetweenDates(DATEADD(mi,-120,GETUTCDATE()),GETUTCDATE(),IC.InstanceID,C.CounterID,C.CounterType) LV WHERE LV.InstanceID = IC.InstanceID AND LV.CounterID = C.CounterID ORDER BY LV.SnapshotDate DESC) AS CurrentValue,
			COALESCE(IC.CriticalFrom,C.CriticalFrom,C.SystemCriticalFrom) AS CriticalFrom,
			COALESCE(IC.CriticalTo,C.CriticalTo,C.SystemCriticalTo) AS CriticalTo,
			COALESCE(IC.WarningFrom,C.WarningFrom,C.SystemWarningFrom) AS WarningFrom,
			COALESCE(IC.WarningTo,C.WarningTo,C.SystemWarningTo) AS WarningTo,
			COALESCE(IC.GoodFrom,C.GoodFrom,C.SystemGoodFrom) AS GoodFrom,
			COALESCE(IC.GoodTo,C.GoodTo,C.SystemGoodTo) AS GoodTo
	FROM dbo.InstanceCounters IC
	JOIN dbo.Instances I ON I.InstanceID = IC.InstanceID
	JOIN dbo.Counters C ON C.CounterID = IC.CounterID
	CROSS APPLY dbo.PerformanceCountersBetweenDates' + CASE WHEN @Use60Min=1 THEN '_60MIN' ELSE '' END + '(@FromDate,@ToDate,IC.InstanceID,C.CounterID,C.CounterType) PC 
	WHERE 1=1
	' + CASE WHEN @InstanceID IS NULL THEN '' ELSE 'AND IC.InstanceID = @InstanceID' END + '
	' + CASE WHEN @InstanceIDs IS NULL THEN '' ELSE 'AND EXISTS(SELECT * FROM STRING_SPLIT(@InstanceIDs,'','') ss WHERE IC.InstanceID = ss.Value)' END + '
	' + CASE WHEN @Counters IS NULL THEN '' ELSE 'AND EXISTS(SELECT * FROM STRING_SPLIT(@Counters,'','') ss WHERE IC.CounterID = ss.Value)' END + '
	' + CASE WHEN @TagIDs IS NULL THEN '' ELSE 'AND EXISTS(SELECT 1 FROM dbo.InstancesMatchingTags(@TagIDs) tg WHERE tg.InstanceID = IC.InstanceID)' END + '
	' + CASE WHEN @Search IS NULL THEN '' ELSE 'AND (C.object_name LIKE @Search
		OR C.instance_name LIKE @Search
		OR C.counter_name LIKE @Search
		)' END + '
	' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, PC.SnapshotDate)) IN (' + @DaysOfWeekCsv + ')' END + '
	' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, PC.SnapshotDate)) IN(' + @HoursCsv + ')' END + '
	' + CASE WHEN @ShowHidden=1 THEN '' ELSE 'AND I.ShowInSummary=1' END + '
GROUP BY	C.CounterID,
			C.object_name,
			C.counter_name,
			C.instance_name,
			IC.InstanceID,
			C.CriticalFrom,
			C.CriticalTo,
			C.WarningFrom,
			C.WarningTo,
			C.GoodFrom,
			C.GoodTo,
			IC.CriticalFrom,
			IC.CriticalTo,
			IC.WarningFrom,
			IC.WarningTo,
			IC.GoodFrom,
			IC.GoodTo,
			C.SystemCriticalFrom,
			C.SystemCriticalTo,
			C.SystemWarningFrom,
			C.SystemWarningTo,
			C.SystemGoodFrom,
			C.SystemGoodTo,
			C.CounterType
)
SELECT *,
		CASE	WHEN MinValueStatus =1 OR MaxValueStatus =1 OR AvgValueStatus=1 OR CurrentValueStatus=1 THEN 1
				WHEN MinValueStatus =2 OR MaxValueStatus =2 OR AvgValueStatus=2 OR CurrentValueStatus=2 THEN 2
				WHEN MinValueStatus =4 OR MaxValueStatus =4 OR AvgValueStatus=4 OR CurrentValueStatus=4 THEN 4
				ELSE 3 END AS Status,
		CASE	WHEN MinValueStatus =1 OR MaxValueStatus =1 OR AvgValueStatus=1 OR CurrentValueStatus=1 THEN 1
				WHEN MinValueStatus =2 OR MaxValueStatus =2 OR AvgValueStatus=2 OR CurrentValueStatus=2 THEN 2
				WHEN MinValueStatus =4 OR MaxValueStatus =4 OR AvgValueStatus=4 OR CurrentValueStatus=4 THEN 4
				WHEN CriticalFrom IS NULL AND CriticalTo IS NULL
					AND WarningFrom IS NULL AND WarningTo IS NULL
					AND GoodFrom IS NULL AND GoodTo IS NULL THEN 6
				ELSE 5 END AS StatusSort
FROM T
OUTER APPLY (SELECT	CASE	WHEN T.MinValue >= T.CriticalFrom AND T.MinValue <= T.CriticalTo THEN 1 
							WHEN T.MinValue >= T.WarningFrom AND T.MinValue <= T.WarningTo THEN 2
							WHEN T.MinValue >= T.GoodFrom AND T.MinValue <= T.GoodTo THEN 4
							ELSE 3 END AS MinValueStatus,
					CASE	WHEN T.MaxValue >= T.CriticalFrom AND T.MaxValue <= T.CriticalTo THEN 1
							WHEN T.MaxValue >= T.WarningFrom AND T.MaxValue <= T.WarningTo THEN 2
							WHEN T.MaxValue >= T.GoodFrom AND T.MaxValue <= T.GoodTo THEN 4
							ELSE 3 END AS MaxValueStatus,
					CASE	WHEN T.AvgValue >= T.CriticalFrom AND T.AvgValue <= T.CriticalTo THEN 1
							WHEN T.AvgValue >= T.WarningFrom AND T.AvgValue <= T.WarningTo THEN 2
							WHEN T.AvgValue >= T.GoodFrom AND T.AvgValue <= T.GoodTo THEN 4
							ELSE 3 END AS AvgValueStatus,
					CASE	WHEN T.CurrentValue >= T.CriticalFrom AND T.CurrentValue <= T.CriticalTo THEN 1
							WHEN T.CurrentValue >= T.WarningFrom AND T.CurrentValue <= T.WarningTo THEN 2
							WHEN T.CurrentValue >= T.GoodFrom AND T.CurrentValue <= T.GoodTo THEN 4
							ELSE 3 END AS CurrentValueStatus
							) AS Calc
ORDER BY StatusSort,
		T.InstanceID,
		T.object_name,
		T.counter_name,
		T.instance_name;'


IF @Debug=1
BEGIN
	PRINT @SQL
END

EXEC sp_executesql @SQL,N'@InstanceID INT,
						@FromDate DATETIME2(2),
						@ToDate DATETIME2(2),
						@Search NVARCHAR(128)=NULL,
						@InstanceIDs VARCHAR(MAX),
						@Counters VARCHAR(MAX),
						@TagIDs VARCHAR(MAX),
						@UTCOffset INT',
						@InstanceID, 
						@FromDate, 
						@ToDate, 
						@Search,
						@InstanceIDs,
						@Counters,
						@TagIDs,
						@UTCOffset