

CREATE VIEW [dbo].[DriveStatus]
AS
SELECT I.InstanceID,
	D.DriveID,
	I.Instance,
	D.Name,
	D.Label,
	Capacity/POWER(1024.0,3) TotalGB,
	UsedSpace/POWER(1024.0,3) UsedGB,
	FreeSpace/POWER(1024.0,3) FreeGB,
	FreeSpace/CAST(Capacity AS DECIMAL) AS PctFreeSpace,
	DATEDIFF(mi,SSD.SnapshotDate,GETUTCDATE()) AS SnapshotAgeMins,
	SSD.SnapshotDate,
	cfg.DriveWarningThreshold,
	cfg.DriveCriticalThreshold,
	cfg.DriveCheckType,
	chk.Status,
	CASE chk.Status WHEN 1 THEN 'Critical' WHEN 2 THEN 'Warning' WHEN 3 THEN 'N/A' WHEN 4 THEN 'OK' END AS StatusDescription,
	CASE WHEN cfg.InstanceID = -1 AND cfg.DriveID = -1 THEN 'Root'
	WHEN cfg.InstanceID =-1 THEN 'Instance'
	WHEN cfg.InstanceID IS NULL THEN 'None'
	ELSE 'Drive' END DriveCheckConfiguredLevel
FROM dbo.Drives D
INNER JOIN dbo.Instances I ON D.InstanceID = I.InstanceID
INNER JOIN dbo.CollectionDates SSD ON SSD.InstanceID = I.InstanceID AND SSD.Reference='Drives'
OUTER APPLY(SELECT TOP(1) T.* 
			FROM dbo.DriveThresholds T 
			WHERE (D.InstanceID = T.InstanceID OR T.InstanceID = -1)
			AND (D.DriveID = T.DriveID  OR T.DriveID = -1)
			ORDER BY InstanceID DESC,DriveID DESC
			) cfg
OUTER APPLY(SELECT CASE WHEN cfg.DriveCheckType = '%' 
		AND FreeSpace/CAST(Capacity AS DECIMAL) < cfg.DriveCriticalThreshold THEN 1
		WHEN cfg.DriveCheckType = '%' 
		AND FreeSpace/CAST(Capacity AS DECIMAL) < cfg.DriveWarningThreshold THEN 2
		WHEN cfg.DriveCheckType = 'G' 
		AND FreeSpace/POWER(1024.0,3)< cfg.DriveCriticalThreshold THEN 1
		WHEN cfg.DriveCheckType = 'G' 
		AND FreeSpace/POWER(1024.0,3) < cfg.DriveWarningThreshold THEN 2
		WHEN cfg.DriveWarningThreshold IS NULL AND cfg.DriveCriticalThreshold IS NULL THEN 3
		ELSE 4 END AS Status) chk
WHERE D.IsActive=1
AND I.IsActive=1
