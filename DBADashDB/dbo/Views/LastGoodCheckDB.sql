CREATE VIEW dbo.LastGoodCheckDB
AS
SELECT I.InstanceID,
	D.DatabaseID,
	I.Instance,
	D.name,
	D.state,
	D.state_desc,
	D.is_in_standby,
	D.LastGoodCheckDbTime,
	DATEADD(mi,CASE WHEN D.LastGoodCheckDbTime = '19000101' THEN 0 ELSE I.UTCOffset END,D.LastGoodCheckDbTime) AS LastGoodCheckDbTimeUTC,
	DATEDIFF(hh,NULLIF(D.LastGoodCheckDbTime,'19000101'),GETUTCDATE()) AS HrsSinceLastGoodCheckDB,
	DATEDIFF(d,NULLIF(D.LastGoodCheckDbTime,'19000101'),GETUTCDATE()) AS DaysSinceLastGoodCheckDB,
	S.Status,
	CASE S.Status WHEN 1 THEN 'Critical' WHEN 2 THEN 'Warning' WHEN 3 THEN 'N/A' WHEN 4 THEN 'OK' ELSE NULL END AS StatusDescription,
	CASE WHEN cfg.InstanceID IS NULL THEN 'None'
	WHEN cfg.DatabaseID = -1 AND CFG.InstanceID =-1 THEN 'Root' 
	WHEN cfg.DatabaseID = -1 THEN 'Instance'
	ELSE 'Database' END AS ConfiguredLevel,
	cfg.WarningThresholdHrs,
	cfg.CriticalThresholdHrs,
	excl.LastGoodCheckDBExcludedReason,
	D.create_date,
	DATEADD(mi,I.UTCOffset,D.create_date)  AS create_date_utc
FROM dbo.Databases D 
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
OUTER APPLY(SELECT TOP(1) T.*
			FROM dbo.LastGoodCheckDBThresholds T
			WHERE (D.InstanceID = T.InstanceID OR T.InstanceID = -1)
				AND (D.DatabaseID = T.DatabaseID  OR T.DatabaseID = -1)
				ORDER BY T.DatabaseID DESC, T.InstanceID DESC
			) cfg
OUTER APPLY(SELECT STUFF(CONCAT(CASE WHEN D.state<>0 THEN ', ' + state_desc ELSE NULL END,
					CASE WHEN D.is_in_standby=1 THEN ', STANDBY' ELSE NULL END,
					CASE WHEN EXISTS(SELECT * FROM STRING_SPLIT(cfg.ExcludedDatabases,',') SS WHERE d.name LIKE SS.value) THEN ', name' ELSE NULL END,
					CASE WHEN D.LastGoodCheckDbTime IS NULL THEN ', Not captured' ELSE NULL END,
					CASE WHEN DATEADD(mi,I.UTCOffset,D.create_date) > DATEADD(mi,-cfg.MinimumAge,GETUTCDATE())  THEN ', create_date' ELSE NULL END,
					CASE WHEN cfg.WarningThresholdHrs IS NULL AND cfg.CriticalThresholdHrs IS NULL THEN ', No threshold' ELSE NULL END
					),1,2,'') as LastGoodCheckDBExcludedReason
			) excl	
OUTER APPLY(SELECT	CASE WHEN excl.LastGoodCheckDBExcludedReason IS NOT NULL THEN 3
					WHEN DATEADD(mi,I.UTCOffset,D.LastGoodCheckDbTime) < DATEADD(hh,-cfg.CriticalThresholdHrs,GETUTCDATE()) THEN 1 
					WHEN DATEADD(mi,I.UTCOffset,D.LastGoodCheckDbTime) < DATEADD(hh,-cfg.WarningThresholdHrs,GETUTCDATE()) THEN 2
					ELSE 4 END AS Status
			) S
WHERE D.IsActive=1
AND I.IsActive=1