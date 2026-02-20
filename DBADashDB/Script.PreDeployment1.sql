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
IF NOT EXISTS(
	SELECT 1
	FROM sys.indexes 
	WHERE name = 'IX_Databases_InstanceID_name'
	AND is_unique=1
	AND object_id = OBJECT_ID('dbo.Databases')
) AND OBJECT_ID('dbo.Databases') IS NOT NULL
BEGIN
	/*	Database name was previously only unique for active databases. 
		Before creating a unique index on InstanceID and name without the IsActive=1 filter, 
		run an update to append a unique identifier to duplicate database names for inactive databases to prevent deployment issues.
	*/
	UPDATE D1
		SET D1.name = CONCAT(LEFT(D1.name,91),' ',NEWID())
	FROM dbo.Databases D1
	WHERE IsActive=0
	AND EXISTS(SELECT 1 
			FROM dbo.Databases D2 
			WHERE D1.name = D2.name
			AND D2.DatabaseID <> D1.DatabaseID
			AND D1.InstanceID = D2.InstanceID
			)
END
/* 
	We shouldn't have duplicates, but just in case, deactivate duplicates by setting IsActive to 0 and file_id to negative FileID (ensuring uniqueness)
	This is added to prevent possible deployment issues caused by adding a unique index on (DatabaseID,file_id)
*/
IF OBJECT_ID('dbo.DBFiles') IS NOT NULL
BEGIN
	IF EXISTS(
		SELECT 1
		FROM dbo.DBFiles
		GROUP BY DatabaseID,file_id
		HAVING COUNT(*)>1
	)
	BEGIN
		WITH T AS (
			SELECT *,ROW_NUMBER() OVER(PARTITION BY DatabaseID,file_id ORDER BY FileID) rnum
			FROM dbo.DBFiles
		)
		UPDATE T
			SET file_id = -FileID,
			IsActive = 0
		WHERE rnum>1

	END
END