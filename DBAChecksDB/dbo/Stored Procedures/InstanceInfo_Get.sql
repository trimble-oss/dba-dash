CREATE PROC dbo.InstanceInfo_Get(@InstanceID INT)
AS
WITH T AS (
SELECT CAST(I.Collation AS NVARCHAR(MAX)) AS Collation,
	CAST(I.SQLVersion AS NVARCHAR(MAX)) AS SQLVersion,
    CAST(CASE WHEN I.InstantFileInitializationEnabled=1THEN N'Yes' ELSE N'No' END AS NVARCHAR(MAX)) AS [Instant File Initialization],
	CAST(I.ActivePowerPlan AS NVARCHAR(MAX)) AS [Power Plan],
	CAST(I.ProcessorNameString AS NVARCHAR(MAX)) AS [Processor],
	CAST('CPUs: ' + FORMAT(I.cpu_count,'N0') + ', Cores: ' + FORMAT(I.cpu_core_count,'N0') + ', Sockets: ' + FORMAT(I.socket_count,'N0') + ', Physical CPUs: ' + FORMAT(I.physical_cpu_count,'N0') 
			+ ', HT Ratio: ' + FORMAT(I.hyperthread_ratio,'N0') + ', NUMA nodes: ' + FORMAT(I.numa_node_count,'N0') 
			+ ', Soft NUMA: ' + I.softnuma_configuration_desc + ', Affinity: ' + I.affinity_type_desc AS NVARCHAR(MAX)) AS [Processor Info],
	CAST(I.OfflineSchedulers AS NVARCHAR(MAX)) AS [Offline Schedulers],
	CAST(FORMAT(I.physical_memory_kb/POWER(1024,2),'N1') + 'GB, Allocated to buffer pool: ' + FORMAT(I.BufferPoolMB,'N0') + 'MB (' + FORMAT(I.PctMemoryAllocatedToBufferPool,'P1') + ') ' +
			', Memory Model: ' + I.sql_memory_model_desc  AS NVARCHAR(MAX)) AS Memory,
	CAST(I.SystemManufacturer AS NVARCHAR(MAX)) Manufacturer,
	CAST(I.SystemProductName AS NVARCHAR(MAX)) Model,
	CAST('Files: ' + FORMAT(tdb.TempDBFileCount,'N0') + ', Total Size: ' + FORMAT(tdb.TempDBTotalMB,'N0') + 'MB, ' + CASE WHEN tdb.IsTempDBFileSizeEven=1 THEN 'Even file size of ' + FORMAT(tdb.TempDBFileSizeMB,'N0') +'MB' ELSE '***Uneven Sizes***' END AS NVARCHAR(MAX)) AS [TempDB Configuration],
	CAST(CASE WHEN I.IsClustered =1 THEN 'Yes' ELSE 'No' END AS NVARCHAR(MAX)) AS [Clustered],
	CAST(I.Instance AS NVARCHAR(MAX)) AS Instance
FROM dbo.InstanceInfo I
OUTER APPLY(SELECT COUNT(*) AS TempDBFileCount,SUM(f.size)/128 TempDBTotalMB, CASE WHEN COUNT(DISTINCT f.size)=1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsTempDBFileSizeEven,MAX(f.size)/128 AS TempDBFileSizeMB
		FROM dbo.Databases d 
		JOIN dbo.DBFiles f ON f.DatabaseID = d.DatabaseID
		WHERE d.InstanceID=i.InstanceID
		AND d.IsActive=1
		AND f.IsActive=1
		AND d.name='tempdb'
		AND f.type=0) tdb
WHERE I.InstanceID=@InstanceID
)
SELECT Name,Value 
FROM T 
UNPIVOT(Value FOR Name IN(Collation,SQLVersion,[Instant File Initialization],[Power Plan],[Processor],Memory,Manufacturer,Model,[TempDB Configuration],[Processor Info],[Offline Schedulers],[Clustered],[Instance])) u
ORDER BY Name