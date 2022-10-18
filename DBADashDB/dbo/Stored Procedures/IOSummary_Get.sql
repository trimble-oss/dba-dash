CREATE PROC dbo.IOSummary_Get(
	@InstanceID INT,
	@FromDate DATETIME2(2), /* UTC */
	@ToDate DATETIME2(2), /* UTC */
	@GroupBy VARCHAR(50)='Database',
	@DatabaseID INT=NULL,
	@Debug BIT =0,
	@Use60MIN BIT=NULL,
	@Instance NVARCHAR(128)=NULL OUT,
	@DatabaseName NVARCHAR(128)=NULL OUT,
	@UTCOffset INT=0, /* Used for Hours filter */
	@DaysOfWeek IDs READONLY, /* e.g. 1=Monday. exclude weekends:  1,2,3,4,5.  Filter applied in local timezone (@UTCOffset) */
	@Hours IDs READONLY/* e.g. 9 to 5 :  9,10,11,12,13,14,15,16. Filter applied in local timezone (@UTCOffset)*/
)
AS
SET DATEFIRST 1 /* Start week on Monday */
SET NOCOUNT ON
IF @DatabaseID=-1
	SET @DatabaseID=NULL

DECLARE @EditionID INT

SELECT @Instance = I.Instance, 
		@EditionID = I.EditionID
FROM dbo.Instances I
WHERE I.InstanceID = @InstanceID

SELECT @DatabaseName = D.name 
FROM dbo.Databases D 
WHERE D.InstanceID = @InstanceID
AND D.DatabaseID = @DatabaseID

-- For Azure DB, instance level stats are the DB level stats.  DatabaseID will be -1 in table.
IF @GroupBy='Database' AND @EditionID=1674378470 
	SET @DatabaseID=NULL

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
DECLARE @TableName SYSNAME
SELECT @TableName = CASE WHEN @GroupBy ='Filegroup' THEN 'dbo.FGIOStats' ELSE 'dbo.DBIOStats' END + CASE WHEN @Use60MIN=1 THEN '_60MIN' ELSE '' END

DECLARE @GroupBySQL NVARCHAR(MAX) 
SELECT @GroupBySQL= CASE @GroupBy WHEN 'Database' THEN 'D.name' WHEN 'Filegroup' THEN 'D.name + '' | '' + IOS.filegroup_name' WHEN 'File' THEN 'D.name + '' | '' + F.name + '' | ('' + IOS.Drive + '':\)''' WHEN 'Drive' THEN 'REPLACE(IOS.Drive + '':\'',''*'',''{All}'') + ISNULL('' ('' + DRV.Label + '')'','''')' END
IF @GroupBySQL IS NULL
BEGIN
	RAISERROR('Invalid @GroupBy.  Valid values: Database,Filegroup,File,Drive',11,1)
END

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

DECLARE @SQL NVARCHAR(MAX)
SET @SQL =N'
SELECT ' + @GroupBySQL + N' AS Grp,
		SUM(IOS.num_of_reads+IOS.num_of_writes)/(MAX(SUM(IOS.sample_ms_diff)) OVER()/1000.0) AS IOPs,
		SUM(IOS.num_of_reads)/(MAX(SUM(IOS.sample_ms_diff)) OVER()/1000.0) AS ReadIOPs,
		SUM(IOS.num_of_writes)/(MAX(SUM(IOS.sample_ms_diff)) OVER()/1000.0)  AS WriteIOPs,
		SUM(IOS.num_of_bytes_read+IOS.num_of_bytes_written)/POWER(1024.0,2)/(MAX(SUM(IOS.sample_ms_diff)) OVER()/1000.0) AS MBsec,
		SUM(IOS.num_of_bytes_read)/POWER(1024.0,2)/(MAX(SUM(IOS.sample_ms_diff)) OVER()/1000.0) AS ReadMBsec,
		SUM(IOS.num_of_bytes_written)/POWER(1024.0,2)/(MAX(SUM(IOS.sample_ms_diff)) OVER()/1000.0) AS WriteMBsec,
		ISNULL(SUM(IOS.io_stall_read_ms)/(NULLIF(SUM(IOS.num_of_reads),0)*1.0),0) AS ReadLatency,
		ISNULL(SUM(IOS.io_stall_write_ms)/(NULLIF(SUM(IOS.num_of_writes),0)*1.0),0) AS WriteLatency,
		ISNULL(SUM(IOS.io_stall_read_ms+IOS.io_stall_write_ms)/(NULLIF(SUM(IOS.num_of_writes+IOS.num_of_reads),0)*1.0),0) AS Latency,
		MAX(MaxIOPs) AS MaxIOPs,
		MAX(MaxReadIOPs) AS MaxReadIOPs,
		MAX(MaxWriteIOPs) AS MaxWriteIOPs,
		MAX(MaxMBsec) MaxMBsec,
		MAX(MaxReadMBsec) MaxReadMBsec,
		MAX(MaxWriteMBsec) MaxWriteMBsec,
		MAX(MaxReadLatency) AS MaxReadLatency,
		MAX(MaxWriteLatency) AS MaxWriteLatency,
		MAX(MaxLatency) AS MaxLatency
FROM ' + @TableName + ' IOS
' + CASE WHEN @GroupBy IN('Filegroup','File','Database') AND @EditionID=1674378470 THEN 'JOIN dbo.Databases D ON D.InstanceID = IOS.InstanceID AND D.IsActive=1 ' ELSE '' END + '
' + CASE WHEN @GroupBy IN('Filegroup','File','Database') AND @EditionID<>1674378470 THEN 'JOIN dbo.Databases D ON D.DatabaseID = IOS.DatabaseID AND D.IsActive=1' ELSE '' END + '
' + CASE WHEN @GroupBy IN('File') THEN 'JOIN dbo.DBFiles F ON D.DatabaseID = F.DatabaseID AND IOS.FileID = F.FileID' ELSE '' END + '
' + CASE WHEN @GroupBy ='Drive' THEN 'LEFT JOIN dbo.Drives DRV ON IOS.InstanceID = DRV.InstanceID AND DRV.Name = IOS.Drive + '':\'' AND DRV.IsActive=1' ELSE '' END + '
WHERE IOS.InstanceID = @InstanceID
AND IOS.SnapshotDate >= @FromDate
AND IOS.SnapshotDate < @ToDate
' + CASE WHEN @DatabaseID IS NULL THEN '' ELSE 'AND IOS.DatabaseID = @DatabaseID' END + '
' + CASE WHEN @GroupBy IN('Drive','Database') THEN 'AND IOS.FileID = -1' ELSE '' END + '
' + CASE WHEN @GroupBy ='Database' THEN 'AND IOS.Drive=''*''' ELSE '' END + '
' + CASE WHEN @GroupBy ='Drive' AND @DatabaseID IS NULL THEN 'AND IOS.DatabaseID = -1' ELSE '' END + '
' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, IOS.SnapshotDate)) IN (' + @DaysOfWeekCsv + ')' END + '
' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, IOS.SnapshotDate)) IN(' + @HoursCsv + ')' END + '
GROUP BY ' + @GroupBySQL + '
ORDER BY Grp'

IF @Debug=1
	PRINT @SQL

EXEC sp_executesql @SQL,
					N'@InstanceID INT,
					@FromDate DATETIME2(2),
					@ToDate DATETIME2(2),
					@DatabaseID INT,
					@UTCOffset INT',
					@InstanceID,
					@FromDate,
					@ToDate,
					@DatabaseID,
					@UTCOffset
