CREATE PROC [dbo].[IOStats_Upd](@IOStats dbo.IOStats READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='IOStats'
DECLARE @EditionID INT 
SELECT @EditionID = EditionID 
FROM dbo.Instances 
WHERE InstanceID = @InstanceID


CREATE TABLE #DBIOStatsTemp(
	[InstanceID] [int] NOT NULL,
	[DatabaseID] [int] NOT NULL,
	[Drive] [char](1) COLLATE DATABASE_DEFAULT NOT NULL ,
	[FileID] [int] NOT NULL,
	[SnapshotDate] [datetime2](2) NOT NULL,
	[num_of_reads] [bigint] NOT NULL,
	[num_of_writes] [bigint] NOT NULL,
	[num_of_bytes_read] [bigint] NOT NULL,
	[num_of_bytes_written] [bigint] NOT NULL,
	[io_stall_read_ms] [bigint] NOT NULL,
	[io_stall_write_ms] [bigint] NOT NULL,
	[sample_ms_diff] [bigint] NOT NULL,
	[size_on_disk_bytes] [bigint] NOT NULL,
	PRIMARY KEY([InstanceID],[DatabaseID],[Drive],[FileID],	[SnapshotDate])
)

IF @EditionID=1674378470
BEGIN
	INSERT INTO #DBIOStatsTemp
	(
		InstanceID,
		SnapshotDate,
		DatabaseID,
		Drive,
		FileID,
		sample_ms_diff,
		num_of_reads,
		num_of_bytes_read,
		io_stall_read_ms,
		num_of_writes,
		num_of_bytes_written,
		io_stall_write_ms,
		size_on_disk_bytes
	)
	SELECT @InstanceID,
			A.SnapshotDate,
			ISNULL(x.DatabaseID,-1) AS DatabaseID,
			ISNULL(x.Drive,'*') AS Drive,
			ISNULL(x.FileID,-1) AS FileID,
			MAX(A.sample_ms-B.sample_ms) AS sample_ms_diff,
			SUM(A.num_of_reads-B.num_of_reads),
			SUM(A.num_of_bytes_read-B.num_of_bytes_read),
			SUM(A.io_stall_read_ms-B.io_stall_read_ms),
			SUM(A.num_of_writes-B.num_of_writes),
			SUM(A.num_of_bytes_written-B.num_of_bytes_written),
			SUM(A.io_stall_write_ms-B.io_stall_write_ms),
			SUM(A.size_on_disk_bytes)
	FROM @IOStats a
	JOIN Staging.IOStats b ON b.database_id = a.database_id AND b.file_id = a.file_id AND b.InstanceID=@InstanceID
	LEFT JOIN dbo.Databases D ON D.database_id = a.database_id AND d.InstanceID=@InstanceID AND D.IsActive=1
	LEFT JOIN dbo.DBFiles F ON F.file_id = a.file_id AND F.DatabaseID = D.DatabaseID AND F.IsActive=1 
	CROSS APPLY(SELECT ISNULL(F.DatabaseID,-999) AS DatabaseID,
						ISNULL(F.FileID,-999) AS FileID,
						'?' AS Drive,
						@InstanceID AS InstanceID
						) x
	WHERE A.sample_ms > b.sample_ms
				AND A.snapshotdate > B.snapshotdate
				AND A.num_of_bytes_read>=B.num_of_bytes_read
				AND A.num_of_reads>=B.num_of_reads
				AND A.num_of_writes>=B.num_of_writes
				AND A.num_of_bytes_written>= B.num_of_bytes_written
				AND NOT (A.num_of_reads=B.num_of_reads AND A.num_of_writes=B.num_of_writes)
	GROUP BY GROUPING SETS(
				(x.InstanceID),
				(x.DatabaseID,x.FileID,x.Drive)
				)
	,a.SnapshotDate
