CREATE PROC Report.HostUpgrade(@InstanceIDs VARCHAR(MAX)=NULL)
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
	SELECT Item
	FROM dbo.SplitStrings(@InstanceIDs,',')
END;
SELECT I.Instance,
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
	   HUH.physical_memory_kb_new
FROM dbo.HostUpgradeHistory HUH
    JOIN dbo.Instances I ON HUH.InstanceID = I.InstanceID
ORDER BY HUH.ChangeDate DESC;