CREATE PROC dbo.OSLoadedModules_Upd(
	@OSLoadedModules OSLoadedModules READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
DECLARE @Ref VARCHAR(30)='OSLoadedModules'
IF NOT EXISTS(	SELECT 1 
				FROM dbo.CollectionDates 
				WHERE SnapshotDate>=@SnapshotDate 
				AND InstanceID = @InstanceID 
				AND Reference=@Ref
				)
BEGIN
	BEGIN TRAN
	DELETE dbo.OSLoadedModules 
	WHERE InstanceID=@InstanceID

	INSERT INTO dbo.OSLoadedModules
	(
	    InstanceID,
	    base_address,
	    file_version,
	    product_version,
	    debug,
	    patched,
	    prerelease,
	    private_build,
	    special_build,
	    language,
	    company,
	    description,
	    name,
		Status,
		Notes
	)
	SELECT @InstanceID,
		   TRY_CONVERT(VARBINARY(8), M.base_address_string, 2),
           M.file_version,
           M.product_version,
           M.debug,
           M.patched,
           M.prerelease,
           M.private_build,
           M.special_build,
           M.language,
           ISNULL(M.company,''),
           ISNULL(M.description,''),
           ISNULL(M.name,''),
		   ISNULL(s.Status,2),
		   CASE WHEN s.Status IS NULL THEN 'Unknown module' ELSE s.Notes END
	FROM @OSLoadedModules M
	OUTER APPLY(SELECT TOP(1) MS.Status,
							  MS.Notes
				FROM dbo.OSLoadedModulesStatus MS 
				WHERE ISNULL(M.company,'') LIKE MS.Company 
				AND ISNULL(M.name,'') LIKE MS.Name
				AND ISNULL(M.description,'') LIKE MS.Description 
				ORDER BY MS.Status
				) s

	COMMIT

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
END