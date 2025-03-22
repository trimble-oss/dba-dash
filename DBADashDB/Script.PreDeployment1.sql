/* Ensure ProductMajorVersion and ProductBuild are INT */
IF EXISTS(
	SELECT * 
	FROM sys.columns
	WHERE object_id = OBJECT_ID('dbo.Instances')
	AND name = 'ProductMajorVersion'
	and system_type_id<>56 /* int */
)
BEGIN
		UPDATE dbo.Instances 
		SET ProductMajorVersion = ISNULL(TRY_CAST(ProductMajorVersion AS INT),TRY_CAST(PARSENAME(ProductVersion,4) AS INT)),
			ProductBuild= ISNULL(TRY_CAST(ProductBuild AS INT),TRY_CAST(PARSENAME(ProductVersion,2) AS INT))
END
/* 
	If DatabasesHADR table doesn't have an InstanceID column, 
	clear out existing data to allow schema change 
	(data is repopulated on next collection)
 */
IF OBJECT_ID('dbo.DatabasesHADR') IS NOT NULL 
	AND COLUMNPROPERTY(OBJECT_ID('dbo.DatabasesHADR'),'InstanceID','ColumnID') IS NULL
BEGIN
	TRUNCATE TABLE dbo.DatabasesHADR
END
/* 
	If LogRestores table doesn't have an InstanceID column, 
	clear out existing data to allow schema change.  Data is copied to LogRestoresTemp table and re-imported.
 */
IF OBJECT_ID('dbo.LogRestores') IS NOT NULL 
	AND COLUMNPROPERTY(OBJECT_ID('dbo.LogRestores'),'InstanceID','ColumnID') IS NULL
BEGIN
	SELECT 	DatabaseID,
			restore_date,
			backup_start_date,
			last_file,
			backup_time_zone 
	INTO dbo.LogRestoresTemp
	FROM dbo.LogRestores

	TRUNCATE TABLE dbo.LogRestores
END
/* Shouldn't be NULL but run update before making column NOT NULL just in case */
IF COLUMNPROPERTY(OBJECT_ID('dbo.Backups'),'is_copy_only','AllowsNull') =1
BEGIN
	UPDATE dbo.Backups
	SET is_copy_only=0
	WHERE is_copy_only IS NULL
END
/* Shouldn't be NULL but run update before making column NOT NULL just in case */
IF COLUMNPROPERTY(OBJECT_ID('dbo.Backups'),'is_snapshot','AllowsNull') =1
BEGIN
	UPDATE dbo.Backups
	SET is_snapshot=0
	WHERE is_snapshot IS NULL
END
