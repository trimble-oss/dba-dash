CREATE PROC [Report].[IOPerformance](
	@InstanceIDs VARCHAR(MAX)=NULL,
	@DatabaseName SYSNAME=NULL,
	@FileGroupName SYSNAME=NULL,
	@Drive CHAR(3)=NULL,
	@GroupByDB BIT=0,
	@GroupByFileGroup BIT=0,
	@GroupByFile BIT=0,
	@GroupByDrive BIT=0,
	@TimeGroup TINYINT=0,
	@FromDate DATETIME=NULL,
	@ToDate DATETIME=NULL
)
AS
IF @FromDate IS NULL	
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
IF @ToDate IS NULL	
	SET @ToDate = GETUTCDATE()
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT Item
	FROM dbo.SplitStrings(@InstanceIDs,',')
END;

WITH instanceAgg AS (
	SELECT I.InstanceID,
			I.Instance,
			CASE WHEN @GroupByDB=1 THEN D.name ELSE NULL END AS DBName,
			CASE WHEN @GroupByFileGroup=1 THEN F.filegroup_name ELSE NULL END AS FileGroup,
			CASE WHEN @GroupByFile=1 THEN F.name ELSE NULL END AS FileName,
			CASE WHEN @GroupByDrive=1 THEN LEFT(F.physical_name,3) ELSE NULL END AS Drive,
			IOS.SnapshotDate,
			SUM(IOS.num_of_reads) AS num_of_reads,
			SUM(IOS.num_of_writes) AS num_of_writes,
			SUM(IOS.num_of_bytes_read) num_of_bytes_read,
			SUM(IOS.num_of_bytes_written) num_of_bytes_written,
			SUM(IOS.io_stall_read_ms) io_stall_read_ms,
			SUM(IOS.io_stall_write_ms) io_stall_write_ms,
			MAX(IOS.sample_ms_diff) sample_ms_diff
	FROM dbo.Instances I
	JOIN dbo.IOStats IOS ON IOS.InstanceID = I.InstanceID
	LEFT JOIN dbo.DBFiles F ON IOS.FileID = F.FileID
	LEFT JOIN dbo.Databases D ON D.DatabaseID = F.DatabaseID
	WHERE IOS.SnapshotDate>=@FromDate
		AND IOS.SnapshotDate<@ToDate
		AND I.IsActive=1
		AND (D.name = @DatabaseName OR @DatabaseName IS NULL)
		AND (F.filegroup_name  = @FileGroupName OR @FileGroupName IS NULL)
		AND (F.physical_name LIKE @Drive + '%' OR @Drive IS NULL)
		AND EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
	GROUP BY I.InstanceID,
			I.Instance,
			IOS.SnapshotDate,
			CASE WHEN @GroupByDB=1 THEN D.name ELSE NULL END,
			CASE WHEN @GroupByFileGroup=1 THEN F.filegroup_name ELSE NULL END,
			CASE WHEN @GroupByFile=1 THEN F.name ELSE NULL END,
			CASE WHEN @GroupByDrive=1 THEN LEFT(F.physical_name,3) ELSE NULL END 
)
SELECT a.InstanceID,
		a.Instance,
		a.DBName,
		a.FileGroup,
		a.FileName,
		a.Drive,
		CASE WHEN @TimeGroup=0 THEN NULL	
			WHEN @TimeGroup=1 THEN CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,a.SnapshotDate,120),16),120)
			WHEN @TimeGroup=2 THEN CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,a.SnapshotDate,120),15) + '0',120)
			WHEN @TimeGroup=3 THEN CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,a.SnapshotDate,120),13) + ':00',120)
			ELSE NULL END AS Time,
		SUM(a.num_of_reads)/(SUM(a.sample_ms_diff)/1000.0) AS ReadIOPs,
		SUM(a.num_of_writes)/(SUM(a.sample_ms_diff)/1000.0) AS WriteIOPs,
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
		MAX((a.num_of_bytes_written+a.num_of_bytes_read)/(a.sample_ms_diff/1000.0))/POWER(1024.0,2) AS MaxMBsec
FROM instanceAgg a
GROUP BY a.InstanceID,a.Instance,a.DBName,a.FileGroup,a.FileName,a.Drive,
			CASE WHEN @TimeGroup=0 THEN NULL	
			WHEN @TimeGroup=1 THEN CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,a.SnapshotDate,120),16),120)
			WHEN @TimeGroup=2 THEN CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,a.SnapshotDate,120),15) + '0',120)
			WHEN @TimeGroup=3 THEN CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,a.SnapshotDate,120),13) + ':00',120)
			ELSE NULL END
OPTION(RECOMPILE)