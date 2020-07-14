CREATE PROC [dbo].[ServerProperties_Upd](@ServerProperties ServerProperties READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
DECLARE @Ref VARCHAR(30)='ServerProperties'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
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
	   SET [BuildClrVersion] = T.BuildClrVersion
		  ,[Collation] = T.Collation
		  ,[CollationID] = T.CollationID
		  ,[ComparisonStyle] = T.ComparisonStyle
		  ,[ComputerNamePhysicalNetBIOS] = T.ComputerNamePhysicalNetBIOS
		  ,[Edition] = T.Edition
		  ,[EditionID] = T.EditionID
		  ,[EngineEdition] = T.EngineEdition
		  ,[FileStreamConfiguredLevel] = T.FileStreamConfiguredLevel
		  ,[FileStreamEffectiveLevel] = T.FileStreamEffectiveLevel
		  ,[FileStreamShareName] = T.FileStreamShareName
		  ,[HadrManagerStatus] = T.HadrManagerStatus
		  ,[InstanceDefaultDataPath] = T.InstanceDefaultDataPath
		  ,[InstanceDefaultLogPath] = T.InstanceDefaultLogPath
		  ,[InstanceName] = T.InstanceName
		  ,[IsAdvancedAnalyticsInstalled] = T.IsAdvancedAnalyticsInstalled
		  ,[IsClustered] = T.IsClustered
		  ,[IsFullTextInstalled] = T.IsFullTextInstalled
		  ,[IsHadrEnabled] = T.IsHadrEnabled
		  ,[IsIntegratedSecurityOnly] = T.IsIntegratedSecurityOnly
		  ,[IsLocalDB] = T.IsLocalDB
		  ,[IsPolybaseInstalled] = T.IsPolybaseInstalled
		  ,[IsXTPSupported] = T.IsXTPSupported
		  ,[LCID] = T.LCID
		  ,[LicenseType] = T.LicenseType
		  ,[MachineName] = T.MachineName
		  ,[NumLicenses] = T.NumLicenses
		  ,[ProductBuild] = T.ProductBuild
		  ,[ProductBuildType] = T.ProductBuildType
		  ,[ProductLevel] = T.ProductLevel
		  ,[ProductMajorVersion] = T.ProductMajorVersion
		  ,[ProductUpdateLevel] = T.ProductUpdateLevel
		  ,[ProductUpdateReference] = T.ProductUpdateReference
		  ,[ProductVersion] = T.ProductVersion
		  ,[ResourceLastUpdateDateTime] = T.ResourceLastUpdateDateTime
		  ,[ResourceVersion] = T.ResourceVersion
		  ,[ServerName] = T.ServerName
		  ,[SqlCharSet] = T.SqlCharSet
		  ,[SqlCharSetName] = T.SqlCharSetName
		  ,[SqlSortOrder] = T.SqlSortOrder
		  ,[SqlSortOrderName] = T.SqlSortOrderName
	FROM dbo.Instances I
	CROSS JOIN @ServerProperties T
	WHERE I.InstanceID = @InstanceID

	EXEC dbo.SystemTags_Upd @InstanceID = @InstanceID
	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate

END