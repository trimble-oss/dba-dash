IF OBJECT_ID('sys.dm_os_volume_stats') IS NULL
BEGIN
	RETURN
END
ELSE IF  SERVERPROPERTY('EngineEdition') = 8
BEGIN
	SELECT  dovs.volume_mount_point AS Name,
			AVG(dovs.total_bytes + dovs.available_bytes) as Capacity, /* Total Bytes = Used for Azure MI */
			AVG(dovs.available_bytes) as FreeSpace,
			dovs.logical_volume_name as Label
	FROM sys.master_files mf
	CROSS APPLY sys.dm_os_volume_stats(mf.database_id, mf.file_id) dovs
	GROUP BY dovs.volume_mount_point,
			 dovs.logical_volume_name;
END
ELSE
BEGIN
	SELECT  dovs.volume_mount_point AS Name,
			AVG(dovs.total_bytes) as Capacity,
			AVG(dovs.available_bytes) as FreeSpace,
			dovs.logical_volume_name as Label
	FROM sys.master_files mf
	CROSS APPLY sys.dm_os_volume_stats(mf.database_id, mf.file_id) dovs
	GROUP BY dovs.volume_mount_point,
			 dovs.logical_volume_name;
END
