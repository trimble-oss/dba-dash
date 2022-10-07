CREATE PROC dbo.OSLoadedModuleSummary_Get(
	@InstanceIDs IDs READONLY
)
AS
SELECT I.InstanceID,
	I.EngineEdition,
	I.InstanceDisplayName,
	ISNULL(Status,3) AS Status,
	CASE Status WHEN 1 THEN 'Critical' WHEN 2 THEN 'Warning' WHEN 4 THEN 'OK' ELSE 'N/A' END AS StatusDescription,
	STUFF((SELECT ', ' + Notes 
			FROM dbo.OSLoadedModules OSLM 
			WHERE OSLM.InstanceID = I.InstanceID 
			AND OSLM.Status IN(1,2)
			AND OSLM.Notes IS NOT NULL
			GROUP BY Notes
			ORDER BY MIN(Status)
			FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,'') AS Notes
FROM dbo.Instances I 
OUTER APPLY(SELECT TOP(1) Status,
						Notes
			FROM dbo.OSLoadedModules OSLM
			WHERE OSLM.InstanceID = I.InstanceID
			AND Status <> 3
			ORDER BY Status 
			) Loaded
WHERE I.IsActive=1
AND I.EngineEdition NOT IN(5,6,8,9,11)
AND EXISTS(	SELECT * 
			FROM @InstanceIDs IDs
			WHERE IDs.ID = I.InstanceID
			)
ORDER BY Loaded.Status 