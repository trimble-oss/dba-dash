/*  
	DECLARE @IOCollectionLevel INT       
	Full = 1,
	InstanceOnly = 2,
	Drive = 3,
	Database = 4,
	DriveAndDatabase = 5
*/
IF SERVERPROPERTY('EngineEdition')=5 -- Azure SQL Database
BEGIN
	SELECT GETUTCDATE() AS SnapshotDate,
		   vfs.database_id,
		   vfs.file_id,
		   vfs.sample_ms,
		   vfs.num_of_reads,
		   vfs.num_of_bytes_read,
		   vfs.io_stall_read_ms,
		   vfs.num_of_writes,
		   vfs.num_of_bytes_written,
		   vfs.io_stall_write_ms,
		   vfs.io_stall,
		   vfs.size_on_disk_bytes,
		   CAST('-' AS CHAR(1)) AS drive
	FROM sys.dm_io_virtual_file_stats(DB_ID(), NULL) vfs
END
ELSE
BEGIN
	IF @IOCollectionLevel = 1 /* File Level - no aggregation */
	BEGIN
		SELECT GETUTCDATE() AS SnapshotDate,
			   vfs.database_id,
			   vfs.file_id,
			   vfs.sample_ms,
			   vfs.num_of_reads,
			   vfs.num_of_bytes_read,
			   vfs.io_stall_read_ms,
			   vfs.num_of_writes,
			   vfs.num_of_bytes_written,
			   vfs.io_stall_write_ms,
			   vfs.io_stall,
			   vfs.size_on_disk_bytes,
			   CAST(LEFT(mf.physical_name,1) AS CHAR(1)) AS drive
		FROM sys.dm_io_virtual_file_stats(NULL, NULL) vfs
		JOIN sys.master_files mf on vfs.database_id = mf.database_id AND vfs.file_id = mf.file_id;
	END
	ELSE IF @IOCollectionLevel IN(2,3,4,5)
	BEGIN
		SELECT GETUTCDATE() AS SnapshotDate,
				CASE WHEN @IOCollectionLevel IN(4,5) THEN mf.database_id ELSE -1 END AS database_id, 
				-1 AS file_id,
				MAX(vfs.sample_ms) AS sample_ms,
				SUM(vfs.num_of_reads) AS num_of_reads,
				SUM(vfs.num_of_bytes_read) AS num_of_bytes_read,
				SUM(vfs.io_stall_read_ms) AS io_stall_read_ms,
				SUM(vfs.num_of_writes) AS num_of_writes,
				SUM(vfs.num_of_bytes_written) AS num_of_bytes_written,
				SUM(vfs.io_stall_write_ms) AS io_stall_write_ms,
				SUM(vfs.io_stall) AS io_stall,
				SUM(vfs.size_on_disk_bytes) AS size_on_disk_bytes,
				CASE WHEN @IOCollectionLevel IN(3,5) THEN CAST(LEFT(mf.physical_name,1) AS CHAR(1)) ELSE '-' END AS drive
		FROM sys.dm_io_virtual_file_stats(NULL, NULL) vfs
		JOIN sys.master_files mf on vfs.database_id = mf.database_id AND vfs.file_id = mf.file_id
		GROUP BY CASE WHEN @IOCollectionLevel IN(3,5) THEN CAST(LEFT(mf.physical_name,1) AS CHAR(1)) ELSE '-' END,
				CASE WHEN @IOCollectionLevel IN(4,5) THEN mf.database_id ELSE -1 END

	END
	ELSE
	BEGIN
		DECLARE @Msg VARCHAR(1000)
		SET @Msg = 'Invalid @IOCollectionLevel: ' + CAST(@IOCollectionLevel AS VARCHAR(10))
		RAISERROR(@Msg,11,1)
	END
END