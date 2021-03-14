IF OBJECT_ID('sys.dm_os_volume_stats') IS NOT NULL
BEGIN
	SELECT DISTINCT dovs.volume_mount_point AS Name,
		dovs.total_bytes as Capacity,
		dovs.available_bytes as FreeSpace,
		dovs.logical_volume_name as Label
	FROM sys.master_files mf
	CROSS APPLY sys.dm_os_volume_stats(mf.database_id, mf.FILE_ID) dovs
END