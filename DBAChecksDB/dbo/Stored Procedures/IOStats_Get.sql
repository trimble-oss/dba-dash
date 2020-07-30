CREATE PROC [dbo].[IOStats_Get](
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

SELECT @DateGroupingSQL= CASE WHEN @DateGrouping = 'None' THEN 'a.SnapshotDate'
			WHEN @DateGrouping = '1MIN' THEN 'DATEADD(mi, DATEDIFF(mi, 0, DATEADD(s, 30, a.SnapshotDate)), 0)'
			WHEN @DateGrouping = '10MIN' THEN 'CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,a.SnapshotDate,120),15) + ''0'',120)'
			WHEN @DateGrouping = '60MIN' THEN 'CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,a.SnapshotDate,120),13) + '':00'',120)'
			WHEN @DateGrouping = '120MIN' THEN 'DATEADD(hh,DATEPART(hh,a.SnapshotDate) - DATEPART(hh,a.SnapshotDate) % 2, CAST(CAST(a.SnapshotDate AS DATE) AS DATETIME))'
			WHEN @DateGrouping ='DAY' THEN 'CAST(CAST(a.SnapshotDate as DATE) as DATETIME)'
			ELSE NULL END

SET @SQL = N'
WITH stats AS(
	SELECT	IOS.SnapshotDate,
			SUM(IOS.num_of_reads) AS num_of_reads,
			SUM(IOS.num_of_writes) AS num_of_writes,
			SUM(IOS.num_of_bytes_read) num_of_bytes_read,
			SUM(IOS.num_of_bytes_written) num_of_bytes_written,
			SUM(IOS.io_stall_read_ms) io_stall_read_ms,
			SUM(IOS.io_stall_write_ms) io_stall_write_ms,
			MAX(IOS.sample_ms_diff) sample_ms_diff,
			SUM(IOS.io_stall_read_ms)/(NULLIF(SUM(IOS.num_of_reads),0)*1.0) AS ReadLatency,
			SUM(IOS.io_stall_write_ms)/(NULLIF(SUM(IOS.num_of_writes),0)*1.0) AS WriteLatency,
			SUM(IOS.io_stall_read_ms+IOS.io_stall_write_ms)/(NULLIF(SUM(IOS.num_of_writes+IOS.num_of_reads),0)*1.0) AS Latency
	FROM dbo.IOStats IOS
	WHERE IOS.InstanceID = @InstanceID
	AND IOS.SnapshotDate >= @FromDate
	AND IOS.SnapshotDate < @ToDate
	GROUP BY IOS.SnapshotDate
)
SELECT ' + @DateGroupingSQL + ' as SnapshotDate,
		SUM(a.num_of_reads+a.num_of_writes)/(SUM(a.sample_ms_diff)/1000.0) AS IOPs,
		SUM(a.num_of_reads)/(SUM(a.sample_ms_diff)/1000.0) AS ReadIOPs,
		SUM(a.num_of_writes)/(SUM(a.sample_ms_diff)/1000.0) AS WriteIOPs,
		SUM(a.num_of_bytes_read+a.num_of_bytes_written)/POWER(1024.0,2)/(SUM(a.sample_ms_diff)/1000.0) MBsec,
		SUM(a.num_of_bytes_read)/POWER(1024.0,2)/(SUM(a.sample_ms_diff)/1000.0) ReadMBsec,
		SUM(a.num_of_bytes_written)/POWER(1024.0,2)/(SUM(a.sample_ms_diff)/1000.0) WriteMBsec,
		SUM(a.io_stall_read_ms)/(NULLIF(SUM(a.num_of_reads),0)*1.0) AS ReadLatency,
		SUM(a.io_stall_write_ms)/(NULLIF(SUM(a.num_of_writes),0)*1.0) AS WriteLatency,
		SUM(a.io_stall_read_ms+a.io_stall_write_ms)/(NULLIF(SUM(a.num_of_writes+a.num_of_reads),0)*1.0) AS Latency,
		MAX(a.num_of_reads/(a.sample_ms_diff/1000.0)) AS MaxReadIOPs,
		MAX(a.num_of_writes/(a.sample_ms_diff/1000.0)) AS MaxWriteIOPs,
		MAX((a.num_of_writes+a.num_of_reads)/(a.sample_ms_diff/1000.0)) AS MaxIOPs,
		MAX(a.num_of_bytes_read/(a.sample_ms_diff/1000.0))/POWER(1024.0,2) AS MaxReadMBsec,
		MAX(a.num_of_bytes_written/(a.sample_ms_diff/1000.0))/POWER(1024.0,2) AS MaxWriteMBsec,
		MAX((a.num_of_bytes_written+a.num_of_bytes_read)/(a.sample_ms_diff/1000.0))/POWER(1024.0,2) AS MaxMBsec,
		MAX(ReadLatency) MaxReadLatency,
		MAX(WriteLatency) MaxWriteLatency,
		MAX(Latency) MaxLatency
FROM stats a
GROUP BY ' + @DateGroupingSQL + '
ORDER BY SnapshotDate'

PRINT @SQL
EXEC sp_executesql @SQL,N'@InstanceID INT,@FromDate DATETIME,@ToDate DATETIME',@InstanceID,@FromDate,@ToDate