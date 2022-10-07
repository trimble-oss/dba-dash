CREATE PROC dbo.OSLoadedModulesStatus_Add(
	@Name NVARCHAR(256),
	@Company NVARCHAR(256),
	@Description NVARCHAR(256),
	@Status TINYINT,
	@Notes NVARCHAR(256),
	@ID INT=NULL OUT
)
AS
INSERT INTO dbo.OSLoadedModulesStatus(Name,
	Company,
	Description,
	Status,
	Notes,
	IsSystem)
VALUES(@Name,@Company,@Description,@Status,@Notes,0)

SET @ID = SCOPE_IDENTITY();
GO