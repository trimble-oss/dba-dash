CREATE   PROC dbo.ObjectExecutionStatsSummary_Get(
		@FromDate DATETIME2(3),
		@ToDate DATETIME2(4),
		@CompareFrom DATETIME2(3)=NULL,
		@CompareTo DATETIME2(3)=NULL,
		@Instance SYSNAME=NULL,
		@InstanceID INT=NULL,
		@DatabaseID INT=NULL,
		@Types VARCHAR(200)=NULL, -- 'P,FN,TR,TA,PC,X'
		@Use60MIN BIT=NULL,
		@Use60MINCompare BIT=NULL,
		@ObjectID BIGINT=NULL,
		@Debug BIT=0,
		@DaysOfWeek IDs READONLY, /* e.g. exclude weekends:  Monday,Tuesday,Wednesday,Thursday,Friday. Filter applied in local timezone (@UTCOffset) */
		@Hours IDs READONLY, /* e.g. 9 to 5 :  9,10,11,12,13,14,15,16. Filter applied in local timezone (@UTCOffset)  */
		@UTCOffset INT=0 /* Used for filtering on hours & weekday in current timezone */
)
AS
SET DATEFIRST 1 /* Start week on Monday */
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
IF @Use60MINCompare IS NULL
BEGIN
	SELECT @Use60MINCompare = CASE WHEN DATEDIFF(hh,@CompareFrom,@CompareTo)>24 THEN 1
						WHEN DATEPART(mi,@CompareFrom)+DATEPART(s,@CompareFrom)+DATEPART(ms,@CompareFrom)=0 
							AND (DATEPART(mi,@CompareTo)+DATEPART(s,@CompareTo)+DATEPART(ms,@CompareTo)=0 
									OR @CompareTo>=DATEADD(s,-2,GETUTCDATE())
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


DECLARE @SQL NVARCHAR(MAX)
SET @SQL = CAST('' AS NVARCHAR(MAX)) +  N'
WITH base AS (
	SELECT 
		OES.InstanceID,
		OES.ObjectID,
		I.ConnectionID,
		D.name as DB,
		O.SchemaName,
		O.ObjectName,
		O.ObjectType,
		OT.TypeDescription,
		SUM(total_elapsed_time)/1000000.0 as total_duration_sec,
		SUM(total_elapsed_time)/1000.0 / MAX(SUM(PeriodTime)/1000000.0) OVER() duration_ms_per_sec,
		SUM(total_elapsed_time/1000000.0)/SUM(execution_count) as avg_duration_sec,
		SUM(execution_count) as execution_count,
		SUM(execution_count) / MAX(SUM(PeriodTime)/60000000.0) OVER() execs_per_min,
		SUM(total_worker_time)/1000000.0 as total_cpu_sec,
		SUM(total_worker_time)/1000.0 / MAX(SUM(PeriodTime)/1000000.0) OVER() cpu_ms_per_sec,
		SUM(total_worker_time) /1000000.0 / SUM(execution_count) as avg_cpu_sec,
		SUM(total_physical_reads) as total_physical_reads,
		SUM(total_logical_reads) as total_logical_reads,
		SUM(total_logical_writes) as total_writes,
		SUM(total_logical_writes)/SUM(execution_count) as avg_writes,
		SUM(total_physical_reads)/SUM(execution_count) as avg_physical_reads,
		SUM(total_logical_reads)/SUM(execution_count) as avg_logical_reads,
		MAX(SUM(PeriodTime)/1000000.0) OVER() as period_time_sec,
		MAX(MaxExecutionsPerMin) as max_execs_per_min
	FROM dbo.ObjectExecutionStats' + CASE WHEN @Use60MIN=1 THEN '_60MIN' ELSE '' END + ' OES 
	JOIN dbo.Instances I ON OES.InstanceID = I.InstanceID
	JOIN dbo.DBObjects O ON OES.InstanceID = I.InstanceID AND OES.ObjectID = O.ObjectID
	JOIN dbo.Databases D ON O.DatabaseID = D.DatabaseID AND D.InstanceID = I.InstanceID
	JOIN dbo.ObjectType OT ON OT.ObjectType = O.ObjectType
	WHERE OES.SnapshotDate>=@FromDate
	AND OES.SnapshotDate<@ToDate
	' + CASE WHEN @Instance IS NULL THEN '' ELSE 'AND I.Instance=@Instance' END + '
	' + CASE WHEN @InstanceID IS NULL THEN '' ELSE 'AND I.InstanceID=@InstanceID' END + '
	' + CASE WHEN @Types IS NULL THEN '' ELSE 'AND EXISTS(SELECT 1 FROM STRING_SPLIT(@Types,'','') ss WHERE ss.Value =  O.ObjectType)' END + '
	' + CASE WHEN @DatabaseID IS NULL THEN '' ELSE 'AND D.DatabaseID = @DatabaseID' END + '
	' + CASE WHEN @ObjectID IS NULL THEN '' ELSE 'AND OES.ObjectID = @ObjectID' END + '
	' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, OES.SnapshotDate)) IN (' + @DaysOfWeekCsv + ')' END + '
	' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, OES.SnapshotDate)) IN(' + @HoursCsv + ')' END + '
	GROUP BY OES.InstanceID,OES.ObjectID,I.ConnectionID,D.name,O.SchemaName,O.ObjectName,O.ObjectType,OT.TypeDescription
),
compare as(
	SELECT OES.InstanceID,
		OES.ObjectID,
		I.ConnectionID,
		D.name as DB,
		O.SchemaName,
		O.ObjectName,
		O.ObjectType,
		OT.TypeDescription,
		SUM(total_elapsed_time)/1000000.0 as compare_total_duration_sec,
		SUM(total_elapsed_time)/1000.0 / MAX(SUM(PeriodTime)/1000000.0) OVER() compare_duration_ms_per_sec,
		SUM(total_elapsed_time/1000000.0)/SUM(execution_count) as compare_avg_duration_sec,
		SUM(execution_count) as compare_execution_count,
		SUM(execution_count) / MAX(SUM(PeriodTime)/60000000.0) OVER() compare_execs_per_min,
		SUM(total_worker_time)/1000000.0 as compare_total_cpu_sec,
		SUM(total_worker_time)/1000.0 / MAX(SUM(PeriodTime)/1000000.0) OVER() compare_cpu_ms_per_sec,
		SUM(total_worker_time) /1000000.0 / SUM(execution_count) as compare_avg_cpu_sec,
		SUM(total_physical_reads) as compare_total_physical_reads,
		SUM(total_logical_reads) as compare_total_logical_reads,
		SUM(total_logical_writes) as compare_total_writes,
		SUM(total_logical_writes)/SUM(execution_count) as compare_avg_writes,
		SUM(total_physical_reads)/SUM(execution_count) as compare_avg_physical_reads,
		SUM(total_logical_reads)/SUM(execution_count) as compare_avg_logical_reads,
		MAX(SUM(PeriodTime)/1000000.0) OVER() as compare_period_time_sec,
		MAX(MaxExecutionsPerMin) as compare_max_execs_per_min
	FROM dbo.ObjectExecutionStats' + CASE WHEN @Use60MIN=1 THEN '_60MIN' ELSE '' END + ' OES 
	JOIN dbo.Instances I ON OES.InstanceID = I.InstanceID
	JOIN dbo.DBObjects O ON OES.InstanceID = I.InstanceID AND OES.ObjectID = O.ObjectID
	JOIN dbo.Databases D ON O.DatabaseID = D.DatabaseID AND D.InstanceID = I.InstanceID
	JOIN dbo.ObjectType OT ON OT.ObjectType = O.ObjectType
	WHERE OES.SnapshotDate>=@CompareFrom
	AND OES.SnapshotDate<@CompareTo
	' + CASE WHEN @Instance IS NULL THEN '' ELSE 'AND I.Instance=@Instance' END + '
	' + CASE WHEN @InstanceID IS NULL THEN '' ELSE 'AND I.InstanceID=@InstanceID' END + '
	' + CASE WHEN @Types IS NULL THEN '' ELSE 'AND EXISTS(SELECT 1 FROM STRING_SPLIT(@Types,'','') ss WHERE ss.Value =  O.ObjectType)' END + '
	' + CASE WHEN @DatabaseID IS NULL THEN '' ELSE 'AND D.DatabaseID = @DatabaseID' END + '
	' + CASE WHEN @ObjectID IS NULL THEN '' ELSE 'AND OES.ObjectID = @ObjectID' END + '
	' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, OES.SnapshotDate)) IN (' + @DaysOfWeekCsv + ')' END + '
	' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, OES.SnapshotDate)) IN(' + @HoursCsv + ')' END + '
	GROUP BY OES.InstanceID,OES.ObjectID,I.ConnectionID,D.name,O.SchemaName,O.ObjectName,O.ObjectType,OT.TypeDescription
)
SELECT ISNULL(base.InstanceID,compare.InstanceID) as InstanceID,
		ISNULL(base.ObjectID,compare.ObjectID) as ObjectID,
		ISNULL(base.ConnectionID,compare.ConnectionID) as ConnectionID,
		ISNULL(base.DB,compare.DB) as DB,
		ISNULL(base.SchemaName,compare.SchemaName) as SchemaName,
		ISNULL(base.ObjectName,compare.ObjectName) as ObjectName,
		ISNULL(base.ObjectType,compare.ObjectType) as ObjectType,
		ISNULL(base.TypeDescription,compare.TypeDescription) as TypeDescription,
		total_duration_sec,
		duration_ms_per_sec,
		avg_duration_sec,
		execution_count,
		execs_per_min,
		max_execs_per_min,
		total_cpu_sec,
		cpu_ms_per_sec,
		avg_cpu_sec,
		total_physical_reads,
		total_logical_reads,
		total_writes,
		avg_writes,
		avg_physical_reads,
		avg_logical_reads,
		period_time_sec,
		compare_total_duration_sec,
		compare_duration_ms_per_sec,
		compare_avg_duration_sec,
		compare_execution_count,
		compare_execs_per_min,
		compare_max_execs_per_min
		compare_total_cpu_sec,
		compare_cpu_ms_per_sec,
		compare_avg_cpu_sec,
		compare_total_physical_reads,
		compare_total_logical_reads,
		compare_total_writes,
		compare_avg_writes,
		compare_avg_physical_reads,
		compare_avg_logical_reads,
		compare_period_time_sec,
		(avg_duration_sec-compare_avg_duration_sec)/compare_avg_duration_sec as diff_avg_duration_pct,
		(avg_cpu_sec-compare_avg_cpu_sec)/compare_avg_cpu_sec as diff_avg_cpu_pct	
FROM base 
FULL JOIN compare on base.ObjectID = compare.ObjectID'

IF @Debug=1
	PRINT @SQL

EXEC sp_executesql @SQL,N'@InstanceID INT,
						@Instance SYSNAME,
						@FromDate DATETIME2(3),
						@ToDate DATETIME2(3),
						@CompareFrom DATETIME2(3),
						@CompareTo DATETIME2(3),
						@Types VARCHAR(200),
						@DatabaseID INT,
						@ObjectID BIGINT,
						@UTCOffset INT',
						@InstanceID,
						@Instance,
						@FromDate,
						@ToDate,
						@CompareFrom,
						@CompareTo,
						@Types,
						@DatabaseID,
						@ObjectID,
						@UTCOffset
;