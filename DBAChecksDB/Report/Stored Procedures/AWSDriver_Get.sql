CREATE PROC Report.AWSDriver_Get
AS
SELECT Instance, 
	MAX(CASE WHEN D.Manufacturer LIKE 'Amazon%' AND D.DeviceName LIKE '%nvme%' THEN D.DriverVersion ELSE NULL END) AS NVMeDriver,
	MAX(CASE WHEN D.Manufacturer LIKE 'Amazon%' AND D.DeviceName LIKE '%elastic%' THEN D.DriverVersion ELSE NULL END) AS  ENADriver,
	MAX(CASE WHEN D.DeviceID='HKEY_LOCAL_MACHINE\SOFTWARE\Amazon\PVDriver' THEN D.DriverVersion ELSE NULL END) AS PVDriver
FROM dbo.Instances I
JOIN dbo.Drivers D ON D.InstanceID = I.InstanceID
GROUP BY I.Instance
ORDER BY Instance