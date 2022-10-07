CREATE PROC dbo.OSLoadedModules_Get(
	@InstanceID INT
)
AS
SELECT I.InstanceID,
       I.InstanceDisplayName,
	   base_address,
       OSLM.file_version,
       OSLM.product_version,
       OSLM.debug,
       OSLM.patched,
       OSLM.prerelease,
       OSLM.private_build,
       OSLM.special_build,
       OSLM.language,
       OSLM.company,
       OSLM.description,
       OSLM.name,
       OSLM.Status,
	   CASE Status WHEN 1 THEN 'Critical' WHEN 2 THEN 'Warning' WHEN 4 THEN 'OK' ELSE 'N/A' END AS StatusDescription,
       OSLM.Notes
FROM dbo.OSLoadedModules OSLM 
JOIN dbo.Instances I ON I.InstanceID = OSLM.InstanceID
WHERE I.IsActive = 1
AND I.InstanceID = @InstanceID
ORDER BY OSLM.Status,name;
