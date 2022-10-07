CREATE PROC dbo.OSLoadedModulesStatus_Get
AS
SELECT ID,
	Name,
	Company,
	Description,
	Status,
	Notes,
	IsSystem
FROM dbo.OSLoadedModulesStatus