END 
ELSE
BEGIN
	INSERT INTO #DBIOStatsTemp
	(
		InstanceID,
		SnapshotDate,
		DatabaseID,
		Drive,
		FileID,
		sample_ms_diff,
		num_of_reads,
		num_of_bytes_read,
		io_stall_read_ms,
		num_of_writes,
		num_of_bytes_written,
		io_stall_write_ms,
		size_on_disk_bytes
	)
	SELECT @InstanceID,
			A.SnapshotDate,
			ISNULL(x.DatabaseID,-1) AS DatabaseID,
			ISNULL(x.Drive,'*') AS Drive,
			ISNULL(x.FileID,-1) AS FileID,
			MAX(A.sample_ms-B.sample_ms) AS sample_ms_diff,
			SUM(A.num_of_reads-B.num_of_reads),
			SUM(A.num_of_bytes_read-B.num_of_bytes_read),
			SUM(A.io_stall_read_ms-B.io_stall_read_ms),
			SUM(A.num_of_writes-B.num_of_writes),
			SUM(A.num_of_bytes_written-B.num_of_bytes_written),
			SUM(A.io_stall_write_ms-B.io_stall_write_ms),
			SUM(A.size_on_disk_bytes)
	FROM @IOStats a
	JOIN Staging.IOStats b ON b.database_id = a.database_id AND b.file_id = a.file_id AND b.InstanceID=@InstanceID
	LEFT JOIN dbo.Databases D ON D.database_id = a.database_id AND d.InstanceID=@InstanceID AND D.IsActive=1
	LEFT JOIN dbo.DBFiles F ON F.file_id = a.file_id AND F.DatabaseID = D.DatabaseID AND F.IsActive=1 
	CROSS APPLY(SELECT ISNULL(F.DatabaseID,-999) AS DatabaseID,
						ISNULL(F.FileID,-999) AS FileID,
						CASE WHEN F.physical_name LIKE '_:\%' THEN LEFT(F.physical_name,1) ELSE '?' END AS Drive,
						@InstanceID AS InstanceID
						) x
	WHERE A.sample_ms > b.sample_ms
				AND A.snapshotdate > B.snapshotdate
				AND A.num_of_bytes_read>=B.num_of_bytes_read
				AND A.num_of_reads>=B.num_of_reads
				AND A.num_of_writes>=B.num_of_writes
				AND A.num_of_bytes_written>= B.num_of_bytes_written
				AND NOT (A.num_of_reads=B.num_of_reads AND A.num_of_writes=B.num_of_writes)
	GROUP BY GROUPING SETS(
				(x.InstanceID),
				(x.DatabaseID),
				(x.DatabaseID,x.Drive),
				(x.DatabaseID,x.FileID,x.Drive),
				(x.Drive)
				)
	,a.SnapshotDate
END

BEGIN TRAN

INSERT INTO dbo.DBIOStats
(
	InstanceID,
	SnapshotDate,
	DatabaseID,
	Drive,
	FileID,
	sample_ms_diff,
	num_of_reads,
	num_of_bytes_read,
	io_stall_read_ms,
	num_of_writes,
	num_of_bytes_written,
	io_stall_write_ms,
	size_on_disk_bytes
)
SELECT 	InstanceID,
	SnapshotDate,
	DatabaseID,
	Drive,
	FileID,
	sample_ms_diff,
	num_of_reads,
	num_of_bytes_read,
	io_stall_read_ms,
	num_of_writes,
	num_of_bytes_written,
	io_stall_write_ms,
	size_on_disk_bytes
FROM #DBIOStatsTemp t
WHERE NOT EXISTS(SELECT 1 
			FROM dbo.DBIOStats IOS 
			WHERE t.InstanceID = IOS.InstanceID
			AND t.DatabaseID = IOS.DatabaseID
			AND t.Drive = IOS.Drive
			AND t.FileID = IOS.FileID
			AND t.SnapshotDate = IOS.SnapshotDate			
			)

