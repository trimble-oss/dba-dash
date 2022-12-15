CREATE PROC dbo.HostUpgradeHistory_Get(
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
SELECT I.Instance,
	   I.InstanceDisplayName,
	   HUH.ChangeDate,
       HUH.SystemManufacturerOld,
       HUH.SystemManufacturerNew,
	   HUH.SystemProductNameOld,
	   HUH.SystemProductNameNew,
       HUH.Processor_old,
       HUH.Processor_new,
       HUH.cpu_count_old,
       HUH.cpu_count_new,
       HUH.cores_per_socket_old,
       HUH.cores_per_socket_new,
	   HUH.socket_count_old,
	   HUH.socket_count_new,
       HUH.hyperthread_ratio_old,
       HUH.hyperthread_ratio_new,
	   HUH.physical_memory_kb_old,
	   HUH.physical_memory_kb_new,
	   HUH.physical_memory_kb_old/POWER(1024.0,2) AS physical_memory_gb_old,
	   HUH.physical_memory_kb_new/POWER(1024.0,2) AS physical_memory_gb_new
FROM dbo.HostUpgradeHistory HUH
    JOIN dbo.Instances I ON HUH.InstanceID = I.InstanceID
WHERE EXISTS(
			SELECT 1 
			FROM @Instances t 
			WHERE t.InstanceID = HUH.InstanceID
			)
AND (I.ShowInSummary=1 OR @ShowHidden=1)
ORDER BY HUH.ChangeDate DESC;