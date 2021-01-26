


CREATE VIEW [dbo].[LastGoodCheckDB]
AS
SELECT I.InstanceID,
	D.DatabaseID,
	I.Instance,
	D.name,
	D.state,
	D.state_desc,
	D.is_in_standby,
	D.LastGoodCheckDbTime,
	CASE WHEN D.state=0 AND D.is_in_standby=0 AND D.name<>'tempdb' AND D.LastGoodCheckDbTime IS NOT NULL THEN 0 ELSE 1 END AS ExcludedFromCheck,
	DATEDIFF(hh,NULLIF(D.LastGoodCheckDbTime,'19000101'),GETUTCDATE()) AS HrsSinceLastGoodCheckDB,
	DATEDIFF(d,NULLIF(D.LastGoodCheckDbTime,'19000101'),GETUTCDATE()) AS DaysSinceLastGoodCheckDB,
	S.Status,
	CASE S.Status WHEN 1 THEN 'Critical' WHEN 2 THEN 'Warning' WHEN 3 THEN 'N/A' WHEN 4 THEN 'OK' ELSE NULL END AS StatusDescription,
	CASE WHEN cfg.InstanceID IS NULL THEN 'None'
	WHEN cfg.DatabaseID = -1 AND CFG.InstanceID =-1 THEN 'Root' 
	WHEN cfg.DatabaseID = -1 THEN 'Instance'
	ELSE 'Database' END AS ConfiguredLevel,
	cfg.WarningThresholdHrs,
	cfg.CriticalThresholdHrs
FROM dbo.Databases D 
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
OUTER APPLY(SELECT TOP(1) T.* 
		FROM dbo.LastGoodCheckDBThresholds T
		WHERE (D.InstanceID = T.InstanceID OR T.InstanceID = -1)
			AND (D.DatabaseID = T.DatabaseID  OR T.DatabaseID = -1)
			ORDER BY T.DatabaseID DESC, T.InstanceID DESC) cfg
OUTER APPLY(SELECT	CASE WHEN NOT (D.state=0 AND D.is_in_standby=0 AND D.name<>'tempdb' AND D.LastGoodCheckDbTime IS NOT NULL) THEN 3
					WHEN D.LastGoodCheckDbTime < DATEADD(hh,-cfg.CriticalThresholdHrs,GETUTCDATE()) THEN 1 
					WHEN D.LastGoodCheckDbTime <DATEADD(hh,-cfg.WarningThresholdHrs,GETUTCDATE()) THEN 2
					WHEN cfg.WarningThresholdHrs IS NULL AND cfg.CriticalThresholdHrs IS NULL THEN 3
					ELSE 4 END AS Status) S
WHERE D.IsActive=1
AND I.IsActive=1