CREATE PROC dbo.ServerProperties_Upd(
		@ServerProperties ServerProperties READONLY,
		@InstanceID INT,
		@SnapshotDate DATETIME2(2)
)
AS
DECLARE @Ref VARCHAR(30)='ServerProperties'
IF NOT EXISTS(	SELECT 1 
				FROM dbo.CollectionDates 
				WHERE SnapshotDate>=@SnapshotDate 
				AND InstanceID = @InstanceID 
				AND Reference=@Ref
			)
BEGIN
	INSERT INTO dbo.SQLPatchingHistory
	(
	    InstanceID,
	    ChangedDate,
	    OldVersion,
	    NewVersion,
	    OldProductLevel,
	    NewProductLevel,
	    OldProductUpdateLevel,
	    NewProductUpdateLevel,
		OldEdition,
		NewEdition
	)
	SELECT I.InstanceID,
		   @SnapshotDate,
		   I.ProductVersion,
		   NULLIF(T.ProductVersion,I.ProductVersion),
		   I.ProductLevel,
		   NULLIF(T.ProductLevel,I.ProductLevel),
		   I.ProductUpdateLevel,
		   NULLIF(T.ProductUpdateLevel,I.ProductUpdateLevel),
		   I.Edition,
		   NULLIF(T.Edition,I.Edition)
	FROM dbo.Instances I
		CROSS JOIN @ServerProperties T
	WHERE I.InstanceID = @InstanceID
	AND   (I.ProductVersion <> T.ProductVersion
			OR I.Edition<> T.Edition)

    UPDATE I
        SET I.BuildClrVersion = T.BuildClrVersion,
            I.Collation = T.Collation,
            I.CollationID = T.CollationID,
            I.ComparisonStyle = T.ComparisonStyle,
            I.ComputerNamePhysicalNetBIOS = T.ComputerNamePhysicalNetBIOS,
            I.Edition = T.Edition,
            I.EditionID = T.EditionID,
            I.EngineEdition = T.EngineEdition,
            I.FileStreamConfiguredLevel = T.FileStreamConfiguredLevel,
            I.FileStreamEffectiveLevel = T.FileStreamEffectiveLevel,
            I.FileStreamShareName = T.FileStreamShareName,
            I.HadrManagerStatus = T.HadrManagerStatus,
            I.InstanceDefaultDataPath = T.InstanceDefaultDataPath,
            I.InstanceDefaultLogPath = T.InstanceDefaultLogPath,
            I.InstanceName = T.InstanceName,
            I.IsAdvancedAnalyticsInstalled = T.IsAdvancedAnalyticsInstalled,
            I.IsClustered = T.IsClustered,
            I.IsFullTextInstalled = T.IsFullTextInstalled,
            I.IsHadrEnabled = T.IsHadrEnabled,
            I.IsIntegratedSecurityOnly = T.IsIntegratedSecurityOnly,
            I.IsLocalDB = T.IsLocalDB,
            I.IsPolybaseInstalled = T.IsPolybaseInstalled,
            I.IsXTPSupported = T.IsXTPSupported,
            I.LCID = T.LCID,
            I.LicenseType = T.LicenseType,
            I.MachineName = T.MachineName,
            I.NumLicenses = T.NumLicenses,
            I.ProductBuild = T.ProductBuild,
            I.ProductBuildType = T.ProductBuildType,
            I.ProductLevel = T.ProductLevel,
            I.ProductMajorVersion = T.ProductMajorVersion,
            I.ProductUpdateLevel = T.ProductUpdateLevel,
            I.ProductUpdateReference = T.ProductUpdateReference,
            I.ProductVersion = T.ProductVersion,
            I.ResourceLastUpdateDateTime = T.ResourceLastUpdateDateTime,
            I.ResourceVersion = T.ResourceVersion,
            I.ServerName = T.ServerName,
            I.SqlCharSet = T.SqlCharSet,
            I.SqlCharSetName = T.SqlCharSetName,
            I.SqlSortOrder = T.SqlSortOrder,
            I.SqlSortOrderName = T.SqlSortOrderName,
            I.ProductMinorVersion = T.ProductMinorVersion,
            I.ProductRevision = T.ProductRevision
    FROM dbo.Instances I
        CROSS JOIN @ServerProperties T
    WHERE I.InstanceID = @InstanceID;

	EXEC dbo.SystemTags_Upd @InstanceID = @InstanceID
	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate

END