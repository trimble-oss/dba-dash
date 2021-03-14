IF SERVERPROPERTY('EditionID')=1674378470
BEGIN
	SELECT GETUTCDATE() AS SnapshotDate,
		   [database_id],
		   [file_id],
		   [sample_ms],
		   [num_of_reads],
		   [num_of_bytes_read],
		   [io_stall_read_ms],
		   [num_of_writes],
		   [num_of_bytes_written],
		   [io_stall_write_ms],
		   [io_stall],
		   [size_on_disk_bytes]
	FROM sys.dm_io_virtual_file_stats(DB_ID(), NULL) vfs;
END
ELSE
BEGIN
	SELECT GETUTCDATE() AS SnapshotDate,
		   [database_id],
		   [file_id],
		   [sample_ms],
		   [num_of_reads],
		   [num_of_bytes_read],
		   [io_stall_read_ms],
		   [num_of_writes],
		   [num_of_bytes_written],
		   [io_stall_write_ms],
		   [io_stall],
		   [size_on_disk_bytes]
	FROM sys.dm_io_virtual_file_stats(NULL, NULL) vfs;
END