IF OBJECT_ID('sys.dm_os_volume_stats') IS NULL
BEGIN
	RETURN
END
ELSE IF  SERVERPROPERTY('EngineEdition') = 8
BEGIN
	SELECT DISTINCT dovs.volume_mount_point AS Name,
		dovs.total_bytes + dovs.available_bytes as Capacity, /* Total Bytes = Used for Azure MI */
		dovs.available_bytes as FreeSpace,
		dovs.logical_volume_name as Label
	FROM sys.master_files mf
	CROSS APPLY sys.dm_os_volume_stats(mf.database_id, mf.FILE_ID) dovs
END
ELSE
BEGIN
	SELECT DISTINCT dovs.volume_mount_point AS Name,
		dovs.total_bytes as Capacity,
		dovs.available_bytes as FreeSpace,
		dovs.logical_volume_name as Label
	FROM sys.master_files mf
	CROSS APPLY sys.dm_os_volume_stats(mf.database_id, mf.FILE_ID) dovs
END
