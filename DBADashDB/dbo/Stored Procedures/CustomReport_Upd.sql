CREATE PROC dbo.CustomReport_Upd(
	@SchemaName NVARCHAR(128),
	@ProcedureName NVARCHAR(128),
	@MetaData NVARCHAR(MAX),
	@Type VARCHAR(50)
)
AS

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
