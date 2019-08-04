CREATE PROC IOStats_Upd(@IOStats dbo.IOStats READONLY,@InstanceID INT,@SnapshotDate DATETIME)
AS
INSERT INTO dbo.IOStats
(
    InstanceID,
    SnapshotDate,
    database_id,
    file_id,
	FileID,
    sample_ms_diff,
    num_of_reads,
    num_of_bytes_read,
    io_stall_read_ms,
    num_of_writes,
    num_of_bytes_written,
    io_stall_write_ms,
    io_stall,
    size_on_disk_bytes
)
SELECT @InstanceID,
		A.SnapshotDate,
		A.database_id,
		a.file_id,
		F.FileID,
		A.sample_ms-B.sample_ms,
		A.num_of_reads-B.num_of_reads,
		A.num_of_bytes_read-B.num_of_bytes_read,
		A.io_stall_read_ms-B.io_stall_read_ms,
		A.num_of_writes-B.num_of_writes,
		A.num_of_bytes_written-B.num_of_bytes_written,
		A.io_stall_write_ms-B.io_stall_write_ms,
		A.io_stall - b.io_stall,
		A.size_on_disk_bytes
FROM @IOStats a
JOIN Staging.IOStats b ON b.database_id = a.database_id AND b.file_id = a.file_id AND b.InstanceID=@InstanceID
LEFT JOIN dbo.Databases D ON D.database_id = a.database_id AND d.InstanceID=@InstanceID AND D.IsActive=1
LEFT JOIN dbo.DBFiles F ON F.file_id = a.file_id AND F.DatabaseID = D.DatabaseID AND F.IsActive=1 
WHERE A.sample_ms > b.sample_ms
			AND A.snapshotdate > B.snapshotdate
			AND A.num_of_bytes_read>=B.num_of_bytes_read
			AND A.num_of_reads>=B.num_of_reads
			AND A.num_of_writes>=B.num_of_writes
			AND A.num_of_bytes_written>= B.num_of_bytes_written

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