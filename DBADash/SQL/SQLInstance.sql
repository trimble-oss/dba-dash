IF OBJECT_ID('sys.dm_os_host_info') IS NOT NULL
BEGIN
	SELECT host_platform,
		host_distribution,
		host_release,
		host_service_pack_level,
		host_sku,os_language_version,
		@@SERVERNAME as Instance,
		GETUTCDATE() As SnapshotDateUTC,
		CAST(SERVERPROPERTY('EditionID') as bigint) as EditionID,
		ISNULL(CAST(SERVERPROPERTY('ComputerNamePhysicalNetBIOS') as nvarchar(128)),'') as ComputerNamePhysicalNetBIOS,
		DB_NAME() as DBName,SERVERPROPERTY ('productversion') as ProductVersion
	FROM sys.dm_os_host_info
END
ELSE IF OBJECT_ID('sys.dm_os_windows_info') IS NOT NULL
BEGIN
	SELECT 'Windows' as host_platform,
		null as host_distribution,
		windows_release as host_release,
		windows_service_pack_level as host_service_pack_level,
		windows_sku as host_sku,
		os_language_version,
		@@SERVERNAME as Instance,
		GETUTCDATE() As SnapshotDateUTC,
		CAST(SERVERPROPERTY('EditionID') as bigint) as EditionID,
		ISNULL(CAST(SERVERPROPERTY('ComputerNamePhysicalNetBIOS') as nvarchar(128)),'') as ComputerNamePhysicalNetBIOS,
		DB_NAME() as DBName,SERVERPROPERTY ('productversion') as ProductVersion
	FROM sys.dm_os_windows_info
END
ELSE
BEGIN
	SELECT 'Windows' as host_platform,
		null as host_distribution,
		null as host_release,'' as host_service_pack_level,
		null as host_sku,
		null os_language_version,
		@@SERVERNAME as Instance,
		GETUTCDATE() As SnapshotDateUTC,
		CAST(SERVERPROPERTY('EditionID') as bigint) as EditionID,
		ISNULL(CAST(SERVERPROPERTY('ComputerNamePhysicalNetBIOS') as nvarchar(128)),'') as ComputerNamePhysicalNetBIOS,
		DB_NAME() as DBName,
		SERVERPROPERTY ('productversion') as ProductVersion
END