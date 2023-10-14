CREATE PROC dbo.CustomReport_Upd(
	@SchemaName NVARCHAR(128),
	@ProcedureName NVARCHAR(128),
	@MetaData NVARCHAR(MAX)
)
AS

UPDATE dbo.CustomReport
SET MetaData = @MetaData
WHERE ProcedureName = @ProcedureName
AND SchemaName = @SchemaName

IF @@ROWCOUNT=0
BEGIN
	INSERT INTO dbo.CustomReport(SchemaName,ProcedureName,MetaData)
	VALUES(@SchemaName,@ProcedureName,@MetaData)
END
