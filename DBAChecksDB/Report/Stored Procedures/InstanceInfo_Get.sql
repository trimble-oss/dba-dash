CREATE PROC [Report].[InstanceInfo_Get](@InstanceID INT)
AS
SELECT InstanceID,
	I.Instance,
	I.SQLVersion,
	I.ProcessorNameString,
	I.ActivePowerPlan,
	I.SystemManufacturer,
	I.SystemProductName,
	I.PhysicalMemoryGB,
	I.BufferPoolMB,
	I.cores_per_socket,
	I.cpu_core_count,
	I.socket_count,
	I.physical_cpu_count,
	I.PctMemoryAllocatedToBufferPool,
	I.sql_memory_model_desc,
	I.OfflineSchedulers,
	I.softnuma_configuration_desc,
	I.InstantFileInitializationEnabled,
	I.host_up_time_mins,
	I.hyperthread_ratio,
	I.sqlserver_uptime,
	tdb.TempDBFileCount,
	tdb.TempDBTotalMB,
	tdb.IsTempDBFileSizeEven,
	tdb.TempDBFileSizeMB
FROM dbo.InstanceInfo I
OUTER APPLY(SELECT COUNT(*) AS TempDBFileCount,SUM(f.size)/128 TempDBTotalMB, CASE WHEN COUNT(DISTINCT f.size)=1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsTempDBFileSizeEven,MAX(f.size)/128 AS TempDBFileSizeMB
		FROM dbo.Databases d 
		JOIN dbo.DBFiles f ON f.DatabaseID = d.DatabaseID
		WHERE d.InstanceID=i.InstanceID
		AND d.IsActive=1
		AND f.IsActive=1
		AND d.name='tempdb'
		AND f.type=0) tdb
WHERE I.InstanceID = @InstanceID