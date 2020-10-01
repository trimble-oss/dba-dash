CREATE PROC [dbo].[IOStats_Get](
	@InstanceID INT,
	@FromDate DATETIME2(2)=NULL, 
	@ToDate DATETIME2(2)=NULL,
	@DateGrouping VARCHAR(50)='None',
	@DatabaseID INT=NULL,
	@Drive CHAR(1)=NULL
)
AS
IF @FromDate IS NULL
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
IF @ToDate IS NULL
	SET @ToDate = GETUTCDATE()
DECLARE @SQL NVARCHAR(MAX)
DECLARE @DateGroupingSQL NVARCHAR(MAX)

SELECT @DatabaseID=ISNULL(@DatabaseID,-1),@Drive=ISNULL(@Drive,'*')

IF EXISTS(SELECT 1 
		FROM dbo.Databases d 
		JOIN dbo.Instances I ON d.InstanceID = I.InstanceID 
		WHERE I.EditionID=1674378470 --azure
		AND d.DatabaseID = @DatabaseID
		)
BEGIN
	SET @DatabaseID=-1
END

SELECT @DateGroupingSQL= CASE WHEN @DateGrouping = 'None' THEN 'IOS.SnapshotDate'
			WHEN @DateGrouping = '1MIN' THEN 'DATEADD(mi, DATEDIFF(mi, 0, DATEADD(s, 30, IOS.SnapshotDate)), 0)'
			WHEN @DateGrouping = '10MIN' THEN 'CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,IOS.SnapshotDate,120),15) + ''0'',120)'
			WHEN @DateGrouping = '60MIN' THEN 'CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,IOS.SnapshotDate,120),13) + '':00'',120)'
			WHEN @DateGrouping = '120MIN' THEN 'DATEADD(hh,DATEPART(hh,IOS.SnapshotDate) - DATEPART(hh,IOS.SnapshotDate) % 2, CAST(CAST(IOS.SnapshotDate AS DATE) AS DATETIME))'
			WHEN @DateGrouping ='DAY' THEN 'CAST(CAST(IOS.SnapshotDate as DATE) as DATETIME)'
			ELSE NULL END

SET @SQL = N'
SELECT	' + @DateGroupingSQL + ' as SnapshotDate,
			SUM(IOS.num_of_reads+IOS.num_of_writes)/(SUM(IOS.sample_ms_diff)/1000.0) AS IOPs,
			SUM(IOS.num_of_reads)/(SUM(IOS.sample_ms_diff)/1000.0) AS ReadIOPs,
			SUM(IOS.num_of_writes)/(SUM(IOS.sample_ms_diff)/1000.0) AS WriteIOPs,
			SUM(IOS.num_of_bytes_read+IOS.num_of_bytes_written)/POWER(1024.0,2)/(SUM(IOS.sample_ms_diff)/1000.0) MBsec,
			SUM(IOS.num_of_bytes_read)/POWER(1024.0,2)/(SUM(IOS.sample_ms_diff)/1000.0) ReadMBsec,
			SUM(IOS.num_of_bytes_written)/POWER(1024.0,2)/(SUM(IOS.sample_ms_diff)/1000.0) WriteMBsec,
			ISNULL(SUM(IOS.io_stall_read_ms)/(NULLIF(SUM(IOS.num_of_reads),0)*1.0),0) AS ReadLatency,
			ISNULL(SUM(IOS.io_stall_write_ms)/(NULLIF(SUM(IOS.num_of_writes),0)*1.0),0) AS WriteLatency,
			ISNULL(SUM(IOS.io_stall_read_ms+IOS.io_stall_write_ms)/(NULLIF(SUM(IOS.num_of_writes+IOS.num_of_reads),0)*1.0),0) AS Latency,
			MAX(MaxIOPs) AS MaxIOPs,
			MAX(MaxReadIOPs)  AS MaxReadIOPs,
			MAX(MaxWriteIOPs)  AS MaxWriteIOPs,
			MAX(MaxMBsec) MaxMBsec,
			MAX(MaxReadMBsec) MaxReadMBsec,
			MAX(MaxWriteMBsec) MaxWriteMBsec,
			MAX(MaxReadLatency) AS MaxReadLatency,
			MAX(MaxWriteLatency) AS MaxWriteLatency,
			MAX(MaxLatency) AS MaxLatency
	FROM ' + CASE WHEN @DateGrouping IN('60MIN','120MIN','DAY') THEN 'dbo.DBIOStats_60MIN' ELSE 'dbo.DBIOStats' END + ' AS IOS
	WHERE IOS.InstanceID = @InstanceID
	AND IOS.DatabaseID = @DatabaseID
	AND IOS.SnapshotDate >= @FromDate
	AND IOS.SnapshotDate < @ToDate
	AND IOS.Drive = @Drive
	AND IOS.FileID = -1
	GROUP BY ' + @DateGroupingSQL  + '
ORDER BY SnapshotDate'

PRINT @SQL
EXEC sp_executesql @SQL,N'@InstanceID INT,@FromDate DATETIME2(2),@ToDate DATETIME2(2),@DatabaseID INT,@Drive CHAR(3)',@InstanceID,@FromDate,@ToDate,@DatabaseID,@Drive