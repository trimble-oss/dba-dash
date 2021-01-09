
CREATE PROC [dbo].[OSInfo_Upd](
	@InstanceID INT,
	@SnapshotDate DATETIME2(2),
	@OSInfo OSInfo READONLY
)
AS
DECLARE @Ref VARCHAR(30)='OSInfo'
IF NOT EXISTS(SELECT * FROM dbo.CollectionDates WHERE InstanceID=@InstanceID AND Reference=@Ref AND SnapshotDate>=@SnapshotDate)
BEGIN
	IF EXISTS(
		SELECT I.cores_per_socket,I.cpu_count,I.hyperthread_ratio,I.physical_memory_kb,I.socket_count
		FROM dbo.Instances I
		WHERE I.InstanceID = @InstanceID
		AND NOT(I.cores_per_socket IS NULL
				AND I.cpu_count IS NULL
				AND I.hyperthread_ratio IS NULL
				AND I.physical_memory_kb IS NULL
				AND I.socket_count IS NULL)
		EXCEPT
		SELECT I.cores_per_socket,I.cpu_count,I.hyperthread_ratio,I.physical_memory_kb,I.socket_count 
		FROM @OSInfo I
	)
	BEGIN
		IF NOT EXISTS(SELECT 1 FROM dbo.HostUpgradeHistory WHERE InstanceID = @InstanceID AND ChangeDate = @SnapshotDate)
		BEGIN
			INSERT INTO dbo.HostUpgradeHistory(InstanceID,ChangeDate)
			VALUES(@InstanceID,@SnapshotDate)
		END

		UPDATE HUH
		SET HUH.cores_per_socket_old = I.cores_per_socket,
			HUH.cores_per_socket_new = T.cores_per_socket,
			HUH.cpu_count_old = I.cpu_count,
			HUH.cpu_count_new = t.cpu_count,
			HUH.hyperthread_ratio_old=I.hyperthread_ratio,
			HUH.hyperthread_ratio_new = t.hyperthread_ratio,
			HUH.physical_memory_kb_old = I.physical_memory_kb,
			HUH.physical_memory_kb_new = t.physical_memory_kb,
			HUH.socket_count_old = I.socket_count,
			HUH.socket_count_new = t.socket_count
		FROM dbo.HostUpgradeHistory HUH
		JOIN dbo.Instances I ON I.InstanceID = HUH.InstanceID 
		CROSS JOIN @OSInfo t 
		WHERE HUH.InstanceID = @InstanceID
		AND HUH.ChangeDate = @SnapshotDate
	END

	

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
		I.scheduler_count = t.scheduler_count,
		I.UTCOffset = t.UTCOffset
	FROM dbo.Instances I
	CROSS JOIN @OSInfo t 
	WHERE I.InstanceID = @InstanceID

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate
	
END