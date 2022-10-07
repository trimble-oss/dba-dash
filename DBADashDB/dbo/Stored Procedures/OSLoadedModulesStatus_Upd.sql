CREATE PROC dbo.OSLoadedModulesStatus_Upd(
	@ID INT,
	@Name NVARCHAR(256),
	@Company NVARCHAR(256),
	@Description NVARCHAR(256),
	@Status TINYINT,
	@Notes NVARCHAR(256)
)
AS
UPDATE dbo.OSLoadedModulesStatus
SET Name = @Name,
	Company = @Company,
	Description = @Description,
	Status = @Status,
	Notes = @Notes
WHERE ID = @ID
GO