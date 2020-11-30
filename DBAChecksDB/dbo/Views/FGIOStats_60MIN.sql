
CREATE VIEW FGIOStats_60MIN
AS
SELECT IOS.InstanceID,
		IOS.DatabaseID,
		IOS.SnapshotDate,
		f.filegroup_name,
		SUM(IOS.num_of_reads+IOS.num_of_writes)/(SUM(IOS.sample_ms_diff)/1000.0) AS IOPs,
		SUM(IOS.num_of_reads)/(SUM(IOS.sample_ms_diff)/1000.0) AS ReadIOPs,
		SUM(IOS.num_of_writes)/(SUM(IOS.sample_ms_diff)/1000.0) AS WriteIOPs,
		SUM(IOS.num_of_bytes_read+IOS.num_of_bytes_written)/POWER(1024.0,2)/(SUM(IOS.sample_ms_diff)/1000.0) MBsec,
		SUM(IOS.num_of_bytes_read)/POWER(1024.0,2)/(SUM(IOS.sample_ms_diff)/1000.0) ReadMBsec,
		SUM(IOS.num_of_bytes_written)/POWER(1024.0,2)/(SUM(IOS.sample_ms_diff)/1000.0) WriteMBsec,
		ISNULL(SUM(IOS.io_stall_read_ms)/(NULLIF(SUM(IOS.num_of_reads),0)*1.0),0) AS ReadLatency,
		ISNULL(SUM(IOS.io_stall_write_ms)/(NULLIF(SUM(IOS.num_of_writes),0)*1.0),0) AS WriteLatency,
		ISNULL(SUM(IOS.io_stall_read_ms+IOS.io_stall_write_ms)/(NULLIF(SUM(IOS.num_of_writes+IOS.num_of_reads),0)*1.0),0) AS Latency,
		SUM(IOS.num_of_reads) num_of_reads,
		SUM(IOS.num_of_writes) num_of_writes,
		SUM(IOS.io_stall_write_ms) AS io_stall_write_ms,
		SUM(IOS.io_stall_read_ms) AS io_stall_read_ms,
		SUM(IOS.num_of_bytes_read) AS num_of_bytes_read,
		SUM(IOS.num_of_bytes_written) AS num_of_bytes_written,
		MAX(IOS.sample_ms_diff) AS sample_ms_diff,
		CAST(NULL AS DECIMAL(19,3)) AS MaxIOPs,
		CAST(NULL AS DECIMAL(19,3)) AS MaxReadIOPs,
		CAST(NULL AS DECIMAL(19,3)) AS MaxWriteIOPs,
		CAST(NULL AS DECIMAL(19,7)) MaxMBsec,
		CAST(NULL AS DECIMAL(19,7)) MaxReadMBsec,
		CAST(NULL AS DECIMAL(19,7))  MaxWriteMBsec,
		CAST(NULL AS DECIMAL(19,10)) AS MaxReadLatency,
		CAST(NULL AS DECIMAL(19,10))  AS MaxWriteLatency,
		CAST(NULL AS DECIMAL(19,10))  AS MaxLatency
FROM dbo.DBIOStats_60MIN IOS
JOIN dbo.DBFiles F ON IOS.FileID = F.FileID AND IOS.DatabaseID = F.DatabaseID
GROUP BY IOS.SnapshotDate,F.filegroup_name,IOS.InstanceID,IOS.DatabaseID