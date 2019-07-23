CREATE PROC OSInfo_Upd(
	@InstanceID INT,
	@SnapshotDate DATETIME2(2),
	@OSInfo OSInfo READONLY
)
AS
DECLARE @Ref VARCHAR(30)='OSInfo'
IF NOT EXISTS(SELECT * FROM dbo.CollectionDates WHERE InstanceID=@InstanceID AND Reference=@Ref AND SnapshotDate>=@SnapshotDate)
BEGIN
	UPDATE I
	SET I.affinity_type = t.affinity_type,
		I.cores_per_socket =t.cores_per_socket, 
		I.cpu_count = t.cpu_count, 
		I.hyperthread_ratio = t.hyperthread_ratio, 
		I.ms_ticks = t.ms_ticks,
		I.numa_node_count = t.numa_node_count, 
		I.os_priority_class = t.os_priority_class,
		I.physical_memory_kb = t.physical_memory_kb, 
		I.socket_count = t.socket_count, 
		I.softnuma_configuration = t.softnuma_configuration, 
		I.sql_memory_model = t.sql_memory_model, 
		I.sqlserver_start_time = t.sqlserver_start_time ,
		I.max_workers_count = t.max_workers_count,
		I.scheduler_count = t.scheduler_count
	FROM dbo.Instances I
	CROSS JOIN @OSInfo t 
	WHERE I.InstanceID = @InstanceID

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate
	
END