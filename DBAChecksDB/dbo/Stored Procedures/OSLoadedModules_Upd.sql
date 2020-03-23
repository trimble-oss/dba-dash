CREATE PROC [dbo].[OSLoadedModules_Upd](@OSLoadedModules OSLoadedModules READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
DECLARE @Ref VARCHAR(30)='OSLoadedModules'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
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
		Status
	)
	SELECT @InstanceID,
		   TRY_CONVERT(VARBINARY(8), base_address_string, 2),
           file_version,
           product_version,
           debug,
           patched,
           prerelease,
           private_build,
           special_build,
           language,
           ISNULL(company,''),
           ISNULL(description,''),
           ISNULL(name,''),
		   ISNULL(S.Status,2)
	FROM @OSLoadedModules M
	OUTER APPLY(SELECT MIN(MS.Status) Status 
				FROM dbo.OSLoadedModulesStatus MS 
				WHERE ISNULL(M.Company,'') LIKE MS.Company 
				AND ISNULL(M.Name,'') LIKE MS.Name 
				AND ISNULL(M.description,'') LIKE MS.Description ) s

	COMMIT

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
END