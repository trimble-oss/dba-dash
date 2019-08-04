CREATE PROC Report.PerformanceDashboard(@InstanceIDs VARCHAR(MAX)=NULL,@Mins INT=60)
AS
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
	SELECT I.InstanceID,I.Instance,IOS.SnapshotDate,
			SUM(IOS.num_of_reads) AS num_of_reads,
			SUM(IOS.num_of_writes) AS num_of_writes,
			SUM(IOS.num_of_bytes_read) num_of_bytes_read,
			SUM(IOS.num_of_bytes_written) num_of_bytes_written,
			SUM(IOS.io_stall_read_ms) io_stall_read_ms,
			SUM(IOS.io_stall_write_ms) io_stall_write_ms,
			MAX(IOS.sample_ms_diff) sample_ms_diff
	FROM dbo.Instances I
	LEFT JOIN dbo.IOStats IOS ON IOS.InstanceID = I.InstanceID
						AND IOS.SnapshotDate>=DATEADD(mi,-@Mins,GETUTCDATE())
	WHERE I.IsActive=1
	AND EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
	GROUP BY I.InstanceID,I.Instance,IOS.SnapshotDate
)
, cpuAgg AS (
	SELECT InstanceID,AVG(100-SystemIdleCPU) AvgCPU
	FROM dbo.CPU
	WHERE EventTime >=DATEADD(mi,-@Mins,GETUTCDATE())
	GROUP BY InstanceID
)
SELECT a.InstanceID,
		a.Instance,
		SUM(a.num_of_reads)/(SUM(a.sample_ms_diff)/1000.0) AS ReadIOPs,
		SUM(a.num_of_writes)/(SUM(a.sample_ms_diff)/1000.0) AS WriteIOPs,
		SUM(a.num_of_bytes_read)/POWER(1024.0,2)/(SUM(a.sample_ms_diff)/1000.0) ReadMBsec,
		SUM(a.num_of_bytes_written)/POWER(1024.0,2)/(SUM(a.sample_ms_diff)/1000.0) WriteMBsec,
		SUM(a.io_stall_read_ms)/(NULLIF(SUM(a.num_of_reads),0)*1.0) AS ReadLatency,
		SUM(a.io_stall_write_ms)/(NULLIF(SUM(a.num_of_writes),0)*1.0) AS WriteLatency,
		SUM(a.io_stall_read_ms+a.io_stall_write_ms)/(NULLIF(SUM(a.num_of_writes+a.num_of_reads),0)*1.0) AS Latency,
		MAX(cpuAgg.AvgCPU) AS AvgCPU
FROM instanceAgg a
LEFT JOIN cpuAgg ON a.InstanceID = cpuAgg.InstanceID
GROUP BY a.InstanceID,a.Instance