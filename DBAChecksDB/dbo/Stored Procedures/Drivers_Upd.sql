
CREATE PROC [dbo].[Drivers_Upd](@Drivers dbo.Drivers READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
INSERT INTO dbo.Drivers
(
    InstanceID,
    ClassGuid,
    DeviceClass,
    DeviceID,
    DeviceName,
    DriverDate,
    DriverProviderName,
    DriverVersion,
    FriendlyName,
    HardWareID,
    Manufacturer,
    PDO,
	ValidFrom
)
SELECT  @InstanceID,
    ClassGuid,
    DeviceClass,
    DeviceID,
    DeviceName,
    DriverDate,
    DriverProviderName,
    DriverVersion,
    FriendlyName,
    HardWareID,
    Manufacturer,
    PDO,
	@SnapshotDate
FROM @Drivers t
WHERE NOT EXISTS(SELECT 1 FROM dbo.Drivers D WHERE D.DeviceID = t.DeviceID AND D.InstanceID = @InstanceID);

WITH S AS(
	SELECT ClassGuid,
		DeviceClass,
		DeviceID,
		DeviceName,
		DriverDate,
		DriverProviderName,
		DriverVersion,
		FriendlyName,
		HardWareID,
		Manufacturer,
		PDO
	FROM @Drivers
	EXCEPT 
	SELECT ClassGuid,
		DeviceClass,
		DeviceID,
		DeviceName,
		DriverDate,
		DriverProviderName,
		DriverVersion,
		FriendlyName,
		HardWareID,
		Manufacturer,
		PDO
	FROM dbo.Drivers D 
	WHERE D.InstanceID = @InstanceID
)
UPDATE T
SET T.ClassGuid = S.ClassGuid,
    T.DeviceClass=S.DeviceClass,
    T.DeviceName=S.DeviceName,
    T.DriverDate=S.DriverDate,
    T.DriverProviderName=S.DriverProviderName,
    T.DriverVersion=S.DriverVersion,
    T.FriendlyName=S.FriendlyName,
    T.HardWareID=S.HardWareID,
    T.Manufacturer=S.Manufacturer,
    T.PDO=S.PDO
OUTPUT DELETED.InstanceID,
    DELETED.ClassGuid,
    DELETED.DeviceClass,
    DELETED.DeviceID,
    DELETED.DeviceName,
    DELETED.DriverDate,
    DELETED.DriverProviderName,
    DELETED.DriverVersion,
    DELETED.FriendlyName,
    DELETED.HardWareID,
    DELETED.Manufacturer,
    DELETED.PDO,
	DELETED.ValidFrom,
	@SnapshotDate INTO dbo.DriversHistory(    InstanceID,
    ClassGuid,
    DeviceClass,
    DeviceID,
    DeviceName,
    DriverDate,
    DriverProviderName,
    DriverVersion,
    FriendlyName,
    HardWareID,
    Manufacturer,
    PDO,
	ValidFrom,
	ValidTo)
FROM dbo.Drivers T
JOIN S ON T.DeviceID = S.DeviceID AND T.InstanceID = @InstanceID


DELETE D 
OUTPUT DELETED.InstanceID,
    DELETED.ClassGuid,
    DELETED.DeviceClass,
    DELETED.DeviceID,
    DELETED.DeviceName,
    DELETED.DriverDate,
    DELETED.DriverProviderName,
    DELETED.DriverVersion,
    DELETED.FriendlyName,
    DELETED.HardWareID,
    DELETED.Manufacturer,
    DELETED.PDO,
	DELETED.ValidFrom,
	@SnapshotDate INTO dbo.DriversHistory(    InstanceID,
    ClassGuid,
    DeviceClass,
    DeviceID,
    DeviceName,
    DriverDate,
    DriverProviderName,
    DriverVersion,
    FriendlyName,
    HardWareID,
    Manufacturer,
    PDO,
	ValidFrom,
	ValidTo)
FROM dbo.Drivers D
WHERE NOT EXISTS(SELECT 1 FROM @Drivers t WHERE d.DeviceID = t.DeviceID)
AND D.InstanceID = @InstanceID