CREATE PROC dbo.OSLoadedModules_RefreshStatus
AS	
UPDATE M 
	SET M.Status= ISNULL(s.Status,2),
	M.Notes = CASE WHEN s.Status IS NULL THEN 'Unknown module' ELSE s.Notes END
FROM dbo.OSLoadedModules M
OUTER APPLY(SELECT TOP(1)	MS.Status,
							MS.Notes
			FROM dbo.OSLoadedModulesStatus MS 
			WHERE ISNULL(M.company,'') LIKE MS.Company 
			AND ISNULL(M.name,'') LIKE MS.Name 
			AND ISNULL(M.description,'') LIKE MS.Description 
			ORDER BY MS.Status
			) s