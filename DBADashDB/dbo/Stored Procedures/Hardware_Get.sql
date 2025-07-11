CREATE PROC dbo.Hardware_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@ShowHidden BIT=1
)
AS
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;
SELECT I.InstanceID,
	I.ConnectionID,
	I.InstanceDisplayName,
	I.SystemManufacturer,
	I.SystemProductName,
	I.ProcessorNameString,
	I.SQLVersion,
	I.cores_per_socket,
	I.socket_count,
	I.cpu_count,
	I.cpu_core_count,
	I.physical_cpu_count,
	I.hyperthread_ratio,
	I.numa_node_count,
	I.softnuma_configuration_desc,
	I.affinity_type_desc,
	I.PhysicalMemoryGB,
	I.BufferPoolMB,
	I.MemoryNotAllocatedToBufferPoolGB,
	I.PctMemoryAllocatedToBufferPool,
	I.sql_memory_model_desc,
	I.OfflineSchedulers,
	I.ActivePowerPlan,
	I.ActivePowerPlanGUID,
	I.os_priority_class,
	I.os_priority_class_desc,
	I.InstantFileInitializationEnabled,
	I.scheduler_count,
	I.max_workers_count,
	I.os_priority_class,
	M.VMSize
FROM dbo.InstanceInfo I
LEFT JOIN dbo.InstanceMetadata IMD ON I.InstanceID = IMD.InstanceID
OUTER APPLY dbo.ParseInstanceMetadata(IMD.Metadata) M
WHERE EXISTS(
			SELECT 1 
			FROM @Instances t 
			WHERE t.InstanceID = I.InstanceID
			)
AND (I.ShowInSummary=1 OR @ShowHidden=1)