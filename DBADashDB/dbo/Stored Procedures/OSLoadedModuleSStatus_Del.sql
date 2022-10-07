CREATE PROC dbo.OSLoadedModulesStatus_Del(
	@ID INT
)
AS
DELETE dbo.OSLoadedModulesStatus
WHERE ID=@ID
AND IsSystem=0