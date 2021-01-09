DECLARE @InstanceID INT

DECLARE @FromDate DATETIME2(2)

DECLARE C1 CURSOR FOR 
	SELECT instanceID
	FROM dbo.Instances i


OPEN c1
WHILE 1=1
BEGIN
	FETCH NEXT FROM c1 INTO @InstanceID

	SELECT @FromDate = ISNULL(DATEADD(hh,1,MAX(SnapshotDate)),'19000101')
	FROM dbo.DBIOStats_60MIN
	WHERE InstanceID = @InstanceID
	AND DatabaseID =-1
	AND Drive='*'
	AND FileID=-1

	IF @@FETCH_STATUS<>0
		BREAK
INSERT INTO dbo.DBIOStats_60MIN
(
    InstanceID,
    DatabaseID,
    Drive,
    FileID,
    SnapshotDate,
    num_of_reads,
    num_of_writes,
    num_of_bytes_read,
    num_of_bytes_written,
    io_stall_read_ms,
    io_stall_write_ms,
    sample_ms_diff,
    MaxReadLatency,
    MaxWriteLatency,
    MaxLatency,
    MaxReadIOPs,
    MaxWriteIOPs,
    MaxIOPs,
    MaxReadMBsec,
    MaxWriteMBsec,
    MaxMBsec
)
SELECT 	InstanceID,
		DatabaseID,
		Drive,
		FileID,
		CONVERT(DATETIME,SUBSTRING(CONVERT(VARCHAR,SnapshotDate,120),0,14) + ':00',120) AS SnapshotDate,
		SUM(num_of_reads) num_of_reads,
		SUM(num_of_writes) num_of_writes,
		SUM(num_of_bytes_read) num_of_bytes_read,
		SUM(num_of_bytes_written) num_of_bytes_written,
		SUM(io_stall_read_ms) io_stall_read_ms,
		SUM(io_stall_write_ms) io_stall_write_ms,
		SUM(IOS.sample_ms_diff) sample_ms_diff,
		ISNULL(MAX(IOS.io_stall_read_ms/NULLIF(IOS.num_of_reads*1.0,0)),0) AS MaxReadLatency,
		ISNULL(MAX(IOS.io_stall_write_ms/NULLIF(IOS.num_of_writes*1.0,0)),0) AS MaxWriteLatency,
		ISNULL(MAX((IOS.io_stall_read_ms+IOS.io_stall_write_ms)/NULLIF(IOS.num_of_writes+IOS.num_of_reads*1.0,0)),0) AS MaxLatency,
		MAX(IOS.num_of_reads/(IOS.sample_ms_diff/1000.0))  AS MaxReadIOPs,
		MAX(IOS.num_of_writes/(IOS.sample_ms_diff/1000.0))  AS MaxWriteIOPs,
		MAX((IOS.num_of_reads+IOS.num_of_writes)/(IOS.sample_ms_diff/1000.0)) AS MaxIOPs,
		MAX(IOS.num_of_bytes_read/(IOS.sample_ms_diff/1000.0))/POWER(1024.0,2) AS MaxReadMBsec,
		MAX(IOS.num_of_bytes_written/(IOS.sample_ms_diff/1000.0))/POWER(1024.0,2) AS MaxWriteMBsec,
		MAX((IOS.num_of_bytes_written+IOS.num_of_bytes_read)/(IOS.sample_ms_diff/1000.0))/POWER(1024.0,2) AS MaxMBsec	
FROM dbo.DBIOStats IOS
WHERE InstanceID = @InstanceID
AND SnapshotDate >= @FromDate
GROUP BY InstanceID,
		DatabaseID,
		Drive,
		FileID,
		CONVERT(DATETIME,SUBSTRING(CONVERT(VARCHAR,SnapshotDate,120),0,14) + ':00',120) 
OPTION(RECOMPILE)
END
CLOSE c1 
DEALLOCATE c1