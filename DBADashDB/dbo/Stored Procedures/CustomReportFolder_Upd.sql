CREATE PROC dbo.CustomReportFolder_Upd(
	@SchemaName NVARCHAR(128),
	@ProcedureName NVARCHAR(128),
	@FolderPath NVARCHAR(400)=NULL /* NULL or empty moves the report back to the top level of the Reports folder */
)
AS
SET XACT_ABORT ON
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

BEGIN TRAN
/* UPDLOCK,HOLDLOCK to prevent a concurrent insert for the same report between the update and insert */
UPDATE dbo.CustomReportFolder WITH(UPDLOCK,HOLDLOCK)
SET FolderPath = @FolderPath
WHERE SchemaName = @SchemaName
AND ProcedureName = @ProcedureName

IF @@ROWCOUNT=0
BEGIN
	INSERT INTO dbo.CustomReportFolder(SchemaName,ProcedureName,FolderPath)
	VALUES(@SchemaName,@ProcedureName,@FolderPath)
END
COMMIT