IF @@ROWCOUNT>0
BEGIN;
	MERGE dbo.DBIOStats_60MIN AS T 
	USING (SELECT InstanceID,
				  DatabaseID,
				  Drive,
				  FileID,
				  DG.DateGroup AS SnapshotDate,
				  num_of_reads,
				  num_of_writes,
				  num_of_bytes_read,
				  num_of_bytes_written,
				  io_stall_read_ms,
				  io_stall_write_ms,
				  sample_ms_diff,
				  size_on_disk_bytes,
				  ISNULL(io_stall_read_ms/NULLIF(num_of_reads*1.0,0),0) AS MaxReadLatency,
				  ISNULL(io_stall_write_ms/NULLIF(num_of_writes*1.0,0),0) AS MaxWriteLatency,
				  ISNULL((io_stall_read_ms+io_stall_write_ms)/NULLIF(num_of_writes+num_of_reads*1.0,0),0) AS MaxLatency,
				  num_of_reads/(sample_ms_diff/1000.0)  AS MaxReadIOPs,
				  num_of_writes/(sample_ms_diff/1000.0)  AS MaxWriteIOPs,
				  (num_of_reads+num_of_writes)/(sample_ms_diff/1000.0) AS MaxIOPs,
				  num_of_bytes_read/(sample_ms_diff/1000.0)/POWER(1024.0,2) AS MaxReadMBsec,
				  num_of_bytes_written/(sample_ms_diff/1000.0)/POWER(1024.0,2) AS MaxWriteMBsec,
				  (num_of_bytes_written+num_of_bytes_read)/(sample_ms_diff/1000.0)/POWER(1024.0,2) AS MaxMBsec	
				  FROM #DBIOStatsTemp
				  CROSS APPLY [dbo].[DateGroupingMins](SnapshotDate,60) DG
				  ) AS S
	ON S.InstanceID= T.InstanceID
	AND S.DatabaseID = T.DatabaseID
	AND S.Drive = T.Drive
	AND S.FileID = T.FileID
	AND S.SnapshotDate = T.SnapshotDate
	WHEN MATCHED THEN UPDATE SET T.num_of_reads += s.num_of_reads,
								T.num_of_writes += S.num_of_writes,
								T.num_of_bytes_read += S.num_of_bytes_read,
								T.num_of_bytes_written += S.num_of_bytes_written,
								T.io_stall_read_ms += S.io_stall_read_ms,
								T.io_stall_write_ms += S.io_stall_write_ms,
								T.sample_ms_diff += S.sample_ms_diff,
								T.MaxReadLatency = CASE WHEN S.MaxReadLatency > t.MaxReadLatency THEN S.MaxReadLatency ELSE T.MaxReadLatency END,
								T.MaxWriteLatency = CASE WHEN S.MaxWriteLatency > t.MaxWriteLatency THEN S.MaxReadLatency ELSE T.MaxReadLatency END,
								T.MaxLatency = CASE WHEN S.MaxLatency > t.MaxLatency THEN S.MaxLatency ELSE T.MaxLatency END,
								T.MaxReadIOPs = CASE WHEN S.MaxReadIOPs > t.MaxReadIOPs THEN S.MaxReadIOPs ELSE T.MaxReadIOPs END,
								T.MaxWriteIOPs = CASE WHEN S.MaxWriteIOPs > t.MaxWriteIOPs THEN S.MaxWriteIOPs ELSE T.MaxWriteIOPs END,
								T.MaxIOPs = CASE WHEN S.MaxIOPs > t.MaxIOPs THEN S.MaxIOPs ELSE T.MaxIOPs END,
								T.MaxReadMBsec = CASE WHEN S.MaxReadMBsec > t.MaxReadMBsec THEN S.MaxReadMBsec ELSE T.MaxReadMBsec END,
								T.MaxWriteMBsec = CASE WHEN S.MaxWriteMBsec > t.MaxWriteMBsec THEN S.MaxWriteMBsec ELSE T.MaxWriteMBsec END,
								T.MaxMBsec =CASE WHEN S.MaxMBsec > t.MaxMBsec THEN S.MaxMBsec ELSE T.MaxMBsec END

	WHEN NOT MATCHED BY TARGET THEN INSERT
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
	VALUES(    InstanceID,
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
		MaxMBsec);

END

DELETE Staging.IOStats
WHERE InstanceID=@InstanceID
INSERT INTO staging.IOStats
(
    InstanceID,
    SnapshotDate,
    database_id,
    file_id,
    sample_ms,
    num_of_reads,
    num_of_bytes_read,
    io_stall_read_ms,
    num_of_writes,
    num_of_bytes_written,
    io_stall_write_ms,
    io_stall,
    size_on_disk_bytes
)
SELECT @InstanceID InstanceID,
    SnapshotDate,
    database_id,
    file_id,
    sample_ms,
    num_of_reads,
    num_of_bytes_read,
    io_stall_read_ms,
    num_of_writes,
    num_of_bytes_written,
    io_stall_write_ms,
    io_stall,
    size_on_disk_bytes
FROM @IOStats

COMMIT

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										@Reference = @Ref,
										@SnapshotDate = @SnapshotDate