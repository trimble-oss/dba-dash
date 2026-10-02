CREATE PROC dbo.CustomReportFolder_Get(
	@CanEditReport BIT=NULL OUT
)
AS
/* Folder placement in the tree for both user custom reports and built-in system reports.  Same edit rule as CustomReport_Get */
SET @CanEditReport = CASE WHEN IS_ROLEMEMBER('db_owner')=1 OR IS_ROLEMEMBER('db_ddladmin')=1 THEN 1 ELSE 0 END;

SELECT	SchemaName,
		ProcedureName,
		FolderPath
FROM dbo.CustomReportFolder
