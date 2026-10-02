CREATE PROC dbo.CustomReport_Upd(
	@SchemaName NVARCHAR(128),
	@ProcedureName NVARCHAR(128),
	@MetaData NVARCHAR(MAX),
	@Type VARCHAR(50)
)
AS
/* Report customizations are shared with all users.  Same rule as CanEditReport in CustomReport_Get */
IF IS_ROLEMEMBER('db_owner')=0 AND IS_ROLEMEMBER('db_ddladmin')=0
BEGIN
	RAISERROR('db_owner or db_ddladmin membership is required to edit reports',11,1)
	RETURN
END

UPDATE dbo.CustomReport
SET MetaData = @MetaData
WHERE ProcedureName = @ProcedureName
AND SchemaName = @SchemaName
AND Type = @Type

IF @@ROWCOUNT=0
BEGIN
	INSERT INTO dbo.CustomReport(SchemaName,ProcedureName,MetaData,Type)
	VALUES(@SchemaName,@ProcedureName,@MetaData,@Type)
END
