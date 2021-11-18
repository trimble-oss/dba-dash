CREATE PROC dbo.Hardware_Get(
	@InstanceIDs VARCHAR(MAX)=NULL
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
SELECT ConnectionID,
	SystemManufacturer,
	SystemProductName,
	ProcessorNameString,
	SQLVersion,
	cores_per_socket,
	socket_count,
	cpu_count,
	cpu_core_count,
	physical_cpu_count,
	hyperthread_ratio,
	numa_node_count,
	softnuma_configuration_desc,
	affinity_type_desc,
	PhysicalMemoryGB,
	BufferPoolMB,
	I.MemoryNotAllocatedToBufferPoolGB,
	PctMemoryAllocatedToBufferPool,
	sql_memory_model_desc,
	OfflineSchedulers,
	ActivePowerPlan,
	I.ActivePowerPlanGUID,
	I.os_priority_class,
	os_priority_class_desc,
	InstantFileInitializationEnabled,
	I.scheduler_count,
	I.max_workers_count,
	I.os_priority_class
FROM dbo.InstanceInfo I
WHERE EngineEdition<>5 -- AzureDB
AND EXISTS(SELECT 1 FROM @Instances t WHERE t.InstanceID = I.InstanceID)