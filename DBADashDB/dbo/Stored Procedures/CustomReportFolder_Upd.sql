CREATE PROC dbo.CustomReportFolder_Upd(
	@SchemaName NVARCHAR(128),
	@ProcedureName NVARCHAR(128),
	@FolderPath NVARCHAR(400)=NULL /* NULL or empty moves the report back to the top level of the Reports folder */
)
AS
IF IS_ROLEMEMBER('db_owner')=0 AND IS_ROLEMEMBER('db_ddladmin')=0
BEGIN
	RAISERROR('db_owner or db_ddladmin membership is required to organize reports',11,1)
	RETURN
END

SET @FolderPath = NULLIF(LTRIM(RTRIM(@FolderPath)),'')

IF @FolderPath IS NULL
BEGIN
	DELETE dbo.CustomReportFolder
	WHERE SchemaName = @SchemaName
	AND ProcedureName = @ProcedureName
	RETURN
END

UPDATE dbo.CustomReportFolder
SET FolderPath = @FolderPath
WHERE SchemaName = @SchemaName
AND ProcedureName = @ProcedureName

IF @@ROWCOUNT=0
BEGIN
	INSERT INTO dbo.CustomReportFolder(SchemaName,ProcedureName,FolderPath)
	VALUES(@SchemaName,@ProcedureName,@FolderPath)
END
